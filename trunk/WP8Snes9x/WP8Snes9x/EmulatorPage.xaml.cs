using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using PhoneDirect3DXamlAppComponent;
using Windows.UI.Core;
using PhoneDirect3DXamlAppInterop.Resources;
using Windows.Storage;
using PhoneDirect3DXamlAppInterop.Database;
using Microsoft.Devices;
using Microsoft.Phone.Tasks;
using System.Windows.Controls.Primitives;
using Coding4Fun.Toolkit.Controls;
using System.IO.IsolatedStorage;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class EmulatorPage : PhoneApplicationPage
    {
        public Popup popupWindow = null;

        public static bool ROMLoaded = false;
        private Direct3DBackground m_d3dBackground = null;
        private static LoadROMParameter cache = null;
        //private bool initialized = false;
        private bool confirmPopupOpened = false;
        bool wasHalfPressed = false;
        private ApplicationBarMenuItem[] menuItems;
        private String[] menuItemLabels;
        public static ROMDBEntry currentROMEntry;
        public static bool IsTombstoned = false;
        private bool RestoreSaveStateAfterTombstoned = false;

        // Constructor
        public EmulatorPage()
        {
            InitializeComponent();
            //this.BackKeyPress += EmulatorPage_BackKeyPress;
            this.OrientationChanged += EmulatorPage_OrientationChanged;
            

            
            switch (EmulatorSettings.Current.Orientation)
            {
                case 0:
                    this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                    break;
                case 1:
                    this.SupportedOrientations = SupportedPageOrientation.Landscape;
                    break;
                case 2:
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                    break;
            }
            
        }

        private void InitAppBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = false;
            ApplicationBar.IsMenuEnabled = true;
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

            var item0 = new ApplicationBarMenuItem(AppResources.SelectState0);
            item0.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(0); };
            var item1 = new ApplicationBarMenuItem(AppResources.SelectState1);
            item1.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(1); };
            var item2 = new ApplicationBarMenuItem(AppResources.SelectState2);
            item2.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(2); };
            var item3 = new ApplicationBarMenuItem(AppResources.SelectState3);
            item3.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(3); };
            var item4 = new ApplicationBarMenuItem(AppResources.SelectState4);
            item4.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(4); };
            var item5 = new ApplicationBarMenuItem(AppResources.SelectState5);
            item5.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(5); };
            var item6 = new ApplicationBarMenuItem(AppResources.SelectState6);
            item6.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(6); };
            var item7 = new ApplicationBarMenuItem(AppResources.SelectState7);
            item7.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(7); };
            var item8 = new ApplicationBarMenuItem(AppResources.SelectState8);
            item8.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(8); };
            var itemA = new ApplicationBarMenuItem(AppResources.SelectStateAuto);
            itemA.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(9); };

            if (EmulatorSettings.Current.ManualSnapshots)
            {
                var itemSnapshot = new ApplicationBarMenuItem(AppResources.CreateSnapshotMenuItem);
                itemSnapshot.Click += (o, e) => { this.m_d3dBackground.TriggerSnapshot(); };

                this.menuItems = new ApplicationBarMenuItem[] 
                {
                    itemSnapshot,
                    item0, item1, item2, item3, item4, 
                    item5, item6, item7, item8, itemA
                };

                this.menuItemLabels = new String[]
                {
                    AppResources.CreateSnapshotMenuItem,
                    AppResources.SelectState0, AppResources.SelectState1, AppResources.SelectState2, AppResources.SelectState3,
                    AppResources.SelectState4, AppResources.SelectState5, AppResources.SelectState6, AppResources.SelectState7,
                    AppResources.SelectState8, AppResources.SelectStateAuto
                };
            }
            else
            {
                this.menuItems = new ApplicationBarMenuItem[] 
                {
                    item0, item1, item2, item3, item4, 
                    item5, item6, item7, item8, itemA
                };

                    this.menuItemLabels = new String[]
                {
                    AppResources.SelectState0, AppResources.SelectState1, AppResources.SelectState2, AppResources.SelectState3,
                    AppResources.SelectState4, AppResources.SelectState5, AppResources.SelectState6, AppResources.SelectState7,
                    AppResources.SelectState8, AppResources.SelectStateAuto
                };
            }

            int offset = EmulatorSettings.Current.ManualSnapshots ? 1 : 0;
            this.menuItems[this.m_d3dBackground.SelectedSavestateSlot + offset].Text = this.menuItemLabels[this.m_d3dBackground.SelectedSavestateSlot + offset] + AppResources.ActiveSavestateText;

            foreach (var item in menuItems)
            {
                ApplicationBar.MenuItems.Add(item);
            }




            var loadstateButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/open.png", UriKind.Relative))
            {
                Text = AppResources.LoadStateButton
            };
            loadstateButton.Click += loadstateButton_Click;
            ApplicationBar.Buttons.Add(loadstateButton);


            var resetButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/refresh.png", UriKind.Relative))
            {
                Text = AppResources.ResetROMButton
            };
            resetButton.Click += (o, e) => { this.resetButton_Click(); };
			ApplicationBar.Buttons.Add(resetButton);
            var savestateButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/save.png", UriKind.Relative))
            {
                Text = AppResources.SaveStateButton
            };
            savestateButton.Click += savestateButton_Click;
			ApplicationBar.Buttons.Add(savestateButton);


            var configButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/feature.settings.png", UriKind.Relative))
            {
                Text = AppResources.SettingsButtonText
            };
            configButton.Click += configButton_Click;
            ApplicationBar.Buttons.Add(configButton); 

            
            

        }

        private void configButton_Click(object sender, EventArgs e)
        {
            ApplicationBar.IsVisible = false;
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void savestateSelected(int slot, int oldSlot)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                int offset = EmulatorSettings.Current.ManualSnapshots ? 1 : 0;
                this.menuItems[oldSlot + offset].Text = this.menuItemLabels[oldSlot + offset];
                this.menuItems[slot + offset].Text = this.menuItemLabels[slot + offset] + AppResources.ActiveSavestateText;
            }));
        }

        void ContinueEmulation()
        {
            if (this.ApplicationBar != null)
            {
                this.ApplicationBar.IsVisible = false;
            }
        }

        void resetButton_Click()
        {
            var result = MessageBox.Show(AppResources.ConfirmResetText, AppResources.InfoCaption, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                this.m_d3dBackground.Reset();
            }
        }


        void loadstateButton_Click(object sender, EventArgs e)
        {

            if (EmulatorSettings.Current.HideLoadConfirmationDialogs)
            {
                //ROMDatabase db = ROMDatabase.Current;
                //var entry = db.GetROM(this.m_d3dBackground.LoadadROMFile.Name);
                //var cheats = await FileHandler.LoadCheatCodes(entry);
                //this.m_d3dBackground.LoadCheatsOnROMLoad(cheats);

                this.m_d3dBackground.LoadState(-1);
            }
            else
            {
                this.ShowLoadDialog();
            }

        }

        void ShowLoadDialog()
        {
            var result = MessageBox.Show(AppResources.ConfirmLoadText, AppResources.InfoCaption, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                this.m_d3dBackground.LoadState(-1);
            }
        }

        void savestateButton_Click(object sender, EventArgs e)
        {
            if (m_d3dBackground.GetCurrentSaveSlot() == 9) //auto save slot
            {
                MessageBox.Show(AppResources.SaveSlotReservedText);
            }
            else
            {
            if (EmulatorSettings.Current.HideConfirmationDialogs)
            {
                this.m_d3dBackground.SaveState();
            }
            else
            {
                ShowSaveDialog();
            }
        }
        }

        void ShowSaveDialog()
        {
            var result = MessageBox.Show(AppResources.ConfirmSaveText, AppResources.InfoCaption, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                this.m_d3dBackground.SaveState();
            }
        }


        

        void EmulatorPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (this.m_d3dBackground != null)
            {
                int orientation = 0;
                switch (e.Orientation)
                {
                    case PageOrientation.LandscapeLeft:
                    case PageOrientation.Landscape:
                        orientation = 0;
                        break;
                    case PageOrientation.LandscapeRight:
                        orientation = 1;
                        break;
                    case PageOrientation.PortraitUp:
                    case PageOrientation.Portrait:
                        orientation = 2;
                        break;
                }
                this.m_d3dBackground.ChangeOrientation(orientation);
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!confirmPopupOpened)
            {
                if (m_d3dBackground == null || m_d3dBackground.IsROMLoaded() == false)
                {
                    MessageBox.Show(AppResources.PleaseWaitROMLoadingText);
                    e.Cancel = true;
                }
                else if (popupWindow != null && popupWindow.IsOpen)
                {
                    //Close the PopUp Window
                    popupWindow.IsOpen = false;

                    //Keep the back button from navigating away from the current page
                    e.Cancel = true;
                }

                else if (!this.ApplicationBar.IsVisible) //if app bar is not visible, cancel the back action and show app bar
                {
                    e.Cancel = true;
                    this.ChangeAppBarVisibility(!this.ApplicationBar.IsVisible);
                }
                else
                    base.OnBackKeyPress(e);
            }

            
        }

 
        void ChangeAppBarVisibility(bool visible)
        {
            this.ApplicationBar.IsVisible = visible;
            this.ApplicationBar.Mode = ApplicationBarMode.Default;
            if (visible)
            {
                this.m_d3dBackground.PauseEmulation();
            }
            else
            {
                this.m_d3dBackground.UnpauseEmulation();
            }
        }

        protected override void OnTap(System.Windows.Input.GestureEventArgs e)
        {
            if (this.ApplicationBar.IsVisible)
            {
                this.ApplicationBar.IsVisible = false;
                this.m_d3dBackground.UnpauseEmulation();
                base.OnTap(e);
            }
        }
        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {


            //disable lock screen
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

            CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;
            CameraButtons.ShutterKeyReleased += CameraButtons_ShutterKeyReleased;

            object param = null;
            PhoneApplicationService.Current.State.TryGetValue("parameter", out param);
            LoadROMParameter romInfo = param as LoadROMParameter;

            PhoneApplicationService.Current.State.Remove("parameter");


            ROMDatabase db = ROMDatabase.Current;

            if (romInfo != null)
            {
                EmulatorPage.cache = romInfo;

            }
            else if (IsTombstoned) //return after tombstone, need to restore state
            {
                romInfo = (LoadROMParameter)State["LoadROMParameter"];
                romInfo = await FileHandler.GetROMFileToPlayAsync(romInfo.RomFileName);

                //load all information again
                EmulatorPage.cache = romInfo;
                EmulatorPage.currentROMEntry = ROMDatabase.Current.GetROM(romInfo.RomFileName);
                MainPage.LoadInitialSettings();

                //set IsTombstoned to false
                IsTombstoned = false;
                RestoreSaveStateAfterTombstoned = true;
            }

            

            base.OnNavigatedTo(e);
        }

        void ToggleTurboMode()
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //change turbo mode and save
                EmulatorSettings.Current.UseTurbo = !(bool)IsolatedStorageSettings.ApplicationSettings[SettingsPage.UseTurboKey];
                IsolatedStorageSettings.ApplicationSettings[SettingsPage.UseTurboKey] = EmulatorSettings.Current.UseTurbo;
            }));
        }

        private void CameraButtons_ShutterKeyReleased(object sender, EventArgs e)
        {
            if (this.m_d3dBackground != null)
            {   //if the camera button was half pressed, we stop the toggle for both cases
                //if the camera button was full pressed, we stop the toggle only when the assignment is not turbo mode and it is not sticky
                if (EmulatorSettings.Current.CameraButtonAssignment == 0)
                {
                    if (wasHalfPressed)
                    {
                        EmulatorSettings.Current.UseTurbo = false;
                    }
                }
                else
                {
                    if (wasHalfPressed || EmulatorSettings.Current.FullPressStickABLR == false)
                    {
                        this.m_d3dBackground.StopCameraPress();

                    }
                }

                wasHalfPressed = false;

            }
        }

        private void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (this.m_d3dBackground != null)
            {
                wasHalfPressed = true;

                if (EmulatorSettings.Current.CameraButtonAssignment == 0)
                    EmulatorSettings.Current.UseTurbo = true;
                else
                    this.m_d3dBackground.StartCameraPress();
            }
        }

        private void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (EmulatorSettings.Current.CameraButtonAssignment == 0)
            {   // Turbo button 

                if (this.m_d3dBackground != null)
                {
                    //change turbo mode and save
                    EmulatorSettings.Current.UseTurbo = !(bool)IsolatedStorageSettings.ApplicationSettings[SettingsPage.UseTurboKey];
                    IsolatedStorageSettings.ApplicationSettings[SettingsPage.UseTurboKey] = EmulatorSettings.Current.UseTurbo;

                    wasHalfPressed = false;
                }

            }
            else
            {
                if (EmulatorSettings.Current.FullPressStickABLR == true) //button stick is on
                {
                    if (this.m_d3dBackground != null)
                    {
                        if (!wasHalfPressed)
                        {
                            this.m_d3dBackground.ToggleCameraPress();
                        }
                        wasHalfPressed = false;
                    }
                }
                else
                {   // A/B/L/R button and not stick
                    if (this.m_d3dBackground != null)
                    {
                        this.m_d3dBackground.StartCameraPress();
                        wasHalfPressed = false;
                    }
                }
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            //enable lock screen
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;

            try
            {
                CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;
                CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;
                CameraButtons.ShutterKeyReleased -= CameraButtons_ShutterKeyReleased;
            }
            catch (Exception) { }



            //if (initialized && this.m_d3dBackground.IsROMLoaded())
            //if ( this.m_d3dBackground.IsROMLoaded())
            //{
            //    this.m_d3dBackground.PauseEmulation();
            //}

            if (e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                // Save the ViewModel variable in the page's State dictionary.
                State["LoadROMParameter"] = EmulatorPage.cache;
            }

            if (this.m_d3dBackground == null || this.m_d3dBackground.LoadadROMFile == null)
            {
                base.OnNavigatingFrom(e);
            }
            else
            {
                ROMDatabase db = ROMDatabase.Current;
                var entry = db.GetROM(this.m_d3dBackground.LoadadROMFile.Name);
                if (entry != null)
                {
                    entry.LastPlayed = DateTime.Now;
                    db.CommitChanges();
                    MainPage.shouldRefreshRecentROMList = true; //signal main page to update rom list
                }
                base.OnNavigatingFrom(e);
            }
        }

        private async void DrawingSurfaceBackground_Loaded(object sender, RoutedEventArgs e)
        {
             if (m_d3dBackground == null)
            {
                this.m_d3dBackground = new Direct3DBackground();

                this.m_d3dBackground.SetContinueNotifier(this.ContinueEmulation);
                this.m_d3dBackground.SnapshotAvailable = FileHandler.CaptureSnapshot;
                this.m_d3dBackground.SavestateCreated = FileHandler.CreateSavestate;
                this.m_d3dBackground.SavestateSelected = this.savestateSelected;
                //Direct3DBackground.WrongCheatVersion = this.wrongCheatVersion;
                Direct3DBackground.ToggleTurboMode = this.ToggleTurboMode;

                this.InitAppBar();

                // Set window bounds in dips
                m_d3dBackground.WindowBounds = new Windows.Foundation.Size(
                    (float)Application.Current.Host.Content.ActualWidth,
                    (float)Application.Current.Host.Content.ActualHeight
                    );

                // Set native resolution in pixels
                m_d3dBackground.NativeResolution = new Windows.Foundation.Size(
                    (float)Math.Floor(Application.Current.Host.Content.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                    (float)Math.Floor(Application.Current.Host.Content.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                    );

                // Set render resolution to the full native resolution
                m_d3dBackground.RenderResolution = m_d3dBackground.NativeResolution;

                // Hook-up native component to DrawingSurfaceBackgroundGrid
                DrawingSurfaceBackground.SetBackgroundContentProvider(m_d3dBackground.CreateContentProvider());
                DrawingSurfaceBackground.SetBackgroundManipulationHandler(m_d3dBackground);
            }

            //this.initialized = true;

            ROMDatabase db = ROMDatabase.Current;

            //if (ROMLoaded && this.cache == null)  //this never happens so just get rid of it
            //{
            //    var entry = db.GetROM(this.m_d3dBackground.LoadadROMFile.Name);
            //    var cheats = await FileHandler.LoadCheatCodes(entry);
            //    this.m_d3dBackground.LoadCheats(cheats);

            //    this.m_d3dBackground.UnpauseEmulation();
            //}


            if (EmulatorPage.cache != null && EmulatorPage.cache.file != null && EmulatorPage.cache.folder != null) // a safeguard to make sure we have enough info to load ROM
                                                                                                                    //this is all null if returned from tombstone
            {
                if (ROMLoaded && this.m_d3dBackground.LoadadROMFile.Name.Equals(EmulatorPage.cache.file.Name))  //name match, we are resuming to current game
                {
                    var entry = db.GetROM(this.m_d3dBackground.LoadadROMFile.Name);
                    //var cheats = await FileHandler.LoadCheatCodes(entry);
                    //this.m_d3dBackground.LoadCheats(cheats);

                    //this.m_d3dBackground.UnpauseEmulation();
                }
                else  //name does not match or ROM is not loaded, we are loading a new rom
                {
                    var entry = db.GetROM(EmulatorPage.cache.file.Name);
                    //var cheats = await FileHandler.LoadCheatCodes(entry);
                    //this.m_d3dBackground.LoadCheatsOnROMLoad(cheats);

                    // Load new ROM

                    await this.m_d3dBackground.LoadROMAsync(EmulatorPage.cache.file, EmulatorPage.cache.folder);
                    //if (EmulatorSettings.Current.SelectLastState)
                    {
                        RestoreLastSavestate(EmulatorPage.cache.file.Name);
                    }

                    ROMLoaded = true;
                }
                

                int orientation = 0;
                switch (this.Orientation)
                {
                    case PageOrientation.LandscapeLeft:
                    case PageOrientation.Landscape:
                        orientation = 0;
                        break;
                    case PageOrientation.LandscapeRight:
                        orientation = 1;
                        break;
                    case PageOrientation.PortraitUp:
                    case PageOrientation.Portrait:
                        orientation = 2;
                        break;
                }
                this.m_d3dBackground.ChangeOrientation(orientation);
            }

            //set app bar color in case returning from setting page
            if (ApplicationBar != null)
            {
                ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
                ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];
            }
        }



        private void RestoreLastSavestate(string filename)
        {
            ROMDatabase db = ROMDatabase.Current;
            int slot = db.GetLastSavestateSlotByFileNameExceptAuto(filename);
            m_d3dBackground.SelectSaveState(slot);

            if (RestoreSaveStateAfterTombstoned ) //restore auto save state no matter what
            {
                m_d3dBackground.LoadState(9); //load from auto save slot
                RestoreSaveStateAfterTombstoned = false;
        }
            else if (currentROMEntry.SuspendAutoLoadLastState == false)  //general this is true, except after importing saves
            {
                if (EmulatorSettings.Current.AutoSaveLoad)
                {
                    slot = db.GetLastSavestateSlotByFileNameIncludingAuto(filename);
                    m_d3dBackground.LoadState(slot);
    }
            }
            else
                currentROMEntry.SuspendAutoLoadLastState = false; //so that next time it autoload

        }
    }
}