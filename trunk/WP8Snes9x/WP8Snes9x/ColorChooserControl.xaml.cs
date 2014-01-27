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
using System.Windows.Media;

namespace PhoneDirect3DXamlAppInterop
{
    public partial class ColorChooserControl : UserControl
    {
        public ColorChooserControl()
        {
            InitializeComponent();

            tileColorPicker.Color = new Color
            {
                R = (byte)EmulatorSettings.Current.BgcolorR,
                G = (byte)EmulatorSettings.Current.BgcolorG,
                B = (byte)EmulatorSettings.Current.BgcolorB,
                A = 255
            };
        }


        private void Cancelbtn_Click(object sender, RoutedEventArgs e)
        {

            ClosePopup();
        }




        private void OKbtn_Click(object sender, RoutedEventArgs e)
        {
            //save settings
            EmulatorSettings.Current.BgcolorR = tileColorPicker.Color.R;
            EmulatorSettings.Current.BgcolorG = tileColorPicker.Color.G;
            EmulatorSettings.Current.BgcolorB = tileColorPicker.Color.B;

            ClosePopup();
        }


        private void ClosePopup()
        {

            Popup selectPop = this.Parent as Popup;

            selectPop.IsOpen = false;

        }
    }
}
