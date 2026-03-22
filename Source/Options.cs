using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Options
    {
        public static ConfigEntry<bool> HitstopOnEnemyParry = null;
        public static ConfigEntry<bool> ParryFollowsEnemy = null;
        public static ConfigEntry<float> EnemyParryDelay = null;
        public static ConfigEntry<float> EnemyParrySoundScalar = null;
        public static ConfigEntry<string> ParryProsString = null;
        public static Dictionary<EnemyType, ConfigEntry<double>> FirstHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> MultiHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);

        public static void Initialize()
        {
            HitstopOnEnemyParry = Config.Bind($"Preferences", "HitstopOnEnemyParry", false);
            EnemyParryDelay = Config.Bind($"Balance", "EnemyParryDelay", 0.3f);
            ParryFollowsEnemy = Config.Bind($"Balance", "ParryFollowsEnemy", true);
            EnemyParrySoundScalar = Config.Bind($"Preference.Audio", "EnemyParrySoundScalar", 4.0f);

            foreach (var enumVal in Enum.GetValues(typeof(EnemyType)))
            {
                double defaultMultiHitSkill = 0.5;
                double defaultFirstHitSkill = 0.75;

                switch ((EnemyType)enumVal)
                {
                    case EnemyType.V2:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.75;
                        break;
                    case EnemyType.V2Second:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.75;
                        break;
                    case EnemyType.MinosPrime:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.75;
                        break;
                    case EnemyType.SisyphusPrime:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.75;
                        break;
                    default:
                        break;
                }

                FirstHitParrySkills[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"FirstHitSkill", defaultFirstHitSkill, $"Sets how good {enumVal} is/are at parrying on first contact with an enemy");
                MultiHitParrySkills[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"MultiHitSkill", defaultMultiHitSkill, $"Sets how good {enumVal} is/are at parrying past the first contact with an enemy");
            }

            Config.ConfigReloaded += OnConfigReload;
        }

        private static void OnConfigReload(object sender, EventArgs e)
        {
        }

        internal static ConfigFile Config = null;
    }
}
