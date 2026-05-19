using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Assets
    {
        public static AssetReference ParryFlashPrefab { get; private set; } = null;
        
        public static GameObject EnemyRevolverBullet { get; private set; } = null;
        public static GameObject EnemyRevolverAltBullet { get; private set; } = null;

        internal static void Initialize()
        {
            NyxLib.Assets.AddAssetPicker<SwordsMachine>((sm) =>
            {
                ParryFlashPrefab = sm.gunFlash;
                
                return true; 
            });

            NyxLib.Assets.AddAssetPicker<EnemyRevolver>((revolver) =>
            {
                EnemyRevolverBullet = GameObject.Instantiate(revolver.bullet);
                EnemyRevolverBullet.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(EnemyRevolverBullet);
            
                EnemyRevolverAltBullet = GameObject.Instantiate(revolver.altBullet);
                EnemyRevolverAltBullet.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(EnemyRevolverAltBullet);
            
                return true; 
            });
        }
    }
}