using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Phone.UserData;
using System.Windows.Data;
using System.Globalization;
using Microsoft.Phone.Data.Linq;
using Microsoft.Phone.Shell;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Resources;
using System.Windows;


//local directive
using PhoneDirect3DXamlAppInterop.Database;
using PhoneDirect3DXamlAppInterop;


namespace PhoneDirect3DXamlAppInterop.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<ROMDBEntry> DesignROMDBEntry {get; set;}  //for design purpose only
        //public ObservableCollection<CheatInfo> DesignCheatInfoList { get; set; }
        //public ObservableCollection<CheatInfo> DesignPartialCheatMatchList { get; set; }
        //public ObservableCollection<CheatText> DesignCheatTextList { get; set; }

        public string FirstName { get; set; }
        //no-argument constructor, used for design only
        public MainViewModel()
        {
        }
    }


}