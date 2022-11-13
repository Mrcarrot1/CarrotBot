using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using ZipFile = System.IO.Compression.ZipFile;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace CarrotBot
{
    class Logger : ILogger
    {
        public static bool firstRun = true;
        public static string logPath = "";

        private static int exceptionCount = 0;
        public static void Setup()
        {
            firstRun = false;
            logPath = $@"{Utils.logsPath}/Log_{DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss")}.txt";
            File.WriteAllText(logPath, "-----Log initiated for CarrotBot.-----\nRun by " + Environment.UserName + ". Local time: " + DateTime.Now + "; UTC: " + DateTime.UtcNow + ".\n---------------------------------------------");

        }
        public static void Log(string Message, CBLogLevel level = CBLogLevel.LOG)
        {
            if (firstRun)
                Setup();
            //ISocketMessageChannel channel = Program.client.GetChannel(490551836323872779) as ISocketMessageChannel;
            File.AppendAllText(logPath, $"\n[{level} {DateTime.Now.ToString("HH:mm:ss")}] {Message}");
            if (level == CBLogLevel.ERR) Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (level == CBLogLevel.WRN) Console.ForegroundColor = ConsoleColor.Yellow;
            if (level == CBLogLevel.EXC) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[{level} {DateTime.Now.ToString("HH:mm:ss")}] {Message}");
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
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (firstRun)
                Setup();
            File.AppendAllText(logPath, $"\n[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now.ToString("HH:mm:ss")}] {formatter(state, exception)}");
            Console.WriteLine($"[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now.ToString("HH:mm:ss")}] {formatter(state, exception)}");
            if (logLevel == LogLevel.Error) Console.WriteLine(exception.ToString());
            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }
            if (logLevel == LogLevel.Critical && eventId.Name == "ConnectionClose")
            {
                Process.Start($@"{Environment.CurrentDirectory}/CarrotBot");
                Environment.Exit(0);
            }
        }
        public IDisposable BeginScope<TState>(TState state)
        {
#nullable disable
            return null;
        }
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
