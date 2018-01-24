using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using LinqToDB;

using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Events;

using MahApps.Metro.Controls;

namespace Argon
{
    public partial class Graph : UserControl, INotifyPropertyChanged
    {
        public ChartValues<DateTimePoint> SentValues { get; set; }
        public ChartValues<DateTimePoint> RecvValues { get; set; }
        public ChartValues<DateTimePoint> ProcLoadValues { get; set; }
        public CollectionViewSource NetAppListViewSource { get; set; }
        public CollectionViewSource ProcAppListViewSource { get; set; }
        public bool SendSeriesVisibility
        {
            get { return _sendSeriesVisibility; }
            set {
                _sendSeriesVisibility = value;
                OnPropertyChanged("SendSeriesVisibility");
            }
        }
        public bool RecvSeriesVisibility
        {
            get { return _recvSeriesVisibility; }
            set {
                _recvSeriesVisibility = value;
                OnPropertyChanged("RecvSeriesVisibility");
            }
        }
        public bool ProcSeriesVisibility
        {
            get { return _procSeriesVisibility; }
            set {
                _procSeriesVisibility = value;
                OnPropertyChanged("ProcSeriesVisibility");
            }
        }

        private MainWindow mainWindow;
        private int duration = 600;
        private int PrevSelectedIndex = 0;
        private bool _sendSeriesVisibility = true;
        private bool _recvSeriesVisibility = true;
        private bool _procSeriesVisibility = true;
        private double _from;
        private double _to;
        private Func<double, string> _formatter;
        private Func<double, string> _netLabelFormatter;
        DispatcherTimer _timer = new DispatcherTimer();

        public Graph()
        {
            InitializeComponent();
            From = DateTime.Now.AddSeconds(-61).Ticks.NextSecond();
            To = DateTime.Now.AddSeconds(-1).Ticks.NextSecond();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            SentValues = new ChartValues<DateTimePoint>();
            RecvValues = new ChartValues<DateTimePoint>();
            ProcLoadValues = new ChartValues<DateTimePoint>();
            GetNetValues(duration);
            GetProcValues(duration);

            NetAppListViewSource = new CollectionViewSource
            {
                Source = GetNetAppList(),
            };
            NetAppListViewSource.View.SortDescriptions.Add(new SortDescription("Total", ListSortDirection.Descending));

            ProcAppListViewSource = new CollectionViewSource
            {
                Source = GetProcAppList(),
            };
            ProcAppListViewSource.View.SortDescriptions.Add(new SortDescription("Processor", ListSortDirection.Descending));

            Formatter = x => new DateTime((long)x).ToString("hh:mm:ss tt");
            NetLabelFormatter = x => x.AddSizeSuffix() + "/s";

            DataContext = this;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += new EventHandler(Run);
            _timer.Start();
        }

