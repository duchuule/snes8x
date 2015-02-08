﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Live;
using Microsoft.Live.Controls;
using PhoneDirect3DXamlAppInterop.Resources;
using System.Threading.Tasks;
using PhoneDirect3DXamlAppComponent;
using PhoneDirect3DXamlAppInterop.Database;
using System.Windows.Media;
using Microsoft.Phone.Tasks;
using Windows.Phone.Storage.SharedAccess;
using System.IO;
using System.Collections.ObjectModel;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;
using System.Windows.Media.Imaging;
using CloudSixConnector.FilePicker;
using CloudSixConnector.FileSaver;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Ionic.Zip;
using System.Windows.Data;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Windows.Foundation;
using DucLe.Extensions;
using Windows.Phone.Speech.VoiceCommands;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Text;
//"C:\Program Files (x86)\Microsoft SDKs\Windows Phone\v8.0\Tools\IsolatedStorageExplorerTool\ISETool.exe" ts xd ed3cc816-1ab0-418a-9bb8-11505804f6b4 "D:\Duc\Documents\Visual Studio 2012\Projects\WP8Snes8x\trunk"


namespace PhoneDirect3DXamlAppInterop
{




    public partial class MainPage : PhoneApplicationPage
    {
        private ApplicationBarIconButton resumeButton;
       private ROMDatabase db;
        private Task createFolderTask, copyDemoTask, initTask;

        public static bool shouldUpdateBackgroud = false;

        public MainPage()
        {
            InitializeComponent();

            //add tilt effect to tiltablegrid
            Microsoft.Phone.Controls.TiltEffect.TiltableItems.Add(typeof(TiltableGrid));
            Microsoft.Phone.Controls.TiltEffect.TiltableItems.Add(typeof(TiltableCanvas));


            this.InitAppBar();

            this.initTask = this.Initialize();

            this.Loaded += MainPage_Loaded;

            
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await this.initTask;

            try
            {
                String romFileName = NavigationContext.QueryString[FileHandler.ROM_URI_STRING];
                NavigationContext.QueryString.Remove(FileHandler.ROM_URI_STRING);

                ROMDBEntry entry = this.db.GetROM(romFileName);
                await this.StartROM(entry);
            }
            catch (KeyNotFoundException)
            { }
            catch (Exception)
            {
                MessageBox.Show(AppResources.TileOpenError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }

            try
            {
                String importRomID = NavigationContext.QueryString["fileToken"];
                NavigationContext.QueryString.Remove("fileToken");

                ROMDBEntry entry = await FileHandler.ImportRomBySharedID(importRomID, this);

            }
            catch (KeyNotFoundException)
            { }
            catch (Exception)
            {
                MessageBox.Show(AppResources.FileAssociationError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }

            //register voice command
            if (App.metroSettings.VoiceCommandVersion < App.VOICE_COMMAND_VERSION || VoiceCommandService.InstalledCommandSets.Count == 0)
            {
                try
                {
                    await RegisterVoiceCommand("");



                    await UpdateGameListForVoiceCommand();

                    App.metroSettings.VoiceCommandVersion = App.VOICE_COMMAND_VERSION;


                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message + "\r\nVoice Commands failed to initialize.");
                }
            }
        }

        private async Task Initialize()
        {
            createFolderTask = FileHandler.CreateInitialFolderStructure();
            copyDemoTask = this.CopyDemoROM();

            await createFolderTask;
            await copyDemoTask;

            this.db = ROMDatabase.Current;
            if (db.Initialize())
            {
                await FileHandler.FillDatabaseAsync();
            }
            this.db.Commit += () =>
            {
                this.RefreshROMList();
            };
            this.RefreshROMList();
        }


        public static async Task RegisterVoiceCommand(string prefix)
        {


            //if user specified a command prefix, copy the original definition file to isolated storage and modified it
            if (prefix != null && prefix.Trim() != "")
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                StorageFile originalFile = await StorageFile.GetFileFromPathAsync("VoiceCommandDefinitions.xml");

                IRandomAccessStream accessStream = await originalFile.OpenReadAsync();

                string originalText;
                using (Stream stream = accessStream.AsStreamForRead((int)accessStream.Size))
                {
                    byte[] content = new byte[stream.Length];
                    await stream.ReadAsync(content, 0, (int)stream.Length);

                    originalText = Encoding.UTF8.GetString(content, 0, content.Length);
                }

                int idx = originalText.IndexOf("    <Example>");

                if (idx < 0)
                {
                    MessageBox.Show("Error! Cannot find <Example>");
                    return;
                }


                string modifiedText = originalText.Substring(0, idx);
                modifiedText += "    <CommandPrefix> " + prefix + "</CommandPrefix>\r\n";
                modifiedText += originalText.Substring(idx);


                IStorageFile storageFile = await localFolder.CreateFileAsync("VoiceCommandDefinitions.xml", CreationCollisionOption.ReplaceExisting);

                using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                {
                    byte[] content = Encoding.UTF8.GetBytes(modifiedText);
                    await stream.WriteAsync(content, 0, content.Length);
                }

                Uri uri = new Uri("ms-appdata:///local/VoiceCommandDefinitions.xml", UriKind.Absolute);
                await Windows.Phone.Speech.VoiceCommands.VoiceCommandService.InstallCommandSetsFromFileAsync(uri);
            }

            else  //default prefix
            {
                Uri uri = new Uri("ms-appx:///VoiceCommandDefinitions.xml", UriKind.Absolute);
                await Windows.Phone.Speech.VoiceCommands.VoiceCommandService.InstallCommandSetsFromFileAsync(uri);
            }
        }


