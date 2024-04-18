using AndroidSqliteLiveReader.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace AndroidSqliteLiveReader
{
    public partial class FilePicker : Window
    {
        public string PathResult { get; private set; }
        private string DeviceId { get; set; }

        public FilePicker(string deviceId)
        {
            InitializeComponent();
            DeviceId = deviceId;
            Initialize();
        }

        private void Initialize()
        {
            AdbHelper.AdbCommand($"root -s {DeviceId}");

            TreeViewItem rootNode = new TreeViewItem
            {
                Header = "/",
                Tag = "/"
            };
            LoadNodes(rootNode);

            rootNode.IsExpanded = true;
            tree.Items.Add(rootNode);
        }

        private void LoadNodes(TreeViewItem currentNode)
        {
            string directorys = AdbHelper.AdbCommandWithResult($"-s {DeviceId} shell ls {currentNode.Tag} -F -A");

            List<SingleNode> singleNodes = directorys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(l => new SingleNode() { Name = l, Path = l }).ToList();
            List<SingleNode> folderNodes = singleNodes.Where(n => n.Name.EndsWith("/")).ToList();
            List<SingleNode> symbolicNodes = singleNodes.Where(n => n.Name.EndsWith("@")).ToList();

            CheckSymbolicLinks(symbolicNodes);

            List<SingleNode> onlyFolders = folderNodes.Concat(symbolicNodes.Where(s => !s.IsFile)).OrderBy(s => s.Name).ToList();
            List<SingleNode> onlyFiles = singleNodes.Except(onlyFolders).OrderBy(s => s.Name).ToList();
            onlyFiles.ForEach(l => { l.IsFile = true; FormatFileNode(l); });
            singleNodes = onlyFolders.Concat(onlyFiles).ToList();

            currentNode.Items.Clear();

            foreach (SingleNode node in singleNodes)
            {
                AddNewNode(currentNode, node);
            }
        }

        private void CheckSymbolicLinks(List<SingleNode> symbolicNodes)
        {
            foreach (SingleNode symbolicLink in symbolicNodes)
            {
                symbolicLink.Name = symbolicLink.Name.Replace("@", "");
                symbolicLink.Path = AdbHelper.AdbCommandWithResult($"-s {DeviceId} shell readlink -f -n {symbolicLink.Name}");
                if (bool.TryParse(AdbHelper.AdbCommandWithResult($"-s {DeviceId} shell test -d {symbolicLink.Path} && echo 'true'"), out bool result))
                {
                    symbolicLink.Path += "/";
                    symbolicLink.Name += "/";
                    continue;
                }
                symbolicLink.IsFile = true;
                FormatFileNode(symbolicLink);
            }
        }

        private void AddNewNode(TreeViewItem currentNode, SingleNode node)
        {
            string name = node.Name;
            node.Path = currentNode.Tag + name;

            TreeViewItem newNode = new TreeViewItem { Header = TreeViewItemLayout(node), Tag = node.Path };
            currentNode.Items.Add(newNode);
            newNode.Expanded += CurrentNodeExpanded;
            if (!node.IsFile)
                newNode.Items.Add(LoadingItem());
        }

        private void CurrentNodeExpanded(object sender, RoutedEventArgs e)
        {
            LoadNodes((TreeViewItem)sender);
            ((TreeViewItem)sender).Expanded -= CurrentNodeExpanded;
        }

        private void FormatFileNode(SingleNode node)
        {
            if (node.Name.EndsWith("*"))
            {
                node.Name = node.Name.Substring(0, node.Name.Length - 1);
                node.Path = node.Path.Substring(0, node.Path.Length - 1);
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

        private StackPanel TreeViewItemLayout(SingleNode node)
        {
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;

            Image img = new Image();
            if (node.IsFile)
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/file.png"));
            }
            else
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folder.png"));
            }

            img.Width = 16;
            img.Height = 16;

            TextBlock lbl = new TextBlock();
            lbl.Text = node.Name;

            stkPanel.Children.Add(img);
            stkPanel.Children.Add(lbl);

            return stkPanel;
        }

        private StackPanel LoadingItem()
        {
            StackPanel stkPanel = new StackPanel();
            stkPanel.Orientation = Orientation.Horizontal;

            TextBlock lbl = new TextBlock();
            lbl.Text = "Loading...";

            stkPanel.Children.Add(lbl);

            return stkPanel;
        }

        private class SingleNode
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public bool IsFile { get; set; }
        }
    }
}