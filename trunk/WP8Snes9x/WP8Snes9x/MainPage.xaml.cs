using System;
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
using Windows.Storage;
using System.Threading.Tasks;
using PhoneDirect3DXamlAppComponent;
using PhoneDirect3DXamlAppInterop.Database;
using Coding4Fun.Phone.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Phone.Tasks;

//"C:\Program Files (x86)\Microsoft SDKs\Windows Phone\v8.0\Tools\IsolatedStorageExplorerTool\ISETool.exe" ts xd e6470260-50e5-4b4b-aaac-223ee7485237 "D:\Duc\Documents\Visual Studio 2012\Projects\WP8Snes8x"


namespace PhoneDirect3DXamlAppInterop
{
    class ROMEntry
    {
        public String Name { get; set; }
    }

    class LoadROMParameter
    {
        public StorageFile file;
        public StorageFolder folder;
    }

    public partial class MainPage : PhoneApplicationPage
    {
        private ApplicationBarIconButton resumeButton;
        private LiveConnectSession session;
        private ROMDatabase db;
        private Task createFolderTask, copyDemoTask, initTask;

        public MainPage()
        {
            InitializeComponent();

            

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

        void btnSignin_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                this.session = e.Session;
                //this.statusLabel.Text = AppResources.StatusSignedIn;
                this.gotoImportButton.IsEnabled = true;
                this.gotoBackupButton.IsEnabled = true;
                this.gotoRestoreButton.IsEnabled = true;
            }
            else
            {
                this.gotoImportButton.IsEnabled = false;
                this.gotoBackupButton.IsEnabled = false;
                this.gotoRestoreButton.IsEnabled = false;
                //this.statusLabel.Text = AppResources.StatusNotSignedIn;
                session = null;

                //if (e.Error != null)
                //{
                //    MessageBox.Show(String.Format(AppResources.SkyDriveError, e.Error.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                //    //statusLabel.Text = e.Error.ToString();
                //}
            }
        }

        private void gotoImportButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (session != null)
            {
                PhoneApplicationService.Current.State["parameter"] = this.session;
                this.NavigationService.Navigate(new Uri("/SkyDriveImportPage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.NotSignedInError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //await this.createFolderTask;
            //await this.copyDemoTask;
            await this.initTask;

            this.LoadInitialSettings();

            this.RefreshROMList();

            this.resumeButton.IsEnabled = EmulatorPage.ROMLoaded;
            
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FileHandler.UpdateLiveTile();

            base.OnNavigatedFrom(e);
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
            this.romList.ItemsSource = this.db.GetROMList();// romNames;
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

            var settingsButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/feature.settings.png", UriKind.Relative))
            {
                Text = AppResources.SettingsButtonText
            };
            settingsButton.Click += settingsButton_Click;
            ApplicationBar.Buttons.Add(settingsButton);


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
            if (session != null)
            {
                PhoneApplicationService.Current.State["parameter"] = this.session;
                this.NavigationService.Navigate(new Uri("/BackupPage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.NotSignedInError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }

        private void gotoRestoreButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (session != null)
            {
                PhoneApplicationService.Current.State["parameter"] = this.session;
                this.NavigationService.Navigate(new Uri("/RestorePage.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(AppResources.NotSignedInError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
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
            wbtask.Uri = new Uri("http://forums.wpcentral.com/windows-phone-apps/252987-trio-nintendo-emulators-new-tutorial-using-cheat-codes.html");
            wbtask.Show();
        }

        private void contactBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailcomposer = new EmailComposeTask();

            emailcomposer.To = AppResources.AboutContact;
            emailcomposer.Subject = "bug report or feature suggestion";
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
    }
}