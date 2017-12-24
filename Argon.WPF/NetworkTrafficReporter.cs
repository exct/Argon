using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Linq;
using System.Windows;
using LinqToDB;
using System.Diagnostics;

namespace Argon.WPF
{
    public sealed class NetworkTrafficReporter : IDisposable
    {
        private TraceEventSession EtwSession;

        private NetworkTrafficReporter() { }

        public static NetworkTrafficReporter Create()
        {
            var ntReporter = new NetworkTrafficReporter();
            ntReporter.Initialise();
            return ntReporter;
        }

        private void Initialise()
        {
            // ETW class blocks processing messages, so should be run on a different thread if you want the application to remain responsive.
            Task.Run(() => StartEtwSession());
        }

        private void StartEtwSession()
        {
            var db = new ArgonDB();

            try
            {
                using (EtwSession = new TraceEventSession("MyKernelAndClrEventsSession"))
                {
                    EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        try
                        {
                            db.NetworkTraffic
                            .Value(x => x.Time, data.TimeStamp.Ticks)
                            .Value(x => x.Process, data.ProcessName)
                            .Value(x => x.FilePath, Process.GetProcessById(data.ProcessID).MainModule.FileName)
                            .Value(x => x.Sent, 0)
                            .Value(x => x.Recv, data.size)
                            .Value(x => x.LocalAddr, data.saddr.ToString())
                            .Value(x => x.LocalPort, data.sport.ToString())
                            .Value(x => x.RemoteAddr, data.daddr.ToString())
                            .Value(x => x.RemotePort, data.dport.ToString())
                            .Insert();
                        }
                        catch (ArgumentException) { }
                    };

                    EtwSession.Source.Kernel.TcpIpSend += data =>
                    {
                        try { 
                        db.NetworkTraffic
                        .Value(x => x.Time, DateTime.Now.Ticks)
                        .Value(x => x.Process, data.ProcessName)
                        .Value(x => x.FilePath, Process.GetProcessById(data.ProcessID).MainModule.FileName)
                        .Value(x => x.Sent, data.size)
                        .Value(x => x.Recv, 0)
                        .Value(x => x.LocalAddr, data.saddr.ToString())
                        .Value(x => x.LocalPort, data.sport.ToString())
                        .Value(x => x.RemoteAddr, data.daddr.ToString())
                        .Value(x => x.RemotePort, data.dport.ToString())
                        .Insert();
                        }
                        catch (ArgumentException) { }
                    };

                    EtwSession.Source.Process();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("ETW monitoring session failure.");
                throw (e);
            }
        }

        public void Dispose()
        {
            EtwSession?.Dispose();
        }
    }
}
