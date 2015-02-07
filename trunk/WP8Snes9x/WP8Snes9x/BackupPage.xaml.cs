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
        public static string backupMedium = ""; //"sdcard", "onedrive" or "cloudsix"

        private ROMDatabase db;
        private LiveConnectSession session;

        public BackupPage()
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
            this.db = ROMDatabase.Current;

            this.romList.ItemsSource = db.GetROMList();

        }



        private async void romList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ROMDBEntry entry = this.romList.SelectedItem as ROMDBEntry;
            this.romList.SelectedItem = null;
            if (entry == null)
            {
                return;
            }



            //transfer to export selection page
            ExportSelectionPage.currentEntry = entry;
            ExportSelectionPage.backupMedium = backupMedium;
            if (backupMedium == "onedrive")
                PhoneApplicationService.Current.State["parameter"] = this.session;

            this.NavigationService.Navigate(new Uri("/ExportSelectionPage.xaml", UriKind.Relative));

            return;



            //            var indicator = SystemTray.GetProgressIndicator(this);
            //            indicator.IsIndeterminate = true;
            //            indicator.Text = String.Format(AppResources.BackupUploadProgressText, entry.DisplayName);



            //            this.uploading = true;

            //            try
            //            {
            //                LiveConnectClient client = new LiveConnectClient(this.session);
            //                String folderID = await this.CreateExportFolder(client);
            //                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
            //                bool errors = false;
            //                foreach (var savestate in entry.Savestates)
            //                {
            //                    ProgressIndicator indicator2 = SystemTray.GetProgressIndicator(this);

            //                    indicator2.Text = String.Format(AppResources.UploadProgressText, savestate.FileName);

            //                    String path = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + savestate.FileName;
            //                    try
            //                    {
            //                        using (IsolatedStorageFileStream fs = iso.OpenFile(path, System.IO.FileMode.Open))
            //                        {
            //                            await client.UploadAsync(folderID, savestate.FileName, fs, OverwriteOption.Overwrite);
            //                        }
            //                    }
            //                    catch (FileNotFoundException)
            //                    {
            //                        errors = true;
            //                    }
            //                    catch (LiveConnectException ex)
            //                    {
            //                        MessageBox.Show(String.Format(AppResources.BackupErrorUpload, ex.Message), AppResources.ErrorCaption, MessageBoxButton.OK);
            //                        errors = true;
            //                    }
            //                }


            //                int index = entry.FileName.LastIndexOf('.');
            //                int diff = entry.FileName.Length - 1 - index;

            //                String sramName = entry.FileName.Substring(0, entry.FileName.Length - diff) + "sav";
            //                String sramPath = FileHandler.ROM_DIRECTORY + "/" + FileHandler.SAVE_DIRECTORY + "/" + sramName;
            //                try
            //                {
            //                    if (iso.FileExists(sramPath))
            //                    {
            //                        ProgressIndicator indicator2 = SystemTray.GetProgressIndicator(this);

            //                        indicator2.Text = String.Format(AppResources.UploadProgressText, sramName);


            //                        using (IsolatedStorageFileStream fs = iso.OpenFile(sramPath, FileMode.Open))
            //                        {
            //                            await client.UploadAsync(folderID, sramName, fs, OverwriteOption.Overwrite);
            //                        }
            //                    }
            //                }
            //                catch (Exception)
            //                {
            //                    errors = true;
            //                }

            //                if (errors)
            //                {
            //                    MessageBox.Show(AppResources.BackupUploadUnsuccessful, AppResources.ErrorCaption, MessageBoxButton.OK);
            //                }
            //                else
            //                {
            //                    MessageBox.Show(AppResources.BackupUploadSuccessful);
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show(ex.Message, AppResources.ErrorCaption, MessageBoxButton.OK);
            //            }
            //            finally
            //            {
            //                this.uploading = false;
            //            }

            //#if GBC
            //            indicator.Text = AppResources.ApplicationTitle2;
            //#else
            //            indicator.Text = AppResources.ApplicationTitle;
            //#endif
            //            indicator.IsIndeterminate = false;
        }


    }
}