using Database;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace BanCa
{
    public class PlatformLogger : ILogger
    {
        private LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch();
        private LogLevel level = LogLevel.INFO;

        public static PlatformLogger CreateFullLogger()
        {
            return new PlatformLogger(true);
        }

        public PlatformLogger()
        {
            ConfigJson.OnConfigChange += () =>
            {
                ((ILogger)this).Dispose();
                newLogger();
            };
            newLogger();
        }

        private PlatformLogger(bool notUse)
        {
            var logLv = LogLevel.DEBUG;
            ((ILogger)this).SetLogLevel(logLv);
            var builder = new LoggerConfiguration();
            builder.MinimumLevel.ControlledBy(levelSwitch);
            builder.WriteTo.Console();
            builder.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            Log.Logger = builder.CreateLogger();
        }

        private void newLogger()
        {
            var logLv = (LogLevel)Enum.Parse(typeof(LogLevel), ConfigJson.Config["log-level"].Value, true);
            ((ILogger)this).SetLogLevel(logLv);
            var builder = new LoggerConfiguration();
            builder.MinimumLevel.ControlledBy(levelSwitch);
            if (ConfigJson.Config["log-to-console"].AsBool)
                builder.WriteTo.Console();
            if (ConfigJson.Config["log-to-file"].AsBool)
                builder.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            Log.Logger = builder.CreateLogger();
        }

        void ILogger.Dispose()
        {
            Log.CloseAndFlush();
        }

        LogLevel ILogger.GetLogLevel()
        {
            return level;
        }

        void ILogger.Error(object msg)
        {
            if (level != LogLevel.NONE)
                Log.Error("{$msg}", msg);
        }

        void ILogger.Info(object msg)
        {
            if (level != LogLevel.NONE)
                Log.Information("{$msg}", msg);
        }

        void ILogger.Log(object msg)
        {
            if (level != LogLevel.NONE)
                Log.Debug("{$msg}", msg);
        }

        void ILogger.SetLogLevel(LogLevel level)
        {
            this.level = level;
            switch (level)
            {
                case LogLevel.NONE:
                    break;
                case LogLevel.DEBUG:
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case LogLevel.INFO:
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case LogLevel.WARNING:
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case LogLevel.ERROR:
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
                case LogLevel.FATAL:
                    levelSwitch.MinimumLevel = LogEventLevel.Fatal;
                    break;
                default:
                    break;
            }
        }
    }
}
