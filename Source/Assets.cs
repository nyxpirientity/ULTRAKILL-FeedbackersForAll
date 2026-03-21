using System;
using UnityEngine.AddressableAssets;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Assets
    {
        public static AssetReference ParryFlashPrefab { get; private set; } = null;

        internal static void Initialize()
        {
            LevelQuickLoader.AddQuickLoadLevel("uk_construct");

            NyxLib.Assets.EnableExplosionsPicking();
            NyxLib.Assets.EnableProjectilePicking();

            NyxLib.Assets.AddAssetPicker<SwordsMachine>((sm) =>
            {
                ParryFlashPrefab = sm.gunFlash;
                
                return true; 
            });
        }
    }
}