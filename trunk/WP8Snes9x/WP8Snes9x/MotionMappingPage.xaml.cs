using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneDirect3DXamlAppComponent;
using System.IO.IsolatedStorage;
using PhoneDirect3DXamlAppInterop.Resources;
using System.Windows.Media;
using Microsoft.Devices.Sensors;
using System.Windows.Media.Imaging;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class MotionMappingPage : PhoneApplicationPage
    {
        private String[] appFunctionList = {"Left", "Right", "Up", "Down", "A", "B", "X", "Y", "L", "R", AppResources.NoneButtonText };

        public MotionMappingPage()
        {
            InitializeComponent();

#if GBC
            SystemTray.GetProgressIndicator(this).Text = AppResources.ApplicationTitle2;
#endif
            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                ((Grid)(LayoutRoot.Children[0])).Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }


            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

            Leftbtn.ItemsSource = appFunctionList;
            Rightbtn.ItemsSource = appFunctionList;
            Upbtn.ItemsSource = appFunctionList;
            Downbtn.ItemsSource = appFunctionList;


            Leftbtn.SelectedIndex = (int)Math.Round(Math.Log(EmulatorSettings.Current.MotionLeft, 2));
            Rightbtn.SelectedIndex = (int)Math.Round(Math.Log(EmulatorSettings.Current.MotionRight, 2));
            Upbtn.SelectedIndex = (int)Math.Round(Math.Log(EmulatorSettings.Current.MotionUp, 2));
            Downbtn.SelectedIndex = (int)Math.Round(Math.Log(EmulatorSettings.Current.MotionDown, 2));

            this.horizontalDeadzoneSlider.Value = EmulatorSettings.Current.MotionDeadzoneH;
            this.verticalDeadzoneSlider.Value = EmulatorSettings.Current.MotionDeadzoneV;
            this.adaptOrientationSwitch.IsChecked = EmulatorSettings.Current.MotionAdaptOrientation;

        }

        private void appBarCancelButton_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void appBarOkButton_Click(object sender, EventArgs e)
        {
            //save to setting object
            EmulatorSettings.Current.MotionLeft =  (int)Math.Pow(2, Leftbtn.SelectedIndex);
            EmulatorSettings.Current.MotionRight = (int)Math.Pow(2, Rightbtn.SelectedIndex);
            EmulatorSettings.Current.MotionUp = (int)Math.Pow(2, Upbtn.SelectedIndex);
            EmulatorSettings.Current.MotionDown = (int)Math.Pow(2, Downbtn.SelectedIndex);

            EmulatorSettings.Current.MotionDeadzoneH = this.horizontalDeadzoneSlider.Value;
            EmulatorSettings.Current.MotionDeadzoneV = this.verticalDeadzoneSlider.Value;
            EmulatorSettings.Current.MotionAdaptOrientation = this.adaptOrientationSwitch.IsChecked.Value;

            //save to disk
            IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
            isoSettings[SettingsPage.MotionLeftKey] = (int)Math.Pow(2, Leftbtn.SelectedIndex);
            isoSettings[SettingsPage.MotionRightKey] = (int)Math.Pow(2, Rightbtn.SelectedIndex);
            isoSettings[SettingsPage.MotionUpKey] = (int)Math.Pow(2, Upbtn.SelectedIndex);
            isoSettings[SettingsPage.MotionDownKey] = (int)Math.Pow(2, Downbtn.SelectedIndex);

            isoSettings[SettingsPage.MotionDeadzoneHKey] = this.horizontalDeadzoneSlider.Value;
            isoSettings[SettingsPage.MotionDeadzoneVKey] = this.verticalDeadzoneSlider.Value;
            isoSettings[SettingsPage.MotionAdaptOrientationKey] = this.adaptOrientationSwitch.IsChecked.Value;

            isoSettings.Save();

            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();

        }

        private void CalibrateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EmulatorSettings.Current.UseMotionControl == 1 && Accelerometer.IsSupported)
            {
                Accelerometer accl = new Accelerometer();

                try
                {
                    accl.CurrentValueChanged +=
                        new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);

                    accl.Start();
                    
                }
                catch (Exception)
                {
                    MessageBox.Show(AppResources.FailedToCalibrateText);
                }
                
            }
            else if (EmulatorSettings.Current.UseMotionControl == 2 && Motion.IsSupported)
            {
                Motion motion = new Motion();
                try
                {
                    motion.CurrentValueChanged +=
                        new EventHandler<SensorReadingEventArgs<MotionReading>>(motion_CurrentValueChanged);

                    motion.Start();
                }
                catch (Exception)
                {
                    MessageBox.Show(AppResources.FailedToCalibrateText);
                }
            }
        } //end function

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            var reading = e.SensorReading.Acceleration;

            //change setting in memory
            EmulatorSettings.Current.RestAngleX = reading.X;   //in "g" unit
            EmulatorSettings.Current.RestAngleY = reading.Y;
            EmulatorSettings.Current.RestAngleZ = reading.Z;

            //save to disk
            IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleXKey] = (double)reading.X;
            IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleYKey] = (double)reading.Y;
            IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleZKey] = (double)reading.Z;

            IsolatedStorageSettings.ApplicationSettings.Save();

            ((Accelerometer)sender).Stop();

            Dispatcher.BeginInvoke(() => MessageBox.Show(AppResources.CalibrationSuccessText));
        }


        void motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            var reading = e.SensorReading.Attitude;

            //change setting in memory
            EmulatorSettings.Current.RestAngleX = reading.Roll / 3.14159265 * 180;   //convert to degree
            EmulatorSettings.Current.RestAngleY = reading.Pitch / 3.14159265 * 180 ;

            //save to disk
            IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleXKey] = (double)reading.Roll / 3.14159265 * 180;
            IsolatedStorageSettings.ApplicationSettings[SettingsPage.RestAngleYKey] = (double)reading.Pitch / 3.14159265 * 180;


            IsolatedStorageSettings.ApplicationSettings.Save();

            ((Motion)sender).Stop();

            Dispatcher.BeginInvoke(() => MessageBox.Show(AppResources.CalibrationSuccessText));
        }

        private void horizontalDeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void horizontalDeadzoneLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (horizontalDeadzoneSlider.IsEnabled)
            {
                horizontalDeadzoneSlider.IsEnabled = false;
                horizontalDeadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                horizontalDeadzoneSlider.IsEnabled = true;
                horizontalDeadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }

        private void verticalDeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void verticalDeadzoneLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (verticalDeadzoneSlider.IsEnabled)
            {
                verticalDeadzoneSlider.IsEnabled = false;
                verticalDeadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.lock.png", UriKind.Relative));
            }
            else
            {
                verticalDeadzoneSlider.IsEnabled = true;
                verticalDeadzoneImage.ImageSource = new BitmapImage(new Uri("Assets/Icons/appbar.unlock.png", UriKind.Relative));
            }
        }


    }
}