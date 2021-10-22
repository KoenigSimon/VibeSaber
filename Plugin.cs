using IPA.Config.Stores;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
using BS_Utils.Utilities;
using IPA;
using BeatSaberMarkupLanguage.Settings;

namespace VibeSaber
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }

        internal static ServerHandler serverInstance;

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger, IPA.Config.Config conf)
        {
            Instance = this;
            Logger.log = logger;
            Logger.log.Info("Logger initialized.");
            Config.Instance = conf.Generated<Config>();

            serverInstance = new ServerHandler();
            serverInstance.InitializeServer();
        }

        #region BSIPA Config
        //Uncomment to use BSIPA's config
        /*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
        }
        */
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            Logger.log.Debug("OnApplicationStart");
            new GameObject("VibeSaberController").AddComponent<VibeSaberController>();

            BSMLSettings.instance.AddSettingsMenu("VibeSaber", "VibeSaber.Settings.bsml", Settings.instance);

            BSEvents.gameSceneActive += GameSceneActive;
        }

        void GameSceneActive()
        {
            VibeSaberController.Instance.GetControllers();
            Logger.log.Info("Controllers initialized");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Logger.log.Debug("OnApplicationQuit");
            BSEvents.gameSceneActive -= GameSceneActive;
            serverInstance.StopServer();
        }
    }
    internal static class Logger
    {
        internal static IPALogger log { get; set; }
    }
}
