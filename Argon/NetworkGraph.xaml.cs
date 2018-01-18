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

namespace Argon
{
    public partial class NetworkGraph : UserControl, INotifyPropertyChanged
    {
        public ChartValues<DateTimePoint> SentValues { get; set; }
        public ChartValues<DateTimePoint> RecvValues { get; set; }
        public CollectionViewSource AppListViewSource { get; set; }
        private ObservableCollection<App> _applicationList = new ObservableCollection<App>();
        public ObservableCollection<App> ApplicationList
        {
            get { return GetAppList(); }
            set { }
        }

        private MainWindow mainWindow;
        private int duration = 600;
        private double _lastValue;
        public double LastValue
        {
            get { return _lastValue; }
            set {
                _lastValue = value;
                OnPropertyChanged("LastValue");
            }
        }

        private Func<double, string> _formatter;
        private double _from;
        private double _to;
        DispatcherTimer _timer = new DispatcherTimer();

        public NetworkGraph()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            SentValues = new ChartValues<DateTimePoint>();
            RecvValues = new ChartValues<DateTimePoint>();

            GetValues(duration);
            From = DateTime.Now.AddSeconds(-61).Ticks.NextSecond();
            To = DateTime.Now.AddSeconds(-1).Ticks.NextSecond();
            Formatter = x => new DateTime((long)x).ToString("hh:mm:ss tt");
            AppListViewSource = new CollectionViewSource
            {
                Source = ApplicationList,
            };
            AppListViewSource.View.SortDescriptions.Add(new SortDescription("Total", ListSortDirection.Descending));

            DataContext = this;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += new System.EventHandler(Run);
            _timer.Start();
        }

        private void Run(object sender, System.EventArgs e)
        {
            Task.Run(() =>
            {
                var GraphValues = GetLast2Values();
                var AppList = new ObservableCollection<App>();
                bool IsScrolling, AtStart, AtEnd, IsActive;
                IsScrolling = AtStart = AtEnd = IsActive = false;

                Dispatcher.Invoke(new Action(() =>
                {
                    IsActive = mainWindow.MainTabControl.SelectedIndex == 0 && mainWindow.GraphTabControl.SelectedIndex == 0;
                    IsScrolling = ScrollChart.IsMouseCaptureWithin;
                    AtStart = ScrollChart.ScrollHorizontalTo > DateTime.Now.AddSeconds(-10).Ticks;
                    AtEnd = ScrollChart.ScrollHorizontalFrom < DateTime.Now.AddSeconds(-duration).Ticks;
                }));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SentValues.RemoveAt(SentValues.Count - 1);
                    RecvValues.RemoveAt(RecvValues.Count - 1);
                    SentValues.RemoveAt(0);
                    RecvValues.RemoveAt(0);
                    SentValues.Add(GraphValues[0]);
                    RecvValues.Add(GraphValues[1]);
                    SentValues.Add(GraphValues[2]);
                    RecvValues.Add(GraphValues[3]);
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

                if (IsActive && !IsScrolling && (AtEnd || AtStart))
                    UpdateDataGrid();
            });
        }

        protected void UpdateDataGrid()
        {
            var AppList = ApplicationList;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                SortDescription sd = AppListViewSource.View.SortDescriptions.FirstOrDefault();
                int PrevSelectedIndex = AppListGridView.SelectedIndex;
                AppListViewSource.Source = AppList;
                AppListViewSource.View.SortDescriptions.Add(sd);
                AppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
                AppListGridView.SelectedIndex = PrevSelectedIndex;
            }));
        }

        protected ObservableCollection<App> GetAppList()
        {
            using (var db = new ArgonDB()) {
                return db.NetworkTraffic
                         .Where(x => ((double)x.Time).Between(From, To))
                         .GroupBy(x => x.ApplicationName)
                         .Select(y => new App
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

        public void GetValues(int duration)
        {
            using (var db = new ArgonDB()) {
                var time = new DateTime(DateTime.Now.AddSeconds(-duration).Ticks.NextSecond());
                var data = db.NetworkTraffic
                             .Where(x => x.Time > time.Ticks)
                             .OrderBy(x => x.Time)
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

        List<DateTimePoint> GetLast2Values()
        {
            var time = new DateTime(DateTime.Now.AddSeconds(-3).Ticks.NextSecond());
            List<NetworkTraffic> data;
            List<DateTimePoint> list = new List<DateTimePoint>();
            using (var db = new ArgonDB()) {
                data = db.NetworkTraffic
                         .Where(x => x.Time.Between(time.Ticks, time.AddSeconds(1).Ticks))
                         .GroupBy(x => x.Time)
                         .Select(y => new NetworkTraffic
                         {
                             Time = y.First().Time,
                             Sent = y.Sum(z => z.Sent),
                             Recv = y.Sum(z => z.Recv)
                         })
                         .OrderBy(x => x.Time)
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

        private void SetValue()
        {
            LastValue = SentValues.Last().Value +
                        RecvValues.Last().Value;
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

        public class App
        {
            public BitmapSource Icon { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public int Sent { get; set; }
            public int Recv { get; set; }
            public int Total { get; set; }
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
    }
}