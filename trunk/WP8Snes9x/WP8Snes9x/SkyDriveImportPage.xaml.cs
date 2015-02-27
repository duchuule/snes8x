using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Live;
using Microsoft.Live.Controls;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using PhoneDirect3DXamlAppInterop.Resources;
using PhoneDirect3DXamlAppInterop.Database;
using System.Windows.Media;
using Microsoft.Phone.Tasks;
using System.IO;
using Ionic.Zip;
using SharpCompress.Reader;
using SharpCompress.Archive;

namespace PhoneDirect3DXamlAppInterop
{


    public partial class SkyDriveImportPage : PhoneApplicationPage
    {
        private LiveConnectSession session;
        List<List<SkyDriveListItem>> skydriveStack;
        private double labelHeight;
        int downloadsInProgress = 0;
        bool initialPageLoaded = false;

        public SkyDriveImportPage()
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


            this.labelHeight = this.statusLabel.Height;
            this.session = PhoneApplicationService.Current.State["parameter"] as LiveConnectSession;
            PhoneApplicationService.Current.State.Remove("parameter");
            if (this.session == null)
            {
                throw new ArgumentException("Parameter passed to SkyDriveImportPage must be a LiveConnectSession.");
            }

            this.skydriveStack = new List<List<SkyDriveListItem>>();
            this.skydriveStack.Add(new List<SkyDriveListItem>());
            this.skydriveStack[0].Add(new SkyDriveListItem()
            {
                Name = "Root",
                SkyDriveID = "me/skydrive",
                Type = SkyDriveItemType.Folder,
                ParentID = ""
            });
            this.currentFolderBox.Text = this.skydriveStack[0][0].Name;


        }

        async void skydriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SkyDriveListItem item = this.skydriveList.SelectedItem as SkyDriveListItem;
            if (item == null)
                return;

            ROMDatabase db = ROMDatabase.Current;


