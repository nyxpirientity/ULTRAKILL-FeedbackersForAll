using System;
using System.Linq;
using System.Reflection;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class RevolverBeamPatches
    {
        internal static void Initialize()
        {
            RevolverBeamEvents.PreRevolverBeamStart += PreRevolverBeamStart;
            RevolverBeamEvents.PreRevolverBeamHitSomething += PreRevolverBeamHitSomething;
            RevolverBeamEvents.PreRevolverBeamPiercingShotCheck += PreRevolverBeamPiercingShotCheck;
        }

        private static FieldInfo _enemiesPiercedFi = typeof(RevolverBeam).GetField("enemiesPierced", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void PreRevolverBeamStart(EventMethodCanceler canceler, RevolverBeam revolverBeam)
        {
            if (revolverBeam.GetComponent<ProjectileBoostTracker>() == null)
            {
                revolverBeam.gameObject.AddComponent<ProjectileBoostTracker>();
            }
        }

        private static void PreRevolverBeamHitSomething(EventMethodCanceler canceler, RevolverBeam revolverBeam, PhysicsCastResult hit)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (!Options.BeamsParryable.Value)
            {
                return;
            }

            if (revolverBeam.beamType == BeamType.Enemy || revolverBeam.beamType == BeamType.MaliciousFace)
            {
                return;
            }

            var boostTracker = revolverBeam.GetComponent<ProjectileBoostTracker>();

            if (boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.Coin && !Options.ShotCoinsParryable.Value)
            {
                return;
            }
            
            if (boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.Grenade && !Options.GrenadesParryable.Value)
            {
                return;
            }

            var parryability = boostTracker.NotifyContact();

            if ((hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var eidid) : hit.collider.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) && (bool)eidid.eid)
            {
                var enemy = eidid.eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    return;
                }
                
                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    return;
                }

                if (!feedbacker.Enabled)
                {
                    return;
                }

                if (!feedbacker.ReadyToParry)
                {
                    return;
                }

                
                feedbacker.ParryEffect(hit.point);
                
                float revBeamDmg = revolverBeam.damage;

                var counterBeamGo = GameObject.Instantiate(Assets.EnemyRevolverBullet);
                var counterBeam = counterBeamGo.GetComponent<Projectile>();
                var counterBeamBoostTracker = counterBeamGo.GetOrAddComponent<ProjectileBoostTracker>();
                counterBeamBoostTracker.CopyFrom(boostTracker);
                
                feedbacker.QueueParry((offset) => 
                {
                    counterBeamBoostTracker.IncrementEnemyBoost();
                    feedbacker.ParryFinishEffect(hit.point + offset);
                    var parryForce = feedbacker.SolveParryForce(hit.point + offset, counterBeamGo.transform.rotation * Vector3.forward * counterBeam.speed);
                    counterBeamGo.transform.position = hit.point + offset;
                    counterBeamGo.transform.rotation = Quaternion.LookRotation(parryForce);
                    counterBeamGo.SetActive(true);
                    
                    var colliders = enemy.Colliders;
                    counterBeamBoostTracker.IgnoreColliders = colliders;

                    //counterBeam.safeEnemyType = enemy.Eid.enemyType;
                    counterBeam.playerBullet = true;
                    counterBeam.damage = revBeamDmg * 25.0f;
                    counterBeam.enemyDamageMultiplier = 3.0f / 25.0f;
                });

                revolverBeam.fake = true;
                canceler.CancelMethod();
                return;
            }

            return;
        }

        private static void PreRevolverBeamPiercingShotCheck(EventMethodCanceler canceler, RevolverBeam revolverBeam)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (!Options.BeamsParryable.Value)
            {
                return;
            }

            if (revolverBeam.beamType == BeamType.Enemy || revolverBeam.beamType == BeamType.MaliciousFace)
            {
                return;
            }

            int enemiesPierced = (int)_enemiesPiercedFi.GetValue(revolverBeam);
            
            if (enemiesPierced != 0)
            {
                return;
            }

            var boostTracker = revolverBeam.GetComponent<ProjectileBoostTracker>();

            if (boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.Coin && !Options.ShotCoinsParryable.Value)
            {
                return;
            }

            var parryability = boostTracker.NotifyContact();

            if (revolverBeam.hitList.Count <= enemiesPierced)
            {
                return;
            }

            var hit = revolverBeam.hitList[enemiesPierced];
            
            if (hit.collider == null)
            {
                return;
            }
            
            if ((hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var eidid) : hit.collider.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) && (bool)eidid.eid)
            {
                var enemy = eidid.eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    return;
                }

                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.CanParry(boostTracker, parryability))
                {
                    return;
                }

                if (!feedbacker.Enabled)
                {
                    return;
                }

                if (!feedbacker.ReadyToParry)
                {
                    return;
                }
                
                feedbacker.ParryEffect(hit.point);

                float revBeamDmg = revolverBeam.damage * 3;

                var counterBeamGo = GameObject.Instantiate(Assets.EnemyRevolverBullet);
                var counterBeam = counterBeamGo.GetComponent<Projectile>();
                var counterBeamBoostTracker = counterBeamGo.GetOrAddComponent<ProjectileBoostTracker>();
                counterBeamBoostTracker.CopyFrom(boostTracker);

                feedbacker.QueueParry((offset) => 
                {
                    counterBeamBoostTracker.IncrementEnemyBoost();
                    counterBeamGo.transform.position = hit.point + offset;
                    feedbacker.ParryFinishEffect(hit.point + offset);
                    var parryForce = feedbacker.SolveParryForce(hit.point + offset, (counterBeam.transform.rotation * Vector3.forward) * counterBeam.speed);
                    counterBeamGo.transform.rotation = Quaternion.LookRotation(parryForce);
                    counterBeamGo.SetActive(true);
                    
                    var colliders = enemy.Colliders;
                    counterBeamBoostTracker.IgnoreColliders = colliders;
                    counterBeamBoostTracker.SafeEid = enemy.Eid;

                    //counterBeam.safeEnemyType = enemy.Eid.enemyType;
                    counterBeam.playerBullet = true;
                    counterBeam.damage = revBeamDmg * 5.0f;
                    counterBeam.enemyDamageMultiplier = 1.0f / 5.0f;
                });

                revolverBeam.fake = true;
                _enemiesPiercedFi.SetValue(revolverBeam, int.MaxValue);
                return;
            }

            return;
        }
    }
}