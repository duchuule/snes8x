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
using System.IO;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class RenamePage : PhoneApplicationPage
    {
        Database.ROMDatabase db;
        Database.ROMDBEntry entry;

        public RenamePage()
        {
            InitializeComponent();

            object tmpObject;
            PhoneApplicationService.Current.State.TryGetValue("parameter", out tmpObject);
            PhoneApplicationService.Current.State.Remove("parameter");
            this.entry = tmpObject as Database.ROMDBEntry;
            PhoneApplicationService.Current.State.TryGetValue("parameter2", out tmpObject);
            PhoneApplicationService.Current.State.Remove("parameter2");
            this.db = tmpObject as Database.ROMDatabase;


            this.nameBox.Text = this.entry.DisplayName;
#if GBC
            SystemTray.GetProgressIndicator(this).Text = AppResources.ApplicationTitle2;
#endif
        }

        private async void renameButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.nameBox.Text))
            {
                MessageBox.Show(AppResources.RenameEmptyString, AppResources.ErrorCaption, MessageBoxButton.OK);
            }
            else
            {
                if (this.nameBox.Text.ToLower().Equals(this.entry.DisplayName.ToLower()))
                {
                    return;
                }
                if (this.db.IsDisplayNameUnique(this.nameBox.Text))
                {
                    this.entry.DisplayName = this.nameBox.Text;
                    this.db.CommitChanges();
                    MainPage.shouldRefreshAllROMList = true; //only need to manually referesh the rom list because colectionviewsource does not update sorting

                    FileHandler.UpdateROMTile(this.entry.FileName);

                    //update voice command list
                    await MainPage.UpdateGameListForVoiceCommand();

                    this.NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show(AppResources.RenameNameAlreadyExisting, AppResources.ErrorCaption, MessageBoxButton.OK);
                }
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}