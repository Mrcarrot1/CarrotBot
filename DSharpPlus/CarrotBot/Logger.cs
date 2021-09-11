using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
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
            Console.WriteLine(Message);
            Thread.Sleep(75);
            //if (channel != null)
            //  channel.SendMessageAsync($"{DateTime.Now}: {Message.Replace(DateTime.Now.ToString("HH:mm:ss"), "")}");
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(firstRun)
                Setup();
            File.AppendAllText(logPath, $"\n[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now.ToString("HH:mm:ss")}] {formatter(state, exception)}");
            Console.WriteLine($"[{GetShortLogLevel(logLevel)} {eventId} {DateTime.Now.ToString("HH:mm:ss")}] {formatter(state, exception)}");
        }
        public IDisposable BeginScope<TState>(TState state)
        {
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