            try
            {
                LiveConnectClient client = new LiveConnectClient(this.session);
                if (item.Type == SkyDriveItemType.Folder)
                {
                    if (this.session != null)
                    {
                        this.skydriveList.ItemsSource = null;
                        this.currentFolderBox.Text = item.Name;
                        LiveOperationResult result = await client.GetAsync(item.SkyDriveID + "/files");
                        this.client_GetCompleted(result);
                    }
                }

                else if (item.Type == SkyDriveItemType.Zip || item.Type == SkyDriveItemType.Rar || item.Type == SkyDriveItemType.SevenZip)
                {
                    if (this.session != null)
                    {
                        this.skydriveList.ItemsSource = null;
                        this.currentFolderBox.Text = item.Name;
                        this.downloadsInProgress++;

                        await this.DownloadFile(item, client);

                        try
                        {
                            List<SkyDriveListItem> listItems;

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
                }

                else if (item.Type == SkyDriveItemType.ROM)
                {
                    // Download
                    if (!item.Downloading)
                    {
                        this.downloadsInProgress++;
                        if (item.Stream == null)
                            await this.DownloadFile(item, client);


                        await ImportROM(item, this);

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

                    if (item.Type == SkyDriveItemType.Savestate)
                        entry = db.GetROMFromSavestateName(item.Name);
                    else if (item.Type == SkyDriveItemType.SRAM)
                        entry = db.GetROMFromSRAMName(item.Name);

                    if (entry == null) //no matching file name
                    {
                        MessageBox.Show(AppResources.NoMatchingNameText, AppResources.ErrorCaption, MessageBoxButton.OK);
                        return;
                    }

                    //determine the slot number
                    if (item.Type == SkyDriveItemType.Savestate)
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
                            await this.DownloadFile(item, client);

                        await ImportSave(item, this);

                        this.downloadsInProgress--;

                    }
                    else
                    {
                        MessageBox.Show(AppResources.AlreadyDownloadingText, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }

                }


                this.statusLabel.Height = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }



        private async Task DownloadFile(SkyDriveListItem item, LiveConnectClient client)
        {
            var indicator = SystemTray.GetProgressIndicator(this);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.DownloadingProgressText, item.Name);

            item.Downloading = true;
            try
            {
                LiveDownloadOperationResult e = await client.DownloadAsync(item.SkyDriveID + "/content");
                if (e != null)
                {
                    item.Stream = e.Stream;
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




        private List<SkyDriveListItem> GetFilesInArchive(SkyDriveListItem item)
        {
            List<SkyDriveListItem> listItems = new List<SkyDriveListItem>();

            if (item.Stream != null)
            {
                //get list of file
                IArchive archive = null;

                if (item.Type == SkyDriveItemType.Rar)
                    archive = SharpCompress.Archive.Rar.RarArchive.Open(item.Stream);
                else if (item.Type == SkyDriveItemType.Zip)
                    archive = SharpCompress.Archive.Zip.ZipArchive.Open(item.Stream);
                else if (item.Type == SkyDriveItemType.SevenZip)
                    archive = SharpCompress.Archive.SevenZip.SevenZipArchive.Open(item.Stream);

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
                            type = GetSkyDriveItemType( substrName);
                        }

                        if (type == SkyDriveItemType.File)
                        {
                            data.Close();
                            continue;
                        }

                        SkyDriveListItem listItem = new SkyDriveListItem()
                        {
                            Name = name,
                            SkyDriveID = "",
                            Type = type,
                            ParentID = item.SkyDriveID,
                            Stream = data
                        };

                        listItems.Add(listItem);

                    }
                }

                //close the zip stream since we have the stream of each item inside it already
                item.Stream.Close();
                item.Stream = null;
            }

            return listItems;
        }

        public static SkyDriveItemType GetSkyDriveItemType( String substrName)
        {
            if (substrName.Equals(".zip") || substrName.Equals(".zib"))
            {
                return SkyDriveItemType.Zip;
            }
            else if (substrName.Equals(".rar"))
            {
                return SkyDriveItemType.Rar;
            }
            else if (substrName.Equals(".7z"))
            {
                return SkyDriveItemType.SevenZip;
            }
            else if (substrName.Equals(".smc") || substrName.Equals(".sfc"))
            {
                return SkyDriveItemType.ROM;
            }
            else if (substrName.Equals(".srm"))
            {
                return SkyDriveItemType.SRAM;
            }
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
                    if (substrName.Equals(ext))
                    {
                        return SkyDriveItemType.Savestate;

                    }
                }
            }
            return SkyDriveItemType.File;

        }


        public static async Task ImportSave(ImportFileItem item, DependencyObject page)
        {
            var indicator = SystemTray.GetProgressIndicator(page);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.ImportingProgressText, item.Name);

