using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Cheats
    {
        public const string FeedbackersForEveryone = "nyxpiri.feedbackers-for-everyone";
    }

    [BepInPlugin("com.nyxpiri.bepinex.plugins.ultrakill.feedbackers-for-everyone", "Feedbackers for Everyone", "0.0.0.1")]
    [BepInProcess("ULTRAKILL.exe")]
    public class FeedbackersForEveryone : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);
            EnemyFeedbacker.Initialize();
            ParryabilityTracker.Initialize();
            CannonballPatches.Initialize();
            CoinPatches.Initialize();
            GrenadePatches.Initialize();
            NailPatches.Initialize();
            ProjectilePatches.Initialize();
            PunchPatches.Initialize();
            RevolverBeamPatches.Initialize();
            Harmony.CreateAndPatchAll(Assembly.GetCallingAssembly());
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
