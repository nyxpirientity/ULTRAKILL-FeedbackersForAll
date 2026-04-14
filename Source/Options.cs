using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Options
    {
        public static ConfigEntry<bool> LogDebugInfo = null;

        public static ConfigEntry<bool> HitstopOnEnemyParry = null;
        public static ConfigEntry<bool> ParryFollowsEnemy = null;
        public static ConfigEntry<float> EnemyParryDelay = null;
        public static ConfigEntry<float> EnemyParrySoundScalar = null;
        public static ConfigEntry<bool> MidwayParryEffect = null;
        public static ConfigEntry<string> ParryProsString = null;

        public static Dictionary<EnemyType, ConfigEntry<double>> ParryStaminaCost = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> ParryStaminaRechargeRate = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> MinParryCooldowns = new Dictionary<EnemyType, ConfigEntry<double>>(64);

        public static Dictionary<EnemyType, ConfigEntry<double>> FirstHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);
        public static Dictionary<EnemyType, ConfigEntry<double>> MultiHitParrySkills = new Dictionary<EnemyType, ConfigEntry<double>>(64);

        public static ConfigEntry<int> ParryabilityMemory = null;

        public static ConfigEntry<float> FirstHitSkillScalar = null;
        public static ConfigEntry<float> MultiHitSkillScalar = null;
        public static ConfigEntry<float> MinParryCooldownScalar = null;
        public static ConfigEntry<float> StaminaRechargeRateScalar = null;
        public static ConfigEntry<float> StaminaCostScalar = null;

        public static ProjectileTypeOptions ShotCoinsOptions = null;
        public static ProjectileTypeOptions PunchedCoinsOptions = null;
        public static ProjectileTypeOptions BeamsOptions = null;
        public static ProjectileTypeOptions RailCannonOptions = null;
        public static ProjectileTypeOptions GrenadesOptions = null;
        public static ProjectileTypeOptions PlayerProjectilesOptions = null;
        public static ProjectileTypeOptions EnemyProjectilesOptions = null;
        public static ProjectileTypeOptions CannonballsOptions = null;
        public static ProjectileTypeOptions SawsOptions = null;

        public static ConfigEntry<bool> DifferentiateRailCannonFromBeams = null;
        public static ConfigEntry<bool> DifferentiateElectricity = null;
        public static ConfigEntry<bool> DifferentiateCoinRicochets = null;
        public static ConfigEntry<int> CoinRicochetDivisor = null;

        public static void Initialize()
        {
            HitstopOnEnemyParry = Config.Bind($"Preferences", "HitstopOnEnemyParry", false);
            EnemyParryDelay = Config.Bind($"Balance", "EnemyParryDelay", 0.4f);
            ParryFollowsEnemy = Config.Bind($"Balance", "ParryFollowsEnemy", true);
            EnemyParrySoundScalar = Config.Bind($"Preferences.Audio", "EnemyParrySoundScalar", 4.0f);
            MidwayParryEffect = Config.Bind($"Preferences", "MidwayParryEffect", true);

            ShotCoinsOptions = new ProjectileTypeOptions("ShotCoins", true, 0.75f);
            PunchedCoinsOptions = new ProjectileTypeOptions("PunchedCoins", true, 0.75f);
            BeamsOptions = new ProjectileTypeOptions("Beams", true, 0.75f);
            RailCannonOptions = new ProjectileTypeOptions("RailCannon", true, 0.75f);
            GrenadesOptions = new ProjectileTypeOptions("Grenades", true, 0.75f);
            PlayerProjectilesOptions = new ProjectileTypeOptions("PlayerProjectiles", true, 0.75f);
            EnemyProjectilesOptions = new ProjectileTypeOptions("EnemyProjectiles", true, 0.75f);
            CannonballsOptions = new ProjectileTypeOptions("Cannonballs", true, 0.75f);
            SawsOptions = new ProjectileTypeOptions("Saws", true, 0.75f);

            FirstHitSkillScalar = Config.Bind($"Balance", "FirstHitSkillScalar", 1.0f);
            MultiHitSkillScalar = Config.Bind($"Balance", "MultiHitSkillScalar", 1.0f);
            MinParryCooldownScalar = Config.Bind($"Balance", "MinParryCooldownScalar", 1.0f);
            StaminaRechargeRateScalar = Config.Bind($"Balance", "StaminaRechargeRateScalar", 1.0f);
            StaminaCostScalar = Config.Bind($"Balance", "StaminaCostScalar", 1.0f);
            ParryabilityMemory = Config.Bind($"Balance", "ParryabilityMemory", 6);

            DifferentiateElectricity = Config.Bind($"Balance", "DifferentiateElectricity", true);
            DifferentiateRailCannonFromBeams = Config.Bind($"Balance", "DifferentiateRailCannonFromBeams", true);

            DifferentiateCoinRicochets = Config.Bind($"Balance", "DifferentiateCoinRicochets", true);
            CoinRicochetDivisor = Config.Bind($"Balance", "CoinRicochetDivisor", 3);

            LogDebugInfo = Config.Bind("Diagnostics", "LogDebug", false);
             
            foreach (var enumVal in Enum.GetValues(typeof(EnemyType)))
            {
                double defaultFirstHitSkill = 0.75;
                double defaultMultiHitSkill = 0.5;

                double defaultStaminaCost = 0.4;
                double defaultStaminaRechargeRate = 0.175;
                double defaultMinParryCooldown = 0.1;

                Action fodderParryStats = () =>
                {
                    defaultFirstHitSkill = 0.4;
                    defaultMultiHitSkill = 0.25;
                    
                    defaultStaminaCost = 0.5;
                    defaultStaminaRechargeRate = 0.2;
                    defaultMinParryCooldown = 0.1;
                };

                Action miniBossParryStats = () =>
                {
                    defaultFirstHitSkill = 0.65;
                    defaultMultiHitSkill = 0.5;
                    
                    defaultStaminaCost = 0.425;
                    defaultStaminaRechargeRate = 0.25;
                    defaultMinParryCooldown = 0.1;
                };

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
                    case EnemyType.CancerousRodent:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.85;
                        defaultStaminaRechargeRate = 0.3f;
                        break;
                    case EnemyType.VeryCancerousRodent:
                        defaultFirstHitSkill = 1.01;
                        defaultMultiHitSkill = 0.5;
                        defaultStaminaRechargeRate = 0.2f;
                        break;
                    case EnemyType.BigJohnator:
                        miniBossParryStats();
                        break;
                    case EnemyType.Centaur:
                        miniBossParryStats();
                        break;
                    case EnemyType.Cerberus:
                        miniBossParryStats();
                        break;
                    case EnemyType.Deathcatcher:
                        break;
                    case EnemyType.Drone:
                        fodderParryStats();
                        break;
                    case EnemyType.Ferryman:
                        miniBossParryStats();
                        break;
                    case EnemyType.Filth:
                        fodderParryStats();
                        break;
                    case EnemyType.FleshPanopticon:
                        miniBossParryStats();
                        break;
                    case EnemyType.FleshPrison:
                        miniBossParryStats();
                        break;
                    case EnemyType.Gabriel:
                        fodderParryStats();
                        break;
                    case EnemyType.Geryon:
                        break;
                    case EnemyType.Gutterman:
                        miniBossParryStats();
                        break;
                    case EnemyType.Guttertank:
                        miniBossParryStats();
                        break;
                    case EnemyType.HideousMass:
                        miniBossParryStats();
                        break;
                    case EnemyType.Idol:
                        break;
                    case EnemyType.Leviathan:
                        miniBossParryStats();
                        break;
                    case EnemyType.MaliciousFace:
                        miniBossParryStats();
                        break;
                    case EnemyType.Mandalore:
                        break;
                    case EnemyType.Mannequin:
                        fodderParryStats();
                        break;
                    case EnemyType.Mindflayer:
                        miniBossParryStats();
                        break;
                    case EnemyType.Minos:
                        fodderParryStats();
                        break;
                    case EnemyType.Minotaur:
                        miniBossParryStats();
                        break;
                    case EnemyType.MirrorReaper:
                        miniBossParryStats();
                        break;
                    case EnemyType.Power:
                        miniBossParryStats();
                        break;
                    case EnemyType.Providence:
                        fodderParryStats();
                        break;
                    case EnemyType.Puppet:
                        fodderParryStats();
                        break;
                    case EnemyType.Schism:
                        fodderParryStats();
                        break;
                    case EnemyType.Sisyphus:
                        miniBossParryStats();
                        break;
                    case EnemyType.Soldier:
                        fodderParryStats();
                        break;
                    case EnemyType.Stalker:
                        fodderParryStats();
                        break;
                    case EnemyType.Stray:
                        fodderParryStats();
                        break;
                    case EnemyType.Streetcleaner:
                        fodderParryStats();
                        break;
                    case EnemyType.Swordsmachine:
                        miniBossParryStats();
                        break;
                    case EnemyType.Turret:
                        miniBossParryStats();
                        break;
                    case EnemyType.Virtue:
                        fodderParryStats();
                        break;
                    case EnemyType.Wicked:
                        defaultFirstHitSkill = 0.75;
                        defaultMultiHitSkill = 0.5;
                        
                        defaultStaminaCost = 0.4;
                        defaultStaminaRechargeRate = 0.175;
                        defaultMinParryCooldown = 0.1;
                        break;
                    default:
                        defaultStaminaCost = 0.5;
                        defaultStaminaRechargeRate = 0.125;
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

        internal static ProjectileTypeOptions GetOptionsForType(ProjectileBoostTracker.ProjectileCategory projectileType)
        {
            switch (projectileType)
            {
                case ProjectileBoostTracker.ProjectileCategory.Null:
                    return null;
                case ProjectileBoostTracker.ProjectileCategory.RevolverBeam:
                    return BeamsOptions;
                case ProjectileBoostTracker.ProjectileCategory.EnemyRevolverBeam:
                    return EnemyProjectilesOptions;
                case ProjectileBoostTracker.ProjectileCategory.PlayerProjectile:
                    return PlayerProjectilesOptions;
                case ProjectileBoostTracker.ProjectileCategory.Projectile:
                    return EnemyProjectilesOptions;
                case ProjectileBoostTracker.ProjectileCategory.HomingProjectile:
                    return EnemyProjectilesOptions;
                case ProjectileBoostTracker.ProjectileCategory.Rocket:
                    return GrenadesOptions;
                case ProjectileBoostTracker.ProjectileCategory.Grenade:
                    return GrenadesOptions;
                case ProjectileBoostTracker.ProjectileCategory.EnemyRocket:
                    return GrenadesOptions;
                case ProjectileBoostTracker.ProjectileCategory.EnemyGrenade:
                    return GrenadesOptions;
                case ProjectileBoostTracker.ProjectileCategory.Coin:
                    return ShotCoinsOptions;
                case ProjectileBoostTracker.ProjectileCategory.Nail:
                    return SawsOptions;
                case ProjectileBoostTracker.ProjectileCategory.Saw:
                    return SawsOptions;
                case ProjectileBoostTracker.ProjectileCategory.RailCannon:
                    return RailCannonOptions;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static ConfigFile Config = null;

        public class ProjectileTypeOptions
        {
            public ProjectileTypeOptions(string name, bool parryable, float minimumParryWindow)
            {
                CanBeParried = Config.Bind($"Balance.{name}", "Parryable", parryable);
                MinimumParryWindow = Config.Bind($"Balance.{name}", "MinimumParryabilityWindow", minimumParryWindow, "projectiles become more parryable the longer they exist via an increasing parryability window, this sets the minimum, especially relevant for hitscan attacks. larger numbers means more frequent parries.");
            }

            public ConfigEntry<bool> CanBeParried = null;
            public ConfigEntry<float> MinimumParryWindow = null;
        }
    }
}
