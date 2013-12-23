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

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ImportPage : PhoneApplicationPage
    {
        private LiveConnectSession session;

        public ImportPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 2);
            }
        }

        void btnSignin_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                PhoneApplicationService.Current.State["parameter"] = this.session = e.Session;
                this.NavigationService.Navigate(new Uri("/SkyDriveImportPage.xaml", UriKind.Relative));

                //LiveConnectClient client = new LiveConnectClient(e.Session);
                //this.session = e.Session;
                //testLabel.Text = "Signed in.";
                //client.GetCompleted += client_GetCompleted;
                //client.GetAsync("me", null);
            }
            else
            {
                session = null;
                if (e.Error != null)
                {
                    statusLabel.Text = "Error calling API: " + e.Error.ToString();
                }
            }
        }
    }
}