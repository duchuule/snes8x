using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Globalization;
using System.Windows.Media;

namespace PhoneDirect3DXamlAppInterop
{
    public class AppSettings: INotifyPropertyChanged
    {

        // Our isolated storage settings
        IsolatedStorageSettings isolatedStore;

        //the following keys are use in metro mode only
        const String ThemeSelectionKey = "ThemeSelectionKey";
        const String ShowThreeDotsKey = "ShowThreeDotsKey";
        const String BackgroundUriKey = "BackgroundUriKey";
        const String BackgroundOpacityKey = "BackgroundOpacityKey";
        const String UseDefaultBackgroundKey = "UseDefaultBackgroundKey";
        const String ShowLastPlayedGameKey = "ShowLastPlayedGameKey";
        const String LastIPAddressKey = "LastIPAddressKey";
        const String LastTimeoutKey = "LastTimeoutKey";
        const String LoadLastStateKey = "LoadLastStateKey";  //abandon
        const String PromotionCodeKey = "PromotionCodeKey";
        const String NAppLaunchKey = "NAppLaunchKey";
        const String CanAskReviewKey = "CanAskReviewKey";
        const String AutoBackupKey = "AutoBackupKey";
        const string AutoBackupModeKey = "AutoBackupModeKey";
        const string NRotatingBackupKey = "NRotatingBackupKey";
        const string BackupIngameSaveKey = "BackupIngameSaveKey";
        const string BackupManualSaveKey = "BackupManualSaveKey";
        const string BackupAutoSaveKey = "BackupAutoSaveKey";
        const string WhenToBackupKey = "WhenToBackupKey";
        const string BackupOnlyWifiKey = "BackupOnlyWifiKey";
        const string AutoBackupIndexKey = "AutoBackupIndexKey";
        const string UseAccentColorKey = "UseAccentColorKey";
        const string VoiceCommandVersionKey = "VoiceCommandVersionKey";
        const string FirstTurboPromptKey = "FirstTurboPromptKey";

