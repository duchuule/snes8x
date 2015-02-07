using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneDirect3DXamlAppInterop.Database
{
    [Table]
    public class SavestateEntry : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        private EntityRef<ROMDBEntry> rom = new EntityRef<ROMDBEntry>();
        private string romFileName;
        private int slot;
        private DateTime savetime;
        private string fileName;
        
        [Association(Name = "FK_ROM_SAVESTATES", Storage = "rom", ThisKey = "ROMFileName", OtherKey = "FileName", IsForeignKey = true)]
        public ROMDBEntry ROM
        {
            get { return this.rom.Entity; }
            set
            {
                ROMDBEntry oldEntry = this.rom.Entity;
                if (oldEntry != value || this.rom.HasLoadedOrAssignedValue == false)
                {
                    if (oldEntry != null)
                    {
                        this.rom.Entity = null;
                        oldEntry.Savestates.Remove(this);
                    }

                    this.rom.Entity = value;

                    if (value != null)
                    {
                        value.Savestates.Add(this);
                        this.romFileName = value.FileName;
                    }
                    else
                    {
                        this.romFileName = default(string);
                    }
                }
            }
        }

        [Column(IsPrimaryKey = true)]
        public string ROMFileName
        {
            get { return this.romFileName; }
            set
            {
                this.NotifyPropertyChanging("ROMFileName");
                this.romFileName = value;
                this.NotifyPropertyChanged("ROMFileName");
            }
        }

        [Column(IsPrimaryKey = true)]
        public int Slot
        {
            get { return this.slot; }
            set
            {
                if (value != this.slot)
                {
                    this.NotifyPropertyChanging("Slot");
                    this.slot = value;
                    this.NotifyPropertyChanged("Slot");
                }
            }
        }

        [Column]
        public DateTime Savetime
        {
            get
            {
                return this.savetime;
            }
            set
            {
                if (value != this.savetime)
                {
                    this.NotifyPropertyChanging("Savetime");
                    this.savetime = value;
                    this.NotifyPropertyChanged("Savetime");
                }
            }
        }

        [Column]
        public string FileName
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

        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void NotifyPropertyChanging(string propertyName)
        {
            if (this.PropertyChanging != null)
            {
                this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
    }
}
