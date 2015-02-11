using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ImageEditorPage : PhoneApplicationPage
    {
        public static WriteableBitmap image;

        public ImageEditorPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.DataContext = ImageEditorPage.image;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            e.Cancel = this.imageEditor.CurrentTool != null;
            if (e.Cancel)
            {
                this.imageEditor.CurrentTool = null;
            }
        }

        private void RadImageEditor_ImageSaved(object sender, ImageSavedEventArgs e)
        {
            this.GoBack();
        }

        private void RadImageEditor_ImageEditCancelled(object sender, ImageEditCancelledEventArgs e)
        {
            this.GoBack();
        }

        private void GoBack()
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                this.DataContext = null;
                image = null;
            }
        }
    }
}