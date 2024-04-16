using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AndroidSqliteLiveReader
{
    public partial class FilePicker : Window
    {
        public FilePicker()
        {
            InitializeComponent();

            string directorys = AdbCommandWithResult("-s emulator-5554 shell ls -F -p");

            string[] lines = directorys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            TreeViewItem rootNode = new TreeViewItem { Header = "/" };
            rootNode.IsExpanded = true;
            TreeViewItem currentNode = rootNode;


            foreach (string line in lines)
            {
                TreeViewItem newNode = new TreeViewItem { Header = CustomizeTreeViewItem(true, line), };
                currentNode.Items.Add(newNode);
                newNode.Items.Add(InvisibleItem());
            }

            tree.Items.Add(rootNode);
        }

        public string AdbCommandWithResult(string command)
        {
            string output = string.Empty;

            using (System.Diagnostics.Process cmd = new System.Diagnostics.Process())
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Button was clicked!");
        }

        private StackPanel CustomizeTreeViewItem(bool folder, string name)
        {
            // Add Icon
            // Create Stack Panel
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;

            // Create Image
            Image img = new Image();
            img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folder.png"));
            img.Width = 16;
            img.Height = 16;

            // Create TextBlock
            TextBlock lbl = new TextBlock();
            lbl.Text = name;

            // Add to stack
            stkPanel.Children.Add(img);
            stkPanel.Children.Add(lbl);

            return stkPanel;
        }

        private StackPanel InvisibleItem()
        {
            // Add Icon
            // Create Stack Panel
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;


            // Create TextBlock
            TextBlock lbl = new TextBlock();
            lbl.Text = "";

            // Add to stack
            stkPanel.Children.Add(lbl);

            return stkPanel;
        }
    }
}