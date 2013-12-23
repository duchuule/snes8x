using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Windows.Phone.Storage.SharedAccess;

namespace PhoneDirect3DXamlAppInterop
{
    class SnesUriMapper : UriMapperBase
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
                string incomingFileType = Path.GetExtension(incomingFileName);

                switch (incomingFileType)
                {
                    case ".smc":
                    case ".sfc":
                        return new Uri("/MainPage.xaml?fileToken=" + fileID, UriKind.Relative);
                    default:
                        return new Uri("/MainPage.xaml", UriKind.Relative);
                }
            }

            return uri;
        }
    }
}
