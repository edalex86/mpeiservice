using System;
using System.ServiceModel;
using MpeCore.Classes;

namespace MPEITestClient
{
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

    class Program
    {
        static void Main(string[] args)
        {
            //using (NamedPipeClientStream client = new NamedPipeClientStream(".", "MPEITest", PipeDirection.InOut))
            //{
            //    Console.WriteLine("Connecting to server ...");
            //    client.Connect(5000);
            //    if(client.IsConnected)
            //    Console.WriteLine("Connected.");

            //    using (StreamWriter sw = new StreamWriter(client))
            //    {

            //        sw.WriteLine("В");
            //        sw.Flush();
            //    }
            //    client.Close();
            //}



            ChannelFactory<ICommand> pipeFactory =
              new ChannelFactory<ICommand>(
                new NetNamedPipeBinding(),
                new EndpointAddress(
                  "net.pipe://localhost/MPEIService"));
            ICommand pipeProxy =
              pipeFactory.CreateChannel();

            while (true)
            {
                bool all = false;
                Console.WriteLine(DateTime.Now + " action started");
                //Console.WriteLine("pipe: " + pipeProxy.Install("b4293f64-9e83-4f1f-b2e3-8bdea2a37425", VersionInfo.Parse("1.1.5.0")));
                //Console.WriteLine("pipe: " + pipeProxy.UnInstall("b4293f64-9e83-4f1f-b2e3-8bdea2a37425"));
                Console.WriteLine(DateTime.Now + " action finished");
                Console.ReadKey();
            }
        }
    }
}
