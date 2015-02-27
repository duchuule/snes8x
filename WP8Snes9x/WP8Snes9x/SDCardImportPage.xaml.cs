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

using Ionic.Zip;
using SharpCompress.Reader;
using SharpCompress.Archive;

namespace PhoneDirect3DXamlAppInterop
{

    public partial class SDCardImportPage : PhoneApplicationPage
    {


        // A collection of routes (.GPX files) for binding to the ListBox.
        public ObservableCollection<ExternalStorageFile> Routes { get; set; }
        List<List<SDCardListItem>> skydriveStack;
        int downloadsInProgress = 0;


        ExternalStorageDevice sdCard = null;

        // Constructor
        public SDCardImportPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                ((Grid)(LayoutRoot.Children[0])).Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }






            // Initialize the collection for routes.
            Routes = new ObservableCollection<ExternalStorageFile>();

            this.skydriveStack = new List<List<SDCardListItem>>();
            this.skydriveStack.Add(new List<SDCardListItem>());


            // Enable data binding to the page itself.
            this.DataContext = this;


        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            // Connect to the current SD card.
            sdCard = (await ExternalStorage.GetExternalStorageDevicesAsync()).FirstOrDefault();

            // If the SD card is present, add GPX files to the Routes collection.
            if (sdCard != null)
            {

                this.skydriveStack[0].Add(new SDCardListItem()
                {
                    Name = "Root",
                    isFolder = true,
                    ThisFolder = sdCard.RootFolder,
                    ParentPath = null
                });


                this.OpenFolder(sdCard.RootFolder);

            }
            else
            {
                // No SD card is present.
                MessageBox.Show(AppResources.SDCardMissingText);
            }

