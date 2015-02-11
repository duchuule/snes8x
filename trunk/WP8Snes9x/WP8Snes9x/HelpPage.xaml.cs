using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using PhoneDirect3DXamlAppInterop.Resources;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class HelpPage : PhoneApplicationPage
    {
        public HelpPage()
        {
            InitializeComponent();

            //create ad control
            if (PhoneDirect3DXamlAppComponent.EmulatorSettings.Current.ShouldShowAds)
            {
                AdControl adControl = new AdControl();
                LayoutRoot.Children.Add(adControl);
                adControl.SetValue(Grid.RowProperty, 1);
            }



        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string strindex = null;
            NavigationContext.QueryString.TryGetValue("index", out strindex);

            if (strindex != null)
                MainPivotControl.SelectedIndex = int.Parse(strindex);


            base.OnNavigatedTo(e);
        }

        private void contactBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailcomposer = new EmailComposeTask();

            emailcomposer.To = AppResources.AboutContact;

            emailcomposer.Subject = AppResources.EmailSubjectText;
            emailcomposer.Body = String.Format(AppResources.EmailBodyText, Microsoft.Phone.Info.DeviceStatus.DeviceName);
            emailcomposer.Show();
        }
    }
}