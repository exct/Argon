using LinqToDB;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Argon
{
    public static class EtwMonitor
    {
        static TraceEventSession EtwSession;
        static List<NetworkTraffic> NetworkTrafficList = new List<NetworkTraffic>();
        private static System.Timers.Timer timer = new System.Timers.Timer(1000);
        private static int Failed = 0;

        public static void Initialize()
        {
            timer.Elapsed += WriteNetTrafficToDb;
            timer.Start();
            Task.Run(() => StartEtwSession());
        }

        static void StartEtwSession()
        {
            try
            {
                using (EtwSession = new TraceEventSession("ArgonTraceEventSession"))
                {
                    EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP |
                                                    KernelTraceEventParser.Keywords.Process);

                    EtwSession.Source.Kernel.ProcessStart += data =>
                    {
                        lock (Processes.NewProcesses)
                            Processes.NewProcesses.Add(data.ProcessID);
                    };

                    EtwSession.Source.Kernel.ProcessStop += data =>
                    {
                        lock (Processes.NewProcesses)
                            Processes.NewProcesses.RemoveAll(x => x == data.ProcessID);
                        lock (Processes.ProcessDataList)
                            Processes.ProcessDataList.RemoveAll(p => p.ID == data.ProcessID);
                    };

                    EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        try
                        {
                            lock (NetworkTrafficList)
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
                                lock (Processes.ProcessDataList)
                                    NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Processes.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
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
            catch
            {
                if (++Failed > 3)
                    throw;
                else
                {
                    Thread.Sleep(1000);
                    Initialize();
                }
            }
        }

        static void WriteNetTrafficToDb(object sender, System.Timers.ElapsedEventArgs e)
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

    }
}
