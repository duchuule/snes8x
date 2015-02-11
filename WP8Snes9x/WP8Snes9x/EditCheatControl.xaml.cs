using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using PhoneDirect3DXamlAppComponent;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class EditCheatControl : UserControl
    {
        public static String TextToEdit;
        public static String PromptText;
        public static bool IsOKClicked;

        public EditCheatControl()
        {
            InitializeComponent();

            IsOKClicked = false;
            if (PromptText != null)
                TblkPromptText.Text = PromptText;
        }


        private void OKbtn_Click(object sender, RoutedEventArgs e)
        {
            //save settings
            TextToEdit = txtCheatCode.Text;
            IsOKClicked = true;
            ClosePopup();
            
        }

       

        private void Cancelbtn_Click(object sender, RoutedEventArgs e)
        {

            ClosePopup();
        }

        private void ClosePopup()
        {

            Popup selectPop = this.Parent as Popup;

            selectPop.IsOpen = false;

        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            txtCheatCode.Text = TextToEdit;
            txtCheatCode.Focus();
        }
    }
}
