using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

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
            try {
                using (EtwSession = new TraceEventSession("ArgonTraceEventSession")) {
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
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID
                                            && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 1).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID
                                            && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 1)
                                            .First().Sent += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 1,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 1).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 1)
                                        .First().Recv += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = 0,
                                            Recv = data.size,
                                            DestAddr = data.saddr.ToString(),
                                            DestPort = data.sport.ToString(),
                                            SourceAddr = data.daddr.ToString(),
                                            SourcePort = data.dport.ToString(),
                                            Type = 1,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpSendIPV6 += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 2).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 2)
                                        .First().Sent += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 2,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.TcpIpRecvIPV6 += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 2).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 2).First().Recv += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = 0,
                                            Recv = data.size,
                                            DestAddr = data.saddr.ToString(),
                                            DestPort = data.sport.ToString(),
                                            SourceAddr = data.daddr.ToString(),
                                            SourcePort = data.dport.ToString(),
                                            Type = 2,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSend += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 3).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 3).First().Sent += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 3,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecv += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 3).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 3).First().Recv += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = 0,
                                            Recv = data.size,
                                            DestAddr = data.saddr.ToString(),
                                            DestPort = data.sport.ToString(),
                                            SourceAddr = data.daddr.ToString(),
                                            SourcePort = data.dport.ToString(),
                                            Type = 3,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpSendIPV6 += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 4).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 4)
                                        .First().Sent += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = data.size,
                                            Recv = 0,
                                            SourceAddr = data.saddr.ToString(),
                                            SourcePort = data.sport.ToString(),
                                            DestAddr = data.daddr.ToString(),
                                            DestPort = data.dport.ToString(),
                                            Type = 4,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Kernel.UdpIpRecvIPV6 += data =>
                    {
                        try {
                            lock (Controller.NetworkTrafficList)
                                lock (Controller.ProcessDataList)
                                    if (Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 4).Any())
                                        Controller.NetworkTrafficList.Where(x => x.ProcessID == data.ProcessID && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Time == data.TimeStamp.Ticks.NextSecond() && x.Type == 4)
                                        .First().Recv += data.size;
                                    else {
                                        Controller.ProcessData p = Controller.ProcessDataList.Where(x => x.ID == data.ProcessID).First();
                                        Controller.NetworkTrafficList.Add(new NetworkTraffic
                                        {
                                            Time = data.TimeStamp.Ticks.NextSecond(),
                                            ApplicationName = p.Name,
                                            ProcessName = data.ProcessName,
                                            FilePath = p.Path,
                                            Sent = 0,
                                            Recv = data.size,
                                            DestAddr = data.saddr.ToString(),
                                            DestPort = data.sport.ToString(),
                                            SourceAddr = data.daddr.ToString(),
                                            SourcePort = data.dport.ToString(),
                                            Type = 4,
                                            ProcessID = data.ProcessID
                                        });
                                    }
                        }
                        catch { }
                    };

                    EtwSession.Source.Process();
                }
            }
            catch {
                if (++Failed > 3)
                    throw;
                else {
                    Thread.Sleep(1000);
                    Initialize();
                }
            }
        }
    }
}
