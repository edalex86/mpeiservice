using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using MpeCore;
using System.ServiceModel.Channels;
using System.ServiceModel;

using MpeCore.Classes;

namespace MPEIService
{

    public partial class MPEIService : ServiceBase
    {
        public static bool IsBusy = false;
        public static string CurrentAction = "";
        public bool silent = false;
        public bool installedOnly = false;
        private static int counter = -1;
        private static List<string> onlineFiles = new List<string>();
        static int runningThreads = 0;
        ServiceHost host = null;

        public MPEIService()
        {
            InitializeComponent();
        }
                
        protected override void OnStart(string[] args)
        {
            if (host != null)
            {
                host.Close();
            }
            host = new ServiceHost(typeof(Command), new Uri[] { new Uri("net.pipe://localhost") });
            host.AddServiceEndpoint(typeof(ICommand), new NetNamedPipeBinding(), "MPEIService");
            host.Open();
            EventLog.WriteEntry("MPEIService", "Service is available. " + "Press <ENTER> to exit.");
        }

        protected override void OnStop()
        {
            if (host != null)
            {
                host.Close();
                host = null;
            }
        }

        /// <summary>
        /// Installation of extension by ID and version
        /// </summary>
        /// <param name="id">Extension ID</param>
        /// <param name="version">Extension version</param>
        public static bool InstallExtension(string id, VersionInfo version)
        {
            if (MpeInstaller.KnownExtensions == null)
                MpeInstaller.Init();
            List<PackageClass> listpack = MpeInstaller.KnownExtensions.GetList(id).Items;
            PackageClass packageClass = listpack.FirstOrDefault(p => p.GeneralInfo.Version.CompareTo(version) == 0);
            string newPackageLocation = FileDownloader.GetPackageLocation(packageClass, Client_DownloadProgressChanged, Client_DownloadFileCompleted);
            //packageClass.FileInstalled += new MpeCore.Classes.Events.FileInstalledEventHandler(packageClass_FileInstalled);

            if (!File.Exists(newPackageLocation))
            {
                Log.Instance().Print("Can't locate the installer package. Install aborted");
                return false;
            }
            PackageClass pak = new PackageClass();

            pak = pak.ZipProvider.Load(newPackageLocation);
            if (pak == null)
            {
                Log.Instance().Print("Package loading error ! Install aborted!");
                try
                {
                    if (newPackageLocation != packageClass.GeneralInfo.Location)
                        File.Delete(newPackageLocation);
                }
                catch { }
                return false;
            }
            if (!pak.CheckDependency(false))
            {
                Log.Instance().Print("Dependency check error! Install aborted!");
                pak.ZipProvider.Dispose();
                try
                {
                    if (newPackageLocation != packageClass.GeneralInfo.Location)
                        File.Delete(newPackageLocation);
                }
                catch { }
                return false;
            }

            if (packageClass.GeneralInfo.Version.CompareTo(pak.GeneralInfo.Version) != 0)
                Log.Instance().Print(string.Format(@"Downloaded version of {0} is {1} and differs from your selected version: {2}!",
                    packageClass.GeneralInfo.Name, pak.GeneralInfo.Version, packageClass.GeneralInfo.Version));


            packageClass = MpeCore.MpeInstaller.InstalledExtensions.Get(packageClass.GeneralInfo.Id);
            if (packageClass != null)
            {
                if (pak.GeneralInfo.Params[ParamNamesConst.FORCE_TO_UNINSTALL_ON_UPDATE].GetValueAsBool())
                {
                    Log.Instance().Print("Another version of this extension is installed  and needs to be uninstalled first");
                    Log.Instance().Print("Old extension version: " + packageClass.GeneralInfo.Version);
                    Log.Instance().Print("New extension version: " + pak.GeneralInfo.Version);
                    UnInstallExtension(packageClass.GeneralInfo.Id);
                }
                else
                {
                    MpeCore.MpeInstaller.InstalledExtensions.Remove(packageClass);
                }
                pak.CopyGroupCheck(packageClass);
            }

            Util.KillAllMediaPortalProcesses();
            pak.Silent = true; 
            if (!Directory.Exists(pak.LocationFolder))
             Directory.CreateDirectory(pak.LocationFolder);
            //copy icon file
            if (!string.IsNullOrEmpty(pak.GeneralInfo.Params[ParamNamesConst.ICON].Value) &&
                File.Exists(pak.GeneralInfo.Params[ParamNamesConst.ICON].Value))
                File.Copy(pak.GeneralInfo.Params[ParamNamesConst.ICON].Value,
                          pak.LocationFolder + "icon" + Path.GetExtension(pak.GeneralInfo.Params[ParamNamesConst.ICON].Value),
                          true);
            //copy the package file 
            string newlocation = pak.LocationFolder + pak.GeneralInfo.Id + ".mpe2";
            if (newlocation.CompareTo(pak.GeneralInfo.Location) != 0)
            {
                File.Copy(pak.GeneralInfo.Location, newlocation, true);
                pak.GeneralInfo.Location = newlocation;
            }
            MpeInstaller.InstalledExtensions.Add(pak);
            MpeInstaller.KnownExtensions.Add(pak);
            MpeInstaller.Save();
            pak.Install();
            pak.UnInstallInfo.Save();
            pak.ZipProvider.Dispose();
            try
            {
                if (newPackageLocation != packageClass.GeneralInfo.Location)
                    File.Delete(newPackageLocation);
            }

            catch { }
            return true;
        }

