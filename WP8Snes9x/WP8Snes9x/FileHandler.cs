using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhoneDirect3DXamlAppInterop.Database;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using PhoneDirect3DXamlAppInterop.Resources;
using Windows.Phone.Storage.SharedAccess;

namespace PhoneDirect3DXamlAppInterop
{
    class FileHandler
    {
        public const String ROM_URI_STRING = "rom";
        public const String ROM_DIRECTORY = "roms";
        public const String SAVE_DIRECTORY = "saves";
        public const String DEFAULT_SNAPSHOT = "Assets/no_snapshot.png";
        public static DateTime DEFAULT_DATETIME = new DateTime(1988, 04, 12);

        public static String DEFAULT_BACKGROUND_IMAGE = "Assets/Snes.jpg";

        public static ROMDBEntry InsertNewDBEntry(string fileName)
        {
            ROMDatabase db = ROMDatabase.Current;
            string displayName = fileName.Substring(0, fileName.Length - 4);
            ROMDBEntry entry = new ROMDBEntry()
            {
                DisplayName = displayName,
                FileName = fileName,
                LastPlayed = DEFAULT_DATETIME,
                SnapshotURI = DEFAULT_SNAPSHOT
            };
            db.Add(entry);
            return entry;
        }

        public static async Task FindExistingSavestatesForNewROM(ROMDBEntry entry)
        {
            ROMDatabase db = ROMDatabase.Current;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFolder saveFolder = await romFolder.GetFolderAsync(SAVE_DIRECTORY);
            IReadOnlyList<StorageFile> saves = await saveFolder.GetFilesAsync();
            // Savestates zuordnen
            foreach (var save in saves)
            {
                if (save.Name.Substring(0, save.Name.Length - 2).Equals(entry.DisplayName + ".0"))
                {
                    // Savestate gehoert zu ROM
                    String number = save.Name.Substring(save.Name.Length - 2);
                    int slot = int.Parse(number);
                    SavestateEntry ssEntry = new SavestateEntry()
                    {
                        ROM = entry,
                        Savetime = save.DateCreated.DateTime,
                        Slot = slot,
                        FileName = save.Name
                    };
                    db.Add(ssEntry);
                }
            }
        }

