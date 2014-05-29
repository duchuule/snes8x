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
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.Windows.Controls.Primitives;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public Popup popupWindow = null;

        private String[] frameskiplist = { AppResources.FrameSkipAutoSetting, "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private String[] frameskiplist2 = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };


        public const String VControllerPosKey = "VirtualControllerOnTop";
        public const String VControllerSizeKey = "VirtualControllerLarge";
        public const String EnableSoundKey = "EnableSound";
        public const String LowFreqModeKey = "LowFrequencyModeNew";
        //public const String LowFreqModeMeasuredKey = "LowFrequencyModeMeasured";
        public const String VControllerButtonStyleKey = "VirtualControllerStyle";
        public const String OrientationKey = "Orientation";
        public const String ControllerScaleKey = "ControllerScale";
        public const String ButtonScaleKey = "ButtonScale";
        public const String StretchKey = "FullscreenStretch";
        public const String OpacityKey = "ControllerOpacity";
        public const String SkipFramesKey = "SkipFramesKey";
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

        bool initdone = false;

        public SettingsPage()
        {
            InitializeComponent();

            //set frameskip option
            frameSkipPicker.ItemsSource = frameskiplist;
            powerFrameSkipPicker.ItemsSource = frameskiplist2;
            turboFrameSkipPicker.ItemsSource = frameskiplist2;

            ReadSettings();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 1);
            }
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
            this.restoreLastStateSwitch.IsChecked = emuSettings.SelectLastState;
            this.manualSnapshotSwitch.IsChecked = emuSettings.ManualSnapshots;
            this.useColorButtonSwitch.IsChecked = !emuSettings.GrayVControllerButtons;

            if (this.useColorButtonSwitch.IsChecked.Value)
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Visible;
            else
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Collapsed;


            this.toggleUseMogaController.IsChecked = emuSettings.UseMogaController;

            this.showAdsSwitch.IsChecked = emuSettings.ShouldShowAds;

            if (EmulatorSettings.Current.UseMogaController)
                MappingBtn.Visibility = Visibility.Visible;
            else
                MappingBtn.Visibility = Visibility.Collapsed;

            this.chkUseAccentColor.IsChecked = App.metroSettings.UseAccentColor;

            this.Loaded += (o, e) =>
            {
                this.dpadStyleBox.SelectedIndex = emuSettings.DPadStyle;
                this.powerFrameSkipPicker.SelectedIndex = emuSettings.PowerFrameSkip;
                this.frameSkipPicker.SelectedIndex = Math.Min(emuSettings.FrameSkip + 1, this.frameSkipPicker.Items.Count - 1);
                this.turboFrameSkipPicker.SelectedIndex = Math.Min(emuSettings.TurboFrameSkip, this.turboFrameSkipPicker.Items.Count - 1);
                this.orientationPicker.SelectedIndex = emuSettings.Orientation;
                this.assignPicker.SelectedIndex = emuSettings.CameraButtonAssignment;


                initdone = true;
            };

        }

        //private void vcontrollerPosSwitch_Checked_1(object sender, RoutedEventArgs e)
        //{
        //    this.vcontrollerPosSwitch.Content = AppResources.VControllerTopSetting;
        //    if (this.initdone)
        //    {
        //        EmulatorSettings.Current.VirtualControllerOnTop = true;
        //    }
        //}

        //private void vcontrollerPosSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        //{
        //    this.vcontrollerPosSwitch.Content = AppResources.VControllerBottomSetting;
        //    if (this.initdone)
        //    {
        //        EmulatorSettings.Current.VirtualControllerOnTop = false;
        //    }
        //}

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

        //private void vcontrollerSizeSwitch_Checked_1(object sender, RoutedEventArgs e)
        //{
        //    this.vcontrollerSizeSwitch.Content = AppResources.VControllerLargeSetting;
        //    if (this.initdone)
        //    {
        //        EmulatorSettings.Current.LargeVController = true;
        //    }
        //}

        //private void vcontrollerSizeSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        //{
        //    this.vcontrollerSizeSwitch.Content = AppResources.VControllerSmallSetting;
        //    if (this.initdone)
        //    {
        //        EmulatorSettings.Current.LargeVController = false;
        //    }
        //}

        
        private void ListPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.Orientation = this.orientationPicker.SelectedIndex;
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

        private void opacitySlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ControllerOpacity = (int) this.opacitySlider.Value;
            }
        }

        private void scaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ControllerScale = (int)this.scaleSlider.Value;
            }
        }

        private void frameSkipPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.FrameSkip = (int)this.frameSkipPicker.SelectedIndex - 1;
            }
        }

        private void powerFrameSkipPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.PowerFrameSkip = this.powerFrameSkipPicker.SelectedIndex;
            }
        }

        private void turboFrameSkipPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.TurboFrameSkip = this.turboFrameSkipPicker.SelectedIndex;
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

        private void dpadStyleBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.DPadStyle = this.dpadStyleBox.SelectedIndex;
            }
        }

        private void deadzoneSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.Deadzone = (float) this.deadzoneSlider.Value;
            }
        }

        private void imageScaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ImageScaling = (int)this.imageScaleSlider.Value;
            }
        }

        private void assignPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.CameraButtonAssignment = this.assignPicker.SelectedIndex;
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

        private void restoreLastStateSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SelectLastState = true;
            }
        }

        private void restoreLastStateSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.SelectLastState = false;
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

        private void TextBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbtask = new WebBrowserTask();
            wbtask.Uri = new Uri("http://www.youtube.com/watch?v=YfqzZhcr__o");
            wbtask.Show();
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

        private void ButtonScaleSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.initdone)
            {
                EmulatorSettings.Current.ButtonScale = (int)this.buttonScaleSlider.Value;
            }
        }

        private void showAdsSwitch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            if (this.initdone)
            {

                EmulatorSettings.Current.ShouldShowAds = showAdsSwitch.IsChecked.Value;
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

        private void MappingBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MogaMappingPage.xaml", UriKind.Relative));
        }

        private void useColorButtonSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (this.useColorButtonSwitch.IsChecked.Value)
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Visible;
            else
                CustomizeBgcolorBtn.Visibility = System.Windows.Visibility.Collapsed;

            if (this.initdone)
            {
                EmulatorSettings.Current.GrayVControllerButtons = !this.useColorButtonSwitch.IsChecked.Value;
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

        private void chkUseAccentColor_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (initdone)
            {

                App.metroSettings.UseAccentColor = chkUseAccentColor.IsChecked.Value;

                FileHandler.UpdateLiveTile();



            }
        }


        //private void clearBackgroundButton_Click_1(object sender, RoutedEventArgs e)
        //{

        //}

        //private void chooseBackgroundButton_Click_1(object sender, RoutedEventArgs e)
        //{
        //    PhotoChooserTask task = new PhotoChooserTask();
        //    task.Completed += (o, ex) =>
        //    {
        //        if (ex.TaskResult == TaskResult.OK)
        //        {
        //            this.SetBackgroundImage(ex.ChosenPhoto);
        //        }
        //    };
        //    task.Show();
        //}

        //private async void SetBackgroundImage(System.IO.Stream stream)
        //{
        //    IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
        //    using (IsolatedStorageFileStream fs = file.CreateFile("bg.jpg"))
        //    {
        //        byte[] tmp = new byte[stream.Length];
        //        await stream.ReadAsync(tmp, 0, (int) stream.Length);
        //        stream.Dispose();
        //        await fs.WriteAsync(tmp, 0, tmp.Length);
        //        await fs.FlushAsync();
        //    }

        //    using (IsolatedStorageFileStream fs = file.OpenFile("bg.jpg", System.IO.FileMode.Open))
        //    {
        //        BitmapImage img = new BitmapImage();
        //        img.SetSource(fs);

        //        this.Background = new ImageBrush() { ImageSource = img, Stretch = Stretch.UniformToFill };
        //    }
            
        //}
    }
}