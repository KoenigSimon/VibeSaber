using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;

namespace VibeSaber
{
    internal class Settings : PersistentSingleton<Settings>
    {
        [UIValue("DisableVibration")]
        public bool disable = Config.Instance.disable;

        [UIValue("VibrationTimeperNote")]
        public float vibeTime = Config.Instance.vibeTime;

        [UIValue("StackVibrationTime")]
        public bool stackVibeTime = Config.Instance.stackVibeTime;

        [UIValue("ScaleVibrationwithEnergy")]
        public bool scaleWithEnergy = Config.Instance.scaleWithEnergy;

        [UIValue("FixedModeIntensityPercentage")]
        public int fixedIntensity = Config.Instance.fixedIntensity;

        [UIValue("MinDynamicIntensityPercentage")]
        public int minVibePercent = Config.Instance.minVibePercent;

        [UIValue("MaxDynamicIntensityPercentage")]
        public int maxVibePercent = Config.Instance.maxVibePercent;

        [UIValue("VibrateonMissedCut")]
        public bool vibeOnMissedCut = Config.Instance.vibeOnMiss;

        [UIValue("VibrateonBadCut")]
        public bool vibeOnBadCut = Config.Instance.vibeOnBadCut;

        [UIValue("VibrateonGoodCut")]
        public bool vibeOnGoodCut = Config.Instance.vibeOnGoodCut;

        [UIValue("VibrationTimeonLevelFail")]
        public float vibeTimeOnFail = Config.Instance.vibeTimeOnFail;

        [UIValue("ShowButtplugActivityOverlay")]
        public bool showActivityOverlay = Config.Instance.showActivityOverlay;


        [UIAction("#apply")]
        public void OnApply()
        {
            Config.Instance.disable = disable;
            Config.Instance.vibeTime = vibeTime;
            Config.Instance.stackVibeTime = stackVibeTime;
            Config.Instance.scaleWithEnergy = scaleWithEnergy;
            Config.Instance.fixedIntensity = fixedIntensity;
            Config.Instance.minVibePercent = minVibePercent;
            Config.Instance.maxVibePercent = maxVibePercent;
            Config.Instance.vibeOnMiss = vibeOnMissedCut;
            Config.Instance.vibeOnBadCut = vibeOnBadCut;
            Config.Instance.vibeOnGoodCut = vibeOnGoodCut;
            Config.Instance.showActivityOverlay = showActivityOverlay;
            Config.Instance.vibeTimeOnFail = vibeTimeOnFail;
        }


        Settings()
        {
            
        }
    }
}
