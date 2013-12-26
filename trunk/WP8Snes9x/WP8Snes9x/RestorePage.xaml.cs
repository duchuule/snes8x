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
using PhoneDirect3DXamlAppInterop.Resources;
using System.Threading.Tasks;
using Windows.Storage;
using PhoneDirect3DXamlAppInterop.Database;
using Windows.Storage.Streams;
using System.IO;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class RestorePage : PhoneApplicationPage
    {
        private LiveConnectSession session;
        List<List<SkyDriveListItem>> skydriveStack;
        private double labelHeight;
        private bool initialPageLoaded = false;

        public RestorePage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }
            this.labelHeight = this.statusLabel.Height;
            this.session = PhoneApplicationService.Current.State["parameter"] as LiveConnectSession;
            PhoneApplicationService.Current.State.Remove("parameter");
            if (this.session == null)
            {
                throw new ArgumentException("Parameter passed to RestorePage must be a LiveConnectSession.");
            }

            this.skydriveStack = new List<List<SkyDriveListItem>>();
            this.skydriveStack.Add(new List<SkyDriveListItem>());
            this.skydriveStack[0].Add(new SkyDriveListItem()
            {
                Name = "SkyDrive",
                SkyDriveID = "me/skydrive",
                Type = SkyDriveItemType.Folder,
                ParentID = ""
            });
            this.currentFolderBox.Text = this.skydriveStack[0][0].Name;

            this.BackKeyPress += SkyDriveImportPage_BackKeyPress;
        }

        async void skydriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SkyDriveListItem item = this.skydriveList.SelectedItem as SkyDriveListItem;
            if (item == null)
                return;

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
                else if (item.Type == SkyDriveItemType.Savestate || item.Type == SkyDriveItemType.SRAM)
                {
                    //check to make sure there is a rom with matching name
                    ROMDatabase db = ROMDatabase.Current;
                    ROMDBEntry entry = db.GetROMFromSavestateName(item.Name);

                    if (entry == null) //no matching file name
                    {
                        MessageBox.Show("Please make sure the name of the save file matches the name of the ROM", AppResources.ErrorCaption, MessageBoxButton.OK);
                        return;
                    }
                    

                    // Download
                    if (!item.Downloading)
                    {
                        try
                        {
                            item.Downloading = true;
                            await this.DownloadFile(item, client);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                        }
                    }
                    else
                    {
                        MessageBox.Show(AppResources.AlreadyDownloadingText, AppResources.ErrorCaption, MessageBoxButton.OK);
                    }
                }
                this.statusLabel.Height = 0;
            }
            catch (LiveConnectException)
            {
                this.statusLabel.Height = this.labelHeight;
                this.statusLabel.Text = AppResources.SkyDriveInternetLost;
            }
        }

        private async Task DownloadFile(SkyDriveListItem item, LiveConnectClient client)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await folder.CreateFolderAsync(FileHandler.ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
            StorageFolder saveFolder = await romFolder.CreateFolderAsync(FileHandler.SAVE_DIRECTORY, CreationCollisionOption.OpenIfExists);

            String path = romFolder.Path;
            String savePath = saveFolder.Path;

            ROMDatabase db = ROMDatabase.Current;
            var indicator = new ProgressIndicator()
            {
                IsIndeterminate = true,
                IsVisible = true,
                Text = String.Format(AppResources.DownloadingProgressText, item.Name)
            };

            SystemTray.SetProgressIndicator(this, indicator);

            LiveDownloadOperationResult e = await client.DownloadAsync(item.SkyDriveID + "/content");
            if (e != null)
            {
                byte[] tmpBuf = new byte[e.Stream.Length];

                
                StorageFile destinationFile = null;

                ROMDBEntry entry = db.GetROMFromSavestateName(item.Name);

                if (item.Type == SkyDriveItemType.SRAM)
                {
                    if (entry != null)
                        destinationFile = await saveFolder.CreateFileAsync(Path.GetFileNameWithoutExtension(entry.FileName) + ".srm", CreationCollisionOption.ReplaceExisting);

                    else
                        destinationFile = await saveFolder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);
                }
                else if (item.Type == SkyDriveItemType.Savestate)
                {

                    if (entry != null)
                        destinationFile = await saveFolder.CreateFileAsync(Path.GetFileNameWithoutExtension(entry.FileName) + item.Name.Substring(item.Name.Length - 4), CreationCollisionOption.ReplaceExisting);
                    else
                        destinationFile = await saveFolder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);
                }



                
                using (IRandomAccessStream destStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                using (DataWriter writer = new DataWriter(destStream))
                {
                    while (e.Stream.Read(tmpBuf, 0, tmpBuf.Length) != 0)
                    {
                        writer.WriteBytes(tmpBuf);
                    }
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }
                e.Stream.Close();
                item.Downloading = false;
                SystemTray.GetProgressIndicator(this).IsVisible = false;

                if (item.Type == SkyDriveItemType.Savestate)
                {
                    String number = item.Name.Substring(item.Name.Length - 3);
                    int slot = int.Parse(number);

                    if (entry != null) // Null = No ROM existing for this file -> skip inserting into database.
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
                SystemTray.GetProgressIndicator(this).IsVisible = false;
                MessageBox.Show(String.Format(AppResources.DownloadErrorText, item.Name, "Api error"), AppResources.ErrorCaption, MessageBoxButton.OK);
            }
        }

        void SkyDriveImportPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.skydriveStack.Count <= 2)
            {
                return;
            }

            try
            {
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
            catch (Exception) { }
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
                            if (substrName.Equals(".srm"))
                            {
                                type = SkyDriveItemType.SRAM;
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
                                        type = SkyDriveItemType.Savestate;
                                        break;
                                    }
                                }
                            }
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
    }
}