            base.OnNavigatedTo(e);
        }


        async void OpenFolder(ExternalStorageFolder folderToOpen)
        {
            try
            {
                //new list
                List<SDCardListItem> listItems = new List<SDCardListItem>();


                // Get all folders on root
                List<ExternalStorageFolder> listFolders = (await folderToOpen.GetFoldersAsync()).ToList();
                foreach (ExternalStorageFolder folder in listFolders)
                {

                    SDCardListItem item = new SDCardListItem()
                    {
                        isFolder = true,
                        Name = folder.Name,
                        ParentPath = folderToOpen.Path,
                        ThisFolder = folder
                    };
                    listItems.Add(item);
                }

                List<ExternalStorageFile> listFiles = (await folderToOpen.GetFilesAsync()).ToList();
                foreach (ExternalStorageFile file in listFiles)
                {

                    SDCardListItem item = new SDCardListItem()
                    {
                        isFolder = false,
                        Name = file.Name,
                        ParentPath = folderToOpen.Path,
                        ThisFile = file
                    };

                    item.Type = SkyDriveItemType.File;  //default type

                    
                    if (item.ThisFile.Path.ToLower().EndsWith(".zib") || item.ThisFile.Path.ToLower().EndsWith(".zip"))
                        item.Type = SkyDriveItemType.Zip;
                    else if (item.ThisFile.Path.ToLower().EndsWith(".rar"))
                        item.Type = SkyDriveItemType.Rar;
                    else if (item.ThisFile.Path.ToLower().EndsWith(".7z"))
                        item.Type = SkyDriveItemType.SevenZip;
                    else if (item.ThisFile.Path.ToLower().EndsWith(".smc") || item.ThisFile.Path.ToLower().EndsWith(".sfc"))
                        item.Type = SkyDriveItemType.ROM;
                    else if (item.ThisFile.Path.ToLower().EndsWith(".srm"))
                        item.Type = SkyDriveItemType.SRAM;
                    else
                    {
                        for (int i = 0; i <= 10; i++)
                        {
                            String ext;
                            if (i <= 9)
                            {
                                ext = ".00" + i;
                            }
                            else
                            {
                                ext = ".0" + i;
                            }
                            if (item.ThisFile.Path.ToLower().EndsWith(ext))
                            {
                                item.Type = SkyDriveItemType.Savestate;
                                break;
                            }
                        }
                    }
                        

                    listItems.Add(item);
                }

                //ordered by name
                listItems = listItems.OrderBy(x => x.Name).ToList();

                this.skydriveStack.Add(listItems);
                this.skydriveList.ItemsSource = listItems;
            }
            catch (Exception)
            {
                MessageBox.Show(AppResources.ErrorReadingSDCardText);
            }
        }


        async void skydriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SDCardListItem item = this.skydriveList.SelectedItem as SDCardListItem;
            if (item == null)
                return;

            ROMDatabase db = ROMDatabase.Current;

            if (item.isFolder)
            {
                this.skydriveList.ItemsSource = null;
                this.currentFolderBox.Text = item.Name;
                this.OpenFolder(item.ThisFolder);

            }

            else  //file
            {
                if (item.Type == SkyDriveItemType.Zip || item.Type == SkyDriveItemType.Rar || item.Type == SkyDriveItemType.SevenZip)
                {
                    this.skydriveList.ItemsSource = null;
                    this.currentFolderBox.Text = item.Name;
                    this.downloadsInProgress++;

                    await this.DownloadFile(item);

                    try
                    {
                        List<SDCardListItem> listItems;

                        listItems = this.GetFilesInArchive(item);

                        this.skydriveStack.Add(listItems);
                        this.skydriveList.ItemsSource = listItems;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }

                    this.downloadsInProgress--;
                }

                else if (item.Type == SkyDriveItemType.ROM)
                {
                    // Download
                    if (!item.Downloading)
                    {
                        this.downloadsInProgress++;
                        if (item.Stream == null)
                            await this.DownloadFile(item);


                        await SkyDriveImportPage.ImportROM(item, this);

                        this.downloadsInProgress--;
                    }
                    else
                    {
                        MessageBox.Show(AppResources.AlreadyDownloadingText, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }

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

                    //determine slot number
                    if (item.Type == SkyDriveItemType.Savestate)  //save state
                    {
                        string slot = item.Name.Substring(item.Name.Length - 1, 1);
                        int parsedSlot = 0;
                        if (!int.TryParse(slot, out parsedSlot))
                        {
                            MessageBox.Show(AppResources.ImportSavestateInvalidFormat, AppResources.ErrorCaption, MessageBoxButton.OK);
                            return;
                        }
                    }

                    // Download
                    if (!item.Downloading)
                    {
                        this.downloadsInProgress++;

                        if (item.Stream == null)
                            await this.DownloadFile(item);

                        await SkyDriveImportPage.ImportSave(item, this);

                        this.downloadsInProgress--;

                    }
                    else
                    {
                        MessageBox.Show(AppResources.AlreadyDownloadingText, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }


                }
            }

        }


        private async Task DownloadFile(SDCardListItem item)
        {
            var indicator = SystemTray.GetProgressIndicator(this);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.DownloadingProgressText, item.Name);
            try
            {
                Stream s = await item.ThisFile.OpenForReadAsync();
                if (s != null)
                {
                    item.Stream = s;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
            }

#if GBC
            indicator.Text = AppResources.ApplicationTitle2;
#else
            indicator.Text = AppResources.ApplicationTitle;
#endif
            indicator.IsIndeterminate = false;

            item.Downloading = false;

        }



        private List<SDCardListItem> GetFilesInArchive(SDCardListItem item)
        {
            List<SDCardListItem> listItems = new List<SDCardListItem>();


            if (item.Stream != null)
            {
                //fix SD card stream bug
                Stream s = new MemoryStream();
                item.Stream.CopyTo(s);
                s.Position = 0;
                item.Stream.Close();// close because we copy it to s already
                item.Stream = null;

                //get list of file
                IArchive archive = null;

                if (item.Type == SkyDriveItemType.Rar)
                    archive = SharpCompress.Archive.Rar.RarArchive.Open(s);
                else if (item.Type == SkyDriveItemType.Zip)
                    archive = SharpCompress.Archive.Zip.ZipArchive.Open(s);
                else if (item.Type == SkyDriveItemType.SevenZip)
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

                        SDCardListItem listItem = new SDCardListItem()
                        {
                            Name = name,
                            Type = type,
                            isFolder = false,
                            ParentPath = item.ThisFile.Path,
                            Stream = data
                        };

                        listItems.Add(listItem);

                    }
                }

                //close the zip stream since we have the stream of each item inside it already
                s.Close();

            }

            return listItems;
        }




        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (this.downloadsInProgress > 0)
            {
                MessageBox.Show(AppResources.ImportWaitComplete);
                e.Cancel = true;
                return;
            }

            if (this.skydriveStack.Count <= 2)
            {
                return;
            }

            try
            {
                //close the zip stream
                foreach (SDCardListItem item in this.skydriveStack[this.skydriveStack.Count - 1])
                {
                    if (item.Stream != null)
                    {
                        item.Stream.Close();
                        item.Stream = null;
                    }
                }


                //remove the last stack
                this.skydriveStack.RemoveAt(this.skydriveStack.Count - 1);
                this.skydriveList.ItemsSource = this.skydriveStack.Last();

                string parentName = this.skydriveStack[this.skydriveStack.Count - 2].Where(x => x.isFolder).First(item =>
                {
                    return item.ThisFolder.Path.Equals(this.skydriveStack[this.skydriveStack.Count - 1][0].ParentPath);
                }).Name;

                this.currentFolderBox.Text = parentName;

                e.Cancel = true;
            }
            catch (Exception) { }

            base.OnBackKeyPress(e);
        }



        //        private async Task ImportROM(ExternalStorageFile file)
        //        {
        //            var indicator = SystemTray.GetProgressIndicator(this);
        //            indicator.IsIndeterminate = true;
        //            indicator.Text = String.Format(AppResources.ImportingProgressText, file.Name);

        //            try
        //            {
        //                ROMDatabase db = ROMDatabase.Current;
        //                StorageFolder folder = ApplicationData.Current.LocalFolder;
        //                StorageFolder romFolder = await folder.CreateFolderAsync("roms", CreationCollisionOption.OpenIfExists);

        //                Stream s = await file.OpenForReadAsync();

        //                byte[] tmpBuf = new byte[s.Length];
        //                StorageFile destinationFile = await romFolder.CreateFileAsync(file.Name, CreationCollisionOption.ReplaceExisting);

        //                using (IRandomAccessStream destStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
        //                using (DataWriter writer = new DataWriter(destStream))
        //                {
        //                    while (s.Read(tmpBuf, 0, tmpBuf.Length) != 0)
        //                    {
        //                        writer.WriteBytes(tmpBuf);
        //                    }
        //                    await writer.StoreAsync();
        //                    await writer.FlushAsync();
        //                    writer.DetachStream();
        //                }
        //                s.Close();



        //                var romEntry = db.GetROM(file.Name);
        //                bool fileExisted = false;
        //                if (romEntry != null)
        //                    fileExisted = true;

        //                if (!fileExisted)
        //                {
        //                    var entry = FileHandler.InsertNewDBEntry(destinationFile.Name);
        //                    await FileHandler.FindExistingSavestatesForNewROM(entry);
        //                    db.CommitChanges();
        //                }

        //                MessageBox.Show(String.Format(AppResources.ImportCompleteText, destinationFile.Name));
        //            }
        //            catch (Exception ex)
        //            {
        //                //MessageBox.Show(String.Format(AppResources.DownloadErrorText, file.Name, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
        //            }

        //#if GBC
        //            indicator.Text = AppResources.ApplicationTitle2;
        //#else
        //            indicator.Text = AppResources.ApplicationTitle;
        //#endif
        //            indicator.IsIndeterminate = false;
        //        }


    } //end class

    public class SDCardListItem : ImportFileItem
    {

        public bool isFolder { get; set; } //true if folder, false if file
        public ExternalStorageFile ThisFile { get; set; } //only 1 of ThisFile or ThisFolder will be set
        public ExternalStorageFolder ThisFolder { get; set; }
        public string ParentPath { get; set; }
        public int FolderChildrenCount { get; set; }


    }

}
