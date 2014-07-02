using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using MpeCore.Classes;
using MpeCore;

namespace MPEIService
{
    public class FileDownloader
    {
        private static string Source;
        private static string Dest;
        public static CompressionWebClient Client = new CompressionWebClient();

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (File.Exists(Dest))
                {
                    WaitForNoBusy();
                    if (!Client.IsBusy)
                    {
                        try
                        {
                            File.Delete(Dest);
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.TotalBytesToReceive > 0)
            {
            }
            else
            {
            }
        }


        public FileDownloader(string source, string dest)
        {
            StartDownload(source, dest);
        }

        public static void StartDownload(string source, string dest)
        {
            Source = source;
            Dest = dest;
            //Client.DownloadProgressChanged += client_DownloadProgressChanged;
            //Client.DownloadFileCompleted += client_DownloadFileCompleted;
            Client.UseDefaultCredentials = true;
            Client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            //Client.CachePolicy = new RequestCachePolicy();
            Client.DownloadFile(source, dest);
        }

        private void DownloadFile_Shown(object sender, EventArgs e)
        {
            Client.DownloadFileAsync(new Uri(Source), Dest);
        }


        private void WaitForNoBusy()
        {
            int counter = 0;
            while (Client.IsBusy || counter < 10)
            {
                counter++;
                Thread.Sleep(100);
            }
        }

        public static string GetPackageLocation(PackageClass packageClass, DownloadProgressChangedEventHandler downloadProgressChanged, AsyncCompletedEventHandler downloadFileCompleted)
        {
            string newPackageLocation = packageClass.GeneralInfo.Location;
            if (!File.Exists(newPackageLocation))
            {
                newPackageLocation = packageClass.LocationFolder + packageClass.GeneralInfo.Id + ".mpe2";
                if (!File.Exists(newPackageLocation))
                {
                    if (!string.IsNullOrEmpty(packageClass.GeneralInfo.OnlineLocation))
                    {try
                        {

                            newPackageLocation = Path.GetTempFileName();
                        StartDownload(packageClass.GeneralInfo.OnlineLocation, newPackageLocation);
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                        }
                    }
                }
            }
            return newPackageLocation;
        }

    }
}