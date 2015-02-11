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

namespace PhoneDirect3DXamlAppInterop
{
    public partial class MogaMappingPage : PhoneApplicationPage
    {
        private String[] appFunctionList = { "A", "B", "X", "Y", "L", "R", "Emulator Menu" };

        public MogaMappingPage()
        {
            InitializeComponent();
            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

            Abtn.ItemsSource = appFunctionList;
            Bbtn.ItemsSource = appFunctionList;
            Xbtn.ItemsSource = appFunctionList;
            Ybtn.ItemsSource = appFunctionList;
            L1btn.ItemsSource = appFunctionList;
            R1btn.ItemsSource = appFunctionList;
            L2btn.ItemsSource = appFunctionList;
            R2btn.ItemsSource = appFunctionList;
            LeftJoystickbtn.ItemsSource = appFunctionList;
            RightJoystickbtn.ItemsSource = appFunctionList;

            Abtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaA, 2);
            Bbtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaB, 2);
            Xbtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaX, 2);
            Ybtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaY, 2);
            L1btn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaL1, 2);
            L2btn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaL2, 2);
            R1btn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaR1, 2);
            R2btn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaR2, 2);
            LeftJoystickbtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaLeftJoystick, 2);
            RightJoystickbtn.SelectedIndex = (int)Math.Log(EmulatorSettings.Current.MogaRightJoystick, 2);

        }

        private void appBarCancelButton_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void appBarOkButton_Click(object sender, EventArgs e)
        {
            //save to setting object
            EmulatorSettings.Current.MogaA = (int)Math.Pow(2, Abtn.SelectedIndex);
            EmulatorSettings.Current.MogaB = (int)Math.Pow(2, Bbtn.SelectedIndex);
            EmulatorSettings.Current.MogaX = (int)Math.Pow(2, Xbtn.SelectedIndex);
            EmulatorSettings.Current.MogaY = (int)Math.Pow(2, Ybtn.SelectedIndex);
            EmulatorSettings.Current.MogaL1 = (int)Math.Pow(2, L1btn.SelectedIndex);
            EmulatorSettings.Current.MogaL2 = (int)Math.Pow(2, L2btn.SelectedIndex);
            EmulatorSettings.Current.MogaR1 = (int)Math.Pow(2, R1btn.SelectedIndex);
            EmulatorSettings.Current.MogaR2 = (int)Math.Pow(2, R2btn.SelectedIndex);
            EmulatorSettings.Current.MogaLeftJoystick = (int)Math.Pow(2, LeftJoystickbtn.SelectedIndex);
            EmulatorSettings.Current.MogaRightJoystick = (int)Math.Pow(2, RightJoystickbtn.SelectedIndex);


            //save to disk
            IsolatedStorageSettings isoSettings = IsolatedStorageSettings.ApplicationSettings;
            isoSettings[SettingsPage.MogaAKey] = (int)Math.Pow(2, Abtn.SelectedIndex);
            isoSettings[SettingsPage.MogaBKey] = (int)Math.Pow(2, Bbtn.SelectedIndex);
            isoSettings[SettingsPage.MogaXKey] = (int)Math.Pow(2, Xbtn.SelectedIndex);
            isoSettings[SettingsPage.MogaYKey] = (int)Math.Pow(2, Ybtn.SelectedIndex);
            isoSettings[SettingsPage.MogaL1Key] = (int)Math.Pow(2, L1btn.SelectedIndex);
            isoSettings[SettingsPage.MogaL2Key] = (int)Math.Pow(2, L2btn.SelectedIndex);
            isoSettings[SettingsPage.MogaR1Key] = (int)Math.Pow(2, R1btn.SelectedIndex);
            isoSettings[SettingsPage.MogaR2Key] = (int)Math.Pow(2, R2btn.SelectedIndex);
            isoSettings[SettingsPage.MogaLeftJoystickKey] = (int)Math.Pow(2, LeftJoystickbtn.SelectedIndex);
            isoSettings[SettingsPage.MogaRightJoystickKey] = (int)Math.Pow(2, RightJoystickbtn.SelectedIndex);

            isoSettings.Save();

            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();

        }
    }
}