        /// <summary>
        /// Uninstall extension with specific ID
        /// </summary>
        /// <param name="id">Extension ID</param>
        /// <returns></returns>
        public static bool UnInstallExtension(string id)
        {
            if (MpeInstaller.InstalledExtensions == null)
                MpeInstaller.Init();
            PackageClass packageClass = MpeInstaller.InstalledExtensions.Get(id);
            if (packageClass == null)
                return false;
            packageClass.UnInstallInfo = new UnInstallInfoCollection(packageClass);
            packageClass.UnInstallInfo = packageClass.UnInstallInfo.Load();
            packageClass.Silent = true;
            if (packageClass.UnInstallInfo == null)
            {
                Log.Instance().Print("No uninstall information found");
                return false;
            }
            packageClass.UnInstall();
            return true;
        }


        /// <summary>
        /// Downloading extension updates from online
        /// </summary>
        /// <param name="all">Option to download updates for all known extensions</param>
        /// <returns></returns>
        public string DownloadUpdates(bool all)
        {
            Log.Instance().Print("Starting updates downloading");
            MpeInstaller.Init();
            onlineFiles = MpeCore.MpeInstaller.InstalledExtensions.GetUpdateUrls(new List<string>());
            if (all)
            {
                onlineFiles = MpeCore.MpeInstaller.KnownExtensions.GetUpdateUrls(onlineFiles);
                onlineFiles = MpeCore.MpeInstaller.GetInitialUrlIndex(onlineFiles);
            }

            if (onlineFiles.Count < 1)
            {
                return "";
            }
            runningThreads = 0;
            List<Thread> threadlist = new List<Thread>();
            for (int i = 1; i <= 5; i++)
            {
                Thread t = new Thread(DownloadThread);
                t.Start();
                threadlist.Add(t);
            }
            foreach(Thread t in threadlist)
            {
                t.Join();
            }
            Log.Instance().Print("Finishing updates downloading");
            return "OK";
        }

        void DownloadThread()
        {
            lock (this) { runningThreads++; }
            try
            {
                string tempFile = System.IO.Path.GetTempFileName();
                CompressionWebClient client = new CompressionWebClient();

                while (onlineFiles.Count > 0)
                {
                    string onlineFile = "";
                    lock (this)
                    {
                        onlineFile = onlineFiles.First();
                        onlineFiles.Remove(onlineFile);
                    }
                    bool success = false;
                    try
                    {
                        client.DownloadFile(onlineFile, tempFile);
                        var extCol = ExtensionCollection.Load(tempFile);
                        lock (this)
                        {
                            MpeCore.MpeInstaller.KnownExtensions.Add(extCol);
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Error downloading '{0}': {1}", onlineFile, ex.Message));
                    }
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
            }
            catch { }
            finally
            {
                lock (this)
                {
                    runningThreads--;
                    if (runningThreads <= 0)
                    {
                        MpeCore.MpeInstaller.Save();
                    }
                }
            }
        }

        //static void packageClass_FileInstalled(object sender, MpeCore.Classes.Events.InstallEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}
        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {

        }

        private static void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {

        }

        //public MpeCore.Classes.Events.FileUnInstalledEventHandler packageClass_FileUnInstalled { get; set; }
    }

    [ServiceContract]
    public interface ICommand
    {
        [OperationContract]
        bool Install(string ID, VersionInfo version);
        [OperationContract]
        bool UnInstall(string ID);
        [OperationContract]
        bool Update(string ID);
        [OperationContract]
        bool DownloadUpdates(bool all);
        [OperationContract]
        string Ping();
    }

    /// <summary>
    /// Communication interface implementation
    /// </summary>
    public class Command : ICommand
    {
        public bool Install(string ID, VersionInfo version)
        {
            return MPEIService.InstallExtension(ID, version);
        }
        public bool UnInstall(string ID)
        {
            return MPEIService.UnInstallExtension(ID);
        }


        public bool Update(string ID)
        {
            return true;
        }

        public bool DownloadUpdates(bool all)
        {
            MPEIService service = new MPEIService();
            if (service.DownloadUpdates(all) == "OK")
            {

                return true;
            }
            return false;
        }

        public string Ping()
        {
            return "OK";
        }
    }
}
