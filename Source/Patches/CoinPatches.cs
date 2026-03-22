using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class CoinPatches
    {
        internal static void Initialize()
        {
            CoinEvents.PostCoinAwake += PostCoinAwake;
        }

        private static void PostCoinAwake(EventMethodCancelInfo cancelInfo, Coin coin)
        {
            Log.Debug($"PostCoinAwake called on {coin}");
            coin.GetOrAddComponent<ProjectileBoostTracker>();
        }
        [HarmonyPatch(typeof(Coin), nameof(Coin.GetDeleted))]
        static class CoinDeletionPatch
        {
        }

        [HarmonyPatch(typeof(Coin), nameof(Coin.ReflectRevolver))]
        static class CoinReflectRevolverPatch
        {
            private static EventMethodCancellationTracker _cancellationTracker = new EventMethodCancellationTracker();

            static void DeliverDamageReplacement(EnemyIdentifier eid, GameObject target, Vector3 force, Vector3 hitPoint, float multiplier, bool tryForExplode, float critMultiplier = 0f, GameObject sourceWeapon = null, bool ignoreTotalDamageTakenMultiplier = false, bool fromExplosion = false)
            {
                Log.Debug($"DeliverDamageReplacement called on {_currentCoin}");

                Action deliverThatDamage = () =>
                {
                    eid.DeliverDamage(target, force, hitPoint, multiplier, tryForExplode, critMultiplier, sourceWeapon, ignoreTotalDamageTakenMultiplier, fromExplosion);  
                };

                if (!NyxLib.Cheats.Enabled)
                {
                    deliverThatDamage();
                    return;
                }
                
                var coin = _currentCoin;

                var boostTracker = coin.GetComponent<ProjectileBoostTracker>();

                var parryability = boostTracker.NotifyContact();

                var enemy = eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    deliverThatDamage();
                    return;
                }

                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    deliverThatDamage();
                    return;
                }

                if (!feedbacker.Enabled)
                {
                    deliverThatDamage();
                    return;
                }

                if (!feedbacker.ReadyToParry)
                {
                    deliverThatDamage();
                    return;
                }

                
                feedbacker.ParryEffect(hitPoint);
                var coinMeshF = coin.GetComponentInChildren<MeshFilter>();
                var coinMeshR = coin.GetComponentInChildren<MeshRenderer>();
                
                var counterBeamGo = GameObject.Instantiate(NyxLib.Assets.EnemyRevolverBullet);
                var counterBeam = counterBeamGo.GetComponent<Projectile>();
                var counterBeamBoostTracker = counterBeamGo.GetOrAddComponent<ProjectileBoostTracker>();
                counterBeam.GetComponentInChildren<MeshFilter>().mesh = coinMeshF.mesh;
                counterBeam.GetComponentInChildren<MeshRenderer>().material = coinMeshR.material;
                counterBeamBoostTracker.CopyFrom(boostTracker);
                counterBeamBoostTracker.IncrementEnemyBoost();
                float coinPower = coin.power;
                
                feedbacker.QueueParry((offset) =>
                {
                    feedbacker.ParryFinishEffect(hitPoint + offset);
                    var parryForce = feedbacker.SolveParryForce(hitPoint + offset, (counterBeam.transform.rotation * Vector3.forward) * counterBeam.speed);
                    counterBeamGo.transform.position = hitPoint + offset;
                    counterBeamGo.transform.rotation = Quaternion.LookRotation(parryForce);
                    counterBeamGo.SetActive(true);
                    
                    var colliders = enemy.Colliders;
                    counterBeamBoostTracker.IgnoreColliders = colliders;
                    counterBeamBoostTracker.SafeEid = eid;

                    counterBeamBoostTracker.SetTempSafeEnemyType(enemy.Eid.enemyType);
                    counterBeam.playerBullet = true;
                    counterBeam.damage = coinPower * 5.0f;
                    counterBeam.enemyDamageMultiplier = 1.0f / 5.0f;
                });

                UnityEngine.Object.Destroy(coin.gameObject);
                return;
            }

            private static Coin _currentCoin = null;

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.Calls(typeof(EnemyIdentifier).GetMethod(nameof(EnemyIdentifier.DeliverDamage))))
                    {
                        instr.operand = typeof(CoinReflectRevolverPatch).GetMethod(nameof(DeliverDamageReplacement), BindingFlags.Static | BindingFlags.NonPublic);
                    }

                    yield return instr;
                }
            }
            
            public static void Prefix(Coin __instance)
            {
                _currentCoin = __instance;
            }

            public static void Postfix(Coin __instance)
            {
                _currentCoin = null;
            }
        }

        [HarmonyPatch(typeof(Coin), nameof(Coin.Punchflection))]
        static class CoinPunchflectionPatch
        {
            private static EventMethodCancellationTracker _cancellationTracker = new EventMethodCancellationTracker();

            static void DeliverDamageReplacement(EnemyIdentifier eid, GameObject target, Vector3 force, Vector3 hitPoint, float multiplier, bool tryForExplode, float critMultiplier = 0f, GameObject sourceWeapon = null, bool ignoreTotalDamageTakenMultiplier = false, bool fromExplosion = false)
            {
                Log.Debug($"Punchflection.DeliverDamageReplacement called on {_currentCoin}");

                Action deliverThatDamage = () =>
                {
                    eid.DeliverDamage(target, force, hitPoint, multiplier, tryForExplode, critMultiplier, sourceWeapon, ignoreTotalDamageTakenMultiplier, fromExplosion);  
                };

                if (!NyxLib.Cheats.Enabled)
                {
                    deliverThatDamage();
                    return;
                }
                
                var coin = _currentCoin;

                var boostTracker = coin.GetComponent<ProjectileBoostTracker>();

                boostTracker.CoinPunched = true;

                var parryability = boostTracker.NotifyContact();

                var enemy = eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    deliverThatDamage();
                    return;
                }
                
                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    deliverThatDamage();
                    return;
                }

                if (!feedbacker.Enabled)
                {
                    deliverThatDamage();
                    return;
                }

                if (!feedbacker.ReadyToParry)
                {
                    deliverThatDamage();
                    return;
                }

                feedbacker.ParryEffect(hitPoint);
                float coinPower = coin.power;
                var coinMeshF = coin.GetComponentInChildren<MeshFilter>();
                var coinMeshR = coin.GetComponentInChildren<MeshRenderer>();
                
                var counterBeamGo = GameObject.Instantiate(NyxLib.Assets.EnemyRevolverBullet);
                var counterBeam = counterBeamGo.GetComponent<Projectile>();
                var counterBeamBoostTracker = counterBeamGo.GetOrAddComponent<ProjectileBoostTracker>();
                counterBeam.GetComponentInChildren<MeshFilter>().mesh = coinMeshF.mesh;
                counterBeam.GetComponentInChildren<MeshRenderer>().material = coinMeshR.material;
                counterBeamBoostTracker.CopyFrom(boostTracker);
                counterBeamBoostTracker.IncrementEnemyBoost();

                feedbacker.QueueParry((offset) =>
                {
                    feedbacker.ParryFinishEffect(hitPoint + offset);
                    var parryForce = feedbacker.SolveParryForce(hitPoint + offset, (counterBeam.transform.rotation * Vector3.forward) * counterBeam.speed);
                    counterBeamGo.transform.position = hitPoint + offset;
                    counterBeamGo.transform.rotation = Quaternion.LookRotation(parryForce);

                    counterBeamGo.SetActive(true);
                    
                    var colliders = enemy.Colliders;
                    counterBeamBoostTracker.IgnoreColliders = colliders;
                    counterBeamBoostTracker.SafeEid = eid;

                    //counterBeam.safeEnemyType = enemy.Eid.enemyType;
                    counterBeam.playerBullet = true;
                    counterBeam.damage = coinPower * 5.0f;
                    counterBeamBoostTracker.SetTempSafeEnemyType(enemy.Eid.enemyType);
                    counterBeam.enemyDamageMultiplier = 1.0f / 5.0f;
                });

                coin.GetDeleted();
                return;
            }

            private static Coin _currentCoin = null;

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instr in instructions)
                {
                    if (instr.Calls(typeof(EnemyIdentifier).GetMethod(nameof(EnemyIdentifier.DeliverDamage))))
                    {
                        instr.operand = typeof(CoinPunchflectionPatch).GetMethod(nameof(DeliverDamageReplacement), BindingFlags.Static | BindingFlags.NonPublic);
                    }

                    yield return instr;
                }
            }

            public static void Prefix(Coin __instance)
            {
                _currentCoin = __instance;
            }

            public static void Postfix(Coin __instance)
            {
                _currentCoin = null;
            }
        }
    }
}