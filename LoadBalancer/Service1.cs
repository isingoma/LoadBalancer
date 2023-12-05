using BalancerClass.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadBalancer
{
    public partial class Service1 : ServiceBase
    {
        Thread Receive;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Receive = new Thread(new ThreadStart(DoWork));
            Receive.Start();
        }

        protected override void OnStop()
        {
            Receive.Abort();
        }
        public void DoWork()
        {
            try
            {
                TCPProcessor server = new TCPProcessor();

                while (true)
                {
                    server.ListenForTraffic();
                }
            }
            catch (Exception ex)
            {
                //File.AppendAllText(@"D:\CollectionsErrorLogs.txt", "Error: " + ex.Message + " " + DateTime.Now);
            }
        }
    }
}
