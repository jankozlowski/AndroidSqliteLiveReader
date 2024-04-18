using AndroidSqliteLiveReader.Helpers;
using Microsoft.VisualStudio.Package;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AndroidSqliteLiveReader
{
    public partial class FilePicker : Window
    {
        public string PathResult { get; private set; }

        public FilePicker()
        {
            InitializeComponent();
            AdbHelper.AdbCommand("root -s emulator-5554");

            TreeViewItem rootNode = new TreeViewItem { Header = "/" };
            rootNode.Items.Add(LoadingItem());
            rootNode.Tag = "/";
            LoadNodes(rootNode);

            rootNode.IsExpanded = true;

            tree.Items.Add(rootNode);
        }

        private void CurrentNode_Expanded(object sender, RoutedEventArgs e)
        {
            LoadNodes((TreeViewItem)sender);
            ((TreeViewItem)sender).Expanded -= CurrentNode_Expanded;
        }

        private void LoadNodes(TreeViewItem currentNode)
        {
            string directorys = AdbHelper.AdbCommandWithResult($"-s emulator-5554 shell ls {currentNode.Tag} -F -A");
            string[] lines = directorys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            string[] folderLines = lines.Where(l => l.EndsWith("/") || l.EndsWith("@")).OrderBy(l => l).ToArray();
            string[] fileLines = lines.Except(folderLines).OrderBy(l => l).ToArray();
            lines = folderLines.Concat(fileLines).ToArray();

            currentNode.Items.Clear();

            foreach (string line in lines)
            {
                string path = line;
                string tag = currentNode.Tag + path;

                if (line.EndsWith("@"))
                {//>adb.exe shell test -d /system/binio && echo "Directory"
                    tag = AdbHelper.AdbCommandWithResult($"-s emulator-5554 shell readlink -f -n {tag.Substring(0, tag.Length - 1)}");
                    try
                    {
                        if (bool.Parse(AdbHelper.AdbCommandWithResult($"-s emulator-5554 shell test -d {tag} && echo 'true'")))
                        {
                            tag += "/";
                        }
                    }
                    catch { }
                }

                TreeViewItem newNode = new TreeViewItem { Header = CustomizeTreeViewItem(path, tag), Tag = tag };
                currentNode.Items.Add(newNode);
                newNode.Expanded += CurrentNode_Expanded;
                if (line.EndsWith("/") || (line.EndsWith("@") && tag.EndsWith("/")))
                    newNode.Items.Add(LoadingItem());
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectClicked(object sender, RoutedEventArgs e)
        {
            PathResult = ((TreeViewItem)tree.SelectedItem).Tag.ToString();
            DialogResult = true;
            Close();
        }


        private StackPanel CustomizeTreeViewItem(string name, string tag)
        {
            // Add Icon
            // Create Stack Panel
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;

            // Create Image
            Image img = new Image();
            if (name.EndsWith("/"))
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folder.png"));
            }
            else if (name.EndsWith("@"))
            {
                if (tag.EndsWith("/"))
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folder.png"));
                    name = name.Substring(0, name.Length - 1) + "/";
                }
                else
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/file.png"));
                    name = name.Substring(0, name.Length - 1);
                }
            }
            else
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/file.png"));

            }

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

        private StackPanel LoadingItem()
        {
            // Add Icon
            // Create Stack Panel
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;


            // Create TextBlock
            TextBlock lbl = new TextBlock();
            lbl.Text = "Loading...";

            // Add to stack
            stkPanel.Children.Add(lbl);

            return stkPanel;
        }


    }
}