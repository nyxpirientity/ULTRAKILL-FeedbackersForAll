using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class NailPatches
    {
        internal static void Initialize()
        {
            NailEvents.PreNailStart += PreNailStart;
            NailEvents.PreNailTouchEnemy += PreNailTouchEnemy;
            NailEvents.PreNailHitEnemy += PreNailHitEnemy;
        }

        static FieldInfo sameEnemyHitCooldownFi = AccessTools.Field(typeof(Nail), "sameEnemyHitCooldown");
        static FieldInfo currentHitEnemyFi =AccessTools.Field(typeof(Nail), "currentHitEnemy");
        static FieldInfo hitLimbsFi = AccessTools.Field(typeof(Nail), "hitLimbs");
        static FieldAccess<Nail, EnemyIdentifier> currentHitEnemyFA = new FieldAccess<Nail, EnemyIdentifier>("currentHitEnemy");

        private static void PreNailStart(EventMethodCanceler canceler, Nail nail)
        {
            nail.GetOrAddComponent<ProjectileBoostTracker>();
        }

        private static void PreNailTouchEnemy(EventMethodCanceler canceler, Nail nail, Transform other)
        {
            if (nail.chainsaw && nail.sawblade)
            {
                var options = Options.SawsOptions;

                if (!options.CanBeParried.Value)
                {
                    return;
                }

                if (!other.TryGetComponent<EnemyIdentifierIdentifier>(out var eidid) || !eidid.eid)
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
                    return;
                }

                var boostTracker = nail.GetComponent<ProjectileBoostTracker>();

                if (boostTracker == null)
                {
                    return;
                }

                if (boostTracker.SafeEid == enemy.Eid)
                {
                    canceler.CancelMethod();
                    return;
                }

                if (nail.stopped)
                {
                    return;
                }

                var parryability = boostTracker.NotifyContact();
                boostTracker.MarkCannotBeEnemyParried();

                if (!feedbacker.ReadyToParry)
                {
                    return;
                }

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    return;
                }

                boostTracker.IncrementEnemyBoost();
                feedbacker.ParryEffect(nail.transform.position);
                nail.gameObject.SetActive(false);
                feedbacker.QueueParry(nail.transform.position, (position) =>
                {
                    nail.transform.position += position;
                    var parryForce = feedbacker.SolveParryForce(nail.transform.position, nail.rb.velocity);
                
                    nail.rb.velocity = parryForce * nail.rb.velocity.magnitude;
                    nail.rb.transform.rotation = Quaternion.LookRotation(parryForce);
                    boostTracker.IgnoreColliders = enemy.Colliders;
                    boostTracker.SafeEid = enemy.Eid;
                    nail.punched = false;
                    nail.punchedTimer = 0.0f;
                    nail.punchable = true;
                    currentHitEnemyFA.SetValue(nail, null);
                    nail.originalVelocity = nail.rb.velocity;
                    sameEnemyHitCooldownFi.SetValue(nail, 0.0f);
                    nail.stopped = false;

                    var v1 = NewMovement.Instance;
                    
                    foreach (var col in boostTracker.Colliders)
                    {
                        Physics.IgnoreCollision(col, v1.playerCollider, false);
                    }
                    nail.gameObject.SetActive(true);
                });


                canceler.CancelMethod();
                return;
            }
        }

        private static void PreNailHitEnemy(EventMethodCanceler canceler, Nail nail, Transform other, EnemyIdentifierIdentifier eidid)
        {
            if (!nail.chainsaw && !nail.sawblade)
            {
                return;
            }

            if (nail.magnets.Count > 0)
            {
                return;
            }
            
            var sameEnemyHitCooldown = (float)sameEnemyHitCooldownFi.GetValue(nail);
            var currentHitEnemy = (EnemyIdentifier)currentHitEnemyFi.GetValue(nail);
            var hitLimbs = (List<Transform>)hitLimbsFi.GetValue(nail);

            if ((eidid == null && !other.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) || !eidid.eid || (nail.enemy && eidid != null && eidid.eid != null && eidid.eid.enemyType == nail.safeEnemyType) || (nail.sawblade && ((sameEnemyHitCooldown > 0f && currentHitEnemy != null && currentHitEnemy == eidid.eid) || hitLimbs.Contains(other))))
            {
                return;
            }

            Assert.IsNotNull(eidid);
            Assert.IsNotNull(eidid.eid);

            var enemy = eidid.eid.GetComponent<EnemyComponents>();
            
            Assert.IsNotNull(enemy);

            var options = Options.SawsOptions;

            if (!options.CanBeParried.Value)
            {
                return;
            }

            if (enemy.Eid.Dead)
            {
                return;
            }                

            var feedbacker = enemy.GetFeedbacker();

            if (!feedbacker.Enabled)
            {
                return;
            }

            var boostTracker = nail.GetComponent<ProjectileBoostTracker>();

            if (boostTracker == null)
            {
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
                return;
            }

            if (!feedbacker.CanParry(boostTracker, parryability))
            {
                return;
            }

            var parryForce = feedbacker.SolveParryForce(nail.transform.position, nail.rb.velocity);
            
            nail.rb.velocity = parryForce * nail.rb.velocity.magnitude;
            nail.rb.transform.rotation = Quaternion.LookRotation(parryForce);

            boostTracker.IncrementEnemyBoost();
            feedbacker.ParryEffect(nail.transform.position);

            boostTracker.IgnoreColliders = enemy.Colliders;
            boostTracker.SafeEid = enemy.Eid;

            nail.punched = false;
            nail.punchedTimer = 0.0f;
            nail.punchable = true;

            var v1 = NewMovement.Instance;
            
            foreach (var col in boostTracker.Colliders)
            {
                Physics.IgnoreCollision(col, v1.playerCollider, false);
            }

            canceler.CancelMethod();
            return;
        }

        [HarmonyPatch(typeof(Nail), "FixedUpdate")]
        static class NailFixedUpdatePatch
        {
            public static void Prefix(Nail __instance)
            {
                if (!__instance.sawblade && !__instance.chainsaw)
                {
                    return;
                }

                var boostTracker = __instance.GetComponent<ProjectileBoostTracker>();

                boostTracker.PreNailFixedUpdate();
            }
            
            public static void Postfix(Nail __instance)
            {
            }
        }
    }
}