using BepInEx.Logging;
using Nyxpiri.ULTRAKILL.NyxLib;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    internal static class Log
    {
        public static void Initialize(ManualLogSource logger)
        {
            Assert.IsNull(_logger, $"Log.Initialize called when _logger wasn't null?");
            _logger = logger;
        }

        public static void Fatal(object data)
        {
            _logger.LogFatal(data);
        }
        
        public static void Error(object data)
        {
            _logger.LogError(data);
        }
        
        public static void Warning(object data)
        {
            _logger.LogWarning(data);
        }
        
        public static void Message(object data)
        {
            _logger.LogMessage(data);
        }
        
        public static void Info(object data)
        {
            _logger.LogInfo(data);
        }

        public static void Debug(object data)
        {
            if (Options.LogDebugInfo.Value)
            {
                _logger.LogDebug(data);
            }
        }

        private static ManualLogSource _logger = null;
    }
}
