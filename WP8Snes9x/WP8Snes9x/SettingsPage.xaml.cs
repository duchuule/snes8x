using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneDirect3DXamlAppInterop.Resources;
using PhoneDirect3DXamlAppComponent;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using System.Security.Cryptography;
using Microsoft.Devices.Sensors;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public Popup popupWindow = null;

        private String[] frameskiplist = { AppResources.FrameSkipAutoSetting, "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private String[] frameskiplist2 = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private String[] orientationList = { AppResources.OrientationBoth, AppResources.OrientationLandscape, AppResources.OrientationPortrait };

        public static bool shouldUpdateBackgroud = false;

        public const String VControllerPosKey = "VirtualControllerOnTop";
        public const String EnableSoundKey = "EnableSound";
        public const String LowFreqModeKey = "LowFrequencyModeNew";
        public const String OrientationKey = "Orientation";
        public const String ControllerScaleKey = "ControllerScale";
        public const String ButtonScaleKey = "ButtonScale";
        public const String StretchKey = "FullscreenStretch";
        public const String OpacityKey = "ControllerOpacity";
        public const String VControllerButtonStyleKey = "VirtualControllerStyle";
        public const String SkipFramesKey = "SkipFramesKey2";
        public const String ImageScalingKey = "ImageScalingKey";
        public const String TurboFrameSkipKey = "TurboSkipFramesKey";
        public const String SyncAudioKey = "SynchronizeAudioKey";
        public const String PowerSaverKey = "PowerSaveSkipKey";
        public const String DPadStyleKey = "DPadStyleKey";
        public const String DeadzoneKey = "DeadzoneKey";
        public const String CameraAssignKey = "CameraAssignmentKey";
        public const String ConfirmationKey = "ConfirmationKey";
        public const String ConfirmationLoadKey = "ConfirmationLoadKey";
        public const String AutoIncKey = "AutoIncKey";
        public const String SelectLastState = "SelectLastStateKey";
        public const String CreateManualSnapshotKey = "ManualSnapshotKey";
        public const String UseMogaControllerKey = "UseMogaControllerKey";
        public const String ShouldShowAdsKey = "ShouldShowAdsKey";
        public const String BgcolorRKey = "BgcolorRKey";
        public const String BgcolorGKey = "BgcolorGKey";
        public const String BgcolorBKey = "BgcolorBKey";
        public const String AutoSaveLoadKey = "AutSaveLoadKey";
        public const String VirtualControllerStyleKey = "VirtualControllerStyleKey";
        public const String VibrationEnabledKey = "VibrationEnabledKey";
        public const String VibrationDurationKey = "VibrationDurationKey";
        public const String EnableAutoFireKey = "EnableAutoFireKey";
        public const String MapABLRTurboKey = "MapABLRTurboKey";
        public const String FullPressStickABLRKey = "FullPressStickABLRKey";
        public const String UseMotionControlKey = "UseMotionControlKey";
        public const String UseTurboKey = "UseTurboKey";

        public const String PadCenterXPKey = "PadCenterXPKey";
        public const String PadCenterYPKey = "PadCenterYPKey";
        public const String ACenterXPKey = "ACenterXPKey";
        public const String ACenterYPKey = "ACenterYPKey";
        public const String BCenterXPKey = "BCenterXPKey";
        public const String BCenterYPKey = "BCenterYPKey";
        public const String StartLeftPKey = "StartLeftPKey";
        public const String StartTopPKey = "StartTopPKey";
        public const String SelectRightPKey = "SelectRightPKey";
        public const String SelectTopPKey = "SelectTopPKey";
        public const String LLeftPKey = "LLeftPKey";
        public const String LTopPKey = "LTopPKey";
        public const String RRightPKey = "RRightPKey";
        public const String RTopPKey = "RTopPKey";
        public const String XCenterXPKey = "XCenterXPKey";
        public const String XCenterYPKey = "XCenterYPKey";
        public const String YCenterXPKey = "YCenterXPKey";
        public const String YCenterYPKey = "YCenterYPKey";


        public const String PadCenterXLKey = "PadCenterXLKey";
        public const String PadCenterYLKey = "PadCenterYLKey";
        public const String ACenterXLKey = "ACenterXLKey";
        public const String ACenterYLKey = "ACenterYLKey";
        public const String BCenterXLKey = "BCenterXLKey";
        public const String BCenterYLKey = "BCenterYLKey";
        public const String StartLeftLKey = "StartLeftLKey";
        public const String StartTopLKey = "StartTopLKey";
        public const String SelectRightLKey = "SelectRightLKey";
        public const String SelectTopLKey = "SelectTopLKey";
        public const String LLeftLKey = "LLeftLKey";
        public const String LTopLKey = "LTopLKey";
        public const String RRightLKey = "RRightLKey";
        public const String RTopLKey = "RTopLKey";
        public const String XCenterXLKey = "XCenterXLKey";
        public const String XCenterYLKey = "XCenterYLKey";
        public const String YCenterXLKey = "YCenterXLKey";
        public const String YCenterYLKey = "YCenterYLKey";

        public const String MogaAKey = "MogaAKey";
        public const String MogaBKey = "MogaBKey";
        public const String MogaXKey = "MogaXKey";
        public const String MogaYKey = "MogaYKey";
        public const String MogaL1Key = "MogaL1Key";
        public const String MogaL2Key = "MogaL2Key";
        public const String MogaR1Key = "MogaR1Key";
        public const String MogaR2Key = "MogaR2Key";
        public const String MogaLeftJoystickKey = "MogaLeftJoystickKey";
        public const String MogaRightJoystickKey = "MogaRightJoystickKey";

        public const String MotionLeftKey = "MotionLeftKey";
        public const String MotionRightKey = "MotionRightKey";
        public const String MotionUpKey = "MotionUpKey";
        public const String MotionDownKey = "MotionDownKey";
        public const String RestAngleXKey = "RestAngleXKey";
        public const String RestAngleYKey = "RestAngleYKey";
        public const String RestAngleZKey = "RestAngleZKey";

        public const String MotionDeadzoneHKey = "MotionDeadzoneHKey";
        public const String MotionDeadzoneVKey = "MotionDeadzoneVKey";
        public const String MotionAdaptOrientationKey = "MotionAdaptOrientationKey";


        bool initdone = false;





        public SettingsPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 1);
            }

            //RSACryptoServiceProvider newrsa = new RSACryptoServiceProvider(



            //set frameskip option
            frameSkipPicker.ItemsSource = frameskiplist;
            //powerFrameSkipPicker.ItemsSource = frameskiplist2;
            turboFrameSkipPicker.ItemsSource = frameskiplist2;
            orientationPicker.ItemsSource = orientationList;

            ReadSettings();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {


            //in case return from image chooser page
            if (shouldUpdateBackgroud)
            {

                //manually update background and signal main page
                this.UpdateBackgroundImage();
                MainPage.shouldUpdateBackgroud = true;

                shouldUpdateBackgroud = false;

            }
            base.OnNavigatedTo(e);
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //Check if the PopUp window is open
            if (popupWindow != null && popupWindow.IsOpen)
            {
                //Close the PopUp Window
                popupWindow.IsOpen = false;

                //Keep the back button from navigating away from the current page
                e.Cancel = true;
            }

            else
            {
                //There is no PopUp open, use the back button normally
                base.OnBackKeyPress(e);
            }

        }

        private void ReadSettings()
        {
            EmulatorSettings emuSettings = EmulatorSettings.Current;


            this.enableSoundSwitch.IsChecked = emuSettings.SoundEnabled;
            this.lowFreqSwitch.IsChecked = emuSettings.LowFrequencyMode;
            this.stretchToggle.IsChecked = emuSettings.FullscreenStretch;
            this.scaleSlider.Value = emuSettings.ControllerScale;
            this.buttonScaleSlider.Value = emuSettings.ButtonScale;
            this.opacitySlider.Value = emuSettings.ControllerOpacity;
            this.imageScaleSlider.Value = emuSettings.ImageScaling;
            this.deadzoneSlider.Value = emuSettings.Deadzone;
            this.syncSoundSwitch.IsChecked = emuSettings.SynchronizeAudio;
            this.confirmationSwitch.IsChecked = emuSettings.HideConfirmationDialogs;
            this.autoIncSwitch.IsChecked = emuSettings.AutoIncrementSavestates;
            this.confirmationLoadSwitch.IsChecked = emuSettings.HideLoadConfirmationDialogs;
            //this.restoreLastStateSwitch.IsChecked = emuSettings.SelectLastState;
            this.manualSnapshotSwitch.IsChecked = emuSettings.ManualSnapshots;
            this.useColorButtonSwitch.IsChecked = !emuSettings.GrayVControllerButtons;


            this.showThreeDotsSwitch.IsChecked = App.metroSettings.ShowThreeDots;
            this.showLastPlayedGameSwitch.IsChecked = App.metroSettings.ShowLastPlayedGame;
            this.loadLastStateSwitch.IsChecked = emuSettings.AutoSaveLoad;

            this.autoBackupSwitch.IsChecked = App.metroSettings.AutoBackup;

            this.autoFireSwitch.IsChecked = emuSettings.EnableAutoFire;
            this.mapABLRTurboSwitch.IsChecked = emuSettings.MapABLRTurbo;
            this.fullPressStickABRLSwitch.IsChecked = emuSettings.FullPressStickABLR;

            if (App.metroSettings.BackgroundUri != null)
            {
                this.useBackgroundImageSwitch.IsChecked = true;
                this.backgroundOpacityPanel.Visibility = Visibility.Visible;
                this.ChooseBackgroundImageGrid.Visibility = Visibility.Visible;
            }
            else
            {
                this.useBackgroundImageSwitch.IsChecked = false;
                this.backgroundOpacityPanel.Visibility = Visibility.Collapsed;
                this.ChooseBackgroundImageGrid.Visibility = Visibility.Collapsed;
            }
            if (this.useColorButtonSwitch.IsChecked.Value)
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Visible;
            else
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Collapsed;

            this.showAdsSwitch.IsChecked = emuSettings.ShouldShowAds;
            this.toggleUseMogaController.IsChecked = emuSettings.UseMogaController;


            if (EmulatorSettings.Current.UseMogaController)
                MappingBtn.Visibility = Visibility.Visible;
            else
                MappingBtn.Visibility = Visibility.Collapsed;

            if (this.autoBackupSwitch.IsChecked.Value)
                SetupAutoBackupBtn.Visibility = System.Windows.Visibility.Visible;
            else
                SetupAutoBackupBtn.Visibility = System.Windows.Visibility.Collapsed;


            this.chkUseAccentColor.IsChecked = App.metroSettings.UseAccentColor;


            this.toggleVibration.IsChecked = emuSettings.VibrationEnabled;

            if (emuSettings.VibrationEnabled)
                this.txtVibrationDuration.Visibility = Visibility.Visible;
            else
                this.txtVibrationDuration.Visibility = Visibility.Collapsed;

            this.toggleTurbo.IsChecked = emuSettings.UseTurbo;

            this.Loaded += (o, e) =>
            {
                this.turboFrameSkipPicker.SelectedIndex = emuSettings.TurboFrameSkip;
                //this.powerFrameSkipPicker.SelectedIndex = emuSettings.PowerFrameSkip;
                this.frameSkipPicker.SelectedIndex = Math.Min(emuSettings.FrameSkip + 1, this.frameSkipPicker.Items.Count - 1);

                this.orientationPicker.SelectedIndex = emuSettings.Orientation;

                this.dpadStyleBox.SelectedIndex = emuSettings.DPadStyle; //dpad, this need to be set after loaded because we set the items in xaml
                this.assignPicker.SelectedIndex = emuSettings.CameraButtonAssignment; //camera assignment



                if (emuSettings.CameraButtonAssignment == 0) //hide auto fire setting
                {
                    this.autoFireSwitch.Visibility = Visibility.Collapsed;
                    this.mapABLRTurboSwitch.Visibility = Visibility.Collapsed;
                    this.fullPressStickABRLSwitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.autoFireSwitch.Visibility = Visibility.Visible;
                    this.mapABLRTurboSwitch.Visibility = Visibility.Visible;
                    this.fullPressStickABRLSwitch.Visibility = Visibility.Visible;
                }

                this.themePicker.SelectedIndex = App.metroSettings.ThemeSelection;


                this.backgroundOpacitySlider.Value = App.metroSettings.BackgroundOpacity * 100;

                this.txtVibrationDuration.Text = emuSettings.VibrationDuration.ToString();


                this.motionControlBox.SelectedIndex = emuSettings.UseMotionControl;

                if (EmulatorSettings.Current.UseMotionControl == 0)
                    MotionSettingBtn.Visibility = Visibility.Collapsed;
                else
                    MotionSettingBtn.Visibility = Visibility.Visible;

                initdone = true;
            };

        }


        private void enableSoundSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SoundEnabled = true;
            }
        }

        private void enableSoundSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SoundEnabled = false;
            }
        }

        private void lowFreqSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            this.lowFreqSwitch.Content = AppResources.RefreshRate30;
            if (this.initdone)
            {
                EmulatorSettings.Current.LowFrequencyMode = true;
            }
        }

        private void lowFreqSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            this.lowFreqSwitch.Content = AppResources.RefreshRate60;
            if (this.initdone)
            {
                EmulatorSettings.Current.LowFrequencyMode = false;
            }
        }

        private void opacitySlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ControllerOpacity = (int)this.opacitySlider.Value;
            }
        }

        private void scaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ControllerScale = (int)this.scaleSlider.Value;
            }
        }

        private void ButtonScaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ButtonScale = (int)this.buttonScaleSlider.Value;
            }
        }

        private void imageScaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ImageScaling = (int)this.imageScaleSlider.Value;
            }
        }

        private void syncSoundSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SynchronizeAudio = true;
            }
        }

        private void syncSoundSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SynchronizeAudio = false;
            }
        }

        private void deadzoneSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.Deadzone = (float)this.deadzoneSlider.Value;
            }
        }

        private void confirmationSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.HideConfirmationDialogs = true;
            }
        }

        private void confirmationSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.HideConfirmationDialogs = false;
            }
        }

        private void autoIncSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.AutoIncrementSavestates = true;
            }
        }

        private void autoIncSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.AutoIncrementSavestates = false;
            }
        }

        private void confirmationLoadSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.HideLoadConfirmationDialogs = true;
            }
        }

        private void confirmationLoadSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.HideLoadConfirmationDialogs = false;
            }
        }





        private void manualSnapshotSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ManualSnapshots = true;
            }
        }

        private void manualSnapshotSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ManualSnapshots = false;
            }
        }
        private void stretchToggle_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.FullscreenStretch = true;
            }
        }

        private void stretchToggle_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.FullscreenStretch = false;
            }
        }




        private void orientationPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.Orientation = orientationPicker.SelectedIndex;
            }
        }



        private void assignPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.CameraButtonAssignment = this.assignPicker.SelectedIndex;

                if (EmulatorSettings.Current.CameraButtonAssignment == 0) //hide the auto fire setting
                {
                    this.autoFireSwitch.Visibility = Visibility.Collapsed;
                    this.mapABLRTurboSwitch.Visibility = Visibility.Collapsed;
                    this.fullPressStickABRLSwitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.autoFireSwitch.Visibility = Visibility.Visible;
                    this.mapABLRTurboSwitch.Visibility = Visibility.Visible;
                    this.fullPressStickABRLSwitch.Visibility = Visibility.Visible;
                }
            }
        }

        private void dpadStyleBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.DPadStyle = this.dpadStyleBox.SelectedIndex;
            }
        }

        private void turboFrameSkipPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.TurboFrameSkip = this.turboFrameSkipPicker.SelectedIndex;
            }
        }
        private void frameSkipPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.FrameSkip = (int)this.frameSkipPicker.SelectedIndex - 1;

            }
        }


        private void toggleUseMogaController_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.initdone)
            {

                EmulatorSettings.Current.UseMogaController = toggleUseMogaController.IsChecked.Value;

                if (EmulatorSettings.Current.UseMogaController)
                    MappingBtn.Visibility = Visibility.Visible;
                else
                    MappingBtn.Visibility = Visibility.Collapsed;

            }
        }

        private void TextBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("http://www.youtube.com/watch?v=YfqzZhcr__o");
            wbtask.Show();
        }

        private void deadzoneLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (deadzoneSlider.IsEnabled)
            {
                deadzoneSlider.IsEnabled = false;
                deadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                deadzoneSlider.IsEnabled = true;
                deadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void scaleLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (scaleSlider.IsEnabled)
            {
                scaleSlider.IsEnabled = false;
                scaleImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                scaleSlider.IsEnabled = true;
                scaleImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void opacityLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (opacitySlider.IsEnabled)
            {
                opacitySlider.IsEnabled = false;
                opacityImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                opacitySlider.IsEnabled = true;
                opacityImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void backgroundOpacityLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (backgroundOpacitySlider.IsEnabled)
            {
                backgroundOpacitySlider.IsEnabled = false;
                backgroundOpacityImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                backgroundOpacitySlider.IsEnabled = true;
                backgroundOpacityImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void buttonScaleLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (buttonScaleSlider.IsEnabled)
            {
                buttonScaleSlider.IsEnabled = false;
                buttonScaleImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                buttonScaleSlider.IsEnabled = true;
                buttonScaleImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void CPositionPortraitBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CustomizeControllerPage.xaml?orientation=2", UriKind.Relative));
        }

        private void CPositionLandscapeBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CustomizeControllerPage.xaml?orientation=0", UriKind.Relative));
        }

        private void showAdsSwitch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            if (this.initdone)
            {

                EmulatorSettings.Current.ShouldShowAds = showAdsSwitch.IsChecked.Value;
            }
        }
        private void CustomizeBgcolorBtn_Click(object sender, RoutedEventArgs e)
        {
            //disable current page
            this.IsHitTestVisible = false;
            //this.Content.Visibility = Visibility.Collapsed;

            //create new popup instance

            popupWindow = new Popup();

            popupWindow.Child = new ColorChooserControl();

            popupWindow.VerticalOffset = 130;
            popupWindow.HorizontalOffset = 10;
            popupWindow.IsOpen = true;

            popupWindow.Closed += (s1, e1) =>
            {
                this.IsHitTestVisible = true;
                //this.Content.Visibility = Visibility.Visible;

            };
        }



        private void MappingBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MogaMappingPage.xaml", UriKind.Relative));
        }


        private void themePicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                App.metroSettings.ThemeSelection = themePicker.SelectedIndex;

                App.MergeCustomColors();
                //CustomMessageBox msgbox = new CustomMessageBox();
                //msgbox.Background = (SolidColorBrush)App.Current.Resources["PhoneChromeBrush"];
                //msgbox.Foreground = (SolidColorBrush)App.Current.Resources["PhoneForegroundBrush"];
                //msgbox.Message = AppResources.RestartPromptText;
                //msgbox.Caption = AppResources.RestartPromptTitle;
                //msgbox.LeftButtonContent = "OK";
                //msgbox.Show();
            }
        }

        private void showThreeDotsSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                App.metroSettings.ShowThreeDots = showThreeDotsSwitch.IsChecked.Value;
            }
        }

        private void useBackgroundImageSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                if (useBackgroundImageSwitch.IsChecked.Value)
                {
                    if (App.metroSettings.UseDefaultBackground)
                        App.metroSettings.BackgroundUri = FileHandler.DEFAULT_BACKGROUND_IMAGE;
                    else
                        App.metroSettings.BackgroundUri = "CustomBackground.jpg";

                    this.backgroundOpacityPanel.Visibility = Visibility.Visible;
                    this.ChooseBackgroundImageGrid.Visibility = Visibility.Visible;


                }
                else
                {
                    App.metroSettings.BackgroundUri = null;
                    this.backgroundOpacityPanel.Visibility = Visibility.Collapsed;
                    this.ChooseBackgroundImageGrid.Visibility = Visibility.Collapsed;


                }

                //manually update background (can't find a better way)
                this.UpdateBackgroundImage();

                //signal main page
                MainPage.shouldUpdateBackgroud = true;
            }
        }

        private void backgroundOpacitySlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                App.metroSettings.BackgroundOpacity = this.backgroundOpacitySlider.Value / 100f;

                //manually update background (can't find a better way)
                this.UpdateBackgroundImage();

                //signal main page
                MainPage.shouldUpdateBackgroud = true;
            }
        }

        private void ChooseBackgroundImageBtn_Click(object sender, RoutedEventArgs e)
        {

            PhotoChooserTask task = new PhotoChooserTask();
            task.Completed += photoChooserTask_Completed;
            task.ShowCamera = true;

            task.Show();




        }

        private void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(e.ChosenPhoto);  //this does not have info about width and length

                WriteableBitmap wb = new WriteableBitmap(bmp); //this has info about width and length

                ImageCroppingPage.wbSource = wb;


                NavigationService.Navigate(new Uri("/ImageCroppingPage.xaml", UriKind.Relative));




            }
        }



        private void ResetBackgroundImageBtn_Click(object sender, RoutedEventArgs e)
        {
            App.metroSettings.BackgroundUri = FileHandler.DEFAULT_BACKGROUND_IMAGE;
            App.metroSettings.UseDefaultBackground = true;

            //manually update background and signal main page
            this.UpdateBackgroundImage();
            MainPage.shouldUpdateBackgroud = true;

        }



        private void UpdateBackgroundImage()
        {
            if (App.metroSettings.BackgroundUri != null)
            {
                pivot.Background = new ImageBrush
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
                pivot.Background = null;
            }
        }

        private void showLastPlayedGameSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                App.metroSettings.ShowLastPlayedGame = showLastPlayedGameSwitch.IsChecked.Value;
            }
        }

        private void loaLastStateSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                if (loadLastStateSwitch.IsChecked.Value == true)
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.AutoSaveWarning, AppResources.WarningTitle, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                        EmulatorSettings.Current.AutoSaveLoad = loadLastStateSwitch.IsChecked.Value;
                    else
                        loadLastStateSwitch.IsChecked = false;

                }
                else
                    EmulatorSettings.Current.AutoSaveLoad = loadLastStateSwitch.IsChecked.Value;
            }
        }

        private void autoBackupSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                App.metroSettings.AutoBackup = autoBackupSwitch.IsChecked.Value;

                if (App.metroSettings.AutoBackup)
                    SetupAutoBackupBtn.Visibility = System.Windows.Visibility.Visible;
                else
                    SetupAutoBackupBtn.Visibility = System.Windows.Visibility.Collapsed;

            }
        }

        private void SetupAutoBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AutoBackupPage.xaml", UriKind.Relative));
        }

        private void useColorButtonSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.GrayVControllerButtons = !this.useColorButtonSwitch.IsChecked.Value;
                if (this.useColorButtonSwitch.IsChecked.Value)
                    CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Visible;
                else
                    CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Collapsed;

            }
        }

        private void chkUseAccentColor_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (initdone)
            {

                App.metroSettings.UseAccentColor = chkUseAccentColor.IsChecked.Value;


                FileHandler.UpdateLiveTile();



            }
        }

        private void toggleVibration_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.VibrationEnabled = this.toggleVibration.IsChecked.Value;

                if (EmulatorSettings.Current.VibrationEnabled)
                    txtVibrationDuration.Visibility = Visibility.Visible;
                else
                    txtVibrationDuration.Visibility = Visibility.Collapsed;
            }
        }


        private void txtVibrationDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            double duration = 0;

            if (txtVibrationDuration.Text != "")
                double.TryParse(txtVibrationDuration.Text, out duration); //duration = 0 if failed


            if (duration < 0 || duration > 5)
            {
                MessageBox.Show(AppResources.VibrationDurationErrorText);
                return;
            }
            EmulatorSettings.Current.VibrationDuration = duration;
        }

        private void autoFireSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.EnableAutoFire = this.autoFireSwitch.IsChecked.Value;
            }
        }

        private void mapABLRTurboSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.MapABLRTurbo = this.mapABLRTurboSwitch.IsChecked.Value;
            }
        }

        private void fullPressStickABRLSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.FullPressStickABLR = this.fullPressStickABRLSwitch.IsChecked.Value;
            }
        }

        private void PinSecondaryTileBtn_Click(object sender, RoutedEventArgs e)
        {
            FileHandler.CreateOrUpdateSecondaryTile(true);
        }

        private void ChangeVoiceCommandPrefixBtn_Click(object sender, RoutedEventArgs e)
        {
            //disable current page
            this.IsHitTestVisible = false;

            //create new popup instance


            popupWindow = new Popup();
            EditCheatControl.TextToEdit = "";
            EditCheatControl.PromptText = AppResources.EnterVoicePrefixText;
            popupWindow.Child = new EditCheatControl();


            popupWindow.VerticalOffset = 0;
            popupWindow.HorizontalOffset = 0;
            popupWindow.IsOpen = true;

            popupWindow.Closed += async (s1, e1) =>
            {
                this.IsHitTestVisible = true;

                if (EditCheatControl.IsOKClicked)
                {
                    if (EditCheatControl.TextToEdit != null && EditCheatControl.TextToEdit.Trim() != "")
                    {
                        await MainPage.RegisterVoiceCommand(EditCheatControl.TextToEdit);

                        await MainPage.UpdateGameListForVoiceCommand();
                    }
                    else
                    {
                        MessageBox.Show(AppResources.InputEmptyError);
                    }
                }

            };




        }

        private void motionControlBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (initdone)
            {
                if (this.motionControlBox.SelectedIndex == 1 && !Accelerometer.IsSupported)
                {
                    MessageBox.Show(AppResources.AccelerometerMissingText);
                    this.motionControlBox.SelectedIndex = 0;
                }
                else if (this.motionControlBox.SelectedIndex == 2)
                {
                    if (!Gyroscope.IsSupported)
                    {
                        MessageBox.Show(AppResources.GyroMissingText);
                        this.motionControlBox.SelectedIndex = 0;
                    }
                    else if (!Motion.IsSupported)
                    {
                        MessageBox.Show(AppResources.CompassMissingText);
                        this.motionControlBox.SelectedIndex = 0;
                    }
                }

                EmulatorSettings.Current.UseMotionControl = this.motionControlBox.SelectedIndex;

                if (EmulatorSettings.Current.UseMotionControl == 0)
                    MotionSettingBtn.Visibility = Visibility.Collapsed;
                else
                {
                    MotionSettingBtn.Visibility = Visibility.Visible;

                    if (EmulatorSettings.Current.UseMotionControl == 1) //default settings
                    {
                        //change setting in memory
                        EmulatorSettings.Current.RestAngleX = 0.0;   //in "g" unit
                        EmulatorSettings.Current.RestAngleY = -0.70711;
                        EmulatorSettings.Current.RestAngleZ = -0.70711;

                        //save to disk
                        IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleXKey] = 0.0;
                        IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleYKey] = -0.70711;
                        IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleZKey] = -0.70711;

                        IsolatedStorageSettings.ApplicationSettings.Save();


                    }
                    else if (EmulatorSettings.Current.UseMotionControl == 2)
                    {
                        //change setting in memory
                        EmulatorSettings.Current.RestAngleX = 0.0;  //in dgree
                        EmulatorSettings.Current.RestAngleY = 45;


                        //save to disk
                        IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleXKey] = 0.0;
                        IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleYKey] = 45.0;


                        IsolatedStorageSettings.ApplicationSettings.Save();
                    }
                }
            }
        }

        private void MotionSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MotionMappingPage.xaml", UriKind.Relative));
        }

        private void toggleTurbo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (initdone)
            {
                EmulatorSettings.Current.UseTurbo = this.toggleTurbo.IsChecked.Value;

                //save to disk
                //do this here instead of SettingsChangedDelegate() so that we don't always save to disk when camera is half press
                IsolatedStorageSettings.ApplicationSettings[SettingsPage.UseTurboKey] = this.toggleTurbo.IsChecked.Value;
            }
        }
    }
}