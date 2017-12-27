using LinqToDB;
using LinqToDB.Mapping;

namespace Argon
{
    public class ArgonDB : LinqToDB.Data.DataConnection
    {
        public ArgonDB() : base("ArgonDB") { }

        public ITable<NetworkTraffic> NetworkTraffic { get { return GetTable<NetworkTraffic>(); } }

    }


    [Table(Name = "NetworkTraffic")]
    public class NetworkTraffic
    {
        [Column(Name = "Time"), PrimaryKey]
        public decimal Time { get; set; }

        [Column(Name = "Process"), NotNull]
        public string Process { get; set; }

        [Column(Name = "FilePath"), NotNull]
        public string FilePath { get; set; }

        [Column(Name = "Sent"), NotNull]
        public int Sent { get; set; }

        [Column(Name = "Recv"), NotNull]
        public int Recv { get; set; }

        [Column(Name = "LocalAddr"), NotNull]
        public string LocalAddr { get; set; }

        [Column(Name = "LocalPort"), NotNull]
        public string LocalPort { get; set; }

        [Column(Name = "RemoteAddr"), NotNull]
        public string RemoteAddr { get; set; }

        [Column(Name = "RemotePort"), NotNull]
        public string RemotePort { get; set; }
    }

    public class _Process
    {
        [Column(Name = "PID"), NotNull]
        public int PID { get; set; }

        [Column(Name = "Name"), NotNull]
        public string Name { get; set; }

        [Column(Name = "Path"), NotNull]
        public string Path { get; set; }
    }


}
