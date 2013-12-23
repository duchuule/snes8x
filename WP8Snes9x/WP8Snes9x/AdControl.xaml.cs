using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using Microsoft.Phone.Shell;
using Microsoft.Advertising;
using GoogleAds;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class AdControl : UserControl
    {
        public AdControl()
        {
            InitializeComponent();


#if GBC
            AdDuplexAdControl.AppId = "71883";
#endif

            mobFoxadControl.NewAd += mobFoxadControl_NewAd;
            mobFoxadControl.NoAd += mobFoxadControl_NoAd;

            AdMobControl.ReceivedAd += AdMobOnAdReceived;
            AdMobControl.FailedToReceiveAd += AdMobOnFailedToReceiveAd;

            MSAdControl.ErrorOccurred += MSAdControl_AdControlError;
            MSAdControl.AdRefreshed += MSAdControl_NewAd;

        }


        private void mobFoxadControl_NewAd(object sender, MobFox.Ads.NewAdEventArgs args)
        {


            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                mobFoxadControl.Visibility = Visibility.Visible;

                MSAdControl.Visibility = Visibility.Collapsed;
                AdMobControl.Visibility = Visibility.Collapsed;
                AdDuplexAdControl.Visibility = Visibility.Collapsed;

            });
        }

        private void mobFoxadControl_NoAd(object sender, MobFox.Ads.NoAdEventArgs args)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                mobFoxadControl.Visibility = Visibility.Collapsed;

                MSAdControl.Visibility = Visibility.Visible;
                AdMobControl.Visibility = Visibility.Collapsed;
                AdMobControl.Visibility = Visibility.Collapsed;

                //try to get ad on pubcenter
                MSAdControl.Refresh();

            });




        }

        void MSAdControl_NewAd(object sender, EventArgs e)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                AdDuplexAdControl.Visibility = Visibility.Collapsed;
                AdMobControl.Visibility = Visibility.Collapsed;


            });
        }

        void MSAdControl_AdControlError(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                MSAdControl.Visibility = Visibility.Collapsed;
                AdMobControl.Visibility = Visibility.Visible;

                //this will make admob to try to get ad
                AdMobControl.LoadAd(new AdRequest());
            });


        }



        private void AdMobOnAdReceived(object sender, AdEventArgs e)
        {

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                AdDuplexAdControl.Visibility = Visibility.Collapsed;
                MSAdControl.Visibility = Visibility.Collapsed;

            });


        }

        private void AdMobOnFailedToReceiveAd(object sender, GoogleAds.AdErrorEventArgs errorCode)
        {

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                AdMobControl.Visibility = Visibility.Collapsed;
                AdDuplexAdControl.Visibility = Visibility.Visible;


            });
        }


    }
}
