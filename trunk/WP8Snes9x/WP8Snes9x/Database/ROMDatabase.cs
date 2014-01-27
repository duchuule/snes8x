using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PhoneDirect3DXamlAppInterop.Database
{
    public delegate void CommitDelegate();

    class ROMDatabase : IDisposable
    {
        private static ROMDatabase singleton;

        public static ROMDatabase Current
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new ROMDatabase();
                }
                return singleton;
            }
        }

        private const string ConnectionString = "isostore:/roms.sdf";

        private class ROMDataContext : DataContext
        {
            public Table<ROMDBEntry> ROMTable { get; set; }
            public Table<SavestateEntry> SavestateTable { get; set; }

            public ROMDataContext(string uri)
                : base(uri)
            {
                this.ROMTable = this.GetTable<ROMDBEntry>();
                this.SavestateTable = this.GetTable<SavestateEntry>();
            }
        }

        public event CommitDelegate Commit = delegate { };

        private ROMDataContext context;
        private bool disposed = false;

        private ROMDatabase()
        {
            this.context = new ROMDataContext(ConnectionString);
        }

        public bool Initialize()
        {
            bool dbCreated = false;
            //if(context.DatabaseExists())
            //    context.DeleteDatabase();
            if (!context.DatabaseExists())
            {
                context.CreateDatabase();
                context.SubmitChanges();
                dbCreated = true;
            }
            context.SubmitChanges();
            return dbCreated;
        }

        public void Add(ROMDBEntry entry)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            context.ROMTable.InsertOnSubmit(entry);
            context.SubmitChanges();
        }

        public bool IsDisplayNameUnique(string displayName)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            displayName = displayName.ToLower();
            return this.context.ROMTable
                .Where(r => r.DisplayName.ToLower().Equals(displayName))
                .Count() == 0;
        }

        public SavestateEntry SavestateEntryExisting(string filename, int slot)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }

            return this.context.SavestateTable.Where(s => s.ROMFileName.ToLower().Equals(filename.ToLower())).Where(s => s.Slot == slot).FirstOrDefault();

        }

        public SavestateEntry GetSavestate(string filename)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            return this.context.SavestateTable
                .Where(s => s.FileName.ToLower().Equals(filename.ToLower()))
                .FirstOrDefault();
        }

        public SavestateEntry GetSavestate(string romFilename, int slot)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            romFilename = romFilename.ToLower();
            return this.context.SavestateTable
                .Where(s => (s.ROMFileName.ToLower().Equals(romFilename)) && (s.Slot == slot))
                .FirstOrDefault();
        }

        public IEnumerable<String> GetRecentSnapshotList()
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            return this.context.ROMTable
                .Where(r => !r.SnapshotURI.Equals(FileHandler.DEFAULT_SNAPSHOT))
                .OrderByDescending(r => r.LastPlayed)
                .Take(3)
                .Select(r => r.SnapshotURI)
                .ToArray();
        }

        public void Add(SavestateEntry entry)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            context.SavestateTable.InsertOnSubmit(entry);
            context.SubmitChanges();
        }

        public ROMDBEntry GetROM(string fileName)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            fileName = fileName.ToLower();
            ROMDBEntry entry = this.context.ROMTable
                .Where(f => f.FileName.ToLower().Equals(fileName))
                .FirstOrDefault();
            return entry;
        }

        public ROMDBEntry GetROMFromSavestateName(string savestateName)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            String name = savestateName.Substring(0, savestateName.Length - 4).ToLower();
            ROMDBEntry entry = this.context.ROMTable
                .Where(r => r.FileName.Substring(0, r.FileName.Length - 4).ToLower().Equals(name))
                .FirstOrDefault();

            if (entry != null)
                return entry;
            else
            {
                //check display name
                return this.context.ROMTable
                .Where(r => (r.DisplayName.ToLower().Equals(name)))
                .FirstOrDefault();
            }
        }


       

        public int GetLastSavestateSlotByFileNameExceptAuto(string filename)
        {
            if (!context.DatabaseExists())
            {
                throw new InvalidOperationException("Database does not exist.");
            }
            filename = filename.ToLower();
            SavestateEntry save = this.context.SavestateTable
                .Where(s => s.ROMFileName.ToLower().Equals(filename) && s.Slot != 10)
                .OrderByDescending(s => s.Savetime)
                .FirstOrDefault();
            if (save != null)
            {
                return save.Slot;
            }
            return 0;
        }

        public void RemoveROM(string fileName)
        {
            ROMDBEntry entry = this.GetROM(fileName);
            if (entry != null)
            {
                this.context.SavestateTable.DeleteAllOnSubmit(
                    this.context.SavestateTable
                    .Where(s => (s.ROM == entry))
                    .ToArray()
                    );
                this.context.ROMTable.DeleteOnSubmit(entry);
            }
        }

        public void CommitChanges()
        {
            this.context.SubmitChanges();
            this.Commit();
        }

        public IEnumerable<ROMDBEntry> GetRecentlyPlayed()
        {
            return this.context.ROMTable
                .Where(r => (r.LastPlayed != FileHandler.DEFAULT_DATETIME))
                .OrderByDescending(f => f.LastPlayed)
                .Take(5)
                .ToArray();
        }

        public IEnumerable<ROMDBEntry> GetROMList()
        {
            return this.context.ROMTable
                .OrderBy(f => f.DisplayName)
                .ToArray();
        }

        public IEnumerable<SavestateEntry> GetSavestatesForROM(ROMDBEntry entry)
        {
            return this.context.SavestateTable
                .Where(s => s.ROM == entry)
                .OrderBy(s => s.Slot);
        }

        public bool RemoveSavestateFromDB(SavestateEntry entry)
        {
            try
            {
                entry.ROM.Savestates.Remove(entry);
                this.context.SavestateTable.DeleteOnSubmit(entry);
                this.context.SubmitChanges();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            this.context.Dispose();
        }
    }
}