        public static async Task UpdateGameListForVoiceCommand()
        {
            try
            {
                //get list of gammes
                IEnumerable<ROMDBEntry> romList = ROMDatabase.Current.GetROMList();
                List<String> nameList = new List<String>();
                foreach (ROMDBEntry entry in romList)
                {
                    nameList.Add(Regex.Replace(entry.DisplayName, @"[^\w\s]+", "").ToLower());
                }

                //safeguard incase the commandsets failed to install
                if (VoiceCommandService.InstalledCommandSets.ContainsKey("en-US") == false)
                {
                    Uri uri = new Uri("ms-appx:///VoiceCommandDefinitions.xml", UriKind.Absolute);
                    await Windows.Phone.Speech.VoiceCommands.VoiceCommandService.InstallCommandSetsFromFileAsync(uri);
                }

                //populate list of games for voice command
                if (VoiceCommandService.InstalledCommandSets.ContainsKey("en-US"))
                {
                    VoiceCommandSet widgetVcs = VoiceCommandService.InstalledCommandSets["en-US"];
                    await widgetVcs.UpdatePhraseListAsync("RomName", nameList.ToArray());
                }



            }
            catch (Exception)
            {

            }
        }


        async void btnSignin_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            try
            {
                if (e.Status == LiveConnectSessionStatus.Connected)
                {
                    App.session = e.Session;
                    //this.statusLabel.Text = AppResources.StatusSignedIn;
                    this.gotoImportButton.IsEnabled = true;
                    this.gotoBackupButton.IsEnabled = true;

                    LiveConnectClient client = new LiveConnectClient(App.session);
                    if (App.metroSettings.AutoBackup && (App.exportFolderID == null || App.exportFolderID == ""))
                        App.exportFolderID = await ExportSelectionPage.CreateExportFolder(client); //get ID of upload folder

                    //this.gotoRestoreButton.IsEnabled = true;
                }
                else
                {
                    this.gotoImportButton.IsEnabled = false;
                    this.gotoBackupButton.IsEnabled = false;
                    //this.gotoRestoreButton.IsEnabled = false;
                    //this.statusLabel.Text = AppResources.StatusNotSignedIn;
                    App.session = null;

                    //if (e.Error != null)
                    //{
                    //    MessageBox.Show(String.Format(AppResources.SkyDriveError, e.Error.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                    //    //statusLabel.Text = e.Error.ToString();
                    //}
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void gotoImportButton_Click_1(object sender, RoutedEventArgs e)
        {


            if (App.session != null)
            {
                PhoneApplicationService.Current.State["parameter"] = App.session;
                this.NavigationService.Navigate(new Uri("/SkyDriveImportPage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.NotSignedInError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
         
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //set app bar color in case the user return from setting page
            if (ApplicationBar != null)
            {
                ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
                ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];
            }

            //await this.createFolderTask;
            //await this.copyDemoTask;
            await this.initTask;

            this.LoadInitialSettings();

            if (shouldUpdateBackgroud)
            {
                UpdateBackgroundImage();
                shouldUpdateBackgroud = false;

            }

            this.RefreshROMList();

            this.resumeButton.IsEnabled = EmulatorPage.ROMLoaded;
            
            base.OnNavigatedTo(e);
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileHandler.UpdateLiveTile();

            base.OnNavigatedFrom(e);
        }

        private void UpdateBackgroundImage()
        {
            if (App.metroSettings.BackgroundUri != null)
            {
                panorama.Background = new ImageBrush
                {
                    Opacity = App.metroSettings.BackgroundOpacity,
                    Stretch = Stretch.None,
                    AlignmentX = System.Windows.Media.AlignmentX.Center,
                    AlignmentY = System.Windows.Media.AlignmentY.Top,
                    ImageSource = FileHandler.getBitmapImage(App.metroSettings.BackgroundUri, FileHandler.DEFAULT_BACKGROUND_IMAGE)

                };
            }
            else
            {
                panorama.Background = null;
            }
        }

        private async Task CopyDemoROM()
        {
            IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
            if (!isoSettings.Contains("DEMOCOPIED"))
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder romFolder = await localFolder.CreateFolderAsync("roms", CreationCollisionOption.OpenIfExists);
                StorageFile file = await StorageFile.GetFileFromPathAsync("Assets/Airwolf 92 (Demo).smc");
                await file.CopyAsync(romFolder);

                isoSettings["DEMOCOPIED"] = true;
                isoSettings.Save();
            }
        }

        private void LoadInitialSettings()
        {
            EmulatorSettings settings = EmulatorSettings.Current;
            if (!settings.Initialized)
            {
                IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
                settings.Initialized = true;

                if (!isoSettings.Contains(SettingsPage.EnableSoundKey))
                {
                    isoSettings[SettingsPage.EnableSoundKey] = true;
                }
                if (!isoSettings.Contains(SettingsPage.VControllerPosKey))
                {
                    isoSettings[SettingsPage.VControllerPosKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.LowFreqModeKey))
                {
                    isoSettings[SettingsPage.LowFreqModeKey] = false;
                }
                //if (!isoSettings.Contains(SettingsPage.LowFreqModeMeasuredKey))
                //{
                //    isoSettings[SettingsPage.LowFreqModeMeasuredKey] = false;
                //}
                if (!isoSettings.Contains(SettingsPage.VControllerSizeKey))
                {
                    isoSettings[SettingsPage.VControllerSizeKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.VControllerButtonStyleKey))
                {
                    isoSettings[SettingsPage.VControllerButtonStyleKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.StretchKey))
                {
                    isoSettings[SettingsPage.StretchKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.OrientationKey))
                {
                    isoSettings[SettingsPage.OrientationKey] = 0;
                }
                if (!isoSettings.Contains(SettingsPage.ControllerScaleKey))
                {
                    isoSettings[SettingsPage.ControllerScaleKey] = 100;
                }
                if (!isoSettings.Contains(SettingsPage.ButtonScaleKey))
                {
                    isoSettings[SettingsPage.ButtonScaleKey] = 100;
                }
                if (!isoSettings.Contains(SettingsPage.OpacityKey))
                {
                    isoSettings[SettingsPage.OpacityKey] = 30;
                }
                if (!isoSettings.Contains(SettingsPage.SkipFramesKey))
                {
                    isoSettings[SettingsPage.SkipFramesKey] = 0;
                }
                if (!isoSettings.Contains(SettingsPage.TurboFrameSkipKey))
                {
                    isoSettings[SettingsPage.TurboFrameSkipKey] = 4;
                }
                if (!isoSettings.Contains(SettingsPage.SyncAudioKey))
                {
                    isoSettings[SettingsPage.SyncAudioKey] = true;
                }
                if (!isoSettings.Contains(SettingsPage.PowerSaverKey))
                {
                    isoSettings[SettingsPage.PowerSaverKey] = 0;
                }
                if (!isoSettings.Contains(SettingsPage.DPadStyleKey))
                {
                    isoSettings[SettingsPage.DPadStyleKey] = 1;
                }
                if (!isoSettings.Contains(SettingsPage.DeadzoneKey))
                {
                    isoSettings[SettingsPage.DeadzoneKey] = 10.0f;
                }
                if (!isoSettings.Contains(SettingsPage.ImageScalingKey))
                {
                    isoSettings[SettingsPage.ImageScalingKey] = 100;
                }
                if (!isoSettings.Contains(SettingsPage.CameraAssignKey))
                {
                    isoSettings[SettingsPage.CameraAssignKey] = 0;
                }
                if (!isoSettings.Contains(SettingsPage.ConfirmationKey))
                {
                    isoSettings[SettingsPage.ConfirmationKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.ConfirmationLoadKey))
                {
                    isoSettings[SettingsPage.ConfirmationLoadKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.AutoIncKey))
                {
                    isoSettings[SettingsPage.AutoIncKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.SelectLastState))
                {
                    isoSettings[SettingsPage.SelectLastState] = true;
                }
                if (!isoSettings.Contains(SettingsPage.CreateManualSnapshotKey))
                {
                    isoSettings[SettingsPage.CreateManualSnapshotKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.UseMogaControllerKey))
                {
                    isoSettings[SettingsPage.UseMogaControllerKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.ShouldShowAdsKey))
                {
                    isoSettings[SettingsPage.ShouldShowAdsKey] = false;
                }
                if (!isoSettings.Contains(SettingsPage.BgcolorRKey))
                {
                    isoSettings[SettingsPage.BgcolorRKey] = 210;
                }
                if (!isoSettings.Contains(SettingsPage.BgcolorGKey))
                {
                    isoSettings[SettingsPage.BgcolorGKey] = 210;
                }
                if (!isoSettings.Contains(SettingsPage.BgcolorBKey))
                {
                    isoSettings[SettingsPage.BgcolorBKey] = 210;
                }


                //get default controller position
                int[] cpos = CustomizeControllerPage.GetDefaultControllerPosition();
                

                //set default controller position
                if (!isoSettings.Contains(SettingsPage.PadCenterXPKey))
                {
                    isoSettings[SettingsPage.PadCenterXPKey] = cpos[0];
                }
                if (!isoSettings.Contains(SettingsPage.PadCenterYPKey))
                {
                    isoSettings[SettingsPage.PadCenterYPKey] = cpos[1];
                }
                if (!isoSettings.Contains(SettingsPage.ACenterXPKey))
                {
                    isoSettings[SettingsPage.ACenterXPKey] = cpos[2];
                }
                if (!isoSettings.Contains(SettingsPage.ACenterYPKey))
                {
                    isoSettings[SettingsPage.ACenterYPKey] = cpos[3];
                }
                if (!isoSettings.Contains(SettingsPage.BCenterXPKey))
                {
                    isoSettings[SettingsPage.BCenterXPKey] = cpos[4];
                }
                if (!isoSettings.Contains(SettingsPage.BCenterYPKey))
                {
                    isoSettings[SettingsPage.BCenterYPKey] = cpos[5];
                }
                if (!isoSettings.Contains(SettingsPage.StartLeftPKey))
                {
                    isoSettings[SettingsPage.StartLeftPKey] = cpos[6];
                }
                if (!isoSettings.Contains(SettingsPage.StartTopPKey))
                {
                    isoSettings[SettingsPage.StartTopPKey] = cpos[7];
                }
                if (!isoSettings.Contains(SettingsPage.SelectRightPKey))
                {
                    isoSettings[SettingsPage.SelectRightPKey] = cpos[8];
                }
                if (!isoSettings.Contains(SettingsPage.SelectTopPKey))
                {
                    isoSettings[SettingsPage.SelectTopPKey] = cpos[9];
                }
                if (!isoSettings.Contains(SettingsPage.LLeftPKey))
                {
                    isoSettings[SettingsPage.LLeftPKey] = cpos[10];
                }
                if (!isoSettings.Contains(SettingsPage.LTopPKey))
                {
                    isoSettings[SettingsPage.LTopPKey] = cpos[11];
                }
                if (!isoSettings.Contains(SettingsPage.RRightPKey))
                {
                    isoSettings[SettingsPage.RRightPKey] = cpos[12];
                }
                if (!isoSettings.Contains(SettingsPage.RTopPKey))
                {
                    isoSettings[SettingsPage.RTopPKey] = cpos[13];
                }
                if (!isoSettings.Contains(SettingsPage.XCenterXPKey))
                {
                    isoSettings[SettingsPage.XCenterXPKey] = cpos[14];
                }
                if (!isoSettings.Contains(SettingsPage.XCenterYPKey))
                {
                    isoSettings[SettingsPage.XCenterYPKey] = cpos[15];
                }
                if (!isoSettings.Contains(SettingsPage.YCenterXPKey))
                {
                    isoSettings[SettingsPage.YCenterXPKey] = cpos[16];
                }
                if (!isoSettings.Contains(SettingsPage.YCenterYPKey))
                {
                    isoSettings[SettingsPage.YCenterYPKey] = cpos[17];
                }


                if (!isoSettings.Contains(SettingsPage.PadCenterXLKey))
                {
                    isoSettings[SettingsPage.PadCenterXLKey] = cpos[18];
                }
                if (!isoSettings.Contains(SettingsPage.PadCenterYLKey))
                {
                    isoSettings[SettingsPage.PadCenterYLKey] = cpos[19];
                }
                if (!isoSettings.Contains(SettingsPage.ACenterXLKey))
                {
                    isoSettings[SettingsPage.ACenterXLKey] = cpos[20];
                }
                if (!isoSettings.Contains(SettingsPage.ACenterYLKey))
                {
                    isoSettings[SettingsPage.ACenterYLKey] = cpos[21];
                }
                if (!isoSettings.Contains(SettingsPage.BCenterXLKey))
                {
                    isoSettings[SettingsPage.BCenterXLKey] = cpos[22];
                }
                if (!isoSettings.Contains(SettingsPage.BCenterYLKey))
                {
                    isoSettings[SettingsPage.BCenterYLKey] = cpos[23];
                }
                if (!isoSettings.Contains(SettingsPage.StartLeftLKey))
                {
                    isoSettings[SettingsPage.StartLeftLKey] = cpos[24];
                }
                if (!isoSettings.Contains(SettingsPage.StartTopLKey))
                {
                    isoSettings[SettingsPage.StartTopLKey] = cpos[25];
                }
                if (!isoSettings.Contains(SettingsPage.SelectRightLKey))
                {
                    isoSettings[SettingsPage.SelectRightLKey] = cpos[26];
                }
                if (!isoSettings.Contains(SettingsPage.SelectTopLKey))
                {
                    isoSettings[SettingsPage.SelectTopLKey] = cpos[27];
                }
                if (!isoSettings.Contains(SettingsPage.LLeftLKey))
                {
                    isoSettings[SettingsPage.LLeftLKey] = cpos[28];
                }
                if (!isoSettings.Contains(SettingsPage.LTopLKey))
                {
                    isoSettings[SettingsPage.LTopLKey] = cpos[29];
                }
                if (!isoSettings.Contains(SettingsPage.RRightLKey))
                {
                    isoSettings[SettingsPage.RRightLKey] = cpos[30];
                }
                if (!isoSettings.Contains(SettingsPage.RTopLKey))
                {
                    isoSettings[SettingsPage.RTopLKey] = cpos[31];
                }
                if (!isoSettings.Contains(SettingsPage.XCenterXLKey))
                {
                    isoSettings[SettingsPage.XCenterXLKey] = cpos[32];
                }
                if (!isoSettings.Contains(SettingsPage.XCenterYLKey))
                {
                    isoSettings[SettingsPage.XCenterYLKey] = cpos[33];
                }
                if (!isoSettings.Contains(SettingsPage.YCenterXLKey))
                {
                    isoSettings[SettingsPage.YCenterXLKey] = cpos[34];
                }
                if (!isoSettings.Contains(SettingsPage.YCenterYLKey))
                {
                    isoSettings[SettingsPage.YCenterYLKey] = cpos[35];
                }


                //moga mapping
                if (!isoSettings.Contains(SettingsPage.MogaAKey))
                {
                    isoSettings[SettingsPage.MogaAKey] = 2;
                }
                if (!isoSettings.Contains(SettingsPage.MogaBKey))
                {
                    isoSettings[SettingsPage.MogaBKey] = 1;
                }
                if (!isoSettings.Contains(SettingsPage.MogaXKey))
                {
                    isoSettings[SettingsPage.MogaXKey] = 8;
                }
                if (!isoSettings.Contains(SettingsPage.MogaYKey))
                {
                    isoSettings[SettingsPage.MogaYKey] = 4;
                }
                if (!isoSettings.Contains(SettingsPage.MogaL1Key))
                {
                    isoSettings[SettingsPage.MogaL1Key] = 16;
                }
                if (!isoSettings.Contains(SettingsPage.MogaL2Key))
                {
                    isoSettings[SettingsPage.MogaL2Key] = 16;
                }
                if (!isoSettings.Contains(SettingsPage.MogaR1Key))
                {
                    isoSettings[SettingsPage.MogaR1Key] = 32;
                }
                if (!isoSettings.Contains(SettingsPage.MogaR2Key))
                {
                    isoSettings[SettingsPage.MogaR2Key] = 32;
                }
                if (!isoSettings.Contains(SettingsPage.MogaLeftJoystickKey))
                {
                    isoSettings[SettingsPage.MogaLeftJoystickKey] = 64;
                }
                if (!isoSettings.Contains(SettingsPage.MogaRightJoystickKey))
                {
                    isoSettings[SettingsPage.MogaRightJoystickKey] = 64;
                }

                isoSettings.Save();

                settings.LowFrequencyMode = (bool)isoSettings[SettingsPage.LowFreqModeKey];
                settings.SoundEnabled = (bool)isoSettings[SettingsPage.EnableSoundKey];
                settings.VirtualControllerOnTop = (bool)isoSettings[SettingsPage.VControllerPosKey];
                //settings.LowFrequencyModeMeasured = (bool)isoSettings[SettingsPage.LowFreqModeMeasuredKey];
                settings.LargeVController = (bool)isoSettings[SettingsPage.VControllerSizeKey];
                settings.GrayVControllerButtons = (bool)isoSettings[SettingsPage.VControllerButtonStyleKey];
                settings.Orientation = (int)isoSettings[SettingsPage.OrientationKey];
                settings.FullscreenStretch = (bool)isoSettings[SettingsPage.StretchKey];
                settings.ControllerScale = (int)isoSettings[SettingsPage.ControllerScaleKey];
                settings.ButtonScale = (int)isoSettings[SettingsPage.ButtonScaleKey];
                settings.ControllerOpacity = (int)isoSettings[SettingsPage.OpacityKey];
                settings.FrameSkip = (int)isoSettings[SettingsPage.SkipFramesKey];
                settings.ImageScaling = (int)isoSettings[SettingsPage.ImageScalingKey];
                settings.TurboFrameSkip = (int)isoSettings[SettingsPage.TurboFrameSkipKey];
                settings.SynchronizeAudio = (bool)isoSettings[SettingsPage.SyncAudioKey];
                settings.PowerFrameSkip = (int)isoSettings[SettingsPage.PowerSaverKey];
                settings.DPadStyle = (int)isoSettings[SettingsPage.DPadStyleKey];
                settings.Deadzone = (float)isoSettings[SettingsPage.DeadzoneKey];
                settings.CameraButtonAssignment = (int)isoSettings[SettingsPage.CameraAssignKey];
                settings.AutoIncrementSavestates = (bool)isoSettings[SettingsPage.AutoIncKey];
                settings.HideConfirmationDialogs = (bool)isoSettings[SettingsPage.ConfirmationKey];
                settings.HideLoadConfirmationDialogs = (bool)isoSettings[SettingsPage.ConfirmationLoadKey];
                settings.SelectLastState = (bool)isoSettings[SettingsPage.SelectLastState];
                settings.ManualSnapshots = (bool)isoSettings[SettingsPage.CreateManualSnapshotKey];
                settings.UseMogaController = (bool)isoSettings[SettingsPage.UseMogaControllerKey];
                settings.ShouldShowAds = (bool)isoSettings[SettingsPage.ShouldShowAdsKey];
                settings.BgcolorR = (int)isoSettings[SettingsPage.BgcolorRKey];
                settings.BgcolorG = (int)isoSettings[SettingsPage.BgcolorGKey];
                settings.BgcolorB = (int)isoSettings[SettingsPage.BgcolorBKey];

                settings.PadCenterXP = (int)isoSettings[SettingsPage.PadCenterXPKey];
                settings.PadCenterYP = (int)isoSettings[SettingsPage.PadCenterYPKey];
                settings.ACenterXP = (int)isoSettings[SettingsPage.ACenterXPKey];
                settings.ACenterYP = (int)isoSettings[SettingsPage.ACenterYPKey];
                settings.BCenterXP = (int)isoSettings[SettingsPage.BCenterXPKey];
                settings.BCenterYP = (int)isoSettings[SettingsPage.BCenterYPKey];
                settings.StartLeftP = (int)isoSettings[SettingsPage.StartLeftPKey];
                settings.StartTopP = (int)isoSettings[SettingsPage.StartTopPKey];
                settings.SelectRightP = (int)isoSettings[SettingsPage.SelectRightPKey];
                settings.SelectTopP = (int)isoSettings[SettingsPage.SelectTopPKey];
                settings.LLeftP = (int)isoSettings[SettingsPage.LLeftPKey];
                settings.LTopP = (int)isoSettings[SettingsPage.LTopPKey];
                settings.RRightP = (int)isoSettings[SettingsPage.RRightPKey];
                settings.RTopP = (int)isoSettings[SettingsPage.RTopPKey];
                settings.XCenterXP = (int)isoSettings[SettingsPage.XCenterXPKey];
                settings.XCenterYP = (int)isoSettings[SettingsPage.XCenterYPKey];
                settings.YCenterXP = (int)isoSettings[SettingsPage.YCenterXPKey];
                settings.YCenterYP = (int)isoSettings[SettingsPage.YCenterYPKey];


                settings.PadCenterXL = (int)isoSettings[SettingsPage.PadCenterXLKey];
                settings.PadCenterYL = (int)isoSettings[SettingsPage.PadCenterYLKey];
                settings.ACenterXL = (int)isoSettings[SettingsPage.ACenterXLKey];
                settings.ACenterYL = (int)isoSettings[SettingsPage.ACenterYLKey];
                settings.BCenterXL = (int)isoSettings[SettingsPage.BCenterXLKey];
                settings.BCenterYL = (int)isoSettings[SettingsPage.BCenterYLKey];
                settings.StartLeftL = (int)isoSettings[SettingsPage.StartLeftLKey];
                settings.StartTopL = (int)isoSettings[SettingsPage.StartTopLKey];
                settings.SelectRightL = (int)isoSettings[SettingsPage.SelectRightLKey];
                settings.SelectTopL = (int)isoSettings[SettingsPage.SelectTopLKey];
                settings.LLeftL = (int)isoSettings[SettingsPage.LLeftLKey];
                settings.LTopL = (int)isoSettings[SettingsPage.LTopLKey];
                settings.RRightL = (int)isoSettings[SettingsPage.RRightLKey];
                settings.RTopL = (int)isoSettings[SettingsPage.RTopLKey];
                settings.XCenterXL = (int)isoSettings[SettingsPage.XCenterXLKey];
                settings.XCenterYL = (int)isoSettings[SettingsPage.XCenterYLKey];
                settings.YCenterXL = (int)isoSettings[SettingsPage.YCenterXLKey];
                settings.YCenterYL = (int)isoSettings[SettingsPage.YCenterYLKey];


                settings.MogaA = (int)isoSettings[SettingsPage.MogaAKey];
                settings.MogaB = (int)isoSettings[SettingsPage.MogaBKey];
                settings.MogaX = (int)isoSettings[SettingsPage.MogaXKey];
                settings.MogaY = (int)isoSettings[SettingsPage.MogaYKey];
                settings.MogaL1 = (int)isoSettings[SettingsPage.MogaL1Key];
                settings.MogaL2 = (int)isoSettings[SettingsPage.MogaL2Key];
                settings.MogaR1 = (int)isoSettings[SettingsPage.MogaR1Key];
                settings.MogaR2 = (int)isoSettings[SettingsPage.MogaR2Key];
                settings.MogaLeftJoystick = (int)isoSettings[SettingsPage.MogaLeftJoystickKey];
                settings.MogaRightJoystick = (int)isoSettings[SettingsPage.MogaRightJoystickKey];

                settings.SettingsChanged = this.SettingsChangedDelegate;

                //create ad control
                if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
                {
                    AdControl adControl = new AdControl();
                    LayoutRoot.Children.Add(adControl);
                    adControl.SetValue(Grid.RowProperty, 1);
                }
            }
        }

        private void SettingsChangedDelegate()
        {
            EmulatorSettings settings = EmulatorSettings.Current;
            IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;

            isoSettings[SettingsPage.EnableSoundKey] = settings.SoundEnabled;
            isoSettings[SettingsPage.VControllerPosKey] = settings.VirtualControllerOnTop;
            isoSettings[SettingsPage.LowFreqModeKey] = settings.LowFrequencyMode;
            //isoSettings[SettingsPage.LowFreqModeMeasuredKey] = settings.LowFrequencyModeMeasured;
            isoSettings[SettingsPage.VControllerSizeKey] = settings.LargeVController;
            isoSettings[SettingsPage.VControllerButtonStyleKey] = settings.GrayVControllerButtons;
            isoSettings[SettingsPage.OrientationKey] = settings.Orientation;
            isoSettings[SettingsPage.StretchKey] = settings.FullscreenStretch;
            isoSettings[SettingsPage.ControllerScaleKey] = settings.ControllerScale;
            isoSettings[SettingsPage.ButtonScaleKey] = settings.ButtonScale;
            isoSettings[SettingsPage.OpacityKey] = settings.ControllerOpacity;
            isoSettings[SettingsPage.ImageScalingKey] = settings.ImageScaling;
            isoSettings[SettingsPage.TurboFrameSkipKey] = settings.TurboFrameSkip;
            isoSettings[SettingsPage.SyncAudioKey] = settings.SynchronizeAudio;
            isoSettings[SettingsPage.PowerSaverKey] = settings.PowerFrameSkip;
            isoSettings[SettingsPage.SkipFramesKey] = settings.FrameSkip;
            isoSettings[SettingsPage.DPadStyleKey] = settings.DPadStyle;
            isoSettings[SettingsPage.DeadzoneKey] = settings.Deadzone;
            isoSettings[SettingsPage.CameraAssignKey] = settings.CameraButtonAssignment;
            isoSettings[SettingsPage.ConfirmationKey] = settings.HideConfirmationDialogs;
            isoSettings[SettingsPage.AutoIncKey] = settings.AutoIncrementSavestates;
            isoSettings[SettingsPage.ConfirmationLoadKey] = settings.HideLoadConfirmationDialogs;
            isoSettings[SettingsPage.SelectLastState] = settings.SelectLastState;
            isoSettings[SettingsPage.CreateManualSnapshotKey] = settings.ManualSnapshots;
            isoSettings[SettingsPage.UseMogaControllerKey] = settings.UseMogaController;
            isoSettings[SettingsPage.ShouldShowAdsKey] = settings.ShouldShowAds;
            isoSettings[SettingsPage.BgcolorRKey] = settings.BgcolorR;
            isoSettings[SettingsPage.BgcolorGKey] = settings.BgcolorG;
            isoSettings[SettingsPage.BgcolorBKey] = settings.BgcolorB;
            isoSettings.Save();
        }

        private void RefreshROMList()
        {
            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
            //IReadOnlyList<StorageFile> roms = await romFolder.GetFilesAsync();
            //IList<ROMEntry> romNames = new List<ROMEntry>(roms.Count);
            //foreach (var file in roms)
            //{
            //    romNames.Add(new ROMEntry() { Name = file.Name } );
            //}

            DataContext = this.db.GetLastPlayed();

            if (DataContext != null && App.metroSettings.ShowLastPlayedGame == true)
                lastRomGrid.Visibility = Visibility.Visible;
            else
                lastRomGrid.Visibility = Visibility.Collapsed;

            this.romList.ItemsSource = this.db.GetROMList();

            this.recentList.ItemsSource = this.db.GetRecentlyPlayed();
        }

        private void romList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.StartROMFromList(this.romList);
        }

        private void recentList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            this.StartROMFromList(this.recentList);
        }

        private async void StartROMFromList(ListBox list)
        {
            if (list.SelectedItem == null)
                return;

            ROMDBEntry entry = (ROMDBEntry)list.SelectedItem;
            list.SelectedItem = null;

            await StartROM(entry);
        }

        private async Task StartROM(ROMDBEntry entry)
        {
            EmulatorPage.currentROMEntry = entry;

            LoadROMParameter param = await FileHandler.GetROMFileToPlayAsync(entry.FileName);

            entry.LastPlayed = DateTime.Now;
            this.db.CommitChanges();

            PhoneApplicationService.Current.State["parameter"] = param;
            this.NavigationService.Navigate(new Uri("/EmulatorPage.xaml", UriKind.Relative));
        }

        private void InitAppBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsMenuEnabled = true;

            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

            var helpButton = new ApplicationBarMenuItem(AppResources.HelpButtonText);
            helpButton.Click += helpButton_Click;
            ApplicationBar.MenuItems.Add(helpButton);

            var aboutItem = new ApplicationBarMenuItem(AppResources.aboutText);
            aboutItem.Click += aboutItem_Click;
            ApplicationBar.MenuItems.Add(aboutItem);

            


            resumeButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/transport.play.png", UriKind.Relative))
            {
                Text = AppResources.ResumeButtonText,
                IsEnabled = false
            };
            resumeButton.Click += resumeButton_Click;
            ApplicationBar.Buttons.Add(resumeButton);

            


            var donateButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/appbar.gift.png", UriKind.Relative))
            {
                Text = AppResources.DonateText
            };
            donateButton.Click += donateButton_Click;
            ApplicationBar.Buttons.Add(donateButton);

            var reviewButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/social.like.png", UriKind.Relative))
            {
                Text = AppResources.ReviewText
            };
            reviewButton.Click += reviewButton_Click;
            ApplicationBar.Buttons.Add(reviewButton);

            var settingsButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/feature.settings.png", UriKind.Relative))
            {
                Text = AppResources.SettingsButtonText
            };
            settingsButton.Click += settingsButton_Click;
            ApplicationBar.Buttons.Add(settingsButton);
        }

