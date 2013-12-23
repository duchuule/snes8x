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
using PhoneDirect3DXamlAppInterop.Database;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ManageSavestatePage : PhoneApplicationPage
    {
        private ROMDBEntry romEntry;
        private ROMDatabase db;

        public ManageSavestatePage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }

            db = ROMDatabase.Current;

            object tmp;
            PhoneApplicationService.Current.State.TryGetValue("parameter", out tmp);
            this.romEntry = tmp as ROMDBEntry;
            PhoneApplicationService.Current.State.Remove("parameter");

            titleLabel.Text = this.romEntry.DisplayName;

            CreateAppBar();

            var savestates = db.GetSavestatesForROM(this.romEntry);

            this.stateList.ItemsSource = savestates;
        }

        private void CreateAppBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = true;

            var removeButton = new ApplicationBarIconButton(new Uri("/Assets/Icons/delete.png", UriKind.Relative))
            {
                Text = AppResources.ManageRemoveState
            };
            removeButton.Click += removeButton_Click;

            ApplicationBar.Buttons.Add(removeButton);
        }

        async void removeButton_Click(object sender, EventArgs e)
        {
            if (this.stateList.SelectedItem == null)
            {
                MessageBox.Show(AppResources.RemoveStateNoSelection, AppResources.ErrorCaption, MessageBoxButton.OK);
                return;
            }

            bool result = await FileHandler.DeleteSaveState(this.stateList.SelectedItem as SavestateEntry);
            if (!result)
            {
                MessageBox.Show(AppResources.ManageDeleteUnknownError, AppResources.ErrorCaption, MessageBoxButton.OK);
                return;
            }

            this.stateList.ItemsSource = null;
            this.stateList.ItemsSource = this.db.GetSavestatesForROM(this.romEntry);
        }
    }
}