        private void Run(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var NetGraphValues = GetLast2NetValues();
                var ProcGraphValues = GetLast2ProcValues();
                var AppList = new ObservableCollection<App>();
                bool IsScrolling, AtStart, AtEnd, IsActive;
                IsScrolling = AtStart = AtEnd = IsActive = false;

                Dispatcher.Invoke(new Action(() =>
                {
                    IsActive = mainWindow.MainTabControl.SelectedIndex == 0;
                    IsScrolling = ScrollChart.IsMouseCaptureWithin;
                    AtStart = ScrollChart.ScrollHorizontalTo > DateTime.Now.AddSeconds(-10).Ticks;
                    AtEnd = ScrollChart.ScrollHorizontalFrom < DateTime.Now.AddSeconds(-duration).Ticks;
                }));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SentValues.RemoveAt(SentValues.Count - 1);
                    RecvValues.RemoveAt(RecvValues.Count - 1);
                    ProcLoadValues.RemoveAt(ProcLoadValues.Count - 1);
                    SentValues.RemoveAt(0);
                    RecvValues.RemoveAt(0);
                    ProcLoadValues.RemoveAt(0);
                    SentValues.Add(NetGraphValues[0]);
                    RecvValues.Add(NetGraphValues[1]);
                    SentValues.Add(NetGraphValues[2]);
                    RecvValues.Add(NetGraphValues[3]);
                    ProcLoadValues.AddRange(ProcGraphValues);

                    if (!IsScrolling)
                        if (AtStart) {
                            ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-61).Ticks;
                            ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-1).Ticks;
                        }
                        else if (AtEnd) {
                            ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-duration).Ticks;
                            ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-duration + 60).Ticks;
                        }
                }));

                Task.Run(() =>
                {
                    if (IsActive && !IsScrolling && (AtEnd || AtStart))
                        UpdateDataGrid();
                });
            });
        }

        protected void UpdateDataGrid()
        {
            int SelectedIndex = 0;
            Dispatcher.Invoke(new Action(() =>
            {
                SelectedIndex = GraphTabControl.SelectedIndex;
            }));
            if (SelectedIndex == 0) {
                var AppList = GetNetAppList();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SortDescription sd = NetAppListViewSource.View.SortDescriptions.FirstOrDefault();
                    int PrevSelectedIndex = NetAppListGridView.SelectedIndex;
                    NetAppListViewSource.Source = AppList;
                    NetAppListViewSource.View.SortDescriptions.Add(sd);
                    NetAppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
                    NetAppListGridView.SelectedIndex = PrevSelectedIndex;
                }));
            }
            else {
                var AppList = GetProcAppList();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SortDescription sd = ProcAppListViewSource.View.SortDescriptions.FirstOrDefault();
                    int PrevSelectedIndex = ProcAppListGridView.SelectedIndex;
                    ProcAppListViewSource.Source = AppList;
                    ProcAppListViewSource.View.SortDescriptions.Add(sd);
                    ProcAppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
                    ProcAppListGridView.SelectedIndex = PrevSelectedIndex;
                }));
            }
        }

        protected ObservableCollection<NetApp> GetNetAppList()
        {
            using (var db = new ArgonDB()) {
                return db.NetworkTraffic
                         .OrderByDescending(x => x.Time)
                         .Take(30000)
                         .Where(x => ((double)x.Time).Between(From, To))
                         .GroupBy(x => x.ApplicationName)
                         .Select(y => new NetApp
                         {
                             Icon = y.First().FilePath.GetIcon(),
                             Name = y.First().ApplicationName,
                             Path = y.First().FilePath,
                             Sent = y.Sum(z => z.Sent),
                             Recv = y.Sum(z => z.Recv),
                             Total = y.Sum(z => z.Sent) + y.Sum(z => z.Recv)
                         }).ToObservableCollection();
            }
        }

        protected List<ProcApp> GetProcAppList()
        {
            using (var db = new ArgonDB()) {
                return db.ProcessCounters
                         .OrderByDescending(x => x.Time)
                         .Take(10000)
                         .Where(x => ((double)x.Time).Between(From, To))
                         .GroupBy(x => x.Name)
                         .Select(y => new ProcApp
                         {
                             Icon = y.First().Path.GetIcon(),
                             Name = y.First().Name,
                             Path = y.First().Path,
                             Processor = Math.Round(y.Average(z => z.ProcessorLoadPercent), 2)
                         }).ToList();
            }
        }

        public void GetNetValues(int duration)
        {
            using (var db = new ArgonDB()) {
                var time = new DateTime(DateTime.Now.AddSeconds(-duration).Ticks.NextSecond());
                var data = db.NetworkTraffic
                             .OrderByDescending(x => x.Time)
                             .Take(30000)
                             .Where(x => x.Time > time.Ticks)
                             .GroupBy(x => x.Time)
                             .Select(y => new
                             {
                                 Time = y.Select(z => z.Time).First(),
                                 Sent = y.Sum(z => z.Sent),
                                 Recv = y.Sum(z => z.Recv)
                             }).ToList();

                for (int i = 0; i < duration; i++) {
                    var _time = time.AddSeconds(i);
                    var val = data.Where(x => x.Time == _time.Ticks).FirstOrDefault();
                    if (val != null) {
                        SentValues.Add(new DateTimePoint(_time, val.Sent));
                        RecvValues.Add(new DateTimePoint(_time, val.Recv));
                    }
                    else {
                        SentValues.Add(new DateTimePoint(_time, 0));
                        RecvValues.Add(new DateTimePoint(_time, 0));
                    }
                }
            }
        }

        public void GetProcValues(int duration)
        {
            using (var db = new ArgonDB()) {
                var time = new DateTime(DateTime.Now.AddSeconds(-duration).Ticks.NextSecond());
                var data = db.ProcessCounters
                             .OrderByDescending(x => x.Time)
                             .Take(200000)
                             .Where(x => x.Time > time.Ticks)
                             .GroupBy(x => x.Time)
                             .Select(y => new
                             {
                                 Time = y.Select(z => z.Time).First(),
                                 ProcLoad = y.Sum(z => z.ProcessorLoadPercent)
                             }).ToList();

                for (int i = 0; i < duration; i++) {
                    var _time = time.AddSeconds(i);
                    var val = data.Where(x => x.Time == _time.Ticks).FirstOrDefault();
                    if (val != null) {
                        ProcLoadValues.Add(new DateTimePoint(_time, (double)val.ProcLoad));
                    }
                    else {
                        ProcLoadValues.Add(new DateTimePoint(_time, 0));
                    }
                }
            }
        }

        List<DateTimePoint> GetLast2NetValues()
        {
            var time = new DateTime(DateTime.Now.AddSeconds(-3).Ticks.NextSecond());
            List<NetworkTraffic> data;
            List<DateTimePoint> list = new List<DateTimePoint>();
            using (var db = new ArgonDB()) {
                data = db.NetworkTraffic
                         .OrderByDescending(x => x.Time)
                         .Take(100)
                         .Where(x => x.Time.Between(time.Ticks, time.AddSeconds(1).Ticks))
                         .GroupBy(x => x.Time)
                         .Select(y => new NetworkTraffic
                         {
                             Time = y.First().Time,
                             Sent = y.Sum(z => z.Sent),
                             Recv = y.Sum(z => z.Recv)
                         })
                         .ToList();
            }

            for (var i = 0; i < 2; i++) {
                if (data.Count > i) {
                    list.Add(new DateTimePoint(time.AddSeconds(i), data[i].Sent));
                    list.Add(new DateTimePoint(time.AddSeconds(i), data[i].Recv));
                }
                else {
                    list.Add(new DateTimePoint(time.AddSeconds(i), 0));
                    list.Add(new DateTimePoint(time.AddSeconds(i), 0));
                }
            }
            return list;

        }

        List<DateTimePoint> GetLast2ProcValues()
        {
            var time = new DateTime(DateTime.Now.AddSeconds(-3).Ticks.NextSecond());
            using (var db = new ArgonDB()) {
                return db.ProcessCounters
                         .OrderByDescending(x => x.Time)
                         .Take(1000)
                         .Where(x => x.Time.Between(time.Ticks, time.AddSeconds(1).Ticks))
                         .GroupBy(x => x.Time)
                         .Select(y => new DateTimePoint
                         {
                             DateTime = new DateTime(y.First().Time),
                             Value = y.Sum(z => z.ProcessorLoadPercent)
                         })
                         .ToList();
            }
        }

        public object Mapper { get; set; }
        public double From
        {
            get { return _from; }
            set {
                _from = value > DateTime.Now.AddSeconds(-61).Ticks.NextSecond() ? DateTime.Now.AddSeconds(-61).Ticks.NextSecond() : value < DateTime.Now.AddSeconds(-duration).Ticks ? DateTime.Now.AddSeconds(-duration).Ticks : value;
                OnPropertyChanged("From");
            }
        }
        public double To
        {
            get { return _to; }
            set {
                _to = value > DateTime.Now.AddSeconds(-1).Ticks.NextSecond() ? DateTime.Now.AddSeconds(-1).Ticks.NextSecond() : value < DateTime.Now.AddSeconds(-duration + 60).Ticks ? DateTime.Now.AddSeconds(-duration + 60).Ticks : value;
                OnPropertyChanged("To");
            }
        }

        public Func<double, string> NetLabelFormatter
        {
            get { return _netLabelFormatter; }
            set {
                _netLabelFormatter = value;
                OnPropertyChanged("NetLabelFormatter");
            }
        }

        public Func<double, string> Formatter
        {
            get { return _formatter; }
            set {
                _formatter = value;
                OnPropertyChanged("Formatter");
            }
        }

        private void Axis_OnRangeChanged(RangeChangedEventArgs eventargs)
        {
            var currentRange = eventargs.Range;

            if (currentRange < TimeSpan.TicksPerDay * 2) {
                Formatter = x => new DateTime((long)x).ToString("hh:mm:ss tt");
                return;
            }

            if (currentRange < TimeSpan.TicksPerDay * 60) {
                Formatter = x => new DateTime((long)x).ToString("dd MMM yy");
                return;
            }

            if (currentRange < TimeSpan.TicksPerDay * 540) {
                Formatter = x => new DateTime((long)x).ToString("MMM yy");
                return;
            }

            Formatter = x => new DateTime((long)x).ToString("yyyy");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class NetApp
        {
            public BitmapSource Icon { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public double Sent { get; set; }
            public double Recv { get; set; }
            public double Total { get; set; }
        }

        public class ProcApp
        {
            public BitmapSource Icon { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public double Processor { get; set; }
        }

        private void CartesianChart_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ScrollChart.ScrollHorizontalTo > DateTime.Now.AddSeconds(-10).Ticks) {
                ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-61).Ticks;
                ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-1).Ticks;
            }
            else if (ScrollChart.ScrollHorizontalFrom < DateTime.Now.AddSeconds(-duration).Ticks) {
                ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-duration).Ticks;
                ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-duration + 60).Ticks;
            }
            else {
                From = ScrollChart.ScrollHorizontalFrom;
                To = ScrollChart.ScrollHorizontalTo;
            }

            Task.Run(() => { UpdateDataGrid(); });
        }

        private void GraphTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((MetroAnimatedSingleRowTabControl)sender).SelectedIndex != PrevSelectedIndex) {
                PrevSelectedIndex = ((MetroAnimatedSingleRowTabControl)sender).SelectedIndex;
                Task.Run(() => { UpdateDataGrid(); });
            }
        }
    }


}