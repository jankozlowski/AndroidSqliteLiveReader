using AndroidSqliteLiveReader.Helpers;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = _devices.Where(d => d.Name.Equals(DevicesComboBox.Text)).FirstOrDefault();
            if (selectedDevice == null)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "No device have been selected, select device from device ComboBox. If ComboBox is empty run emulator or connect real device."), "No device selected");
                return;
            }

            FilePicker filePicker = new FilePicker(selectedDevice.Id);

            bool? result = filePicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                dbPathBox.Text = filePicker.PathResult;
            }

        }

        private List<Device> GetConnectedDevices()
        {
            List<Device> devices = new List<Device>();

            string devicesAdbResult = AdbHelper.AdbCommandWithResult("devices -l");

            string[] lineResult = SplitByLine(devicesAdbResult);

            List<string> deviceIds = new List<string>();

            foreach (string line in lineResult.Skip(1))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] parameters = line.Split();
                Device device = new Device();
                device.Id = parameters[0];
                devices.Add(device);
            }

            foreach (Device device in devices)
            {
                string properties = AdbHelper.AdbCommandWithResult($"-s {device.Id} shell getprop");
                string[] propertieslines = SplitByLine(properties);
                string avdNameLine = propertieslines.Where(l => l.Contains("ro.boot.qemu.avd_name")).FirstOrDefault();
                string avdName = avdNameLine.Split(':')[1];
                avdName = avdName.Trim().Replace("_", " ");
                avdName = avdName.Substring(1, avdName.Length - 2);
                avdName = avdName[0].ToString().ToUpper() + avdName.Substring(1);
                device.Name = avdName;
            }


            return devices;
        }

        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.None
            );
            return lines;
        }


        private void DevicesComboBox_DropDownOpened(object sender, EventArgs e)
        {
            _devices = GetConnectedDevices();
            DevicesComboBox.ItemsSource = _devices.Select(d => d.Name).ToList();
        }

        private void CheckAdbClick(object sender, RoutedEventArgs e)
        {
            string adbPath = Path.Combine(AdbPath.Text, "adb.exe");
            bool adbExists = File.Exists(adbPath);
            if (adbExists)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb found at given path"), "Adb found");
            }
            else
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb not found at given path. Check if path is correct and contains adb.exe"), "Adb not found");
            }
        }

        private void GetDataClick(object sender, RoutedEventArgs e)
        {
            string path = dbPathBox.Text;

            string fileName = path.Substring(path.LastIndexOf("/") + 1);

            AdbHelper.AdbCommand($"-s emulator-5554 shell \"su 0 mkdir /storage/emulated/0/DbCopyPathTemp\"");// make Directory
            AdbHelper.AdbCommand($"-s emulator-5554 shell \"su 0 cp -F {path} /storage/emulated/0/DbCopyPathTemp/\""); //copy Db to readable folder
            AdbHelper.AdbCommand($"-s emulator-5554 pull /storage/emulated/0/DbCopyPathTemp/HostileCity.db {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}"); // copy db to disc
            AdbHelper.AdbCommand($"-s emulator-5554 shell \"su 0 rm -r /storage/emulated/0/DbCopyPathTemp\"");// delete Directory

            string currentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName);
            _connection = new SqliteConnection($"data source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName)}");
            _connection.Open();

            SqliteCommand sql = new SqliteCommand("SELECT name FROM sqlite_master WHERE type = \"table\"", _connection);

            List<string> tables = new List<string>();

            SqliteDataReader query = sql.ExecuteReader();

            while (query.Read())
            {
                tables.Add(query.GetString(0));
            }

            TableComboBox.SelectedIndex = -1;
            TableComboBox.ItemsSource = tables;
            TableComboBox.SelectedIndex = _lastSelectedTableIndex == -1 ? 0 : _lastSelectedTableIndex;
        }

        private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (((ComboBox)e.Source).SelectedIndex == -1)
                return;

            SqliteCommand sql = new SqliteCommand($"PRAGMA table_info({((ComboBox)e.Source).SelectedItem})", _connection);
            SqliteDataReader query = sql.ExecuteReader();
            List<string> colums = new List<string>();
            while (query.Read())
            {
                colums.Add(query.GetString(1)); //table name
            }




            SqliteCommand sql2 = new SqliteCommand($"Select * from {((ComboBox)e.Source).SelectedItem}", _connection);
            SqliteDataReader query2 = sql2.ExecuteReader();
            List<string> data = new List<string>();
            while (query2.Read())
            {
                for (int i = 0; i < colums.Count; i++)
                {
                    if (query2.IsDBNull(i))
                        data.Add("NULL");
                    else
                        data.Add(query2.GetString(i));
                }
            }

            DataTable Data = new DataTable();

            foreach (string column in colums)
            {
                Data.Columns.Add(column);
            }

            List<object> oneRow = new List<object>();
            foreach (string cell in data)
            {
                oneRow.Add(cell);
                if (oneRow.Count == colums.Count())
                {
                    Data.Rows.Add(oneRow.ToArray());
                    oneRow.Clear();
                }
            }


            DatabaseGrid.ItemsSource = Data.DefaultView;
            _lastSelectedTableIndex = ((ComboBox)e.Source).SelectedIndex;

        }

        private SqliteConnection _connection;
        private int _lastSelectedTableIndex = -1; // add db path changed listener
        private List<Device> _devices = new List<Device>();


        public class Device
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}