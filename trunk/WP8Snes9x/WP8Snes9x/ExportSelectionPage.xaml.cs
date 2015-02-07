using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

using PhoneDirect3DXamlAppInterop.Database;
using PhoneDirect3DXamlAppInterop.Resources;
using System.Windows.Media;
using Telerik.Windows.Controls;
using Ionic.Zip;
using System.IO;
using CloudSixConnector.FileSaver;
using Microsoft.Live;
using System.Threading.Tasks;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ExportSelectionPage : PhoneApplicationPage
    {
        public static string backupMedium = ""; //"sdcard", "onedrive" or "cloudsix"

        public static ROMDBEntry currentEntry = null;
        private LiveConnectSession session;
        private bool uploading = false;

        private const string EXPORT_FOLDER = "Snes8x export";


        public ExportSelectionPage()
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

            if (backupMedium == "onedrive")
            {
                this.session = PhoneApplicationService.Current.State["parameter"] as LiveConnectSession;
                PhoneApplicationService.Current.State.Remove("parameter");

                if (this.session == null)
                {
                    throw new ArgumentException("Parameter passed to SkyDriveImportPage must be a LiveConnectSession.");
                }
            }


            ZipCheckBox.IsChecked = true;

            BuildLocalizedApplicationBar1();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //build the list
            List<ExportFileItem> filelist = new List<ExportFileItem>();

            //check in-game save and add
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                int index = currentEntry.FileName.LastIndexOf('.');
                int diff = currentEntry.FileName.Length - 1 - index;

                String sramName = currentEntry.FileName.Substring(0, currentEntry.FileName.Length - diff) + "sav";
                String sramPath = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + sramName;

                if (iso.FileExists(sramPath))
                {
                    ExportFileItem item = new ExportFileItem()
                    {
                        Name = sramName,
                        Path = sramPath
                    };

                    filelist.Add(item);
                }
            }

            //add save states
            foreach (var savestate in currentEntry.Savestates)
            {
                String path = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + savestate.FileName;
                ExportFileItem item = new ExportFileItem()
                {
                    Name = savestate.FileName,
                    Path = path
                };

                filelist.Add(item);
            }

            this.fileList.ItemsSource = filelist;
            this.fileList.CheckedItems.CheckAll();
            base.OnNavigatedTo(e);
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.uploading)
            {
                e.Cancel = true;
                MessageBox.Show(AppResources.BackupWaitForUpload, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
            base.OnBackKeyPress(e);
        }


        private void BuildLocalizedApplicationBar1()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            ApplicationBar.BackgroundColor = (Color)App.Current.Resources["CustomChromeColor"];
            ApplicationBar.ForegroundColor = (Color)App.Current.Resources["CustomForegroundColor"];

            // Create a new button and set the text value to the localized string from AppResources.
            
            ApplicationBarIconButton  button1 = new ApplicationBarIconButton(new Uri("/Assets/Icons/selectall.png", UriKind.Relative));
            button1.Text = AppResources.SelectAllText;
            button1.Click += SelectAll_Click;
            ApplicationBar.Buttons.Add(button1);

            ApplicationBarIconButton button2 = new ApplicationBarIconButton(new Uri("/Assets/Icons/selectnone.png", UriKind.Relative));
            button2.Text = AppResources.SelectNoneText;
            button2.Click += SelectNone_Click;
            ApplicationBar.Buttons.Add(button2);

            ApplicationBarIconButton button3 = new ApplicationBarIconButton(new Uri("/Assets/Icons/check.png", UriKind.Relative));
            button3.Text = AppResources.OKButtonText;
            button3.Click += Export_Click;
            ApplicationBar.Buttons.Add(button3);

        }

        private void SelectNone_Click(object sender, EventArgs e)
        {
            this.fileList.CheckedItems.Clear();
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            this.fileList.CheckedItems.CheckAll();
        }

        private async void Export_Click(object sender, EventArgs e)
        {
            if (fileList.CheckedItems.Count == 0)
            {
                MessageBox.Show(AppResources.ExportNoSelection);
                return;
            }

            //can only export 1 file if use cloudsix
            if (backupMedium == "cloudsix" && fileList.CheckedItems.Count > 1 && ZipCheckBox.IsChecked.Value == false)
            {
                MessageBox.Show(AppResources.CloudSixOneFileLimitText);
                return;
            }

            if (this.uploading)
            {
                MessageBox.Show(AppResources.BackupWaitForUpload, AppResources.ErrorCaption, MessageBoxButton.OK);
                return;
            }

            //zip file if users choose to do so
            if (ZipCheckBox.IsChecked.Value)
            {
                using (ZipFile zip = new ZipFile())
                {
                    using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        foreach (ExportFileItem file in fileList.CheckedItems)
                        {
                            IsolatedStorageFileStream fs = iso.OpenFile(file.Path, System.IO.FileMode.Open);
                            zip.AddEntry(file.Name, fs);
                        }

                        MemoryStream stream = new MemoryStream();
                        zip.Save(stream);

                        if (backupMedium == "cloudsix")
                        {
                            var saver = new CloudSixSaver(currentEntry.DisplayName + ".zip", stream);
                            await saver.Launch();
                        }
                        else if (backupMedium == "onedrive")
                        {
                            var indicator = SystemTray.GetProgressIndicator(this);
                            indicator.IsIndeterminate = true;

                            this.uploading = true;
                            bool errors = false;

                            try
                            {
                                LiveConnectClient client = new LiveConnectClient(this.session);
                                String folderID = await CreateExportFolder(client);

                                indicator.Text = String.Format(AppResources.UploadProgressText, currentEntry.DisplayName + ".zip");
                                stream.Seek(0, SeekOrigin.Begin);
                                await client.UploadAsync(folderID, currentEntry.DisplayName + ".zip", stream, OverwriteOption.Overwrite);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(String.Format(AppResources.BackupErrorUpload, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                                errors = true;
                            }

                            this.uploading = false;

                            if (errors)
                            {
                                MessageBox.Show(AppResources.BackupUploadUnsuccessful, AppResources.ErrorCaption, MessageBoxButton.OK);
                            }
                            else
                            {
                                MessageBox.Show(AppResources.BackupUploadSuccessful);
                            }


#if GBC
                            indicator.Text = AppResources.ApplicationTitle2;
#else
                            indicator.Text = AppResources.ApplicationTitle;
#endif
                            indicator.IsIndeterminate = false;

                        } //end of if (backupMedium == "onedrive")

                        stream.Close();
                    }

                }
            }
            else //does not use zip function
            {
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (backupMedium == "cloudsix")
                    {
                        ExportFileItem file = this.fileList.CheckedItems[0] as ExportFileItem;
                        using (IsolatedStorageFileStream fs = iso.OpenFile(file.Path, System.IO.FileMode.Open))
                        {
                            var saver = new CloudSixSaver(file.Name, fs);
                            await saver.Launch();
                        }
                    }
                    else if (backupMedium == "onedrive")
                    {
                        var indicator = SystemTray.GetProgressIndicator(this);
                        indicator.IsIndeterminate = true;

                        this.uploading = true;
                        bool errors = false;

                        try
                        {
                            LiveConnectClient client = new LiveConnectClient(this.session);
                            String folderID = await CreateExportFolder(client);

                            foreach (ExportFileItem file in this.fileList.CheckedItems)
                            {
                                indicator.Text = String.Format(AppResources.UploadProgressText, file.Name);

                                using (IsolatedStorageFileStream fs = iso.OpenFile(file.Path, System.IO.FileMode.Open))
                                {
                                    await client.UploadAsync(folderID, file.Name, fs, OverwriteOption.Overwrite);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format(AppResources.BackupErrorUpload, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                            errors = true;
                        }

                        this.uploading = false;

                        if (errors)
                        {
                            MessageBox.Show(AppResources.BackupUploadUnsuccessful, AppResources.ErrorCaption, MessageBoxButton.OK);
                        }
                        else
                        {
                            MessageBox.Show(AppResources.BackupUploadSuccessful);
                        }


#if GBC
                        indicator.Text = AppResources.ApplicationTitle2;
#else
                        indicator.Text = AppResources.ApplicationTitle;
#endif
                        indicator.IsIndeterminate = false;
                    }
                } //IsolatedStorage
            }  //using zip upload or not
        }


        


        public static async Task<String> CreateExportFolder(LiveConnectClient client)
        {
            LiveOperationResult opResult = await client.GetAsync("me/skydrive/files");
            var result = opResult.Result;
            IList<object> results = result["data"] as IList<object>;
            if (results == null)
            {
                throw new LiveConnectException();
            }
            object o = results
                .Where(d => (d as IDictionary<string, object>)["name"].Equals(EXPORT_FOLDER))
                .FirstOrDefault();
            string id = null;
            if (o == null)
            {
                var folderData = new Dictionary<string, object>();
                folderData.Add("name", EXPORT_FOLDER);
                opResult = await client.PostAsync("me/skydrive", folderData);
                dynamic postResult = opResult.Result;
                id = postResult.id;
            }
            else
            {
                IDictionary<string, object> folderProperties = o as IDictionary<string, object>;
                id = folderProperties["id"] as string;
            }
            return id;
        }






        private void ZipCheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }


    } //end class


    public class ExportFileItem
    {
        public String Name { get; set; }
        public String Path { get; set; }
    }
}