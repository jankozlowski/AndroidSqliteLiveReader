using System.Diagnostics;

namespace AndroidSqliteLiveReader.Helpers
{
    public static class AdbHelper
    {
        public static void AdbCommand(string command)
        {
            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;

                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;

                cmd.Start();
                string stdout = cmd.StandardOutput.ReadToEnd();
                string stderr = cmd.StandardError.ReadToEnd();
                cmd.WaitForExit();
            }
        }

        public static string AdbCommandWithResult(string command)
        {
            string output = string.Empty;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
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
    }
}
