using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ConfirmationPage : PhoneApplicationPage
    {
        public event EventHandler Closed = delegate { };
        public event EventHandler Confirmed = delegate { };

        public bool DoNotShowAgain
        {
            get { return (this.doNotShowAgainBox.IsChecked.HasValue) ? this.doNotShowAgainBox.IsChecked.Value : false; }
        }

        public ConfirmationPage(String text)
        {
            InitializeComponent();
            this.InfoTextBlock.Text = text;
        }

        private void confirmButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Confirmed(this, EventArgs.Empty);
        }

        private void cancelButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Closed(this, EventArgs.Empty);
        }
    }
}