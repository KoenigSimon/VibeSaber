using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace VibeSaber
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class VibeSaberController : MonoBehaviour
    {
        public static VibeSaberController Instance { get; private set; }

        ScoreController scoreController;
        GameEnergyCounter energyCounter;
        ILevelEndActions endActions;

        public void GetControllers()
        {
            scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().LastOrDefault();
            energyCounter = Resources.FindObjectsOfTypeAll<GameEnergyCounter>().LastOrDefault();
            endActions = Resources.FindObjectsOfTypeAll<StandardLevelGameplayManager>().LastOrDefault();

            if (scoreController != null && energyCounter != null && endActions != null)
            {
                scoreController.noteWasMissedEvent += NoteMiss;
                scoreController.noteWasCutEvent += NoteCut;
                endActions.levelFinishedEvent += LevelFinished;
                endActions.levelFailedEvent += LevelFailed;
                Logger.log.Info("Controller Events set successfully");
            }
            else
            {
                Logger.log.Info("Could not reload VibeSaber. May happen when playing online");
                scoreController = null;
                energyCounter = null;
            }
        }

        private void NoteMiss(NoteData data, int score)
        {
            if (energyCounter.noFail == false && Config.Instance.vibeOnMiss)
                Plugin.serverInstance.SetVibration(energyCounter.energy);
        }

        private void NoteCut(NoteData data, in NoteCutInfo info, int multiplier)
        {
            //if swingratingcounter object is zero, it was a bad cut
            if(Config.Instance.vibeOnBadCut && info.swingRatingCounter == null)
                Plugin.serverInstance.SetVibration(energyCounter.energy);

            //otherwise good cut
            if (Config.Instance.vibeOnGoodCut && info.swingRatingCounter == null)
                Plugin.serverInstance.SetVibration(energyCounter.energy);
        }

        private void LevelFailed()
        {
            LevelOver(true);
        }

        private void LevelFinished()
        {
            LevelOver(false);
        }

        private void LevelOver(bool failed)
        {
            Logger.log.Info("Level Ended");
            Plugin.serverInstance.LevelOver(failed);
            if (scoreController != null)
            {
                scoreController.noteWasMissedEvent -= NoteMiss;
                scoreController.noteWasCutEvent -= NoteCut;
            }
            if (endActions != null)
            {
                endActions.levelFinishedEvent -= LevelFinished;
                endActions.levelFailedEvent -= LevelFailed;
            }            
        }

        // These methods are automatically called by Unity, you should remove any you aren't using.
        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (Instance != null)
            {
                Logger.log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            Instance = this;
            Logger.log?.Debug($"{name}: Awake()");
        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after any other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }

        /// <summary>
        /// Called every frame if the script is enabled.
        /// </summary>
        private void Update()
        {
            if (Plugin.serverInstance != null)
                Plugin.serverInstance.OnFrameServerUpdate();
        }

        /// <summary>
        /// Called every frame after every other enabled script's Update().
        /// </summary>
        private void LateUpdate()
        {

        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {

        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {

        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Logger.log?.Debug($"{name}: OnDestroy()");
            if (Instance == this)
                Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.
        }
        #endregion
    }
}
