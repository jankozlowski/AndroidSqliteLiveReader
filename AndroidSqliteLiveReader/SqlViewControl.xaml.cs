﻿using AndroidSqliteLiveReader.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
            AdbHelper.AdbPath = AdbPath.Text;
            Loaded += SqlViewControlLoaded;
            Unloaded += SqlViewControl_Unloaded;
        }

        private void SqlViewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveSetting("SqlViewSettings", "adbPath", AdbPath.Text);
            SaveSetting("SqlViewSettings", "dbPath", dbPathBox.Text);
        }

        private void SqlViewControlLoaded(object sender, RoutedEventArgs e)
        {
            _devices = GetConnectedDevices();
            DevicesComboBox.ItemsSource = _devices.Select(d => d.Name).ToList();
            if (_devices.Count != 0)
                DevicesComboBox.SelectedIndex = 0;
            var sdk = GetSetting("SqlViewSettings", "adbPath");
            var db = GetSetting("SqlViewSettings", "dbPath");
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
            Device selectedDevice = _selectedDevice;
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

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "No db path provided"), "No db Path");
                return;
            }

            if (!bool.TryParse(AdbHelper.AdbCommandWithResult($"-s {_selectedDevice.Id} shell test -f {path} && echo 'true'"), out bool result))
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Db file not found, are you sure that provided db path is correct and you have selected correct device?"), "Db file not found");
                return;
            }


            try
            {
                string fileName = path.Substring(path.LastIndexOf("/") + 1);

                AdbHelper.AdbCommand($"-s {_selectedDevice.Id} shell \"su 0 mkdir /storage/emulated/0/DbCopyPathTemp\"");// make Directory
                AdbHelper.AdbCommand($"-s {_selectedDevice.Id} shell \"su 0 cp -F {path} /storage/emulated/0/DbCopyPathTemp/\""); //copy Db to readable folder
                AdbHelper.AdbCommand($"-s {_selectedDevice.Id} pull /storage/emulated/0/DbCopyPathTemp/HostileCity.db {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}"); // copy db to disc
                AdbHelper.AdbCommand($"-s {_selectedDevice.Id} shell \"su 0 rm -r /storage/emulated/0/DbCopyPathTemp\"");// delete Directory

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
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ex.Message), "Error");

            }
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
        private int _lastSelectedTableIndex = -1;
        private List<Device> _devices = new List<Device>();
        private Device _selectedDevice { get { return _devices.Where(d => d.Name.Equals(DevicesComboBox.Text)).FirstOrDefault(); } }


        public class Device
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private void AdbPathTextChanged(object sender, TextChangedEventArgs e)
        {
            AdbHelper.AdbPath = ((TextBox)e.Source).Text;

            if (DevicesComboBox == null)
                return;

            dbPathBox.Text = string.Empty;
            DevicesComboBox.SelectedIndex = -1;
            TableComboBox.SelectedIndex = -1;
            DevicesComboBox.Items.Clear();
            TableComboBox.Items.Clear();
            DatabaseGrid.Items.Clear();
        }

        private void DbPathTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TableComboBox == null)
                return;

            TableComboBox.Items.Clear();
            DatabaseGrid.Items.Clear();
        }

        public WritableSettingsStore GetWritableSettingsStore()
        {
            ServiceProvider serviceProvider = ServiceProvider.GlobalProvider;
            var settingsManager = serviceProvider.GetService(typeof(SVsSettingsManager)) as IVsSettingsManager;
            if (settingsManager != null)
            {
                IVsWritableSettingsStore writableSettingsStore;
                settingsManager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out writableSettingsStore);
                return writableSettingsStore as WritableSettingsStore;
            }
            return null;
        }

        public void SaveSetting(string collectionPath, string propertyName, string value)
        {
            WritableSettingsStore writableSettingsStore = GetWritableSettingsStore();
            if (writableSettingsStore != null)
            {
                writableSettingsStore.CreateCollection(collectionPath);
                writableSettingsStore.SetString(collectionPath, propertyName, value);
            }
        }

        public string GetSetting(string collectionPath, string propertyName)
        {
            WritableSettingsStore writableSettingsStore = GetWritableSettingsStore();
            if (writableSettingsStore != null && writableSettingsStore.CollectionExists(collectionPath))
            {
                return writableSettingsStore.GetString(collectionPath, propertyName, defaultValue: null);
            }
            return null;
        }
    }
}