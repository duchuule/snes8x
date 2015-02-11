using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using PhoneDirect3DXamlAppInterop.Resources;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ImageCroppingPage : PhoneApplicationPage
    {
        public static WriteableBitmap wbSource;


        public ImageCroppingPage()
        {
            InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
            this.cropControl.Source = wbSource;

            base.OnNavigatedTo(e);
        }

        private void appBarCancelButton_Click(object sender, EventArgs e)
        {
            this.GoBack();
        }

        private void appBarOkButton_Click(object sender, EventArgs e)
        {
            WriteableBitmap wb = this.cropControl.CropImage();
            int height = 772; //system tray is 32 pixel, so there is only 768 pixel of content

            if (App.Current.Host.Content.ScaleFactor == 150) //only 720p has this scale factor
                height = 825;  //853.3 is total screen height

            wb = wb.Resize((int)((double)wb.PixelWidth / wb.PixelHeight * height), height, WriteableBitmapExtensions.Interpolation.Bilinear);

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string filename = "CustomBackground.jpg";

                //delete old picture if exist
                if (store.FileExists(filename))
                    store.DeleteFile(filename);


                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
                {
                    wb.SaveJpeg(fileStream, wb.PixelWidth, wb.PixelHeight, 0, 100);
                }
            }

            //save settings
            App.metroSettings.BackgroundUri = "CustomBackground.jpg";
            App.metroSettings.UseDefaultBackground = false;
            SettingsPage.shouldUpdateBackgroud = true;

            //save to isolated storage
            this.GoBack();
        }

        private void GoBack()
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                wbSource = null;
            }
        }

        



    }
}