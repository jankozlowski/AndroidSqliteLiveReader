using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace AndroidSqliteLiveReader.Services
{
    public class Settings
    {
        private WritableSettingsStore GetWritableSettingsStore()
        {
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        public void SaveSetting(string collectionPath, string propertyName, string value)
        {
            WritableSettingsStore writableSettingsStore = GetWritableSettingsStore();
            if (writableSettingsStore != null)
            {
                if (!writableSettingsStore.CollectionExists(collectionPath))
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
