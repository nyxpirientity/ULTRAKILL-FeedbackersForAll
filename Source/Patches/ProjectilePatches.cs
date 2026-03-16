using System;
using System.Linq;
using System.Reflection;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class ProjectilePatches
    {
        static FieldInfo _activeFi = typeof(Projectile).GetField("active", BindingFlags.Instance | BindingFlags.NonPublic); 

        internal static void Initialize()
        {
            ProjectileEvents.PostProjectileAwake += PostProjectileAwake;
            ProjectileEvents.PreProjectileCollided += PreProjectileCollided;
        }

        private static void PostProjectileAwake(EventMethodCancelInfo cancelInfo, Projectile projectile)
        {
            projectile.GetOrAddComponent<ProjectileBoostTracker>();
        }

        private static void PreProjectileCollided(EventMethodCanceler canceler, Projectile projectile, Collider other)
        {
            if (!(bool)_activeFi.GetValue(projectile))
            {
                return;
            }

            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            var boostTracker = projectile.GetComponent<ProjectileBoostTracker>();
                
            if (boostTracker.IgnoreColliders.Contains(other))
            {
                canceler.CancelMethod();
                return;
            }

            if (!projectile.friendly)
            {
                return;
            }


            var parryability = boostTracker.NotifyContact();

            Action failedParry = () =>
            {
                if (boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.Coin && boostTracker.NumPlayerBoosts > 0 && boostTracker.NumEnemyBoosts > 0)
                {
                    StyleHUD.Instance.AddPoints(10, "<color=#ffd000>KEEP THE CHANGE</color>");
                }
            };

            EnemyIdentifierIdentifier eidid = null;

            if (!projectile.friendly && !projectile.hittingPlayer && other.gameObject.CompareTag("Player"))
            {
                return;
            }
            else if (projectile.canHitCoin && other.gameObject.CompareTag("Coin"))
            {
                return;
            }
            else if ((other.gameObject.CompareTag("Armor") && (projectile.friendly || !other.TryGetComponent(out eidid) || !eidid.eid || eidid.eid.enemyType != projectile.safeEnemyType)) || (projectile.boosted && other.gameObject.layer == 11 && other.gameObject.CompareTag("Body") && other.TryGetComponent(out eidid) && (bool)eidid.eid && eidid.eid.enemyType == EnemyType.MaliciousFace && !eidid.eid.isGasolined))
            {
                EnemyIdentifier eid = null;

                if (eidid != null && eidid.eid != null)
                {
                    eid = eidid.eid;
                }

                var enemy = eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.Enabled)
                {
                    return;
                }

                if (boostTracker.SafeEid == eid)
                {
                    canceler.CancelMethod();
                    return;
                }

                return;
            }
            else if ((other.gameObject.CompareTag("Head") || other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Limb") || other.gameObject.CompareTag("EndLimb")) && !other.gameObject.CompareTag("Armor"))
            {
                eidid = other.gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
                
                EnemyIdentifier eid = null;

                if (eidid != null && eidid.eid != null)
                {
                    eid = eidid.eid;
                }

                if ((eid == null) || (projectile.alreadyHitEnemies.Count != 0 && projectile.alreadyHitEnemies.Contains(eid)) || ((eid.enemyType == projectile.safeEnemyType || EnemyIdentifier.CheckHurtException(projectile.safeEnemyType, eid.enemyType, projectile.targetHandle)) && (!projectile.friendly || eid.immuneToFriendlyFire) && !projectile.playerBullet && !projectile.parried))
                {
                    return;
                }

                if (eid.Dead)
                {
                    return;
                }

                Log.Debug($"Deciding parry capability for enemy {eid}, for projectile {projectile} with a hit that hit collider {other}");
                Log.Debug($"boostTracker.IgnoreEid = {boostTracker.SafeEid}");

                var enemy = eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                var feedbacker = enemy.GetFeedbacker();

                if (!feedbacker.Enabled)
                {
                    failedParry();
                    return;
                }
                
                if (boostTracker.SafeEid == eid)
                {
                    canceler.CancelMethod();
                    return;
                }

                if (!feedbacker.ReadyToParry)
                {
                    failedParry();
                    return;
                }

                if (projectile.unparryable || projectile.undeflectable)
                {
                    failedParry();
                    return;
                }

                if (parryability < 0.5f)
                {
                    failedParry();
                    return;
                }
                
                boostTracker.IncrementEnemyBoost();

                feedbacker.ParryEffect(projectile.transform.position);
                projectile.gameObject.SetActive(false);
                feedbacker.QueueParry((offset) => 
                {
                    var parryForce = feedbacker.SolveParryForce(projectile.transform.position + offset, projectile.GetComponent<Rigidbody>().velocity);
                    projectile.homingType = HomingType.None;
                    projectile.transform.rotation = Quaternion.LookRotation(parryForce);
                    boostTracker.IgnoreColliders = enemy.Colliders;
                    boostTracker.SetTempSafeEnemyType(enemy.Eid.enemyType);
                    boostTracker.SafeEid = enemy.Eid;
                    projectile.friendly = false;
                    projectile.gameObject.SetActive(true);
                });

                canceler.CancelMethod();
                return;
            }

            return;
        }
    }
}