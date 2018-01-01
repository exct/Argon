using LinqToDB;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Argon
{
    public sealed class EtwMonitor
    {
        static TraceEventSession EtwSession;
        private static int Failed = 0;

        public static void Initialize()
        {
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
                        lock (Controller.NewProcesses)
                            Controller.NewProcesses.Add(data.ProcessID);
                    };

                    EtwSession.Source.Kernel.ProcessStop += data =>
                    {
                        lock (Controller.NewProcesses)
                            Controller.NewProcesses.RemoveAll(x => x == data.ProcessID);
                        lock (Controller.ProcessDataList)
                            Controller.ProcessDataList.RemoveAll(p => p.ID == data.ProcessID);
                        lock (Controller.Services)
                            Controller.Services.TryRemove(data.ProcessID, out string x);
                    };

                    EtwSession.Source.Kernel.TcpIpSend += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 1).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            Process = data.ProcessName,
                                            FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 1
                                        });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 1).First().Sent += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 1).Count() == 0)
                                    {
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            Process = data.ProcessName,
                                            FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                            Sent = 0,
                                            Recv = data.size,
                                            DestAddr = data.saddr.ToString(),
                                            DestPort = data.sport.ToString(),
                                            SourceAddr = data.daddr.ToString(),
                                            SourcePort = data.dport.ToString(),
                                            Type = 1
                                        });
                                    }
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 1).First().Recv += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpSendIPV6 += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 2).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            Process = data.ProcessName,
                                            FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 2
                                        });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 2).First().Sent += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpRecvIPV6 += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 2).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = 2
                                    });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 2).First().Recv += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSend += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 3).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = 3
                                    });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 3).First().Sent += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecv += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 3).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = 3
                                    });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 3).First().Recv += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSendIPV6 += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 4).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                        Sent = data.size,
                                        Recv = 0,
                                        SourceAddr = data.saddr.ToString(),
                                        SourcePort = data.sport.ToString(),
                                        DestAddr = data.daddr.ToString(),
                                        DestPort = data.dport.ToString(),
                                        Type = 4
                                    });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 4).First().Sent += data.size;
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecvIPV6 += data =>
                    {
                        try
                        {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 4).Count() == 0)
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                    {
                                        Time = data.TimeStamp.Ticks.NextSecond(),
                                        Process = data.ProcessName,
                                        FilePath = Controller.ProcessDataList.Where(p => p.ID == data.ProcessID).Select(p => p.Path).First(),
                                        Sent = 0,
                                        Recv = data.size,
                                        DestAddr = data.saddr.ToString(),
                                        DestPort = data.sport.ToString(),
                                        SourceAddr = data.daddr.ToString(),
                                        SourcePort = data.dport.ToString(),
                                        Type = 4
                                    });
                                    else
                                        Controller.NetworkTrafficList.Where(x => x.Process == data.ProcessName && x.Type == 4).First().Recv += data.size;
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
    }
}
