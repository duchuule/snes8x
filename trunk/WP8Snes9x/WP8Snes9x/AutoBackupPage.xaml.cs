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

namespace PhoneDirect3DXamlAppInterop
{
    public partial class AutoBackupPage : PhoneApplicationPage
    {
        private bool initdone = false;
        private String[] NBackupList = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public AutoBackupPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                ((Grid)(LayoutRoot.Children[0])).Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }



            NBackupsPicker.ItemsSource = NBackupList;
            

            backupTypePicker.SelectedIndex = App.metroSettings.AutoBackupMode;

            if (App.metroSettings.AutoBackupMode == 0)
                NBackupsPicker.Visibility = System.Windows.Visibility.Collapsed;
            else
                NBackupsPicker.Visibility = System.Windows.Visibility.Visible;

            NBackupsPicker.SelectedIndex = App.metroSettings.NRotatingBackup - 1;


            backupIngameSaveCheck.IsChecked = App.metroSettings.BackupIngameSave;
            backupLastManualSLotCheck.IsChecked = App.metroSettings.BackupManualSave;
            backupAutoSLotCheck.IsChecked = App.metroSettings.BackupAutoSave;
            backupOnlyWifiCheck.IsChecked = App.metroSettings.BackupOnlyWifi;



            this.Loaded += (o, e) =>
                {
                initdone = true;
            };
        }

        private void backupTypePicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (initdone)
            {

                    App.metroSettings.AutoBackupMode = backupTypePicker.SelectedIndex;

                    if (App.metroSettings.AutoBackupMode == 0)
                        NBackupsPicker.Visibility = System.Windows.Visibility.Collapsed;
                    else
                        NBackupsPicker.Visibility = System.Windows.Visibility.Visible;
             }
        }

        private void appBarOkButton_Click(object sender, EventArgs e)
        {

            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void appBarCancelButton_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }


        private void backupIngameSaveCheck_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
                App.metroSettings.BackupIngameSave = backupIngameSaveCheck.IsChecked.Value;
        }

        private void backupLastManualSLotCheck_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
                App.metroSettings.BackupManualSave = backupLastManualSLotCheck.IsChecked.Value;
        }

        private void backupAutoSLotCheck_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
                App.metroSettings.BackupAutoSave = backupAutoSLotCheck.IsChecked.Value;
        }

        //private void whenToBackupPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (initdone)
        //        App.metroSettings.WhenToBackup = whenToBackupPicker.SelectedIndex;
        //}

        private void NBackupsPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initdone)
            {
                App.metroSettings.NRotatingBackup = NBackupsPicker.SelectedIndex + 1;
            }
        }

        private void backupOnlyWifiCheck_Click(object sender, RoutedEventArgs e)
        {
            if (initdone)
            {
                App.metroSettings.BackupOnlyWifi = backupOnlyWifiCheck.IsChecked.Value;
            }
        }
    } //end class


    public class ToolTipData
    {
        public string Description
        {
            get;
            set;
        }
    }
}