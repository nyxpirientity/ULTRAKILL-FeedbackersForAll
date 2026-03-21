using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using System;
using System.IO;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Cheats
    {
        public const string FeedbackersForEveryone = "nyxpiri.feedbackers-for-everyone";
    }

    [BepInPlugin("nyxpiri.ultrakill.feedbackers-for-everyone", "Feedbackers for Everyone", "0.0.0.1")]
    [BepInProcess("ULTRAKILL.exe")]
    public class FeedbackersForEveryone : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);
            Options.Config = Config;
            Options.Initialize();
            EnemyFeedbacker.Initialize();
            ParryabilityTracker.Initialize();
            CannonballPatches.Initialize();
            CoinPatches.Initialize();
            GrenadePatches.Initialize();
            NailPatches.Initialize();
            ProjectilePatches.Initialize();
            PunchPatches.Initialize();
            RevolverBeamPatches.Initialize();
            Assets.Initialize();
            Harmony.CreateAndPatchAll(GetType().Assembly);
            NyxLib.Cheats.ReadyForCheatRegistration += RegisterCheats;

            if (!File.Exists(Config.ConfigFilePath))
            {
                Config.Save();
            }
        }

        private void RegisterCheats(CheatsManager cheatsManager)
        {            
            cheatsManager.RegisterCheat(new ToggleCheat(
                "Feedbackers for Everyone!", 
                Cheats.FeedbackersForEveryone,
                onDisable: (cheat) =>
                {
                    
                },
                onEnable: (cheat, manager) =>
                {
                    
                }
            ), "FAIRNESS AND EQUALITY");
        }

        protected void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Config.Reload();
            }
        }

        protected void Start()
        {
        }

        protected void Update()
        {

        }

        protected void LateUpdate()
        {

        }
    }
}
