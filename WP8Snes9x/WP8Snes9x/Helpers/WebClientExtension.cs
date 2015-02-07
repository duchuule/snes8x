using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DucLe.Extensions
{
    public static class WebClientExtension
    {

        public static Task<string> DownloadStringTaskAsync(this WebClient client, Uri uri)
        {
            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
            DownloadStringCompletedEventHandler cancelled = null;
            cancelled = (object sender, DownloadStringCompletedEventArgs e) =>
            {
                client.DownloadStringCompleted -= cancelled;
                if (e.Cancelled)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }
                if (e.Error != null)
                {
                    taskCompletionSource.SetException(e.Error);
                    return;
                }
                taskCompletionSource.SetResult(e.Result);
            };
            client.DownloadStringCompleted += cancelled;
            client.DownloadStringAsync(uri);
            return taskCompletionSource.Task;
        }


        public static Task<string> UploadStringTaskAsync(this WebClient client, Uri uri, string data)
        {
            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
            UploadStringCompletedEventHandler cancelled = null;
            cancelled = (object sender, UploadStringCompletedEventArgs e) =>
            {
                client.UploadStringCompleted -= cancelled;
                if (e.Cancelled)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }
                if (e.Error != null)
                {
                    taskCompletionSource.SetException(e.Error);
                    return;
                }
                taskCompletionSource.SetResult(e.Result);
            };
            client.UploadStringCompleted += cancelled;
            client.UploadStringAsync(uri, data);
            return taskCompletionSource.Task;
        }
    }

}
