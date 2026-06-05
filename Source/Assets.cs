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

        private static GameObject _prefabHolder = null;
        public static GameObject PrefabHolder
        {
            get
            {
                if (_prefabHolder == null)
                {
                    _prefabHolder = new GameObject();
                    GameObject.DontDestroyOnLoad(_prefabHolder);
                    _prefabHolder.SetActive(false);
                }

                return _prefabHolder;
            }
        }

        internal static void Initialize()
        {
            NyxLib.Assets.AddAssetPicker<SwordsMachine>((sm) =>
            {
                ParryFlashPrefab = sm.gunFlash;

                return true;
            });

            NyxLib.Assets.AddAssetPicker<EnemyRevolver>((revolver) =>
            {
                if (revolver.bullet.GetComponent<Projectile>() == null)
                {
                    return false;
                }


                EnemyRevolverBullet = GameObject.Instantiate(revolver.bullet, PrefabHolder.transform);
                EnemyRevolverBullet.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(EnemyRevolverBullet);

                EnemyRevolverAltBullet = GameObject.Instantiate(revolver.altBullet, PrefabHolder.transform);
                EnemyRevolverAltBullet.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(EnemyRevolverAltBullet);

                return true;
            });
        }
    }
}