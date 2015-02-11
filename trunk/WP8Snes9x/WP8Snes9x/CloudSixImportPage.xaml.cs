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
using System.Threading.Tasks;



using PhoneDirect3DXamlAppInterop.Resources;
using PhoneDirect3DXamlAppInterop.Database;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Phone.Storage.SharedAccess;

using CloudSixConnector.FilePicker;
using Ionic.Zip;
using SharpCompress.Archive;

namespace PhoneDirect3DXamlAppInterop
{

    public partial class CloudSixImportPage : PhoneApplicationPage
    {


        // A collection of routes (.GPX files) for binding to the ListBox.
        public List<ImportFileItem> skydriveStack { get; set; }

        int downloadsInProgress = 0;
        IStorageFile tempZipFile = null;

        // Constructor
        public CloudSixImportPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                ((Grid)(LayoutRoot.Children[0])).Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }



#if GBC
            SystemTray.GetProgressIndicator(this).Text = AppResources.ApplicationTitle2;
#endif



            // Initialize the collection for routes.
            skydriveStack = new List<ImportFileItem>();
            

            
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            //get the fileID
            try
            {
                String fileID = NavigationContext.QueryString["fileToken"];
                NavigationContext.QueryString.Remove("fileToken");

                //currently only zip file need to use this page, copy the zip file to local storage
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                string fileName = SharedStorageAccessManager.GetSharedFileName(fileID);
                tempZipFile = await SharedStorageAccessManager.CopySharedFileAsync(localFolder, fileName, NameCollisionOption.ReplaceExisting, fileID);

                //set the title
                CloudSixFileSelected fileinfo = CloudSixPicker.GetAnswer(fileID);
                currentFolderBox.Text = fileinfo.Filename;
                string ext = Path.GetExtension(fileinfo.Filename).ToLower();

                //open zip file or rar file
                try
                {
                    SkyDriveItemType type = SkyDriveItemType.File;
                    if (ext == ".zip" || ext == ".zib")
                        type = SkyDriveItemType.Zip;
                    else if (ext == ".rar")
                        type = SkyDriveItemType.Rar;
                    else if (ext == ".7z")
                        type = SkyDriveItemType.SevenZip;

                    skydriveStack = await GetFilesInArchive(type, tempZipFile);

                    this.skydriveList.ItemsSource = skydriveStack;

                    var indicator = SystemTray.GetProgressIndicator(this);
                    indicator.IsIndeterminate = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
                }
                
                

            }
            catch (Exception)
            {
                MessageBox.Show(AppResources.FileAssociationError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }


           


            base.OnNavigatedTo(e);
        }



        private async Task<List<ImportFileItem>> GetFilesInArchive(SkyDriveItemType parentType, IStorageFile file)
        {
            List<ImportFileItem> listItems = new List<ImportFileItem>();

            if (file != null)
            {
                IRandomAccessStream accessStream = await file.OpenReadAsync();
                Stream s = accessStream.AsStreamForRead((int)accessStream.Size);

                //get list of file
                IArchive archive = null;

                if (parentType == SkyDriveItemType.Rar)
                    archive = SharpCompress.Archive.Rar.RarArchive.Open(s);
                else if (parentType == SkyDriveItemType.Zip)
                    archive = SharpCompress.Archive.Zip.ZipArchive.Open(s);
                else if (parentType == SkyDriveItemType.SevenZip)
                    archive = SharpCompress.Archive.SevenZip.SevenZipArchive.Open(s);

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        Stream data = new MemoryStream();
                        entry.WriteTo(data);
                        data.Position = 0;
                        String name = entry.FilePath;

                        SkyDriveItemType type = SkyDriveItemType.File;
                        int dotIndex = -1;
                        if ((dotIndex = name.LastIndexOf('.')) != -1)
                        {
                            String substrName = name.Substring(dotIndex).ToLower();
                            type = SkyDriveImportPage.GetSkyDriveItemType(substrName);
                        }

                        if (type == SkyDriveItemType.File)
                        {
                            data.Close();
                            continue;
                        }

                        ImportFileItem listItem = new ImportFileItem()
                        {
                            Name = name,
                            Type = type,
                            Stream = data
                        };

                        listItems.Add(listItem);

                    }
                }

                //close the zip stream since we have the stream of each item inside it already
                s.Close();
                s = null;
            }

            return listItems;
        }


        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {

            //delete the temporary zip file
            if (tempZipFile != null)
                await tempZipFile.DeleteAsync();

            base.OnNavigatedFrom(e);
        }





        async void skydriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImportFileItem item = this.skydriveList.SelectedItem as ImportFileItem;
            if (item == null)
                return;

            ROMDatabase db = ROMDatabase.Current;


            if (item.Type == SkyDriveItemType.ROM)
            {

                if (item.Stream != null)
                    await SkyDriveImportPage.ImportROM(item, this);

            }

            else if (item.Type == SkyDriveItemType.Savestate || item.Type == SkyDriveItemType.SRAM)
            {
                //check to make sure there is a rom with matching name
                ROMDBEntry entry = null;

                if (item.Type == SkyDriveItemType.Savestate)  //save state
                    entry = db.GetROMFromSavestateName(item.Name);
                else if (item.Type == SkyDriveItemType.SRAM) //sram
                    entry = db.GetROMFromSRAMName(item.Name);

                if (entry == null) //no matching file name
                {
                    MessageBox.Show(AppResources.NoMatchingNameText, AppResources.ErrorCaption, MessageBoxButton.OK);
                    return;
                }

                //check to make sure format is right
                if (item.Type == SkyDriveItemType.Savestate)  //save state
                {
                    string slot = item.Name.Substring(item.Name.Length - 5, 1);
                    int parsedSlot = 0;
                    if (!int.TryParse(slot, out parsedSlot))
                    {
                        MessageBox.Show(AppResources.ImportSavestateInvalidFormat, AppResources.ErrorCaption, MessageBoxButton.OK);
                        return;
                    }
                }

               

                if (item.Stream != null)
                    await SkyDriveImportPage.ImportSave(item, this);


            }

        }


//        private async Task DownloadFile(FileListItem item)
//        {
//            var indicator = SystemTray.GetProgressIndicator(this);
//            indicator.IsIndeterminate = true;
//            indicator.Text = String.Format(AppResources.DownloadingProgressText, item.Name);
//            try
//            {
//                Stream s = await item.ThisFile.OpenForReadAsync();
//                if (s != null)
//                {
//                    item.Stream = s;
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
//            }

//#if GBC
//            indicator.Text = AppResources.ApplicationTitle2;
//#else
//            indicator.Text = AppResources.ApplicationTitle;
//#endif
//            indicator.IsIndeterminate = false;

//            item.Downloading = false;

//        }


       


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (this.downloadsInProgress > 0)
            {
                MessageBox.Show(AppResources.ImportWaitComplete);
                e.Cancel = true;
                return;
            }


            try
            {
                //close the zip stream
                foreach (ImportFileItem item in this.skydriveStack)
                {
                    if (item.Stream != null)
                    {
                        item.Stream.Close();
                        item.Stream = null;
                    }
                }
                
            }
            catch (Exception) { }


            base.OnBackKeyPress(e);
        }




    } //end class



}
