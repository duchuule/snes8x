using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace PhoneDirect3DXamlAppInterop.Database
{
    [Table(Name = "ROMs")]
    public class ROMDBEntry : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        private String displayName;
        private String fileName;
        private DateTime lastPlayed;
        private readonly EntitySet<SavestateEntry> savestateRefs = new EntitySet<SavestateEntry>();
        private string snapshotUri;

        public bool SuspendAutoLoadLastState = false; //to suspend auto-load when import .sav file

        public string Header; //the value of this will be set at run time

        [Column(Storage="displayName", CanBeNull=false)]
        public String DisplayName
        {
            get
            {
                return this.displayName;
            }
            set
            {
                if (value != this.displayName)
                {
                    this.NotifyPropertyChanging("DisplayName");
                    this.displayName = value;
                    this.NotifyPropertyChanged("DisplayName");
                }
            }
        }

        [Column(Storage = "fileName", CanBeNull = false, IsPrimaryKey = true)]
        public String FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                if (value != this.fileName)
                {
                    this.NotifyPropertyChanging("FileName");
                    this.fileName = value;
                    this.NotifyPropertyChanged("FileName");
                }
            }
        }

        [Column(Storage = "lastPlayed", CanBeNull = false)]
        public DateTime LastPlayed
        {
            get
            {
                return this.lastPlayed;
            }
            set
            {
                if (value != this.lastPlayed)
                {
                    this.NotifyPropertyChanging("LastPlayed");
                    this.lastPlayed = value;
                    this.NotifyPropertyChanged("LastPlayed");
                }
            }
        }

        [Column]
        public string SnapshotURI
        {
            get
            {
                return this.snapshotUri;
            }
            set
            {
                if (value != this.snapshotUri)
                {
                    this.NotifyPropertyChanging("SnapshotURI");
                    this.snapshotUri = value;
                    this.NotifyPropertyChanged("SnapshotURI");
                }
            }
        }

        [Association(Name = "FK_ROM_SAVESTATES", Storage = "savestateRefs", ThisKey = "FileName", OtherKey = "ROMFileName")]
        public EntitySet<SavestateEntry> Savestates
        {
            get { return this.savestateRefs; }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
    }
}
