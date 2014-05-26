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

using MpeCore.Classes;

namespace MPEIService
{
    public partial class MPEIService : ServiceBase
    {
        static string NAME = "MPEITest";
        static ManualResetEvent evt = new ManualResetEvent(false);
        public MPEIService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            new Thread(StartPipeServer).Start();
            evt.WaitOne();

        }

        protected override void OnStop()
        {
        }

        static void StartPipeServer()
        {

            using (NamedPipeServerStream server = new NamedPipeServerStream(NAME, PipeDirection.InOut))
            {
                evt.Set();
                EventLog.WriteEntry("MPEIService", "Waiting for connection ...");
                server.WaitForConnection();
                EventLog.WriteEntry("MPEIService", "Connection established.");

                using (StreamReader sr = new StreamReader(server))
                {
                    //using (StreamWriter sw = new StreamWriter(server))
                    //{
                    while (server.IsConnected)
                    {
                        string s = sr.ReadLine();
                        if (!string.IsNullOrEmpty(s))
                        {
                            EventLog.WriteEntry("MPEITest", string.Format("Received command: {0}", s));
                            ParseCommand(s);
                        }
                        //sw.WriteLine(s);
                        //sw.Flush();
                        Thread.Sleep(1000);
                    }
                    //}
                }
            }
        }
        static void ParseCommand(string command)
        {
            if (command.StartsWith("MPEI"))
            {
                string[] args = command.Split();
                switch (args[1])
                {
                    case "Install":
                        {
                            string packID = args[2];
                            string version = args[3];
                            Process mp = Process.GetProcessesByName("MediaPortal").FirstOrDefault();
                            mp.CloseMainWindow();
                            mp.Close();
                            InstallExtension(packID, version);

                            break;
                        }
                    case "Uninstall":
                        {

                            break;
                        }
                    case "DownloadUpdates":
                        {

                            break;
                        }
                    case "Update":
                        {
                            break;
                        }




                }
            }
        }

        static void InstallExtension(string id, string version)
        {
            PackageClass packageClass = MpeInstaller.KnownExtensions.GetList(id).Items.FirstOrDefault(p => p.GeneralInfo.Version.ToString() == version);
            string newPackageLocation = ExtensionUpdateDownloader.GetPackageLocation(packageClass, Client_DownloadProgressChanged, Client_DownloadFileCompleted);
            if (!File.Exists(newPackageLocation))
            {
                //MessageBox.Show("Can't locate the installer package. Install aborted");
                return;
            }
            PackageClass pak = new PackageClass();
            pak = pak.ZipProvider.Load(newPackageLocation);
            if (pak == null)
            {
                //MessageBox.Show("Package loading error ! Install aborted!");
                try
                {
                    if (newPackageLocation != packageClass.GeneralInfo.Location)
                        File.Delete(newPackageLocation);
                }
                catch { }
                return;
            }
            if (!pak.CheckDependency(false))
            {
                //if (MessageBox.Show("Dependency check error! Install aborted!\nWould you like to view more details?", pak.GeneralInfo.Name,
                //  MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                //{
                //    //DependencyForm frm = new DependencyForm(pak);
                //    //frm.ShowDialog();
                //}
                pak.ZipProvider.Dispose();
                try
                {
                    if (newPackageLocation != packageClass.GeneralInfo.Location)
                        File.Delete(newPackageLocation);
                }
                catch { }
                return;
            }

            if (packageClass.GeneralInfo.Version.CompareTo(pak.GeneralInfo.Version) != 0)
            {
//                if (MessageBox.Show(
//                  string.Format(@"Downloaded version of {0} is {1} and differs from your selected version: {2}!
//Do you want to continue ?", packageClass.GeneralInfo.Name, pak.GeneralInfo.Version, packageClass.GeneralInfo.Version), "Install extension", MessageBoxButtons.YesNo,
//                  MessageBoxIcon.Error) != DialogResult.Yes)
//                    return;
            }

            //if (
            //  //MessageBox.Show(
            //    "This operation will install " + packageClass.GeneralInfo.Name + " version " +
            //    pak.GeneralInfo.Version + "\n Do you want to continue ?", "Install extension", MessageBoxButtons.YesNo,
            //    MessageBoxIcon.Exclamation) != DialogResult.Yes)
            //    return;
            //this.Hide();
            packageClass = MpeCore.MpeInstaller.InstalledExtensions.Get(packageClass.GeneralInfo.Id);
            if (packageClass != null)
            {
                if (pak.GeneralInfo.Params[ParamNamesConst.FORCE_TO_UNINSTALL_ON_UPDATE].GetValueAsBool())
                {
                    //if (
                    //  MessageBox.Show(
                    //    "Another version of this extension is installed\nand needs to be uninstalled first.\nDo you want to continue?\n" +
                    //    "Old extension version: " + packageClass.GeneralInfo.Version + "\n" +
                    //    "New extension version: " + pak.GeneralInfo.Version,
                    //    "Install extension", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                    //{
                    //    //this.Show();
                    //    return;
                    //}
                    //UnInstall dlg = new UnInstall();
                    //dlg.Execute(packageClass, false);
                }
                else
                {
                    MpeCore.MpeInstaller.InstalledExtensions.Remove(packageClass);
                }
                pak.CopyGroupCheck(packageClass);
            }
            pak.StartInstallWizard();
            //RefreshListControls();
            pak.ZipProvider.Dispose();
            try
            {
                if (newPackageLocation != packageClass.GeneralInfo.Location)
                    File.Delete(newPackageLocation);
            }
            catch { }
            //this.Show();
        }
        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //if (splashScreen.Visible)
            //{
            //    splashScreen.ResetProgress();
            //}
        }

        private static void Client_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            //if (splashScreen.Visible)
            //{
            //    splashScreen.SetProgress("Downloading", e.ProgressPercentage);
            //}
        }
    }
}
