using System.IO;
using System;
using System.Diagnostics;

namespace CarrotBot
{
    public class Updater
    {
        public static void UpdateBot(string newExeUrl)
        {
            if (!newExeUrl.EndsWith(".exe")) return;

            File.WriteAllText($@"{Environment.CurrentDirectory}/UpdatePath.cb", newExeUrl);
            System.Threading.Thread.Sleep(1000);
            Process.Start($@"{Environment.CurrentDirectory}/updater.exe");
            Environment.Exit(0);
        }
    }
}