            try
            {

                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder romFolder = await folder.CreateFolderAsync(FileHandler.ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
                StorageFolder saveFolder = await romFolder.CreateFolderAsync(FileHandler.SAVE_DIRECTORY, CreationCollisionOption.OpenIfExists);


                ROMDatabase db = ROMDatabase.Current;


                if (item.Stream != null)
                {
                    byte[] tmpBuf = new byte[32];
                    StorageFile destinationFile = null;

                    ROMDBEntry entry = null;

                    if (item.Type == SkyDriveItemType.SRAM)
                    {
                        entry = db.GetROMFromSRAMName(item.Name);
                        if (entry != null)
                        {
                            entry.SuspendAutoLoadLastState = true;
                            destinationFile = await saveFolder.CreateFileAsync(Path.GetFileNameWithoutExtension(entry.FileName) + ".srm", CreationCollisionOption.ReplaceExisting);

                        }
                        else
                            destinationFile = await saveFolder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);


                    }
                    else if (item.Type == SkyDriveItemType.Savestate)
                    {
                        entry = db.GetROMFromSavestateName(item.Name);


                        if (entry != null)
                            destinationFile = await saveFolder.CreateFileAsync(Path.GetFileNameWithoutExtension(entry.FileName) + item.Name.Substring(item.Name.Length - 4), CreationCollisionOption.ReplaceExisting);
                        else
                            destinationFile = await saveFolder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);

                    }




                    using (IRandomAccessStream destStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                    using (DataWriter writer = new DataWriter(destStream))
                    {
                        if (item.Stream.CanSeek)
                            item.Stream.Seek(0, SeekOrigin.Begin);

                        while (item.Stream.Read(tmpBuf, 0, tmpBuf.Length) != 0)
                        {
                            writer.WriteBytes(tmpBuf);
                        }
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        writer.DetachStream();
                    }

                    item.Downloading = false;

                    if (item.Type == SkyDriveItemType.Savestate)
                    {
                        String number = item.Name.Substring(item.Name.Length - 1, 1);
                        int slot = int.Parse(number);

                        if (entry != null) //NULL = do nothing
                        {
                            SavestateEntry saveentry = db.SavestateEntryExisting(entry.FileName, slot);
                            if (saveentry != null)
                            {
                                //delete entry
                                db.RemoveSavestateFromDB(saveentry);

                            }
                            SavestateEntry ssEntry = new SavestateEntry()
                            {
                                ROM = entry,
                                Savetime = DateTime.Now,
                                Slot = slot,
                                FileName = item.Name
                            };
                            db.Add(ssEntry);
                            db.CommitChanges();


                        }
                    }

                    MessageBox.Show(String.Format(AppResources.DownloadCompleteText, item.Name));
                }
                else
                {

                    MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, "Import error"), AppResources.ErrorCaption, MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
            }

#if GBC
            indicator.Text = AppResources.ApplicationTitle2;
#else
            indicator.Text = AppResources.ApplicationTitle;
#endif
            indicator.IsIndeterminate = false;

        }





        public static async Task ImportROM(ImportFileItem item, DependencyObject page)
        {
            var indicator = SystemTray.GetProgressIndicator(page);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.ImportingProgressText, item.Name);

            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder romFolder = await folder.CreateFolderAsync("roms", CreationCollisionOption.OpenIfExists);


                ROMDatabase db = ROMDatabase.Current;
                var romEntry = db.GetROM(item.Name);
                bool fileExisted = false;
                if (romEntry != null)
                {
                    fileExisted = true;
                    //MessageBox.Show(String.Format(AppResources.ROMAlreadyExistingError, item.Name), AppResources.ErrorCaption, MessageBoxButton.OK);
                    //return;
                }


                if (item.Stream != null)
                {
                    byte[] tmpBuf = new byte[32];
                    StorageFile destinationFile = await romFolder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);
                    using (IRandomAccessStream destStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                    using (DataWriter writer = new DataWriter(destStream))
                    {
                        if (item.Stream.CanSeek)
                            item.Stream.Seek(0, SeekOrigin.Begin);
                        while (item.Stream.Read(tmpBuf, 0, tmpBuf.Length) != 0)
                        {
                            writer.WriteBytes(tmpBuf);
                        }


                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        writer.DetachStream();
                    }

                    item.Downloading = false;
                    if (!fileExisted)
                    {
                        var entry = FileHandler.InsertNewDBEntry(destinationFile.Name);
                        await FileHandler.FindExistingSavestatesForNewROM(entry);
                        db.CommitChanges();
                    }

                    //update voice command list
                    await MainPage.UpdateGameListForVoiceCommand();

                    MessageBox.Show(String.Format(AppResources.DownloadCompleteText, item.Name));
                }
                else
                {
                    MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, "Import error"), AppResources.ErrorCaption, MessageBoxButton.OK);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
#if GBC
            indicator.Text = AppResources.ApplicationTitle2;
#else
            indicator.Text = AppResources.ApplicationTitle;
