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
using PhoneDirect3DXamlAppComponent;
using System.Windows.Media;
using System.IO;
using System.Windows.Resources;
using DucLe.Imaging;


namespace PhoneDirect3DXamlAppInterop
{
    class FileHandler
    {
        public const String ROM_URI_STRING = "rom";
        public const String ROM_DIRECTORY = "roms";
        public const String SAVE_DIRECTORY = "saves";
        public const String DEFAULT_SNAPSHOT = "Assets/no_snapshot.png";
        public static DateTime DEFAULT_DATETIME = new DateTime(1988, 04, 12);

        public static String DEFAULT_BACKGROUND_IMAGE = "Assets/SNES_PAL.jpg";
        public static String CUSTOM_TILE_FILENAME = "myCustomTile1.png";

        public static BitmapImage getBitmapImage(String path, String default_path)
        {

            BitmapImage img = new BitmapImage();

            if (path.Equals(FileHandler.DEFAULT_BACKGROUND_IMAGE))
            {
                Uri uri = new Uri(path, UriKind.Relative);

                img = new BitmapImage(uri);
                return img;
            }

            if (!String.IsNullOrEmpty(path))
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream fs = isoStore.OpenFile(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        img.SetSource(fs);
                    }
                }
            }

            return img;

        }


        public static ROMDBEntry InsertNewDBEntry(string fileName)
        {

            int index = fileName.LastIndexOf('.');
            int diff = fileName.Length - index;

            string displayName = fileName.Substring(0, fileName.Length - diff);
            ROMDBEntry entry = new ROMDBEntry()
            {
                DisplayName = displayName,
                FileName = fileName,
                LastPlayed = DEFAULT_DATETIME,
                SnapshotURI = DEFAULT_SNAPSHOT
            };
            ROMDatabase.Current.Add(entry);
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
                    int slot = 0;
                    if (!int.TryParse(number, out slot))
                    {
                        continue;
                    }
                    if (slot != 9) //dont include auto save
                    {
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
        }

        public static void CaptureSnapshot(ushort[] pixeldata, int pitch, string filename)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                captureSnapshot(pixeldata, pitch, filename);
            }));
        }

        private static void captureSnapshot(ushort[] pixeldata, int pitch, string filename)
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

                CreateOrUpdateSecondaryTile(false);

                UpdateROMTile(filename);
            }
            catch (Exception)
            {
            }

        }

        public static void UpdateLiveTile()
        {
            ROMDatabase db = ROMDatabase.Current;
            ShellTile tile = ShellTile.ActiveTiles.FirstOrDefault();
            FlipTileData data = new FlipTileData();
            data.Title = AppResources.ApplicationTitle;

            //get last snapshot
            IEnumerable<String> lastSnapshots = db.GetRecentSnapshotList();

            if (App.metroSettings.UseAccentColor ||  lastSnapshots.Count() == 0 )  //create see through tile
            {

                data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative);
                data.BackgroundImage = new Uri("Assets/Tiles/FlipCycleTileMedium.png", UriKind.Relative);
                data.WideBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative);

                tile.Update(data);
            }
            else  //create opaque tile
            {
                data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmallFilled.png", UriKind.Relative);


                data.BackgroundImage = new Uri("isostore:/" + lastSnapshots.ElementAt(0), UriKind.Absolute);
                data.WideBackgroundImage = new Uri("isostore:/" + lastSnapshots.ElementAt(0), UriKind.Absolute);

                if (lastSnapshots.Count() >= 2)
                {
                    data.BackBackgroundImage = new Uri("isostore:/" + lastSnapshots.ElementAt(1), UriKind.Absolute);
                    data.WideBackBackgroundImage = new Uri("isostore:/" + lastSnapshots.ElementAt(1), UriKind.Absolute);
                }

                tile.Update(data);
            }

        }


        public static void CreateOrUpdateSecondaryTile(bool forceCreate)
        {
            
            

            CycleTileData data = new CycleTileData();



            data.Title = AppResources.ApplicationTitle;

            IEnumerable<String> snapshots = ROMDatabase.Current.GetRecentSnapshotList();
            List<Uri> uris = new List<Uri>();
            if (snapshots.Count() == 0)
            {
#if !GBC
                uris.Add(new Uri("Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative));
#else
                    uris.Add(new Uri("Assets/Tiles/FlipCycleTileLargeGBC.png", UriKind.Relative));
#endif
            }
            else
            {
                foreach (var snapshot in snapshots)
                {
                    uris.Add(new Uri("isostore:/" + snapshot, UriKind.Absolute));
                }

            }
            data.CycleImages = uris;




            data.SmallBackgroundImage = new Uri("Assets/Tiles/FlipCycleTileSmall.png", UriKind.Relative);

            //find the tile to update
            bool secondaryTileAlreadyExists = false;

            
            foreach (var tile in ShellTile.ActiveTiles)
            {
                //make sure if it is not the default
                if (tile.NavigationUri.OriginalString == "/")
                    continue;

                //rom tile has '=' sign, secondary does not have it
                int index = tile.NavigationUri.OriginalString.LastIndexOf('=');
                if (index >= 0)
                {
                    continue; //this is a rom tile
                }

                //if arrive at this point, it means secondary tile already exists
                secondaryTileAlreadyExists = true;

                tile.Update(data);

                if (forceCreate)
                    MessageBox.Show(AppResources.TileAlreadyPinnedText);

                //no need to find more tile
                break;
            }



            if (secondaryTileAlreadyExists == false && forceCreate)
                ShellTile.Create(new Uri("/MainPage.xaml", UriKind.Relative), data, true);
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
            String fileName = rom.FileName;
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFile file = await folder.GetFileAsync(fileName);
            DeleteROMTile(file.Name);
            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);



            ROMDatabase.Current.RemoveROM(file.Name);
            ROMDatabase.Current.CommitChanges();
        }

        public static async Task<LoadROMParameter> GetROMFileToPlayAsync(string fileName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.GetFolderAsync(ROM_DIRECTORY);
            StorageFile romFile = await romFolder.GetFileAsync(fileName);
            LoadROMParameter param = new LoadROMParameter()
            {
                file = romFile,
                folder = romFolder,
                RomFileName = fileName
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

        internal static async Task<ROMDBEntry> ImportRomBySharedID(string fileID, string desiredName, DependencyObject page)
        {
            //note: desiredName can be different from the file name obtained from fileID
            ROMDatabase db = ROMDatabase.Current;


            //set status bar
            var indicator = SystemTray.GetProgressIndicator(page);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.ImportingProgressText, desiredName);



            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);

            IStorageFile file = await SharedStorageAccessManager.CopySharedFileAsync(romFolder, desiredName, NameCollisionOption.ReplaceExisting, fileID);


            ROMDBEntry entry = db.GetROM(file.Name);

            if (entry == null)
            {
                entry = FileHandler.InsertNewDBEntry(file.Name);
                await FileHandler.FindExistingSavestatesForNewROM(entry);
                db.CommitChanges();
            }

            //update voice command list
            await MainPage.UpdateGameListForVoiceCommand();


            indicator.Text = AppResources.ApplicationTitle;
            indicator.IsIndeterminate = false;

            MessageBox.Show(String.Format(AppResources.ImportCompleteText, entry.DisplayName));


            return entry;
        }


        internal static async Task ImportSaveBySharedID(string fileID, string actualName, DependencyObject page)
        {
            //note:  the file name obtained from fileID can be different from actualName if the file is obtained through cloudsix
            ROMDatabase db = ROMDatabase.Current;


            //check to make sure there is a rom with matching name
            ROMDBEntry entry = null;
            string extension = Path.GetExtension(actualName).ToLower();

            if (extension == ".000" || extension == ".001" || extension == ".002" || extension == ".003" || extension == ".004"
                 || extension == ".005" || extension == ".006" || extension == ".007" || extension == ".008" || extension == ".009")
                entry = db.GetROMFromSavestateName(actualName);
            else if (extension == ".srm")
                entry = db.GetROMFromSRAMName(actualName);

            if (entry == null) //no matching file name
            {
                MessageBox.Show(AppResources.NoMatchingNameText, AppResources.ErrorCaption, MessageBoxButton.OK);
                return;
            }

            //check to make sure format is right
            if (extension == ".000" || extension == ".001" || extension == ".002" || extension == ".003" || extension == ".004"
                 || extension == ".005" || extension == ".006" || extension == ".007" || extension == ".008" || extension == ".009")
            {
                string slot = actualName.Substring(actualName.Length - 1, 1);
                int parsedSlot = 0;
                if (!int.TryParse(slot, out parsedSlot))
                {
                    MessageBox.Show(AppResources.ImportSavestateInvalidFormat, AppResources.ErrorCaption, MessageBoxButton.OK);
                    return;
                }
            }



            //set status bar
            var indicator = SystemTray.GetProgressIndicator(page);
            indicator.IsIndeterminate = true;
            indicator.Text = String.Format(AppResources.ImportingProgressText, actualName);



            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder romFolder = await localFolder.CreateFolderAsync(ROM_DIRECTORY, CreationCollisionOption.OpenIfExists);
            StorageFolder saveFolder = await romFolder.CreateFolderAsync(FileHandler.SAVE_DIRECTORY, CreationCollisionOption.OpenIfExists);


            //if arrive here, entry cannot be null, we can copy the file
            IStorageFile file = null;
            if (extension == ".000" || extension == ".001" || extension == ".002" || extension == ".003" || extension == ".004"
                 || extension == ".005" || extension == ".006" || extension == ".007" || extension == ".008" || extension == ".009")
                file = await SharedStorageAccessManager.CopySharedFileAsync(saveFolder, Path.GetFileNameWithoutExtension(entry.FileName) + actualName.Substring(actualName.Length - 4), NameCollisionOption.ReplaceExisting, fileID);
            else if (extension == ".srm")
            {
                file = await SharedStorageAccessManager.CopySharedFileAsync(saveFolder, Path.GetFileNameWithoutExtension(entry.FileName) + ".srm", NameCollisionOption.ReplaceExisting, fileID);
                entry.SuspendAutoLoadLastState = true;
            }

            //update database
            if (extension == ".000" || extension == ".001" || extension == ".002" || extension == ".003" || extension == ".004"
                  || extension == ".005" || extension == ".006" || extension == ".007" || extension == ".008" || extension == ".009")
            {
                String number = actualName.Substring(actualName.Length - 1, 1);
                int slot = int.Parse(number);

                if (entry != null) //NULL = do nothing
                {
                    SavestateEntry saveentry = db.SavestateEntryExisting(entry.FileName, slot);
                    if (saveentry != null)
                    {
                        //delete entry
                        db.RemoveSavestateFromDB(saveentry);

                    }
                    SavestateEntry ssEntry = new SavestateEntry()
                    {
                        ROM = entry,
                        Savetime = DateTime.Now,
                        Slot = slot,
                        FileName = actualName
                    };
                    db.Add(ssEntry);
                    db.CommitChanges();

                }
            }





            indicator.Text = AppResources.ApplicationTitle;
            indicator.IsIndeterminate = false;

            MessageBox.Show(String.Format(AppResources.ImportCompleteText, entry.DisplayName));


            return;
        }

        internal static async Task DeleteSRAMFile(ROMDBEntry re)
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

        private static Uri CreateAndSavePNGToIsolatedStorage(Uri logo, Color tileColor)
        {
            // Obtain the virtual store for the application.
            using (IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream("Shared/ShellContent/" + CUSTOM_TILE_FILENAME, FileMode.Create, myStore))
                {
                    using (Stream pngStream = RenderAsPNGStream(logo, tileColor))
                    {
                        pngStream.CopyTo(fileStream);
                    }
                }
            }

            return new Uri("isostore:/" + "Shared/ShellContent/" + CUSTOM_TILE_FILENAME, UriKind.Absolute);
        }

        private static Stream RenderAsPNGStream(Uri logo, Color tileColor)
        {
            try
            {
                StreamResourceInfo info;

                info = Application.GetResourceStream(logo);

                // create source bitmap for Image control (image is assumed to be alread 173x173)
                WriteableBitmap wbmp3 = new WriteableBitmap(1, 1);
                try
                {
                    wbmp3.SetSource(info.Stream);
                }
                catch
                {
                }

                WriteableBitmap wb = WriteableBitmapEx.CreateTile(wbmp3, tileColor);



                EditableImage edit = new EditableImage(wb.PixelWidth, wb.PixelHeight);

                for (int y = 0; y < wb.PixelHeight; ++y)
                {
                    for (int x = 0; x < wb.PixelWidth; ++x)
                    {
                        try
                        {
                            byte[] rgba = ControlToPng.ExtractRGBAfromPremultipliedARGB(wb.Pixels[wb.PixelWidth * y + x]);
                            edit.SetPixel(x, y, rgba[0], rgba[1], rgba[2], rgba[3]);
                        }
                        catch (Exception ex)
                        { }
                    }
                }

                return edit.GetStream();

            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
