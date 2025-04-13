using NLog;

namespace NzbDrone.Common.Instrumentation.Extensions
{
    public static class LoggerExtensions
    {
        [MessageTemplateFormatMethod("message")]
        public static void ProgressInfo(this Logger logger, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            LogProgressMessage(logger, LogLevel.Info, formattedMessage);
        }

        [MessageTemplateFormatMethod("message")]
        public static void ProgressDebug(this Logger logger, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            LogProgressMessage(logger, LogLevel.Debug, formattedMessage);
        }

        [MessageTemplateFormatMethod("message")]
        public static void ProgressTrace(this Logger logger, string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            LogProgressMessage(logger, LogLevel.Trace, formattedMessage);
        }

        private static void LogProgressMessage(Logger logger, LogLevel level, string message)
        {
            var logEvent = new LogEventInfo(level, logger.Name, message);
            logEvent.Properties.Add("Status", "");

            logger.Log(logEvent);
        }
    }
}
