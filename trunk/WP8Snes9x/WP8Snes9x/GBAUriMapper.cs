using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Windows.Phone.Storage.SharedAccess;
using CloudSixConnector.FilePicker;
using System.Net;

namespace PhoneDirect3DXamlAppInterop
{
    class GBAUriMapper : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            this.tempUri = uri.ToString();

            if (tempUri.Contains("/FileTypeAssociation"))
            {
                int fileIdIndex = tempUri.IndexOf("fileToken=") + 10;
                string fileID = tempUri.Substring(fileIdIndex);

                string incomingFileName = SharedStorageAccessManager.GetSharedFileName(fileID);
                string incomingFileType = Path.GetExtension(incomingFileName).ToLower();

                if (incomingFileType.Contains("cloudsix")) //this is from cloudsix, need to get the true file name and file type
                {
                    CloudSixFileSelected fileinfo = CloudSixPicker.GetAnswer(fileID);
                    incomingFileName = fileinfo.Filename;
                    incomingFileType = Path.GetExtension(incomingFileName).ToLower();
                }


                if (incomingFileType == ".smc" || incomingFileType == ".sfc" || incomingFileType == ".srm"
                    || incomingFileType == ".000" || incomingFileType == ".001" || incomingFileType == ".002" || incomingFileType == ".003"
                    || incomingFileType == ".004" || incomingFileType == ".005" || incomingFileType == ".006" || incomingFileType == ".007"
                    || incomingFileType == ".008" || incomingFileType == ".009" 
                    || incomingFileType == ".zip" || incomingFileType == ".zib" || incomingFileType == ".rar" || incomingFileType == ".7z")
                        return new Uri("/MainPage.xaml?fileToken=" + fileID, UriKind.Relative);
                else
                        return new Uri("/MainPage.xaml", UriKind.Relative);

                }

            return uri;
        }
    }
}
