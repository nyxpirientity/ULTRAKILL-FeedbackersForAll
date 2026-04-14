using System;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class CannonballPatches
    {
        internal static void Initialize()
        {
            CannonballEvents.PreCannonballStart += PreCannonballStart;
            CannonballEvents.PreCannonballCollide += PreCannonballCollide;
            CannonballEvents.PreCannonballLaunch += PreCannonballLaunch;
            CannonballEvents.PreCannonballExplode += PreCannonballExplode;
        }

        private static readonly FieldInfo _checkingForBreakFi = AccessTools.Field(typeof(Cannonball), "checkingForBreak");

        private static void PreCannonballExplode(EventMethodCanceler canceler, Cannonball cannonball)
        {
            var boostTracker = cannonball.GetComponent<ProjectileBoostTracker>();
            
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (boostTracker == null)
            {
                return;
            }

            if (boostTracker.CannonballExplodingForPlayer)
            {
                return;
            }

            if (boostTracker.NumEnemyBoosts == 0)
            {
                return;
            }

            if (boostTracker.LastBoostedByPlayer)
            {
                StyleHUD.Instance.AddPoints(100, "<color=#ffab02>PARRY THIS");
            }
            else
            {
                StyleHUD.Instance.AddPoints(50, "<color=#02ff17>HOLD THIS FOR ME");
            }

        }

        private static void PreCannonballLaunch(EventMethodCanceler canceler, Cannonball cannonball)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            Log.Debug($"PreCannonballLaunch called on {cannonball}");
            cannonball.GetComponent<ProjectileBoostTracker>().IncrementPlayerBoosts();
        }

        private static void PreCannonballCollide(EventMethodCanceler canceler, Cannonball cannonball, Collider other)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            Log.Debug($"PreCannonballCollide called on {cannonball}");
            Collider col = cannonball.GetComponent<Collider>();
            var boostTracker = cannonball.GetComponent<ProjectileBoostTracker>();

            var options = Options.CannonballsOptions;

            if (!options.CanBeParried.Value)
            {
                return;
            }

            Action failedParry = () =>
            {
                if (boostTracker.NumPlayerBoosts > 0 && boostTracker.NumEnemyBoosts > 0)
                {
                    StyleHUD.Instance.AddPoints(150, "<color=#00c3ff>VOLLEYBALL</color>");
                }
            };

            if (other.TryGetComponent<NewMovement>(out var _) && !boostTracker.LastBoostedByPlayer && boostTracker.HasBeenBoosted)
            {
                boostTracker.CannonballExplodingForPlayer = true;
                cannonball.Explode();
                return;
            }

            if ((cannonball.launched || cannonball.canBreakBeforeLaunched) && !other.isTrigger && (LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment) || (cannonball.launched && other.gameObject.layer == 0 && (!other.gameObject.CompareTag("Player") || !col.isTrigger))))
            {
                return;
            }
            else
            {
                var checkingForBreak = (bool)(_checkingForBreakFi.GetValue(cannonball));

                if ((!cannonball.launched && !cannonball.physicsCannonball) || (other.gameObject.layer != 10 && other.gameObject.layer != 11 && other.gameObject.layer != 12) || checkingForBreak)
                {
                    return;
                }

                if (!(other.attachedRigidbody ? other.attachedRigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var eidid) : other.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) || eidid.eid == null)
                {
                    return;
                }
                
                var enemy = eidid.eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    return;
                }                

                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.Enabled)
                {
                    failedParry();
                    return;
                }

                if (boostTracker.SafeEid == enemy.Eid)
                {
                    canceler.CancelMethod();
                    return;
                }

                var parryability = boostTracker.NotifyContact();
                boostTracker.MarkCannotBeEnemyParried();

                if (!feedbacker.ReadyToParry)
                {
                    failedParry();
                    return;
                }

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    failedParry();
                    return;
                }

                boostTracker.IncrementEnemyBoost();
                feedbacker.ParryEffect(cannonball.transform.position);
                cannonball.gameObject.SetActive(false);
                                
                boostTracker.IgnoreColliders = enemy.Colliders;
                boostTracker.SafeEid = enemy.Eid;
                cannonball.hitEnemies.Add(enemy.Eid);
                float cannonballSpeed = cannonball.Rigidbody.velocity.magnitude;
                
                feedbacker.QueueParry(cannonball.Rigidbody.transform.position, (offset) =>
                {
                    cannonball.Rigidbody.transform.position += offset;
                    feedbacker.ParryFinishEffect(cannonball.transform.position);
                    cannonball.gameObject.SetActive(true);
                    var parryForce = enemy.GetFeedbacker().SolveParryForce(cannonball.transform.position, cannonball.Rigidbody.velocity);
                    
                    cannonball.Rigidbody.velocity = parryForce * cannonballSpeed;
                    cannonball.Rigidbody.transform.rotation = Quaternion.LookRotation(parryForce);
                });

                var v1 = NewMovement.Instance;
                Physics.IgnoreCollision(cannonball.GetComponent<Collider>(), v1.playerCollider, false);

                canceler.CancelMethod();
                return;
            }
        }

        private static void PreCannonballStart(EventMethodCanceler canceler, Cannonball cannonball)
        {
            Log.Debug($"PreCannonballStart called on {cannonball}");
            cannonball.GetOrAddComponent<ProjectileBoostTracker>();
        }
    }
}