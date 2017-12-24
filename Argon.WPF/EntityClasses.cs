using LinqToDB;
using LinqToDB.Mapping;

namespace Argon.WPF
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
        public long Time { get; set; }

        [Column(Name = "Process"), NotNull]
        public string Process { get; set; }

        [Column(Name = "FilePath"), NotNull]
        public string FilePath { get; set; }

        [Column(Name = "Sent"), NotNull]
        public int Sent { get; set; }

        [Column(Name = "Recv"), NotNull]
        public int Recv { get; set; }

        [Column(Name = "LocalAddr")]
        public string LocalAddr { get; set; }

        [Column(Name = "LocalPort")]
        public string LocalPort { get; set; }

        [Column(Name = "RemoteAddr")]
        public string RemoteAddr { get; set; }

        [Column(Name = "RemotePort")]
        public string RemotePort { get; set; }


    }

}
