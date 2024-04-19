using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AndroidSqliteLiveReader.Helpers
{
    public static class AdbHelper
    {
        public static string AdbPath = string.Empty;

        public static void AdbCommand(string command)
        {
            if (!CheckAdbPath())
                return;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AdbExePath();
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;

                cmd.Start();
                cmd.WaitForExit();
            }
        }

        public static string AdbCommandWithResult(string command)
        {
            if (!CheckAdbPath())
                return string.Empty;

            string output = string.Empty;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AdbExePath();
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;

                cmd.Start();
                output = cmd.StandardOutput.ReadToEnd();
                cmd.WaitForExit();
            }

            return output;
        }

        private static string AdbExePath()
        {
            return Path.Combine(AdbPath, "adb.exe");
        }

        private static bool CheckAdbPath()
        {
            bool adbExists = File.Exists(AdbExePath());
            if (adbExists)
            {
                return true;
            }

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb not found at given path. Check if path is correct and contains adb.exe"), "Adb not found");
            return false;
        }
    }
}
