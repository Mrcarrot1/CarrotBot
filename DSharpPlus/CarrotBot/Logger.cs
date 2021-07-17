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
    class Logger
    {
        public static bool firstRun = true;
        public static string logPath = "";
        public static void Setup()
        {
            firstRun = false;
            logPath = $@"{Utils.logsPath}/Log_{DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss")}.txt";
            File.WriteAllText(logPath, "-----Log initiated for CarrotBot.-----\nRun by " + Environment.UserName + ". Local time: " + DateTime.Now + "; UTC: " + DateTime.UtcNow + ".\n---------------------------------------------");

        }
        public static void Log(string Message, LogLevel level = LogLevel.LOG)
        {
            if (firstRun)
                Setup();
            //ISocketMessageChannel channel = Program.client.GetChannel(490551836323872779) as ISocketMessageChannel;
            File.AppendAllText(logPath, $"\n[{level} {DateTime.Now.ToString("HH:mm:ss")}]{Message}");
            Console.WriteLine(Message);
            Thread.Sleep(75);
            //if (channel != null)
            //  channel.SendMessageAsync($"{DateTime.Now}: {Message.Replace(DateTime.Now.ToString("HH:mm:ss"), "")}");
        }
        public enum LogLevel
        {
            LOG,
            WRN,
            ERR,
            EXC
        }
    }
}
