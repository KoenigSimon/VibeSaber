using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Buttplug;
using System.Diagnostics;
using System.Threading;

namespace VibeSaber
{
    public static class BuildInfo
    {
        public const string Name = "VibeSaber";
        public const string Author = "Smoin";
        public const string Version = "0.0.3";
    }

    static public class NativeMethods
    {
        public static string TempPath
        {
            get
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"{BuildInfo.Name}-{BuildInfo.Version}");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                return tempPath;
            }
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        public static string LoadUnmanagedLibraryFromResource(Assembly assembly, string libraryResourceName, string libraryName)
        {
            string assemblyPath = Path.Combine(TempPath, libraryName);

            //sMsg($"Unpacking and loading {libraryName}");
#if DEBUG
            try
            {
#endif
                using (Stream s = assembly.GetManifestResourceStream(libraryResourceName))
                {
                    var data = new BinaryReader(s).ReadBytes((int)s.Length);
                    File.WriteAllBytes(assemblyPath, data);
                }
#if DEBUG
            }
            catch (System.IO.IOException) { }
#endif
            NativeMethods.LoadLibrary(assemblyPath);

            return assemblyPath;
        }
    }
    class ServerHandler
    {
        private ButtplugClient mButtplug = null;
        private Task mConnectionTask = Task.CompletedTask;
        private int mMaxSeenDevices = 0;
        private Task mScanTask = Task.CompletedTask;
        private float mNextUpdate = 0;
        private Dictionary<uint, float[]> mDeviceIntensities = new Dictionary<uint, float[]>();
        private string mServerURI = "ws://localhost:12345";
        private Process mIntifaceServer;
        private float mScanDuration = 15f;
        private float mScanWaitDuration = 5f;

        bool levelActive = false;
        public float decayTime = 0f;
        public float intensity = 0f;

        Mutex mDeviceMutex = new Mutex();
        Dictionary<uint, ButtplugClientDevice> mDevicesAdded = new Dictionary<uint, ButtplugClientDevice>();
        Dictionary<uint, ButtplugClientDevice> mDevicesRemoved = new Dictionary<uint, ButtplugClientDevice>();

        public ServerHandler()
        {
            Logger.log.Info("Available Assemblies");
            foreach(string message in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.log.Info(message);
            }

            // Load Buttplug native library from resources
            NativeMethods.LoadUnmanagedLibraryFromResource(Assembly.GetExecutingAssembly(), "VibeSaber.lib.buttplug_rs_ffi.csharp.ButtplugCSharpFFI.bin.Debug.net472.buttplug_rs_ffi.dll", "buttplug_rs_ffi.dll");

        }

        public void OnFrameServerUpdate()
        {
            if (mButtplug == null)
            {
                return;
            }


            if (!mButtplug.Connected)
            {
                if (mConnectionTask.IsCompleted)
                {
                    mConnectionTask = InitializeButtplugClient();
                }
                return;
            }

            // Scan forever!!!
            if (mScanTask.IsCompleted)
            {
                mScanTask = Scan();
            }

            //ignore when disabled
            if (Config.Instance.disable) return;

            //TODO: limit update message amount and add max messages per second setting

            if(decayTime > 0)
            {
                decayTime -= Time.deltaTime;
                SendChangedVibrationIntensity(intensity);
            }
            else
            {
                SendChangedVibrationIntensity(0);
            }
        }

        public void SendStopDeviceCommand()
        {
            foreach (var device in mButtplug.Devices)
                _ = device.SendStopDeviceCmd();
        }

        public void SendChangedVibrationIntensity(float intensity)
        {
            //set all devices to same intensity (for spectators i guess)
            var deviceIntensities = new Dictionary<uint, float?[]>();
            foreach (var device in mButtplug.Devices)
            {
                if (!device.AllowedMessages.ContainsKey(Buttplug.ServerMessage.Types.MessageAttributeType.VibrateCmd)) continue;
                uint motorCount = device.AllowedMessages[Buttplug.ServerMessage.Types.MessageAttributeType.VibrateCmd].FeatureCount;
                deviceIntensities[device.Index] = new float?[motorCount];

                var motorIntensities = deviceIntensities[device.Index];
                for (int i = 0; i < motorIntensities.Length; i++)
                {
                    deviceIntensities[device.Index][i] = intensity;
                }
            }

            // Send device commands
            foreach (var device in mButtplug.Devices)
            {
                if (!deviceIntensities.ContainsKey(device.Index)) continue;

                // Refrain from updating with the same values, since this seems to increase the chance of device hangs
                var motorIntensityValues = Array.ConvertAll(deviceIntensities[device.Index], i => i ?? 0);
                if (!motorIntensityValues.SequenceEqual(mDeviceIntensities[device.Index]))
                {
                    // VIBRATE!!!
                    _ = device.SendVibrateCmd(Array.ConvertAll(motorIntensityValues, i => (double)i));
                    mDeviceIntensities[device.Index] = motorIntensityValues;
                    Logger.log.Info($"Sending command with intensity: {intensity}");
                }
            }
        }

        public async Task SendDelayedIntensity(float intesity, int delayInMS)
        {
            await Task.Delay(delayInMS);

            ForceSendVibrationIntensity(intensity);
        }

        private void ForceSendVibrationIntensity(float intensity)
        {
            //set all devices to same intensity (for spectators i guess)
            var deviceIntensities = new Dictionary<uint, float?[]>();
            foreach (var device in mButtplug.Devices)
            {
                if (!device.AllowedMessages.ContainsKey(Buttplug.ServerMessage.Types.MessageAttributeType.VibrateCmd)) continue;
                uint motorCount = device.AllowedMessages[Buttplug.ServerMessage.Types.MessageAttributeType.VibrateCmd].FeatureCount;
                deviceIntensities[device.Index] = new float?[motorCount];

                var motorIntensities = deviceIntensities[device.Index];
                for (int i = 0; i < motorIntensities.Length; i++)
                {
                    deviceIntensities[device.Index][i] = intensity;
                }
            }

            // Send device commands
            foreach (var device in mButtplug.Devices)
            {
                if (!deviceIntensities.ContainsKey(device.Index)) continue;

                // Refrain from updating with the same values, since this seems to increase the chance of device hangs
                var motorIntensityValues = Array.ConvertAll(deviceIntensities[device.Index], i => i ?? 0);
                // VIBRATE!!!
                _ = device.SendVibrateCmd(Array.ConvertAll(motorIntensityValues, i => (double)i));
                mDeviceIntensities[device.Index] = motorIntensityValues;
                Logger.log.Info($"Sending command with intensity: {intensity}");
            }
        }

        public void InitializeServer()
        {
            mConnectionTask = InitializeButtplugClient();
        }

        private async Task InitializeButtplugClient()
        {
            if (mButtplug != null)
            {
                mButtplug.Dispose();
            }

            mButtplug = new ButtplugClient(BuildInfo.Name);

            mButtplug.ServerDisconnect += (sender, e) => {
                Logger.log.Warn($"Lost connection to Buttplug server! Attempting to reconnect...");
            };

            mButtplug.DeviceAdded += (sender, e) => {
                mMaxSeenDevices = Math.Max(mMaxSeenDevices, mButtplug.Devices.Length);

                var message = $"Device \"{e.Device.Name}\" connected";
                var supporting = new List<string>();
                try
                {
                    var motorCount = e.Device.AllowedMessages[Buttplug.ServerMessage.Types.MessageAttributeType.VibrateCmd].FeatureCount;
                    if (motorCount > 0)
                    {
                        supporting.Add($"{motorCount} vibration motor{(motorCount > 1 ? "s" : "")}");
                        mDeviceIntensities[e.Device.Index] = new float[motorCount];
                    }
                }
                catch (KeyNotFoundException) { }
                try
                {
                    var rotatorCount = e.Device.AllowedMessages[Buttplug.ServerMessage.Types.MessageAttributeType.RotateCmd].FeatureCount;
                    if (rotatorCount > 0)
                    {
                        supporting.Add($"{rotatorCount} rotation actuator{(rotatorCount > 1 ? "s" : "")}");
                    }
                }
                catch (KeyNotFoundException) { }
                if (supporting.Count > 0)
                {
                    message += ", supporting " + String.Join(", ", supporting) + ".";
                }
                Logger.log.Info(message);
#if DEBUG
                Logger.log.Info($"{e.Device.Name} supports the following messages:");
                foreach (var msgInfo in e.Device.AllowedMessages)
                {
                    Logger.log.Info($"- {msgInfo.Key.ToString()}");
                    if (msgInfo.Value.FeatureCount > 0)
                    {
                        Logger.log.Info($"  - Feature Count: {msgInfo.Value.FeatureCount}");
                    }
                }
#endif
            };

            mButtplug.DeviceRemoved += (sender, e) => {
                mDeviceIntensities.Remove(e.Device.Index);
                Logger.log.Info($"Device \"{e.Device.Name}\" disconnected");
            };

            mButtplug.ErrorReceived += (sender, e) => {
                Logger.log.Warn($"Device error: {e.Exception.Message}");
            };

            if (mButtplug != null)
            {
                mButtplug.DeviceRemoved -= OnDeviceRemoved;
                mButtplug.DeviceAdded -= OnDeviceAdded;
                mButtplug.ServerDisconnect -= OnButtplugDisconnect;
            }

            mButtplug.DeviceAdded += OnDeviceAdded;
            mButtplug.DeviceRemoved += OnDeviceRemoved;
            mButtplug.ServerDisconnect += OnButtplugDisconnect;

            Logger.log.Info("Event Setup done");

            await ConnectButtplugClient();
        }

        async Task ConnectButtplugClient()
        {
            if (mButtplug.Connected)
            {
                Logger.log.Info("Disconnecting...");
                await mButtplug.DisconnectAsync();
            }

            var conn = new ButtplugWebsocketConnectorOptions(new System.Uri(mServerURI));
            try
            {
                Logger.log.Info("Connecting...");
                await mButtplug.ConnectAsync(conn);
                if (mIntifaceServer == null)
                {
                    Logger.log.Warn("Connected to Intiface Desktop server. It's recommended not to have Intiface Desktop running, so Vibe Goes Brrr is able to start and manage the server automatically and restart it if something goes wrong. Only run Intiface Desktop manually if you know what you're doing.");
                }
            }
            catch (ButtplugConnectorException)
            {
                Logger.log.Info($"Starting embedded Intiface server...");
                try
                {
                    StartIntifaceServer();
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Failed to start Intiface server. Is Intiface Desktop properly installed?");
                    Logger.log.Error(e.Message);
                    return;
                }
                Logger.log.Info("Connecting to embedded...");
                try
                {
                    await mButtplug.ConnectAsync(conn);
                }
                catch (Exception e)
                {
                    Logger.log.Error(e);
                    Logger.log.Error("Failed to connect to embedded server. RIP.");
                }
            }

            Logger.log.Info($"Connected to Buttplug server at {mServerURI}");
        }

        void StartIntifaceServer()
        {
            if (mIntifaceServer != null && !mIntifaceServer.HasExited)
            {
                return;
            }

            var intifaceCLI = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IntifaceDesktop", "engine", "IntifaceCLI.exe");

            mIntifaceServer = new Process();
            mIntifaceServer.StartInfo.FileName = intifaceCLI;
            var port = new Uri(mServerURI).Port;
            mIntifaceServer.StartInfo.Arguments = $"--servername \"{BuildInfo.Name} Server\" --wsinsecureport {port} --log warn";
            mIntifaceServer.StartInfo.UseShellExecute = false;
            mIntifaceServer.StartInfo.RedirectStandardOutput = true;
            mIntifaceServer.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                if (e.Data != null)
                {
                    Logger.log.Info("[Intiface] " + e.Data);
                }
            };
            mIntifaceServer.StartInfo.RedirectStandardError = true;
            mIntifaceServer.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                if (e.Data != null)
                {
                    Logger.log.Error("[Intiface] " + e.Data);
                }
            };
            mIntifaceServer.EnableRaisingEvents = true;
            mIntifaceServer.Exited += (object sender, EventArgs args) => {
                Logger.log.Warn("Intiface server process died.");
            };

            mIntifaceServer.Start();
            mIntifaceServer.BeginOutputReadLine();
            mIntifaceServer.BeginErrorReadLine();
        }

        async void DootDoot(ButtplugClientDevice device)
        {
            try
            {
                await device.SendVibrateCmd(0.15f);
                await Task.Delay(150);
                await device.SendStopDeviceCmd();
                await Task.Delay(100);
                await device.SendVibrateCmd(0.15f);
                await Task.Delay(150);
                await device.SendStopDeviceCmd();
            }
            catch { }
        }

        private async Task Scan()
        {
            // Util.DebugLog("Starting scan...");
            await mButtplug.StartScanningAsync();
            await Task.Delay((int)(mScanDuration * 1000));
            // Util.DebugLog("Stopping scan.");
            await mButtplug.StopScanningAsync();
            await Task.Delay((int)(mScanWaitDuration * 1000));
        }

        void OnDeviceAdded(object buttplug, Buttplug.DeviceAddedEventArgs e)
        {
            mDeviceMutex.WaitOne();
            mDevicesRemoved.Remove(e.Device.Index);
            mDevicesAdded[e.Device.Index] = e.Device;
            mDeviceMutex.ReleaseMutex();
        }

        void OnDeviceRemoved(object buttplug, Buttplug.DeviceRemovedEventArgs e)
        {
            mDeviceMutex.WaitOne();
            mDevicesAdded.Remove(e.Device.Index);
            mDevicesRemoved[e.Device.Index] = e.Device;
            mDeviceMutex.ReleaseMutex();
        }

        void OnButtplugDisconnect(object buttplug, EventArgs e)
        {
            mDeviceMutex.WaitOne();
            foreach (var device in mButtplug.Devices)
            {
                OnDeviceRemoved(mButtplug, new DeviceRemovedEventArgs(device));
            }
            mDeviceMutex.ReleaseMutex();
        }

        public void StopServer()
        {
            //_ = mButtplug.DisconnectAsync();
            //_ = mButtplug.StopScanningAsync();

        }
    }
}
