using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
        public List<App> _ApplicationList = new List<App>();
        public List<App> ApplicationList
        {
            get { return _ApplicationList; }
            set {
                _ApplicationList = value;
                OnPropertyChanged("ApplicationList");
            }
        }

        private ArgonDB db = new ArgonDB();
        private int duration = 60;
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

        public NetworkGraph()
        {
            InitializeComponent();
            SentValues = new ChartValues<DateTimePoint>();
            RecvValues = new ChartValues<DateTimePoint>();
            GetValues(duration);
            From = DateTime.Now.AddSeconds(-61).Ticks.NextSecond();
            To = DateTime.Now.AddSeconds(-1).Ticks.NextSecond();
            Formatter = x => new DateTime((long)x).ToString("hh:mm:ss tt");
            ApplicationList = GetAppList();
            AppListViewSource = new CollectionViewSource
            {
                Source = ApplicationList,
            };
            AppListViewSource.View.SortDescriptions.Add(new SortDescription("Total", ListSortDirection.Descending));

            Task.Run(() =>
            {
                while (true) {
                    Thread.Sleep(1000);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SentValues.RemoveAt(SentValues.Count - 1);
                        RecvValues.RemoveAt(RecvValues.Count - 1);
                        SentValues.Add(GetLastValue(true, 3));
                        RecvValues.Add(GetLastValue(false, 3));
                        SentValues.Add(GetLastValue(true, 2));
                        RecvValues.Add(GetLastValue(false, 2));
                        SetValue();
                        From += TimeSpan.FromSeconds(1).Ticks;
                        To += TimeSpan.FromSeconds(1).Ticks;
                        SortDescription sd = AppListViewSource.View.SortDescriptions.FirstOrDefault();
                        int PrevSelectedIndex = AppListGridView.SelectedIndex;
                        ApplicationList = GetAppList();
                        AppListViewSource.Source = ApplicationList;
                        AppListViewSource.View.SortDescriptions.Add(sd);
                        AppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
                        AppListGridView.SelectedIndex = PrevSelectedIndex;
                    });
                }
            });

            DataContext = this;
        }

        protected List<App> GetAppList()
        {
            return db.NetworkTraffic
                     .Where(x => (double)x.Time > From && (double)x.Time < To)
                     .GroupBy(x => x.ApplicationName)
                     .Select(y => new App
                     {
                         Name = y.First().ApplicationName,
                         Path = y.First().FilePath,
                         Sent = y.Sum(z => z.Sent),
                         Recv = y.Sum(z => z.Recv),
                         Total = y.Sum(z => z.Sent) + y.Sum(z => z.Recv)
                     }).ToList();
        }

        public void GetValues(int duration)
        {
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

        DateTimePoint GetLastValue(bool send, int sec)
        {
            var time = new DateTime(DateTime.Now.AddSeconds(-sec).Ticks.NextSecond());
            int data;
            if (send)
                data = db.NetworkTraffic
                         .Where(x => x.Time == time.Ticks)
                         .GroupBy(x => x.Time)
                         .Select(x => x.Sum(y => y.Sent))
                         .FirstOrDefault();
            else
                data = db.NetworkTraffic
                         .Where(x => x.Time == time.Ticks)
                         .GroupBy(x => x.Time)
                         .Select(x => x.Sum(y => y.Recv))
                         .FirstOrDefault();

            return new DateTimePoint(time, data);

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
                _from = value > DateTime.Now.AddSeconds(-61).Ticks.NextSecond() ? DateTime.Now.AddSeconds(-61).Ticks.NextSecond() : value;
                OnPropertyChanged("From");
            }
        }
        public double To
        {
            get { return _to; }
            set {
                _to = value > DateTime.Now.AddSeconds(-1).Ticks.NextSecond() ? DateTime.Now.AddSeconds(-1).Ticks.NextSecond() : value;
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
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class App
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public int Sent { get; set; }
            public int Recv { get; set; }
            public int Total { get; set; }
        }
    }
}