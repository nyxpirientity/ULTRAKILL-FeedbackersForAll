using System.Collections.Generic;
using BepInEx.Configuration;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class Options
    {
        public static ConfigEntry<bool> HitstopOnEnemyParry = null;

        public static void Initialize()
        {
            HitstopOnEnemyParry = Config.Bind($"Preferences", "HitstopOnEnemyParry", false);
        }
        
        internal static ConfigFile Config = null;
    }
}