        private void donateButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/DonatePage.xaml", UriKind.Relative));
        }

        private void reviewButton_Click(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();

            marketplaceReviewTask.Show();
        }

        void helpButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HelpPage.xaml", UriKind.Relative));
        }

        void aboutItem_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        void resumeButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/EmulatorPage.xaml", UriKind.Relative));
            this.romList.SelectedItem = null;
        }

        void settingsButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        void importButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/ImportPage.xaml", UriKind.Relative));
        }

        private async void DeleteMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            ListBox list = this.romList;
            await DeleteListEntry(sender, list);
        }

        private async Task DeleteListEntry(object sender, ListBox list)
        {
            ListBoxItem contextMenuListItem = list.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            ROMDBEntry re = contextMenuListItem.DataContext as ROMDBEntry;
            try
            {
                await FileHandler.DeleteROMAsync(re);
                this.RefreshROMList();
            }
            catch (System.IO.FileNotFoundException ex)
            { }
        }

        private void RenameMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            ListBox list = this.romList;
            RenameListEntry(sender, list);
        }

        private void RenameListEntry(object sender, ListBox list)
        {
            ListBoxItem contextMenuListItem = list.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            ROMDBEntry re = contextMenuListItem.DataContext as ROMDBEntry;
            InputPrompt prompt = new InputPrompt();
            prompt.Completed += (o, e2) =>
            {
                if (e2.PopUpResult == PopUpResult.Ok)
                {
                    if (String.IsNullOrWhiteSpace(e2.Result))
                    {
                        MessageBox.Show(AppResources.RenameEmptyString, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }
                    else
                    {
                        if (e2.Result.ToLower().Equals(re.DisplayName.ToLower()))
                        {
                            return;
                        }
                        if (this.db.IsDisplayNameUnique(e2.Result))
                        {
                            re.DisplayName = e2.Result;
                            this.db.CommitChanges();
                            FileHandler.UpdateROMTile(re.FileName);
                        }
                        else
                        {
                            MessageBox.Show(AppResources.RenameNameAlreadyExisting, AppResources.ErrorCaption, MessageBoxButton.OK);
                        }
                    }
                }
            };
            prompt.Title = AppResources.RenamePromptTitle;
            prompt.Message = AppResources.RenamePromptMessage;
            prompt.Value = re.DisplayName;
            prompt.Show();
        }

        private void gotoBackupButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (App.session != null)
            {
                PhoneApplicationService.Current.State["parameter"] = App.session;
                BackupPage.backupMedium = "onedrive";
                this.NavigationService.Navigate(new Uri("/BackupPage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.NotSignedInError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }

 
        private void TextBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("http://www.youtube.com/watch?v=YfqzZhcr__o");
            wbtask.Show();
        }

        private void TextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("http://www.youtube.com/watch?v=3WopTRM4ets");
            wbtask.Show();
        }

        private void TextBlock_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("http://forums.wpcentral.com/showthread.php?t=252987");
            wbtask.Show();
        }

        private void contactBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailcomposer = new EmailComposeTask();

            emailcomposer.To = AppResources.AboutContact;
            emailcomposer.Subject = AppResources.EmailSubjectText;
            emailcomposer.Body = String.Format(AppResources.EmailBodyText, Microsoft.Phone.Info.DeviceStatus.DeviceName);
            emailcomposer.Show();
        }

        private void TextBlock_Tap_3(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HelpPage.xaml", UriKind.Relative));
        }

        private void TextBlock_Tap_4(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HelpPage.xaml?index=1", UriKind.Relative));
        }

        private void TextBlock_Tap_5(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/HelpPage.xaml?index=2", UriKind.Relative));
        }

        private async void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ROMDBEntry entry = (ROMDBEntry)this.lastRomImage.DataContext;
            await StartROM(entry);
        }




        private void pinBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {

            try
            {
                var menuItem = sender as RadContextMenuItem;
                var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;
                ROMDBEntry entry = fe.DataContext as ROMDBEntry;


                FileHandler.CreateROMTile(entry);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(AppResources.MaximumTilesPinned);
            }
 
        }


        private void renameBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {
            var menuItem = sender as RadContextMenuItem;
            var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;
            ROMDBEntry entry = fe.DataContext as ROMDBEntry;

            PhoneApplicationService.Current.State["parameter"] = entry;
            PhoneApplicationService.Current.State["parameter2"] = ROMDatabase.Current;

            this.NavigationService.Navigate(new Uri("/RenamePage.xaml", UriKind.Relative));
        }


        private void cheatBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {

            var menuItem = sender as RadContextMenuItem;
            var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;
            ROMDBEntry entry = fe.DataContext as ROMDBEntry;

            PhoneApplicationService.Current.State["parameter"] = entry;
            this.NavigationService.Navigate(new Uri("/CheatPage.xaml", UriKind.Relative));

        }

        private void deleteManageBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {
            var menuItem = sender as RadContextMenuItem;
            var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;
            ROMDBEntry entry = fe.DataContext as ROMDBEntry;

            PhoneApplicationService.Current.State["parameter"] = entry;
            this.NavigationService.Navigate(new Uri("/ManageSavestatePage.xaml", UriKind.Relative));
        }

        private async void deleteSavesBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.DeleteConfirmText, AppResources.DeleteConfirmTitle, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;


            var menuItem = sender as RadContextMenuItem;
            var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;

            ROMDBEntry entry = fe.DataContext as ROMDBEntry;

            await FileHandler.DeleteSRAMFile(entry);

            //CustomMessageBox msgbox = new CustomMessageBox();
            //msgbox.Background = (SolidColorBrush)App.Current.Resources["PhoneChromeBrush"];
            //msgbox.Foreground = (SolidColorBrush)App.Current.Resources["PhoneForegroundBrush"];
            //msgbox.Message = AppResources.SRAMDeletedSuccessfully;
            //msgbox.Caption = AppResources.InfoCaption;
            //msgbox.LeftButtonContent = "OK";
            //msgbox.Show();
            MessageBox.Show(AppResources.SRAMDeletedSuccessfully, AppResources.InfoCaption, MessageBoxButton.OK);
        }

        private async void deleteBlock_Tap(object sender, ContextMenuItemSelectedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show(AppResources.DeleteConfirmText, AppResources.DeleteConfirmTitle, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                var menuItem = sender as RadContextMenuItem;
                var fe = VisualTreeHelper.GetParent(menuItem) as FrameworkElement;

                ROMDBEntry entry = fe.DataContext as ROMDBEntry;
                await FileHandler.DeleteROMAsync(entry);

                //update voice command list
                await MainPage.UpdateGameListForVoiceCommand();

                this.RefreshRecentROMList();

            }
            catch (System.IO.FileNotFoundException)
            { }
        }

        private void contactBlock_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("https://twitter.com/wp8emu");
            wbtask.Show();

        }

        private void romList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            this.StartROMFromList(this.romList);
        }

        private void recentList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.StartROMFromList(this.recentList);
        }


        private void PinToStartMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            PinToStart(sender, this.romList);
        }

        private static void PinToStart(object sender, ListBox list)
        {
            ListBoxItem contextMenuListItem = list.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            ROMDBEntry re = contextMenuListItem.DataContext as ROMDBEntry;

            try
            {
                FileHandler.CreateROMTile(re);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(AppResources.MaximumTilesPinned);
            }
        }

        private void DeleteSaveMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            ListBoxItem contextMenuListItem = this.romList.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            ROMDBEntry re = contextMenuListItem.DataContext as ROMDBEntry;

            FileHandler.DeleteSRAMFile(re);
        }

        private void SaveStateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem contextMenuListItem = this.romList.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            ROMDBEntry re = contextMenuListItem.DataContext as ROMDBEntry;

            PhoneApplicationService.Current.State["parameter"] = re;
            this.NavigationService.Navigate(new Uri("/ManageSavestatePage.xaml", UriKind.Relative));
        }

        private void ImportSD_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SDCardImportPage.xaml", UriKind.Relative));
        }


        private void RefreshRecentROMList()
        {


            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
            //IReadOnlyList<StorageFile> roms = await romFolder.GetFilesAsync();
            //IList<ROMEntry> romNames = new List<ROMEntry>(roms.Count);
            //foreach (var file in roms)
            //{
            //    romNames.Add(new ROMEntry() { Name = file.Name } );
            //}
            this.lastRomImage.DataContext = ROMDatabase.Current.GetLastPlayed();

            if (this.lastRomImage.DataContext != null)
                this.resumeButton.IsEnabled = true;
            else
                this.resumeButton.IsEnabled = false;

            if (this.lastRomImage.DataContext != null && App.metroSettings.ShowLastPlayedGame == true)
                lastRomGrid.Visibility = Visibility.Visible;
            else
                lastRomGrid.Visibility = Visibility.Collapsed;

            //this.romList.ItemsSource = ROMDatabase.Current.GetROMList();

            this.recentList.ItemsSource = ROMDatabase.Current.GetRecentlyPlayed();

        }

        private void CloudSixImportButton_Click(object sender, RoutedEventArgs e)
        {
            var launcher = new CloudSixConnector.FilePicker.CloudSixPicker("cloudsix2snes8x");

            launcher.Token = "FromCloudSix";
            launcher.Caption = "NEED TO CHANGE, .zip, .rar, .7z";

            launcher.FileExtensions.Add(new CloudSixFileExtension() { Extension = "zip" });
            launcher.FileExtensions.Add(new CloudSixFileExtension() { Extension = "zib" });
            launcher.FileExtensions.Add(new CloudSixFileExtension() { Extension = "rar" });
            launcher.FileExtensions.Add(new CloudSixFileExtension() { Extension = "7z" });
            launcher.Show();

        }

        private void CloudSixExportButton_Click(object sender, RoutedEventArgs e)
        {
            BackupPage.backupMedium = "cloudsix";
            this.NavigationService.Navigate(new Uri("/BackupPage.xaml", UriKind.Relative));

            //var saver = new CloudSixSaver("df.dd", )
        }


    


    }  //end main class

    class ROMEntry
    {
        public String Name { get; set; }
    }

    [DataContract]
    public class LoadROMParameter
    {
        public StorageFile file { get; set; }
        public StorageFolder folder { get; set; }
        [DataMember]
        public string RomFileName { get; set; } //store this information so that we can store in State in case the app is tombstoned
    }

    // add these 3 lines  
    public class TiltableGrid : Grid
    {
    }

    public class TiltableCanvas : Canvas
    {
    }  
}