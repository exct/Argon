using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using System.Windows;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using LinqToDB;

namespace Argon
{
    public sealed class EtwMonitor : IDisposable
    {
        Timer timer = new Timer(1000);
        public List<NetworkTraffic> NetworkTrafficList = new List<NetworkTraffic>();
        private TraceEventSession EtwSession;

        private EtwMonitor() { }

        public static EtwMonitor Create()
        {
            var mon = new EtwMonitor();
            mon.Initialise();
            return mon;
        }

        private void Initialise()
        {
            timer.Elapsed += WriteNetTrafficToDb;
            timer.Start();
            Processes.GetCurrentProcesses();
            Task.Run(() => StartEtwSession());
        }

        private void StartEtwSession()
        {
            try
            {
                using (EtwSession = new TraceEventSession("ArgonTraceEventSession"))
                {
                    EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP
                                                    | KernelTraceEventParser.Keywords.Process
                                                    | KernelTraceEventParser.Keywords.ProcessCounters);

                    //Process Start/Stops
                    EtwSession.Source.Kernel.ProcessStart += data => { Processes.GetCurrentProcesses(); };
                    EtwSession.Source.Kernel.ProcessStop += data => { Processes.GetCurrentProcesses(); };

                    //Network traffic
                    EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = "T4R"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpSend += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = "T4S"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpRecvIPV6 += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = "T6R"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpSendIPV6 += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = "T6S"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecv += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                            Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = "U4R"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSend += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = "U4S"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecvIPV6 += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = "U6R"
                                    });
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSendIPV6 += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = data.ProcessID == 0 || data.ProcessID == 4 ? "System" :
                                        Processes.ProcessList.Where(p => p.Id == data.ProcessID).Select(p => p.MainModule.FileName).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = "U6S"
                                    });
                        }
                        catch { }
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

        public void WriteNetTrafficToDb(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (NetworkTrafficList)
            {
                using (var db = new ArgonDB())
                    try
                    {
                        db.BeginTransaction();
                        foreach (NetworkTraffic n in NetworkTrafficList)
                            db.Insert(n);
                        db.CommitTransaction();
                    }
                    catch
                    {
                        db.RollbackTransaction();
                    }
                NetworkTrafficList.Clear();
            }
        }

        public void Dispose()
        {
            EtwSession?.Dispose();
        }
    }
}
