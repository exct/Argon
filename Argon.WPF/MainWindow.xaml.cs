using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using LinqToDB;
using LinqToDB.Common;

namespace Argon.WPF
{
    public partial class MainWindow : Window
    {
        NetworkTrafficReporter NetReporter = NetworkTrafficReporter.Create();
        System.Timers.Timer timer = new System.Timers.Timer(1000);  //Trace resolution of 1sec 
        long tSent, tRecv = 0;


        public MainWindow()
        {
            InitializeComponent();
            timer.Elapsed += Poll;
            //timer.Enabled = true;
        }

        private void Poll(Object source, System.Timers.ElapsedEventArgs e)
        {
            //NetData = NetReporter.GetNetworkTrafficData();
            Dispatcher.BeginInvoke(new Action(UpdateValues));
        }

        private void UpdateValues()
        {

        }


    }
}