#endif
            indicator.IsIndeterminate = false;

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
                foreach (SkyDriveListItem item in this.skydriveStack[this.skydriveStack.Count - 1])
                {
                    if (item.Stream != null)
                    {
                        if (item.Stream.CanSeek) //this is random access stream of zip file
                            item.Stream.Close();

                        item.Stream = null;
                    }
                }

                // Ordner zurückspringen
                this.skydriveStack.RemoveAt(this.skydriveStack.Count - 1);
                this.skydriveList.ItemsSource = this.skydriveStack.Last();

                string parentName = this.skydriveStack[this.skydriveStack.Count - 2].First(item =>
                {
                    return item.SkyDriveID.Equals(this.skydriveStack[this.skydriveStack.Count - 1][0].ParentID);
                }).Name;

                this.currentFolderBox.Text = parentName;

                e.Cancel = true;
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!this.initialPageLoaded)
            {
                try
                {
                    if (this.session != null)
                    {
                        LiveConnectClient client = new LiveConnectClient(this.session);
                        LiveOperationResult result = await client.GetAsync(this.skydriveStack[0][0].SkyDriveID + "/files");
                        this.client_GetCompleted(result);
                        this.statusLabel.Height = 0;
                        this.initialPageLoaded = true;
                    }
                }
                catch (LiveConnectException)
                {
                    this.statusLabel.Height = this.labelHeight;
                    this.statusLabel.Text = AppResources.SkyDriveInternetLost;
                }
            }

            base.OnNavigatedTo(e);
        }

        void client_GetCompleted(LiveOperationResult e)
        {
            if (e != null)
            {
                List<object> list = e.Result["data"] as List<object>;

                if (list == null)
                {
                    return;
                }
                List<SkyDriveListItem> listItems = new List<SkyDriveListItem>();
                foreach (var item in list)
                {

                    IDictionary<string, object> dict = item as IDictionary<string, object>;
                    if (dict == null)
                    {
                        continue;
                    }

                    if (this.skydriveStack.Count == 1)
                    {
                        this.skydriveStack.Last()[0].SkyDriveID = dict["parent_id"].ToString();
                    }

                    String name = dict["name"].ToString();
                    SkyDriveItemType type;
                    if (dict["type"].Equals("folder") || dict["type"].Equals("album"))
                    {
                        type = SkyDriveItemType.Folder;
                    }
                    else
                    {
                        type = SkyDriveItemType.File;
                        int dotIndex = -1;
                        if ((dotIndex = name.LastIndexOf('.')) != -1)
                        {
                            String substrName = name.Substring(dotIndex).ToLower();

                            type = GetSkyDriveItemType(substrName);
                        }
                    }

                    if (type == SkyDriveItemType.File)
                    {
                        continue;
                    }

                    SkyDriveListItem listItem = new SkyDriveListItem()
                    {
                        Name = dict["name"].ToString(),
                        SkyDriveID = dict["id"].ToString(),
                        Type = type,
                        ParentID = dict["parent_id"].ToString()
                    };
                    if (type == SkyDriveItemType.Folder)
                    {
                        int count = 0;
                        int.TryParse(dict["count"].ToString(), out count);
                        listItem.FolderChildrenCount = count;
                    }

                    listItems.Add(listItem);
                }
                this.skydriveStack.Add(listItems);
                this.skydriveList.ItemsSource = listItems;
            }
            else
            {
                MessageBox.Show(String.Format(AppResources.SkyDriveGeneralError, "Api Error"), AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }

        void ShowBuyDialog()
        {
            var result = MessageBox.Show(AppResources.BuyNowImportText, AppResources.InfoCaption, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                if (this.downloadsInProgress == 0)
                {
                    MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
                    marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
#if !GBC
                    marketplaceDetailTask.ContentIdentifier = "4e3142c4-b99c-4075-bedc-b10a3086327d";
#else
                    marketplaceDetailTask.ContentIdentifier = "be33ce3e-e519-4d2c-b30e-83347601ed57";                    
#endif
                    marketplaceDetailTask.Show();
                }
                else
                {
                    MessageBox.Show(AppResources.BuyWaitForDownloadText);
                }
            }
        }
    } //end class

    public enum SkyDriveItemType
    {
        File,
        Folder,
        ROM,
        SRAM,
        Savestate,
        Zip,
        Rar,
        SevenZip
    }

    public class ImportFileItem
    {
        public String Name { get; set; }
        public SkyDriveItemType Type { get; set; }
        public bool Downloading { get; set; }
        public Stream Stream { get; set; } //the Stream corresponding to this item
    }


    public class SkyDriveListItem : ImportFileItem
    {
        public String SkyDriveID { get; set; }
        public String ParentID { get; set; }
        public int FolderChildrenCount { get; set; }

    }
}