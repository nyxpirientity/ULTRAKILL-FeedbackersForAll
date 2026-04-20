using System;
using System.Reflection;
using Nyxpiri.ULTRAKILL.NyxLib;
using ULTRAKILL.Portal;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class GrenadePatches
    {
        internal static void Initialize()
        {
            GrenadeEvents.PreGrenadeStart += PreGrenadeStart;
            GrenadeEvents.PreGrenadeBeam += PreGrenadeBeam;
            GrenadeEvents.PostGrenadeBeam += PostGrenadeBeam;
            GrenadeEvents.PreGrenadeCollision += PreGrenadeCollision;
            GrenadeEvents.PreGrenadeExplode += PreGrenadeExplode;
        }

        private static void PreGrenadeExplode(EventMethodCanceler canceler, Grenade grenade, bool big, bool harmless, bool super, float sizeMultiplier, bool ultrabooster, GameObject exploderWeapon, bool fup)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (exploderWeapon == null)
            {
                return;
            }

            var boostTracker = grenade.GetComponent<ProjectileBoostTracker>();

            if (boostTracker == null)
            {
                return;
            }

            if (boostTracker.NumEnemyBoosts <= 0)
            {
                return;
            }

            if (boostTracker.LastBoostedByPlayer)
            {
                StyleHUD.Instance.AddPoints(150, "<color=#ba42ff>IDK HOW YOU DID THAT</color>");
            }
            else
            {
                StyleHUD.Instance.AddPoints(60, "<color=#d883ff>HIGHLY VOLATILE</color>");
            }
        }

        private static FieldInfo grenadeBeamFi = typeof(Grenade).GetField("grenadeBeam", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void PreGrenadeStart(EventMethodCanceler canceler, Grenade grenade)
        {
            grenade.GetOrAddComponent<ProjectileBoostTracker>();
        }

        private static void PreGrenadeBeam(EventMethodCanceler canceler, Grenade grenade, Vector3 targetPoint, GameObject newSourceWeapon)
        {
            var grenadeBeamPrefab = (RevolverBeam)(grenadeBeamFi.GetValue(grenade));
            var boostTracker = grenadeBeamPrefab.gameObject.AddComponent<ProjectileBoostTracker>();
            var oldBoostTracker = grenade.GetComponent<ProjectileBoostTracker>();
            Assert.IsNotNull(boostTracker);
            Assert.IsNotNull(grenade);
            Assert.IsNotNull(grenade.GetComponent<ProjectileBoostTracker>());

            if (oldBoostTracker.NumEnemyBoosts > 0)
            {
                if (grenade.rocket)
                {
                    StyleHUD.Instance.AddPoints(500, "<color=#ae57ff>MODERN <color=#ff0000>T<color=#ffaa00>E<color=#0dff00>C<color=#ffd500>H<color=#7bff00>N<color=#00ff59>O<color=#00c3ff>L<color=#0080ff>O<color=#7300ff>G<color=#ff00ee>Y</color>");
                }
                else
                {
                    StyleHUD.Instance.AddPoints(350, "<color=#00fff7>CONVERSION</color>");
                }
            }

            boostTracker.CopyFrom(oldBoostTracker);
            boostTracker.IncrementPlayerBoosts();
        }

        private static void PostGrenadeBeam(EventMethodCancelInfo cancelInfo, Grenade grenade, Vector3 targetPoint, GameObject newSourceWeapon)
        {
            var grenadeBeamPrefab = (RevolverBeam)grenadeBeamFi.GetValue(grenade);
            //UnityEngine.Object.Destroy(grenadeBeamPrefab.gameObject.GetComponent<ProjectileBoostTracker>());
        }

        private static void PreGrenadeCollision(EventMethodCanceler canceler, Grenade grenade, Collider other, Vector3 velocity)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            var options = Options.GrenadesOptions;

            if (!options.CanBeParried.Value)
            {
                return;
            }

            var boostTracker = grenade.GetComponent<ProjectileBoostTracker>();

            var parryability = boostTracker.NotifyContact();

            Action failedParry = () =>
            {
                if (boostTracker.NumEnemyBoosts == 0)
                {
                    return;
                }

                if (grenade.playerRiding)
                {
                    StyleHUD.Instance.AddPoints(750, "<color=#ff0000>NOW PARRY US");
                }
            };

            if (other.TryGetComponent<PortalAwarePlayerColliderClone>(out var _) || grenade.IsExploded() || (!grenade.enemy && other.CompareTag("Player")) || other.gameObject.layer == 14 || other.gameObject.layer == 20)
            {
                return;
            }
            
            if ((other.gameObject.layer == 11 || other.gameObject.layer == 10) && (other.attachedRigidbody ? other.attachedRigidbody.TryGetComponent<EnemyIdentifierIdentifier>(out var eidid) : other.TryGetComponent<EnemyIdentifierIdentifier>(out eidid)) && (bool)eidid.eid)
            {
                var enemy = eidid.eid.GetComponent<EnemyComponents>();

                Assert.IsNotNull(enemy);

                if (enemy.Eid.Dead)
                {
                    return;
                }

                if (grenade.ignoreEnemyType.Count > 0 && grenade.ignoreEnemyType.Contains(enemy.Eid.enemyType))
                {
                    return;
                }

                var feedbacker = enemy.GetFeedbacker();


                if (!feedbacker.Enabled)
                {
                    return;
                }

                if (grenade.playerRiding)
                {
                    failedParry();
                    return;
                }

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
                
                feedbacker.ParryEffect(grenade.transform.position);
                boostTracker.IncrementEnemyBoost();
                
                grenade.gameObject.SetActive(false);
                
                feedbacker.QueueParry(grenade.rb.transform.position, (offset) =>
                {
                    if (grenade == null)
                    {
                        return; // todo: to preven the parry flash warning leading to nothing, maybe this should be available as a sort of validate parry validity action param
                    }

                    grenade.gameObject.SetActive(true);
                    feedbacker.ParryFinishEffect(grenade.transform.position  + offset);
                    Vector3 parryForce;
                    if (grenade.rocket)
                    {
                        parryForce = feedbacker.SolveParryForce(grenade.transform.position + offset, (grenade.transform.rotation * Vector3.forward) * grenade.rocketSpeed);
                        grenade.rb.velocity = parryForce * grenade.rb.velocity.magnitude;
                        grenade.rb.rotation = Quaternion.LookRotation(parryForce);
                    }
                    else
                    {
                        parryForce = feedbacker.SolveParryForce(grenade.transform.position + offset, grenade.rb.velocity * 5.0f);
                        var vel = (parryForce * grenade.rb.velocity.magnitude * 5.0f);

                        if (vel.magnitude > 80.0f)
                        {
                            vel = vel.normalized * 80.0f;
                        }

                        grenade.rb.velocity = vel;
                    }

                    grenade.enemy = true;

                    boostTracker.IgnoreColliders = enemy.Colliders;
                    boostTracker.SafeEid = enemy.Eid;
                    grenade.transform.position += offset;

                    var v1 = NewMovement.Instance;
                    Physics.IgnoreCollision(grenade.GetComponent<Collider>(), v1.playerCollider, false);
                });

                canceler.CancelMethod();
                return;
            }

            return;
        }
    }
}