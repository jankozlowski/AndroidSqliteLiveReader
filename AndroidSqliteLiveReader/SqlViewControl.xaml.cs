using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace AndroidSqliteLiveReader
{
    /// <summary>
    /// Interaction logic for SqlViewControl.
    /// </summary>
    public partial class SqlViewControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlViewControl"/> class.
        /// </summary>
        public SqlViewControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void SyncDataClick(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(
            //    string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
            //    "Sql");

            using (Process cmd = new Process())
            {
                //adb devices
                //C:\Program Files (x86)\Android\android-sdk\platform-tools
                // The full executable path is required if its directory is not in %PATH%
                cmd.StartInfo.FileName = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
                cmd.StartInfo.Arguments = @"devices";
                //     cmd.StartInfo.WorkingDirectory = @"C:\Users\NewSystem\source\repos\";
                cmd.StartInfo.CreateNoWindow = false;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;

                cmd.Start();
                string output = cmd.StandardOutput.ReadToEnd();
                cmd.WaitForExit();
            }
        }

        private void SyncEmulatorClick(object sender, RoutedEventArgs e)
        {

        }
    }
}