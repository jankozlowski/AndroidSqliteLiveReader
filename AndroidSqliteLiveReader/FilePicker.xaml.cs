using AndroidSqliteLiveReader.Services;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AndroidSqliteLiveReader
{
    public partial class FilePicker : Window
    {
        public string PathResult { get; private set; }
        private string DeviceId { get; set; }

        private readonly Adb Adb;

        public FilePicker(string deviceId)
        {
            InitializeComponent();
            Adb = new Adb();
            DeviceId = deviceId;
            Initialize();
        }

        private async Task Initialize()
        {
            TreeViewItem rootNode = new TreeViewItem
            {
                Header = "/",
                Tag = "/"
            };
            await LoadNodesAsync(rootNode);

            rootNode.IsExpanded = true;
            tree.Items.Add(rootNode);
        }

        private async Task LoadNodesAsync(TreeViewItem currentNode)
        {
            loading.Visibility = Visibility.Visible;
            string directorys = Adb.AdbCommandWithResult($"-s {DeviceId} shell ls {currentNode.Tag} -F -A");
            currentNode.Items.Clear();
            await Task.Run(() =>
            {
                List<SingleNode> singleNodes = directorys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(l => new SingleNode() { Name = l, Path = l }).ToList();
                List<SingleNode> folderNodes = singleNodes.Where(n => n.Name.EndsWith("/")).ToList();
                List<SingleNode> symbolicNodes = singleNodes.Where(n => n.Name.EndsWith("@")).ToList();

                CheckSymbolicLinks(symbolicNodes);

                List<SingleNode> onlyFolders = folderNodes.Concat(symbolicNodes.Where(s => !s.IsFile)).OrderBy(s => s.Name).ToList();
                List<SingleNode> onlyFiles = singleNodes.Except(onlyFolders).OrderBy(s => s.Name).ToList();
                onlyFiles.ForEach(l => { l.IsFile = true; FormatFileNode(l); });
                singleNodes = onlyFolders.Concat(onlyFiles).ToList();


                foreach (SingleNode node in singleNodes)
                {
                    this.Dispatcher.Invoke(() =>
                    {

                        AddNewNode(currentNode, node);
                    });
                }
            });


            loading.Visibility = Visibility.Hidden;
        }

        private void CheckSymbolicLinks(List<SingleNode> symbolicNodes)
        {
            foreach (SingleNode symbolicLink in symbolicNodes)
            {
                symbolicLink.Name = symbolicLink.Name.Replace("@", "");
                symbolicLink.Path = Adb.AdbCommandWithResult($"-s {DeviceId} shell readlink -f -n {symbolicLink.Name}");
                if (bool.TryParse(Adb.AdbCommandWithResult($"-s {DeviceId} shell test -d {symbolicLink.Path} && echo 'true'"), out bool result))
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
            if (!node.IsFile)
            {
                newNode.Expanded += CurrentNodeExpanded;
                newNode.Items.Add(LoadingItem());
            }
        }

        private async void CurrentNodeExpanded(object sender, RoutedEventArgs e)
        {
            await LoadNodesAsync((TreeViewItem)sender);
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
            if ((TreeViewItem)tree.SelectedItem == null)
            {
                PathResult = "/";
            }
            else
            {
                PathResult = ((TreeViewItem)tree.SelectedItem).Tag.ToString();
            }
            DialogResult = true;
            Close();
        }

        private StackPanel TreeViewItemLayout(SingleNode node)
        {
            StackPanel stkPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            SolidColorBrush systemColor = (SolidColorBrush)FindResource(VsBrushes.WindowTextKey);
            bool darkTheme = systemColor.Color.ToString().Equals("#FFFAFAFA");

            Image img = new Image();
            if (node.IsFile)
            {
                if (!darkTheme)
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/fileblack.png"));
                }
                else
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/filewhite.png"));
                }
            }
            else
            {
                if (!darkTheme)
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folderblack.png"));
                }
                else
                {
                    img.Source = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/folderwhite.png"));
                }
            }

            img.Width = 16;
            img.Height = 16;

            TextBlock lbl = new TextBlock();
            lbl.Text = node.Name;
            lbl.Foreground = (SolidColorBrush)FindResource(VsBrushes.WindowTextKey);
            lbl.Background = (SolidColorBrush)FindResource(VsBrushes.WindowKey);

            stkPanel.Children.Add(img);
            stkPanel.Children.Add(lbl);

            return stkPanel;
        }

        private StackPanel LoadingItem()
        {
            StackPanel stkPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBlock lbl = new TextBlock
            {
                Text = "Loading..."
            };

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