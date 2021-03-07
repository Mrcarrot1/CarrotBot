using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Discord;
using Discord.WebSocket;
using System.Threading;
using ZipFile = System.IO.Compression.ZipFile;

namespace CarrotBot
{
    class Logger
    {
        public static bool firstRun = true;
        public static string logPath = "Placeholder path";
        public static void Setup()
        {
            firstRun = false;
            logPath = $"{Environment.CurrentDirectory}\\Logs\\{DateTime.Today.Day + "_" + DateTime.Today.Month + "_" + DateTime.Today.Year + "_" + DateTime.Now.ToString("HH:mm:ss").Replace(":", "_")}.log";
            File.WriteAllText(logPath, "-----Log initiated for CarrotBot.-----\nRun by " + Environment.UserName + ". Local time: " + DateTime.Now + "; UTC: " + DateTime.UtcNow + ".\n---------------------------------------------");

        }
        public static void Lawg(string Message)
        {
            if (firstRun)
                Setup();
            ISocketMessageChannel channel = Program.client.GetChannel(490551836323872779) as ISocketMessageChannel;
            File.AppendAllText(logPath, $"\n{Message}");
            Console.WriteLine(Message);
            Thread.Sleep(75);
            //if (channel != null)
              //  channel.SendMessageAsync($"{DateTime.Now}: {Message.Replace(DateTime.Now.ToString("HH:mm:ss"), "")}");
        }
        public static string GetAllLogsZipped()
        {
            if (File.Exists($@"{Environment.CurrentDirectory}/Logs.zip"))
                File.Delete($@"{Environment.CurrentDirectory}/Logs.zip");
            ZipFile.CreateFromDirectory($@"{Environment.CurrentDirectory}/Logs", $@"{Environment.CurrentDirectory}/Logs.zip");
            return $@"{Environment.CurrentDirectory}/Logs.zip";
        }
        public static string GetLatestLogZipped()
        {
            if (!Directory.Exists($@"{Environment.CurrentDirectory}/ZipWorkspace"))
                Directory.CreateDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace");
            else
            {
                Directory.Delete($@"{Environment.CurrentDirectory}/ZipWorkspace", true);
                Directory.CreateDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace");
            }
            if (firstRun)
                Setup();
            File.Copy(logPath, $@"{Environment.CurrentDirectory}/ZipWorkspace/{Path.GetFileName(logPath)}");
            ZipFile.CreateFromDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace", $@"{Environment.CurrentDirectory}/{Path.GetFileNameWithoutExtension(logPath)}.zip");
            return $@"{Environment.CurrentDirectory}/{Path.GetFileNameWithoutExtension(logPath)}.zip";
        }
        public static string GetGuildLogZipped(ulong guildId)
        {
            if (!Directory.Exists($@"{Environment.CurrentDirectory}/ZipWorkspace"))
                Directory.CreateDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace");
            else
            {
                Directory.Delete($@"{Environment.CurrentDirectory}/ZipWorkspace", true);
                Directory.CreateDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace");
            }

            try
            {
                if (File.Exists($@"{Environment.CurrentDirectory}/guild_{guildId}_log.zip"))
                    File.Delete($@"{Environment.CurrentDirectory}/guild_{guildId}_log.zip");

                File.Copy($@"{Environment.CurrentDirectory}/Logs/guild_{guildId}.log", $@"{ Environment.CurrentDirectory}/ZipWorkspace/guild_{guildId}.log");
                ZipFile.CreateFromDirectory($@"{Environment.CurrentDirectory}/ZipWorkspace", $@"{Environment.CurrentDirectory}/guild_{guildId}_log.zip");
                return $@"{Environment.CurrentDirectory}/guild_{guildId}_log.zip";
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }
    }
}
