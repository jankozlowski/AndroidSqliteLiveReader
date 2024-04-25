using AndroidSqliteLiveReader.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AndroidSqliteLiveReader
{

    public partial class SqlViewControl : UserControl
    {
        private readonly Adb Adb;
        private readonly Settings Settings;
        private readonly HashSet<string> CretedFilesPaths;
        private SqliteConnection Connection;

        private List<Device> Devices;
        private Device SelectedDevice { get { return Devices.Where(d => d.Name.Equals(DevicesComboBox.Text)).FirstOrDefault(); } }
        private int LastSelectedTableIndex = -1;
        private bool SettingsVisible { get; set; }
        public const string SettingsName = "SqlViewSettings";


        public SqlViewControl()
        {
            InitializeComponent();
            Adb = new Adb();
            Settings = new Settings();
            CretedFilesPaths = new HashSet<string>();

            Adb.AdbPath = AdbPathBox.Text;
            Loaded += SqlViewControlLoaded;
            Unloaded += SqlViewControlUnloaded;
        }

        private void SqlViewControlLoaded(object sender, RoutedEventArgs e)
        {
            Devices = GetConnectedDevices();
            DevicesComboBox.ItemsSource = Devices.Select(d => d.Name).ToList();
            if (Devices.Count != 0)
                DevicesComboBox.SelectedIndex = 0;

            string adbPath = Settings.GetSetting(SettingsName, "adbPath");
            if (!string.IsNullOrEmpty(adbPath))
                AdbPathBox.Text = adbPath;

            string dbPath = Settings.GetSetting(SettingsName, "dbPath");
            dbPathBox.Text = dbPath;
            var visibility = Settings.GetSetting(SettingsName, "settingsVisible");
            bool.TryParse(visibility, out bool result);
            SettingsVisible = result;
            SetSettingsVisibility(SettingsVisible);
        }

        private void SqlViewControlUnloaded(object sender, RoutedEventArgs e)
        {
            CloseConnectionIfOpen();
            Settings.SaveSetting(SettingsName, "adbPath", AdbPathBox.Text);
            Settings.SaveSetting(SettingsName, "dbPath", dbPathBox.Text);
            Settings.SaveSetting(SettingsName, "settingsVisible", SettingsVisible.ToString());
            TableComboBox.SelectedIndex = -1;
            TableComboBox.ItemsSource = new string[] { };
            DatabaseGrid.ItemsSource = new HashSet<string>();
            DeleteCreatedFiles();
        }

        private List<Device> GetConnectedDevices()
        {
            List<Device> devices = new List<Device>();

            string devicesAdbResult = Adb.AdbCommandWithResult("devices -l");

            string[] lineResult = SplitByLine(devicesAdbResult);

            List<string> deviceIds = new List<string>();

            foreach (string line in lineResult.Skip(1))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] parameters = line.Split();
                Device device = new Device
                {
                    Id = parameters[0]
                };
                devices.Add(device);
            }

            foreach (Device device in devices)
            {
                string properties = Adb.AdbCommandWithResult($"-s {device.Id} shell getprop");
                string[] propertieslines = SplitByLine(properties);
                string avdNameLine = propertieslines.Where(l => l.Contains("ro.boot.qemu.avd_name")).FirstOrDefault();

                if (string.IsNullOrEmpty(avdNameLine))
                    avdNameLine = propertieslines.Where(l => l.Contains("ro.product.device")).FirstOrDefault();
                if (string.IsNullOrEmpty(avdNameLine))
                    avdNameLine = propertieslines.Where(l => l.Contains("ro.product.product.model")).FirstOrDefault();
                if (string.IsNullOrEmpty(avdNameLine))
                    avdNameLine = propertieslines.Where(l => l.Contains("ro.boot.hardware.sku")).FirstOrDefault();
                if (string.IsNullOrEmpty(avdNameLine))
                    avdNameLine = ":Unknown_Device";

                string avdName = avdNameLine.Split(':')[1];
                avdName = avdName.Trim().Replace("_", " ");
                avdName = avdName.Substring(1, avdName.Length - 2);
                avdName = avdName[0].ToString().ToUpper() + avdName.Substring(1);
                device.Name = avdName;
            }

            return devices;
        }

        private void CloseConnectionIfOpen()
        {
            if (Connection != null && Connection.State == ConnectionState.Open)
            {
                Connection.Close();
                Connection.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void DeleteCreatedFiles()
        {
            foreach (string path in CretedFilesPaths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }

        private void CheckAdbClick(object sender, RoutedEventArgs e)
        {
            string adbPath = Path.Combine(AdbPathBox.Text, "adb.exe");
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

        private void DevicesComboBoxDropDownOpened(object sender, EventArgs e)
        {
            Devices = GetConnectedDevices();
            DevicesComboBox.ItemsSource = Devices.Select(d => d.Name).ToList();
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            Device selectedDevice = SelectedDevice;
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

        private void GetDataClick(object sender, RoutedEventArgs e)
        {
            string dbPath = dbPathBox.Text;

            if (string.IsNullOrEmpty(dbPath))
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "No db path provided"), "No db Path");
                return;
            }

            if (!bool.TryParse(Adb.AdbCommandWithResult($"-s {SelectedDevice.Id} shell test -f {dbPath} && echo 'true'"), out bool result))
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Db file not found, are you sure that provided db path is correct and you have selected correct device?"), "Db file not found");
                return;
            }

            try
            {
                CopyFileFromDeviceToApplicationData(dbPath);
                OpenConnectionToDb(dbPath.Substring(dbPath.LastIndexOf("/") + 1));
                TableComboBox.SelectedIndex = -1;
                TableComboBox.ItemsSource = GetDbTables();
                TableComboBox.SelectedIndex = LastSelectedTableIndex == -1 ? 0 : LastSelectedTableIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ex.Message), "Error");
            }
        }

        private void CopyFileFromDeviceToApplicationData(string dbPath)
        {
            string fileName = dbPath.Substring(dbPath.LastIndexOf("/") + 1);
            Adb.AdbCommand($"-s {SelectedDevice.Id} shell mkdir /storage/emulated/0/DbCopyPathTemp");// make Temp Directory
            Adb.AdbCommand($"-s {SelectedDevice.Id} shell sqlite3 {dbPath} \"'.backup /storage/emulated/0/DbCopyPathTemp/{fileName}'\""); //copy Db to readable folder
            Adb.AdbCommand($"-s {SelectedDevice.Id} pull /storage/emulated/0/DbCopyPathTemp/{fileName} {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}"); // copy db to disc
            Adb.AdbCommand($"-s {SelectedDevice.Id} shell rm -r /storage/emulated/0/DbCopyPathTemp");// delete Temp Directory
        }

        private void OpenConnectionToDb(string fileName)
        {
            string currentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName);
            CretedFilesPaths.Add(currentPath);
            Connection = new SqliteConnection($"data source={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileName)}; POOLING=FALSE;");
            Connection.Open();
        }

        private List<string> GetDbTables()
        {
            List<string> reuslt = new List<string>();

            using (SqliteCommand sqlCommand = new SqliteCommand("SELECT name FROM sqlite_master WHERE type = 'table'", Connection))
            {
                using (SqliteDataReader query = sqlCommand.ExecuteReader())
                {
                    while (query.Read())
                    {
                        reuslt.Add(query.GetString(0));
                    }
                    query.Close();
                }
            }

            return reuslt;
        }

        private void ExecuteSqlClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Connection == null || Connection.State != ConnectionState.Open)
                {
                    MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Db Connection is not open, synchronize data first by clicking Get Data"), "Connection not open");
                    return;
                }

                using (SqliteCommand sql = new SqliteCommand(sqlBox.Text, Connection))
                {
                    LoadTableData(sql);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ex.Message), "Error");
            }
        }

        private void LoadTableData(SqliteCommand sql)
        {
            DataTable dataTable = new DataTable();
            using (SqliteDataReader sqlDataReader = sql.ExecuteReader())
            {
                dataTable.Load(sqlDataReader);
                sqlDataReader.Close();
            }
            DatabaseGrid.ItemsSource = dataTable.DefaultView;
        }

        private void SettingsToogleClick(object sender, RoutedEventArgs e)
        {
            SettingsVisible = !SettingsVisible;
            SetSettingsVisibility(SettingsVisible);
        }

        private void SetSettingsVisibility(bool visible)
        {
            if (!visible)
            {
                row1.Height = new GridLength(0);
                row2.Height = new GridLength(0);
                row3.Height = new GridLength(0);
            }

            if (visible)
            {
                row1.Height = new GridLength(30);
                row2.Height = new GridLength(30);
                row3.Height = new GridLength(30);
            }
        }

        private void AdbPathTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Adb == null)
                return;

            Adb.AdbPath = ((TextBox)e.Source).Text;

            dbPathBox.Text = string.Empty;
            DevicesComboBox.SelectedIndex = -1;
            TableComboBox.SelectedIndex = -1;
            DevicesComboBox.ItemsSource = new string[] { };
            TableComboBox.ItemsSource = new string[] { };
            DatabaseGrid.ItemsSource = new HashSet<string>();
            CloseConnectionIfOpen();
        }

        private void DbPathTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TableComboBox == null)
                return;

            TableComboBox.ItemsSource = new string[] { };
            DatabaseGrid.ItemsSource = new HashSet<string>();
            CloseConnectionIfOpen();
        }

        private void TableComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)e.Source).SelectedIndex == -1)
                return;

            try
            {
                using (SqliteCommand sql = new SqliteCommand($"Select * from '{((ComboBox)e.Source).SelectedItem}'", Connection))
                {
                    LoadTableData(sql);
                }

                LastSelectedTableIndex = ((ComboBox)e.Source).SelectedIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ex.Message), "Error");
            }
        }


        private class Device
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}