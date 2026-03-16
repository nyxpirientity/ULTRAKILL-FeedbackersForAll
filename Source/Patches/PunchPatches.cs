using System;
using Nyxpiri.ULTRAKILL.NyxLib;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class PunchPatches
    {
        internal static void Initialize()
        {
            PlayerPunchEvents.PreParryProjectile += PreParryProjectile;
        }

        private static void PreParryProjectile(EventMethodCanceler canceler, Punch punch, Projectile proj)
        {
            if (NyxLib.Cheats.Enabled)
            {
                var boostTracker = proj.GetComponent<ProjectileBoostTracker>();
                if (boostTracker != null)
                {
                    boostTracker.IncrementPlayerBoosts();
                    
                    if (boostTracker.NumPlayerBoosts > 1)
                    {
                        proj.speed *= 0.55f; // player parry boosts speed by 2x, so this counteracts it
                    }

                    if (boostTracker.NumEnemyBoosts > 0 && (boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.RevolverBeam || boostTracker.ProjectileType == ProjectileBoostTracker.ProjectileCategory.PlayerProjectile))
                    {
                        StyleHUD.Instance.AddPoints(10, "<color=#26ff00>PARRY PONG</color>");
                    }
                }
            }
        }
    }
}