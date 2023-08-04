using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CarrotBot.Conversation;
using CarrotBot.Data;
using CarrotBot.Leveling;
using Microsoft.Extensions.Logging;

namespace CarrotBot
{
    class Logger : ILogger
    {
        private static bool firstRun = true;
        private static string logPath = "";

        private static int exceptionCount;

        private static void Setup()
        {
            firstRun = false;
            logPath = $@"{Utils.logsPath}/Log_{DateTime.Now:yyyy-MM-dd_HH:mm:ss}.txt";
            File.WriteAllText(logPath, "-----Log initiated for CarrotBot.-----\nRun by " + Environment.UserName + ". Local time: " + DateTime.Now + "; UTC: " + DateTime.UtcNow + ".\n---------------------------------------------");

        }
        public static void Log(string Message, CBLogLevel level = CBLogLevel.LOG)
        {
            if (firstRun)
                Setup();
            //ISocketMessageChannel channel = Program.client.GetChannel(490551836323872779) as ISocketMessageChannel;
            File.AppendAllText(logPath, $"\n[{level} {DateTime.Now:HH:mm:ss}] {Message}");
            Console.ForegroundColor = level switch
            {
                CBLogLevel.ERR => ConsoleColor.DarkYellow,
                CBLogLevel.WRN => ConsoleColor.Yellow,
                CBLogLevel.EXC => ConsoleColor.Red,
                _ => Console.ForegroundColor
            };
            Console.WriteLine($"\n[{level} {DateTime.Now:HH:mm:ss}] {Message}");
            Console.ForegroundColor = ConsoleColor.White;
            if (level == CBLogLevel.EXC)
            {
                exceptionCount++;
                if (exceptionCount > 25 && Program.Mrcarrot != null)
                {
                    Program.Mrcarrot.SendMessageAsync("WARNING: EXPERIENCED MORE THAN 25 EXCEPTIONS");
                    exceptionCount = 0;
                }
            }
            //Thread.Sleep(75);
            //if (channel != null)
            //  channel.SendMessageAsync($"{DateTime.Now}: {Message.Replace(DateTime.Now.ToString("HH:mm:ss"), "")}");
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (firstRun)
                Setup();
            File.AppendAllText(logPath, $"\n[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now:HH:mm:ss}] {formatter(state, exception)}");
            Console.WriteLine($"[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now:HH:mm:ss}] {formatter(state, exception)}");
            if (exception is not null) Console.WriteLine(exception.ToString());
            if (eventId.ToString() == "ConnectionClose" || eventId.ToString() == "HeartbeatFailure")
            {
                //Task.Run(async () => await Program.discord!.StopAsync()).GetAwaiter().GetResult();
                Task.Run(async () => await Program.discord!.StartAsync());
                /*Database.FlushDatabase(true);
                ConversationData.WriteDatabase();
                LevelingData.FlushAllData();
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
                Environment.Exit(0);*/
            }
        }
#nullable disable
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
#nullable enable
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }
        string GetShortLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "TRCE";
                case LogLevel.Debug:
                    return "DBUG";
                case LogLevel.Information:
                    return "INFO";
                case LogLevel.Warning:
                    return "WARN";
                case LogLevel.Error:
                    return "ERR";
                case LogLevel.Critical:
                    return "CRIT";
            }
            return logLevel.ToString().ToUpper();
        }
        public enum CBLogLevel
        {
            LOG,
            WRN,
            ERR,
            EXC
        }
    }
    class CBLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new Logger();
        }
        public void Dispose()
        {

        }
    }
}
