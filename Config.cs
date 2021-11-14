using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace VibeSaber
{
    public class Config
    {
        public static Config Instance { get; set; }

        public virtual bool stackVibeTime { get; set; } = false;
        public virtual float vibeTime { get; set; } = 0.3f;
        public virtual bool vibeOnMiss { get; set; } = true;
        public virtual bool vibeOnBadCut { get; set; } = false;
        public virtual bool vibeOnGoodCut { get; set; } = false;
        public virtual bool scaleWithEnergy { get; set; } = false;
        public virtual int fixedIntensity { get; set; } = 50;
        public virtual int maxVibePercent { get; set; } = 100;
        public virtual int minVibePercent { get; set; } = 0;
        public virtual float vibeTimeOnFail { get; set; } = 0;
        public virtual bool showActivityOverlay { get; set; } = false;
        public virtual bool disable { get; set; } = false;
        public virtual int maxServerUpdatedPerSecond { get; set; } = 10;


        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
            Logger.log.Debug("Updated configuration");
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Config other)
        {
            // This instance's members populated from other
        }
    }
}
