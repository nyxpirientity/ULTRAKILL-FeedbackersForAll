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

        public static Dictionary<EnemyType, ConfigEntry<double>> ParryStaminaCost = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> ParryStaminaRechargeRate = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> MinParryCooldowns = new Dictionary<EnemyType, ConfigEntry<double>>(64);

        public static Dictionary<EnemyType, ConfigEntry<double>> FirstHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> MultiHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);

        public static ConfigEntry<float> FirstHitSkillScalar = null;
        public static ConfigEntry<float> MultiHitSkillScalar = null;
        public static ConfigEntry<float> MinParryCooldownScalar = null;
        public static ConfigEntry<float> StaminaRechargeRateScalar = null;
        public static ConfigEntry<float> StaminaCostScalar = null;

        public static ConfigEntry<bool> ShotCoinsParryable = null;
        public static ConfigEntry<bool> PunchedCoinsParryable = null;
        public static ConfigEntry<bool> BeamsParryable = null;
        public static ConfigEntry<bool> GrenadesParryable = null;
        public static ConfigEntry<bool> PlayerProjectilesParryable = null;
        public static ConfigEntry<bool> CannonballsParryable = null;
        public static ConfigEntry<bool> SawsParryable = null;

        public static void Initialize()
        {
            HitstopOnEnemyParry = Config.Bind($"Preferences", "HitstopOnEnemyParry", false);
            EnemyParryDelay = Config.Bind($"Balance", "EnemyParryDelay", 0.3f);
            ParryFollowsEnemy = Config.Bind($"Balance", "ParryFollowsEnemy", true);
            EnemyParrySoundScalar = Config.Bind($"Preference.Audio", "EnemyParrySoundScalar", 4.0f);

            ShotCoinsParryable = Config.Bind($"Balance", "ShotCoinsParryable", true);
            PunchedCoinsParryable = Config.Bind($"Balance", "PunchedCoinsParryable", true);
            BeamsParryable = Config.Bind($"Balance", "BeamsParryable", true);
            GrenadesParryable = Config.Bind($"Balance", "GrenadesParryable", true);
            PlayerProjectilesParryable = Config.Bind($"Balance", "PlayerProjectilesParryable", true);
            CannonballsParryable = Config.Bind($"Balance", "CannonballsParryable", true);
            SawsParryable = Config.Bind($"Balance", "SawsParryable", true);

            FirstHitSkillScalar = Config.Bind($"Balance", "FirstHitSkillScalar", 1.0f);
            MultiHitSkillScalar = Config.Bind($"Balance", "MultiHitSkillScalar", 1.0f);
            MinParryCooldownScalar = Config.Bind($"Balance", "MinParryCooldownScalar", 1.0f);
            StaminaRechargeRateScalar = Config.Bind($"Balance", "StaminaRechargeRateScalar", 1.0f);
            StaminaCostScalar = Config.Bind($"Balance", "StaminaCostScalar", 1.0f);
             
            foreach (var enumVal in Enum.GetValues(typeof(EnemyType)))
            {
                double defaultMultiHitSkill = 0.5;
                double defaultFirstHitSkill = 0.75;

                double defaultStaminaCost = 0.4;
                double defaultStaminaRechargeRate = 0.175;
                double defaultMinParryCooldown = 0.1;

                switch ((EnemyType)enumVal)
                {
                    case EnemyType.V2:
                        defaultFirstHitSkill = 0.8;
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
                    case EnemyType.GabrielSecond:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.5;
                        break;
                    default:
                        break;
                }

                FirstHitParrySkills[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"FirstHitSkill", defaultFirstHitSkill, $"Sets how good {enumVal} is/are at parrying on first contact with an enemy");
                MultiHitParrySkills[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"MultiHitSkill", defaultMultiHitSkill, $"Sets how good {enumVal} is/are at parrying past the first contact with an enemy");
                MinParryCooldowns[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"MinParryCooldown", defaultMinParryCooldown);
                ParryStaminaRechargeRate[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"ParryStaminaRechargeRate", defaultStaminaRechargeRate);
                ParryStaminaCost[(EnemyType)enumVal] = Config.Bind($"Balance.{enumVal}", $"ParryStaminaCost", defaultStaminaCost);
            }

            Config.ConfigReloaded += OnConfigReload;
        }

        private static void OnConfigReload(object sender, EventArgs e)
        {
        }

        internal static ConfigFile Config = null;
    }
}
