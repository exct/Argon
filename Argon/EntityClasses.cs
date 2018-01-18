using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace Argon
{
    public class ArgonDB : DataConnection
    {
        public ArgonDB() : base("ArgonDB") { }

        public ITable<NetworkTraffic> NetworkTraffic { get { return GetTable<NetworkTraffic>(); } }
        public ITable<ProcessCounter> ProcessCounters { get { return GetTable<ProcessCounter>(); } }
        public ITable<Icon> Icons { get { return GetTable<Icon>(); } }

    }


    [Table(Name = "NetworkTraffic")]
    public class NetworkTraffic
    {
        [Column(Name = "Time"), NotNull]
        public decimal Time { get; set; }

        [Column(Name = "ApplicationName"), NotNull]
        public string ApplicationName { get; set; }

        [Column(Name = "ProcessName"), NotNull]
        public string ProcessName { get; set; }

        [Column(Name = "FilePath"), NotNull]
        public string FilePath { get; set; }

        [Column(Name = "Sent"), NotNull]
        public int Sent { get; set; }

        [Column(Name = "Recv"), NotNull]
        public int Recv { get; set; }

        [Column(Name = "SourceAddr"), NotNull]
        public string SourceAddr { get; set; }

        [Column(Name = "SourcePort"), NotNull]
        public string SourcePort { get; set; }

        [Column(Name = "DestAddr"), NotNull]
        public string DestAddr { get; set; }

        [Column(Name = "DestPort"), NotNull]
        public string DestPort { get; set; }

        [Column(Name = "Type"), NotNull]
        public int Type { get; set; }

        [Column(Name = "ProcessID"), NotNull]
        public int ProcessID { get; set; }
    }

    [Table(Name = "ProcessCounters")]
    public class ProcessCounter
    {
        [Column(Name = "Time"), NotNull]
        public long Time { get; set; }

        [Column(Name = "Name"), NotNull]
        public string Name { get; set; }

        [Column(Name = "Path"), NotNull]
        public string Path { get; set; }

        [Column(Name = "ProcessorLoadPercent"), NotNull]
        public decimal ProcessorLoadPercent { get; set; }
    }

    [Table(Name = "Icons")]
    public class Icon
    {
        [Column(Name = "Path"), NotNull]
        public string Path { get; set; }

        [Column(Name = "Icon"), NotNull]
        public byte[] File { get; set; }
    }


}
