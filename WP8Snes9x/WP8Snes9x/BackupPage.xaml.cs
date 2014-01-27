using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneDirect3DXamlAppInterop.Database;
using PhoneDirect3DXamlAppInterop.Resources;
using Microsoft.Live;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using System.IO;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class BackupPage : PhoneApplicationPage
    {
        private const string EXPORT_FOLDER = "Snes8x export";
        private ROMDatabase db;
        private bool uploading = false;
        private LiveConnectSession session;

        public BackupPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }


            this.session = PhoneApplicationService.Current.State["parameter"] as LiveConnectSession;
            PhoneApplicationService.Current.State.Remove("parameter");
            if (this.session == null)
            {
                throw new ArgumentException("Parameter passed to SkyDriveImportPage must be a LiveConnectSession.");
            }
            this.db = ROMDatabase.Current;

            this.romList.ItemsSource = db.GetROMList();
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

        private async void romList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ROMDBEntry entry = this.romList.SelectedItem as ROMDBEntry;
            if (entry == null)
            {
                return;
            }

            if (this.uploading)
            {
                MessageBox.Show(AppResources.BackupWaitForUpload, AppResources.ErrorCaption, MessageBoxButton.OK);
                this.romList.SelectedItem = null;
                return;
            }

            var indicator = new ProgressIndicator()
            {
                IsIndeterminate = true,
                IsVisible = true,
                Text = String.Format(AppResources.BackupUploadProgressText, entry.DisplayName)
            };
            SystemTray.SetProgressIndicator(this, indicator);
            this.uploading = true;

            try
            {
                LiveConnectClient client = new LiveConnectClient(this.session);
                String folderID = await this.CreateExportFolder(client);
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                bool errors = false;
                foreach (var savestate in entry.Savestates)
                {
                    ProgressIndicator indicator2 = SystemTray.GetProgressIndicator(this);

                    indicator2.Text = String.Format(AppResources.UploadProgressText, savestate.FileName);

                    String path = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + savestate.FileName;
                    try
                    {
                        using (IsolatedStorageFileStream fs = iso.OpenFile(path, System.IO.FileMode.Open))
                        {
                            await client.UploadAsync(folderID, savestate.FileName, fs, OverwriteOption.Overwrite);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        errors = true;
                    }
                    catch (LiveConnectException ex)
                    {
                        MessageBox.Show(String.Format(AppResources.BackupErrorUpload, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
                        errors = true;
                    }
                }

                String sramName = entry.FileName.Substring(0, entry.FileName.Length - 3) + "srm";
                String sramPath = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + sramName;
                try
                {
                    if (iso.FileExists(sramPath))
                    {
                        ProgressIndicator indicator2 = SystemTray.GetProgressIndicator(this);

                        indicator2.Text = String.Format(AppResources.UploadProgressText, sramName);

                        using (IsolatedStorageFileStream fs = iso.OpenFile(sramPath, FileMode.Open))
                        {
                            await client.UploadAsync(folderID, sramName, fs, OverwriteOption.Overwrite);
                        }
                    }
                }
                catch (Exception)
                {
                    errors = true;
                }

                if (errors)
                {
                    MessageBox.Show(AppResources.BackupUploadUnsuccessful, AppResources.ErrorCaption, MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show(AppResources.BackupUploadSuccessful);
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(AppResources.CreateBackupFolderError, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
            catch (LiveConnectException)
            {
                MessageBox.Show(AppResources.SkyDriveInternetLost, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
            finally
            {
                SystemTray.GetProgressIndicator(this).IsVisible = false;
                this.uploading = false;
            }
        }

        private async Task<String> CreateExportFolder(LiveConnectClient client)
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
    }
}