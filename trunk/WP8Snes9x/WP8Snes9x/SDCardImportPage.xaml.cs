/* 
    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using Microsoft.Phone.Controls;
using Microsoft.Phone.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;



using PhoneDirect3DXamlAppInterop.Resources;
using PhoneDirect3DXamlAppInterop.Database;
using Windows.Storage;
using Windows.Storage.Streams;



namespace PhoneDirect3DXamlAppInterop
{

    public partial class SDCardImportPage : PhoneApplicationPage
    {


        // A collection of routes (.GPX files) for binding to the ListBox.
        public ObservableCollection<ExternalStorageFile> Routes { get; set; }

        ExternalStorageDevice sdCard = null;

        // Constructor
        public SDCardImportPage()
        {
            InitializeComponent();

            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 3);
            }


            // Initialize the collection for routes.
            Routes = new ObservableCollection<ExternalStorageFile>();

            // Enable data binding to the page itself.
            this.DataContext = this;
        }




        // Scan the SD card to see if it contains any GPX files.
        private async void scanExternalStorageButton_Click_1(object sender, RoutedEventArgs e)
        {
            // Clear the collection bound to the page.
            Routes.Clear();

            // Connect to the current SD card.
            sdCard = (await ExternalStorage.GetExternalStorageDevicesAsync()).FirstOrDefault();

            // If the SD card is present, add GPX files to the Routes collection.
            if (sdCard != null)
            {
                try
                {

                    // Look for a folder on the SD card named roms.
                    ExternalStorageFolder routesFolder = await sdCard.GetFolderAsync("roms");

                    // Get all files from the Routes folder.
                    IEnumerable<ExternalStorageFile> routeFiles = await routesFolder.GetFilesAsync();

                    // Add each rom file to the Routes collection.
                    foreach (ExternalStorageFile esf in routeFiles)
                    {
                        if (esf.Path.ToLower().EndsWith(".smc") || esf.Path.ToLower().EndsWith(".sfc"))

                        {
                            Routes.Add(esf);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // No Routes folder is present.
                    MessageBox.Show("The 'roms' folder is missing on your SD card. Add a 'roms' folder containing at least one rom file and try again.");
                }
            }
            else
            {
                // No SD card is present.
                MessageBox.Show("The SD card is missing. Insert an SD card try again.");
            }
        }


        // When a different route is selected, copy and import the file
        private void romFilesListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;

            if (lb.SelectedItem != null)
            {
                ExternalStorageFile esf = (ExternalStorageFile)lb.SelectedItem;

                ImportFile(esf);

            }
        }


        private async void ImportFile(ExternalStorageFile file)
        {
            var indicator = new ProgressIndicator()
            {
                IsIndeterminate = true,
                IsVisible = true,
                Text = String.Format(AppResources.ImportingProgressText, file.Name)
            };

            SystemTray.SetProgressIndicator(this, indicator);
            ROMDatabase db = ROMDatabase.Current;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await folder.CreateFolderAsync("roms", CreationCollisionOption.OpenIfExists);

            Stream s = await file.OpenForReadAsync();

            byte[] tmpBuf = new byte[s.Length];
            StorageFile destinationFile = await romFolder.CreateFileAsync(file.Name, CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream destStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
            using (DataWriter writer = new DataWriter(destStream))
            {
                while (s.Read(tmpBuf, 0, tmpBuf.Length) != 0)
                {
                    writer.WriteBytes(tmpBuf);
                }
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            s.Close();
            SystemTray.GetProgressIndicator(this).IsVisible = false;


            var entry = FileHandler.InsertNewDBEntry(destinationFile.Name);
            await FileHandler.FindExistingSavestatesForNewROM(entry);
            db.CommitChanges();

            MessageBox.Show(String.Format(AppResources.ImportCompleteText, destinationFile.Name));
        }
    }
}
