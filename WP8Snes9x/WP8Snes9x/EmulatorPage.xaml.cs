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
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Tasks;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class EmulatorPage : PhoneApplicationPage
    {
        public static bool ROMLoaded = false;
        private Direct3DBackground m_d3dBackground = null;
        private LoadROMParameter cache = null;
        private bool initialized = false;
        private bool confirmPopupOpened = false;
        bool wasHalfPressed = false;
        private ApplicationBarMenuItem[] menuItems;
        private String[] menuItemLabels;

        // Constructor
        public EmulatorPage()
        {
            InitializeComponent();
            this.BackKeyPress += EmulatorPage_BackKeyPress;
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
            var item9 = new ApplicationBarMenuItem(AppResources.SelectState9);
            item9.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(9); };
            var itemA = new ApplicationBarMenuItem(AppResources.SelectStateAuto);
            itemA.Click += (o, e) => { this.m_d3dBackground.SelectSaveState(10); };

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

            //ApplicationBar.MenuItems.Add(item0);
            //ApplicationBar.MenuItems.Add(item1);
            //ApplicationBar.MenuItems.Add(item2);
            //ApplicationBar.MenuItems.Add(item3);
            //ApplicationBar.MenuItems.Add(item4);
            //ApplicationBar.MenuItems.Add(item5);
            //ApplicationBar.MenuItems.Add(item6);
            //ApplicationBar.MenuItems.Add(item7);
            //ApplicationBar.MenuItems.Add(item8);
            //ApplicationBar.MenuItems.Add(item9);
            //ApplicationBar.MenuItems.Add(itemA);

            //var resumeButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/transport.play.png", UriKind.Relative))
            //{
            //    Text = AppResources.ResumeButtonText
            //};
            //resumeButton.Click += resumeButton_Click;
            //ApplicationBar.Buttons.Add(resumeButton);

            var backButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/back.png", UriKind.Relative))
            {
                Text = AppResources.EmulatorBackIcon
            };
            backButton.Click += backbutton_click;

            var resetButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/refresh.png", UriKind.Relative))
            {
                Text = AppResources.ResetROMButton
            };
            resetButton.Click += resetButton_Click;

            var savestateButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/save.png", UriKind.Relative))
            {
                Text = AppResources.SaveStateButton
            };
            savestateButton.Click += savestateButton_Click;

            var loadstateButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/open.png", UriKind.Relative))
            {
                Text = AppResources.LoadStateButton
            };
            loadstateButton.Click += loadstateButton_Click;

            var configButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/feature.settings.png", UriKind.Relative))
            {
                Text = AppResources.SettingsButtonText
            };
            configButton.Click += configButton_Click;

            ApplicationBar.Buttons.Add(loadstateButton);
            ApplicationBar.Buttons.Add(resetButton);
            ApplicationBar.Buttons.Add(savestateButton);
            ApplicationBar.Buttons.Add(configButton); 
            //ApplicationBar.Buttons.Add(backButton);
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

        void resetButton_Click(object sender, EventArgs e)
        {
            this.m_d3dBackground.Reset();
        }

        void resumeButton_Click(object sender, EventArgs e)
        {
            this.ChangeAppBarVisibility(false);
        }

        void loadstateButton_Click(object sender, EventArgs e)
        {
            if (EmulatorSettings.Current.HideLoadConfirmationDialogs)
            {
                this.m_d3dBackground.LoadState();
            }
            else
            {
                this.ShowLoadDialog();
            }
        }

        void ShowLoadDialog()
        {
            confirmPopupOpened = true;
            MessagePrompt prompt = new MessagePrompt();
            ConfirmationPage page = new ConfirmationPage(AppResources.ConfirmLoadText);
            page.Closed += (o, e) =>
            {
                confirmPopupOpened = false;
                prompt.Hide();
            };
            prompt.Completed += (o, e) =>
            {
                confirmPopupOpened = false;
            };
            page.Confirmed += (o, e) =>
            {
                confirmPopupOpened = false;
                prompt.Hide();
                EmulatorSettings.Current.HideLoadConfirmationDialogs = page.DoNotShowAgain;
                this.m_d3dBackground.LoadState();
            };
            prompt.Body = page;
            prompt.ActionPopUpButtons.Clear();
            prompt.Overlay = new SolidColorBrush(Color.FromArgb(155, 41, 41, 41));
            prompt.Show();
        }

        void savestateButton_Click(object sender, EventArgs e)
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

        void ShowSaveDialog()
        {
            confirmPopupOpened = true;
            MessagePrompt prompt = new MessagePrompt();
            ConfirmationPage page = new ConfirmationPage(AppResources.ConfirmSaveText);
            page.Closed += (o, e) =>
            {
                confirmPopupOpened = false;
                prompt.Hide();
            };
            prompt.Completed += (o, e) =>
            {
                confirmPopupOpened = false;
            };
            page.Confirmed += (o, e) =>
            {
                confirmPopupOpened = false;
                prompt.Hide();
                EmulatorSettings.Current.HideConfirmationDialogs = page.DoNotShowAgain;
                this.m_d3dBackground.SaveState();
            };
            prompt.Body = page;
            prompt.ActionPopUpButtons.Clear();
            prompt.Overlay = new SolidColorBrush(Color.FromArgb(155, 41, 41, 41));
            prompt.Show();
        }

        void backbutton_click(object sender, EventArgs e)
        {
            //this.NavigationService.GoBack();
            this.ChangeAppBarVisibility(false);
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

        void EmulatorPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!confirmPopupOpened)
            {
                if (m_d3dBackground == null || m_d3dBackground.IsROMLoaded() == false)
                {
                    MessageBox.Show("Please wait until ROM finishes loading");
                    e.Cancel = true;
                }

                else  if (!this.ApplicationBar.IsVisible)
                {
                    e.Cancel = true;
                    this.ChangeAppBarVisibility(!this.ApplicationBar.IsVisible);
                }
            }
        }

        void ChangeAppBarVisibility(bool visible)
        {
            this.ApplicationBar.IsVisible = visible;
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
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

            if (initialized && this.m_d3dBackground.IsROMLoaded() && romInfo == null)
            {
                //this.m_d3dBackground.UnpauseEmulation();  //this will produce exception when returning from Settings page
            }
            else if (romInfo != null)
            {
                if (this.initialized)
                {
                    if (this.m_d3dBackground.LoadadROMFile.Name.Equals(romInfo.file.Name))
                    {
                        this.m_d3dBackground.UnpauseEmulation();
                    }
                    else
                    {
                        cache = null;
                        m_d3dBackground.LoadROMAsync(romInfo.file, romInfo.folder);
                        if (EmulatorSettings.Current.SelectLastState)
                        {
                            RestoreLastSavestate(romInfo.file.Name);
                        }
                        ROMLoaded = true;
                    }
                }
                else
                {
                    this.cache = romInfo;
                }
            }

            base.OnNavigatedTo(e);
        }

        private void CameraButtons_ShutterKeyReleased(object sender, EventArgs e)
        {
            if (this.m_d3dBackground != null && (wasHalfPressed || EmulatorSettings.Current.CameraButtonAssignment != 0))
            {
                this.m_d3dBackground.StopTurboMode();
                wasHalfPressed = false;
            }
        }

        private void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (EmulatorSettings.Current.CameraButtonAssignment == 0)
            {   // Turbo button
                if (this.m_d3dBackground != null)
                {
                    wasHalfPressed = true;
                    this.m_d3dBackground.StartTurboMode();
                }
            }
            else
            {   // L or R button
                if (this.m_d3dBackground != null)
                {
                    wasHalfPressed = true;
                    this.m_d3dBackground.StartTurboMode();
                }
            }
        }

        private void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (EmulatorSettings.Current.CameraButtonAssignment == 0)
            {   // Turbo button

                if (this.m_d3dBackground != null)
                {
                    if (!wasHalfPressed)
                    {
                        this.m_d3dBackground.ToggleTurboMode();
                    }
                    wasHalfPressed = false;
                }
            }
            else
            {   // L or R button
                if (this.m_d3dBackground != null)
                {
                    this.m_d3dBackground.StartTurboMode();
                    wasHalfPressed = false;
                }
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {

            //disable lock screen
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

            if (initialized && this.m_d3dBackground.IsROMLoaded())
            {
                //this.m_d3dBackground.PauseEmulation(); //don't need this
            }
            if (this.m_d3dBackground.LoadadROMFile == null)
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
                }
                base.OnNavigatingFrom(e);
            }
        }

        private void DrawingSurfaceBackground_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_d3dBackground == null)
            {
                this.m_d3dBackground = new Direct3DBackground();

                this.m_d3dBackground.SetContinueNotifier(this.ContinueEmulation);
                this.m_d3dBackground.SnapshotAvailable = FileHandler.CaptureSnapshot;
                this.m_d3dBackground.SavestateCreated = FileHandler.CreateSavestate;
                this.m_d3dBackground.SavestateSelected = this.savestateSelected;
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

                this.initialized = true;

                if (ROMLoaded && this.cache == null)
                {
                    this.m_d3dBackground.UnpauseEmulation();
                }
                else if (this.cache != null && this.cache.file != null && this.cache.folder != null)
                {
                    if (ROMLoaded && this.m_d3dBackground.LoadadROMFile.Name.Equals(this.cache.file.Name))
                    {
                        this.m_d3dBackground.UnpauseEmulation();
                    }
                    else
                    {
                        this.m_d3dBackground.LoadROMAsync(this.cache.file, this.cache.folder);
                        if (EmulatorSettings.Current.SelectLastState)
                        {
                            RestoreLastSavestate(this.cache.file.Name);
                        }
                        ROMLoaded = true;
                    }
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
        }

        private void RestoreLastSavestate(string filename)
        {
            ROMDatabase db = ROMDatabase.Current;
            int slot = db.GetLastSavestateSlotByFileNameExceptAuto(filename);
            m_d3dBackground.SelectSaveState(slot);
        }
    }
}