        /// <summary>
        /// Constructor that gets the application settings.
        /// </summary>
        public AppSettings()
        {
            try
            {
                // Get the settings for this application.
                isolatedStore = IsolatedStorageSettings.ApplicationSettings;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using IsolatedStorageSettings: " + e.ToString());
            }
        }

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (isolatedStore.Contains(Key))
            {
                // If the value has changed
                if (isolatedStore[Key] != value)
                {
                    // Store the new value
                    isolatedStore[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                isolatedStore.Add(Key, value);
                valueChanged = true;
            }

            return valueChanged;
        }


        // Helper method for removing a key/value pair from isolated storage
        public void RemoveValue(string Key)
        {
            // If the key exists
            if (isolatedStore.Contains(Key))
            {
                isolatedStore.Remove(Key);
            }
        }


        public bool Contains(string Key)
        {
            // If the key exists
            if (isolatedStore.Contains(Key))
                return true;
            else
                return false;
        }


        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="valueType"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
        {
            valueType value;

            // If the key exists, retrieve the value.
            if (isolatedStore.Contains(Key))
            {
                value = (valueType)isolatedStore[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }

            return value;
        }


        /// <summary>
        /// Save the settings.
        /// </summary>
        public void Save()
        {
            isolatedStore.Save();
        }


        public int ThemeSelection
        {
            get
            {
                return GetValueOrDefault<int>(ThemeSelectionKey, 0);
            }
            set
            {
                AddOrUpdateValue(ThemeSelectionKey, value);
                Save();
            }
        }


        public bool ShowThreeDots
        {
            get
            {
                return GetValueOrDefault<bool>(ShowThreeDotsKey, true);
            }
            set
            {
                AddOrUpdateValue(ShowThreeDotsKey, value);
                Save();
            }
        }

        public String BackgroundUri
        {
            get
            {
                return GetValueOrDefault<String>(BackgroundUriKey, FileHandler.DEFAULT_BACKGROUND_IMAGE);
            }
            set
            {
                AddOrUpdateValue(BackgroundUriKey, value);
                Save();
                NotifyPropertyChanged("BackgroundUri");
            }
        }


        public double BackgroundOpacity
        {
            get
            {
                return GetValueOrDefault<double>(BackgroundOpacityKey, 0.2);
            }
            set
            {
                AddOrUpdateValue(BackgroundOpacityKey, value);
                Save();
            }
        }

        public bool UseDefaultBackground
        {
            get
            {
                return GetValueOrDefault<bool>(UseDefaultBackgroundKey, true);
            }
            set
            {
                AddOrUpdateValue(UseDefaultBackgroundKey, value);
                Save();
            }
        }

        public bool ShowLastPlayedGame
        {
            get
            {
                return GetValueOrDefault<bool>(ShowLastPlayedGameKey, true);
            }
            set
            {
                AddOrUpdateValue(ShowLastPlayedGameKey, value);
                Save();
            }
        }

        public string LastIPAddress
        {
            get
            {
                return GetValueOrDefault<string>(LastIPAddressKey, "");
            }
            set
            {
                AddOrUpdateValue(LastIPAddressKey, value);
                Save();
            }
        }


        public int LastTimeout
        {
            get
            {
                return GetValueOrDefault<int>(LastTimeoutKey, 3000);
            }
            set
            {
                AddOrUpdateValue(LastTimeoutKey, value);
                Save();
            }
        }

        public bool LoadLastState
        {
            get
            {
                return GetValueOrDefault<bool>(LoadLastStateKey, false);
            }
            set
            {
                AddOrUpdateValue(LoadLastStateKey, value);
                Save();
            }
        }

        public string PromotionCode
        {
            get
            {
                return GetValueOrDefault<string>(PromotionCodeKey, "");
            }
            set
            {
                AddOrUpdateValue(PromotionCodeKey, value);
                Save();
            }
        }


        public int NAppLaunch
        {
            get
            {
                return GetValueOrDefault<int>(NAppLaunchKey, 0);
            }
            set
            {
                AddOrUpdateValue(NAppLaunchKey, value);
                Save();
            }
        }


        public bool CanAskReview
        {
            get
            {
                return GetValueOrDefault<bool>(CanAskReviewKey, true);
            }
            set
            {
                AddOrUpdateValue(CanAskReviewKey, value);
                Save();
            }
        }


        public bool AutoBackup
        {
            get
            {
                return GetValueOrDefault<bool>(AutoBackupKey, false);
            }
            set
            {
                AddOrUpdateValue(AutoBackupKey, value);
                Save();
            }
        }


        //0: simple, 1: rotating
        public int AutoBackupMode
        {
            get
            {
                return GetValueOrDefault<int>(AutoBackupModeKey, 1);
            }
            set
            {
                AddOrUpdateValue(AutoBackupModeKey, value);
                Save();
            }
        }

        public int NRotatingBackup
        {
            get
            {
                return GetValueOrDefault<int>(NRotatingBackupKey, 5);
            }
            set
            {
                AddOrUpdateValue(NRotatingBackupKey, value);
                Save();
            }
        }

        public bool BackupIngameSave
        {
            get
            {
                return GetValueOrDefault<bool>(BackupIngameSaveKey, true);
            }
            set
            {
                AddOrUpdateValue(BackupIngameSaveKey, value);
                Save();
            }
        }


        public bool BackupManualSave
        {
            get
            {
                return GetValueOrDefault<bool>(BackupManualSaveKey, true);
            }
            set
            {
                AddOrUpdateValue(BackupManualSaveKey, value);
                Save();
            }
        }


        public bool BackupAutoSave
        {
            get
            {
                return GetValueOrDefault<bool>(BackupAutoSaveKey, true);
            }
            set
            {
                AddOrUpdateValue(BackupAutoSaveKey, value);
                Save();
            }
        }

        public int WhenToBackup
        {
            get
            {
                return GetValueOrDefault<int>(WhenToBackupKey, 0);
            }
            set
            {
                AddOrUpdateValue(WhenToBackupKey, value);
                Save();
            }
        }


        public bool BackupOnlyWifi
        {
            get
            {
                return GetValueOrDefault<bool>(BackupOnlyWifiKey, false);
            }
            set
            {
                AddOrUpdateValue(BackupOnlyWifiKey, value);
                Save();
            }
        }

        public bool UseAccentColor
        {
            get
            {
                return GetValueOrDefault<bool>(UseAccentColorKey, true);
            }
            set
            {
                AddOrUpdateValue(UseAccentColorKey, value);
                Save();
            }
        }

        public int VoiceCommandVersion
        {
            get
            {
                return GetValueOrDefault<int>(VoiceCommandVersionKey, 0);
            }
            set
            {
                AddOrUpdateValue(VoiceCommandVersionKey, value);
                Save();
            }
        }



        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


    }
}