        public static void CaptureSnapshot(ushort[] pixeldata, int pitch, string filename)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                captureSnapshot(pixeldata, pitch, filename);
            }));
        }

        private static async void captureSnapshot(ushort[] pixeldata, int pitch, string filename)
        {
            WriteableBitmap bitmap = new WriteableBitmap(pitch / 2, (int)pixeldata.Length / (pitch / 2));
            int x = 0;
            int y = 0;
            for (int i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
            {
                ushort pixel = pixeldata[i];
                byte r = (byte)((pixel & 0xf800) >> 11);
                byte g = (byte)((pixel & 0x07e0) >> 5);
                byte b = (byte)(pixel & 0x001f);
                r = (byte)((255 * r) / 31);
                g = (byte)((255 * g) / 63);
                b = (byte)((255 * b) / 31);
                bitmap.SetPixel(x, y, r, g, b);
                x++;
                if (x >= bitmap.PixelWidth)
                {
                    y++;
                    x = 0;
                }
            }
            String snapshotName = filename.Substring(0, filename.Length - 3) + "jpg";
            //StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ROM_DIRECTORY);
            ////StorageFolder saveFolder = await folder.GetFolderAsync(SAVE_DIRECTORY);
            //StorageFolder shared = await folder.GetFolderAsync("Shared");
            //StorageFolder shellContent = await shared.GetFolderAsync("ShellContent");
            //StorageFile file = await shellContent.CreateFileAsync(snapshotName, CreationCollisionOption.ReplaceExisting);

            try
            {
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                using (IsolatedStorageFileStream fs = new IsolatedStorageFileStream("/Shared/ShellContent/" + snapshotName, System.IO.FileMode.Create, iso))
                {
                    bitmap.SaveJpeg(fs, bitmap.PixelWidth, bitmap.PixelHeight, 0, 90);
                    //await fs.FlushAsync();
                    fs.Flush(true);
                }
                ROMDatabase db = ROMDatabase.Current;
                ROMDBEntry entry = db.GetROM(filename);
                entry.SnapshotURI = "Shared/ShellContent/" + snapshotName;
                db.CommitChanges();

                UpdateLiveTile();

                UpdateROMTile(filename);
            }
            catch (Exception)
            {
            }



            //try
            //{
            //    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            //    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            //    {
            //        bitmap.SaveJpeg(ms, bitmap.PixelWidth, bitmap.PixelHeight, 0, 90);
            //        byte[] bytes = ms.ToArray();
            //        DataWriter writer = new DataWriter(stream);
            //        writer.WriteBytes(bytes);
            //        await writer.StoreAsync();
            //        writer.DetachStream();
            //        await stream.FlushAsync();
            //    }

            //    ROMDatabase db = ROMDatabase.Current;
            //    ROMDBEntry entry = db.GetROM(filename);
            //    entry.SnapshotURI = "Shared/ShellContent/" + snapshotName;
            //    db.CommitChanges();
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}

            //await file.CopyAsync(shellContent);
        }

        public static void UpdateLiveTile()
        {
            ROMDatabase db = ROMDatabase.Current;
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault();
            FlipTileData data = new FlipTileData();
            data.Title = AppResources.ApplicationTitle;

            //get last snapshot
            String lastSnapshot = db.GetLastSnapshot();

            if (App.metroSettings.UseAccentColor || lastSnapshot == null)  //create see through tile
            {

                data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative);
                data.BackgroundImage = new Uri("Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative);
                data.WideBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative);

                tile.Update(data);
            }
            else  //create opaque tile
            {
                data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmallFilled.png", UriKind.Relative);



                data.BackgroundImage = new Uri("isostore:/" + lastSnapshot, UriKind.Absolute);
                data.WideBackgroundImage = new Uri("isostore:/" + lastSnapshot, UriKind.Absolute);

                tile.Update(data);
            }
        }

        public static void DeleteROMTile(string romFileName)
        {
            var tiles = ShellTile.ActiveTiles;
            romFileName = romFileName.ToLower();
            foreach (var tile in tiles)
            {
                int index = tile.NavigationUri.OriginalString.LastIndexOf('=');
                if (index < 0)
                {
                    continue;
                }
                String romName = tile.NavigationUri.OriginalString.Substring(index + 1);
                if (romName.ToLower().Equals(romFileName))
                {
                    tile.Delete();
                }
            }
        }

        public static void CreateROMTile(ROMDBEntry re)
        {
            FlipTileData data = CreateFlipTileData(re);

            ShellTile.Create(new Uri("/MainPage.xaml?" + ROM_URI_STRING + "=" + re.FileName, UriKind.Relative), data, true);
        }

        public static void UpdateROMTile(string romFileName)
        {
            var tiles = ShellTile.ActiveTiles;
            romFileName = romFileName.ToLower();
            foreach (var tile in tiles)
            {
                int index = tile.NavigationUri.OriginalString.LastIndexOf('=');
                if (index < 0)
                {
                    continue;
                }
                String romName = tile.NavigationUri.OriginalString.Substring(index + 1);
                if (romName.ToLower().Equals(romFileName))
                {
                    ROMDatabase db = ROMDatabase.Current;
                    ROMDBEntry entry = db.GetROM(romFileName);
                    if (entry == null)
                    {
                        break;
                    }

                    FlipTileData data = CreateFlipTileData(entry);
                    tile.Update(data);

                    break;
                }
            }
        }

        private static FlipTileData CreateFlipTileData(ROMDBEntry re)
        {
            FlipTileData data = new FlipTileData();
            data.Title = re.DisplayName;
            if (re.SnapshotURI.Equals(FileHandler.DEFAULT_SNAPSHOT))
            {
                data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative);
                data.BackgroundImage = new Uri("Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative);
                data.WideBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative);
            }
            else
            {
                data.SmallBackgroundImage = new Uri("isostore:/" + re.SnapshotURI, UriKind.Absolute);
                data.BackgroundImage = new Uri("isostore:/" + re.SnapshotURI, UriKind.Absolute);
                data.WideBackgroundImage = new Uri("isostore:/" + re.SnapshotURI, UriKind.Absolute);
            }
            return data;
        }

        public static async Task DeleteROMAsync(ROMDBEntry rom)
        {
            ROMDatabase db = ROMDatabase.Current;
            String fileName = rom.FileName;
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFile file = await folder.GetFileAsync(fileName);
            DeleteROMTile(file.Name);
            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            db.RemoveROM(file.Name);
            db.CommitChanges();
        }

        public static async Task<LoadROMParameter> GetROMFileToPlayAsync(string fileName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFile romFile = await romFolder.GetFileAsync(fileName);
            LoadROMParameter param = new LoadROMParameter()
            {
                file = romFile,
                folder = romFolder
            };
            return param;
        }

        public static async Task FillDatabaseAsync()
        {
            ROMDatabase db = ROMDatabase.Current;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            IReadOnlyList<StorageFile> roms = await romFolder.GetFilesAsync();

            foreach (var file in roms)
            {
                ROMDBEntry entry = FileHandler.InsertNewDBEntry(file.Name);
                await FileHandler.FindExistingSavestatesForNewROM(entry);
            }
            db.CommitChanges();
        }

        public static async Task CreateInitialFolderStructure()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
            StorageFolder saveFolder = await romFolder.CreateFolderAsync(SAVE_DIRECTORY, CreationCollisionOption.OpenIfExists);
        }

        public static void CreateSavestate(int slot, string romFileName)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                createSavestate(slot, romFileName);
            }));
        }

        private static void createSavestate(int slot, string romFileName)
        {
            ROMDatabase db = ROMDatabase.Current;
            string saveFileName = romFileName.Substring(0, romFileName.Length - 3);
            if (slot < 10)
            {
                saveFileName += "00" + slot;
            }
            else
            {
                saveFileName += "0" + slot;
            }
            SavestateEntry entry;
            if ((entry = db.GetSavestate(romFileName, slot)) == null)
            {
                ROMDBEntry rom = db.GetROM(romFileName);
                entry = new SavestateEntry()
                {
                    ROM = rom,
                    FileName = saveFileName,
                    ROMFileName = romFileName,
                    Savetime = DateTime.Now,
                    Slot = slot
                };
                db.Add(entry);
            }
            else
            {
                entry.Savetime = DateTime.Now;
            }
            db.CommitChanges();
        }

        internal static async Task<ROMDBEntry> ImportRomBySharedID(string importRomID,  DependencyObject page)
        {
            ROMDatabase db = ROMDatabase.Current;
            string filename = SharedStorageAccessManager.GetSharedFileName(importRomID).Replace("[1]", "").Replace("%20", " ");


            //set status bar
            var indicator = new ProgressIndicator()
            {
                IsIndeterminate = true,
                IsVisible = true,
                Text = String.Format(AppResources.ImportingProgressText, filename)
            };

            SystemTray.SetProgressIndicator(page, indicator);


            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);

            IStorageFile file = await SharedStorageAccessManager.CopySharedFileAsync(romFolder, filename, NameCollisionOption.ReplaceExisting, importRomID);

            ROMDBEntry entry = db.GetROM(file.Name);

            if (entry == null)
            {
                entry = FileHandler.InsertNewDBEntry(file.Name);
                await FileHandler.FindExistingSavestatesForNewROM(entry);
                db.CommitChanges();
            }

            indicator = new ProgressIndicator()
            {
                IsIndeterminate = false,
                IsVisible = true,

                Text = AppResources.ApplicationTitle

            };

            SystemTray.SetProgressIndicator(page, indicator);
            MessageBox.Show(String.Format(AppResources.ImportCompleteText, entry.DisplayName));


            return entry;
        }

        internal static async void DeleteSRAMFile(ROMDBEntry re)
        {
            string sramName = re.FileName.Substring(0, re.FileName.LastIndexOf('.')) + ".srm";

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFolder saveFolder = await romFolder.GetFolderAsync(SAVE_DIRECTORY);
            try
            {
                IStorageFile file = await saveFolder.GetFileAsync(sramName);
                await file.DeleteAsync();
            }
            catch (Exception) { }
        }

        public static async Task<bool> DeleteSaveState(SavestateEntry entry)
        {
            ROMDatabase db = ROMDatabase.Current;
            if (!db.RemoveSavestateFromDB(entry))
            {
                return false;
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFolder saveFolder = await romFolder.GetFolderAsync(SAVE_DIRECTORY);

            try
            {
                StorageFile file = await saveFolder.GetFileAsync(entry.FileName);
                await file.DeleteAsync();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
