﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

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
        public ObservableCollection<App> ApplicationList
        {
            get {
                using (var db = new ArgonDB()) {
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
                             }).ToObservableCollection();
                }
            }
            set { }
        }

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

        private async void Run(object sender, System.EventArgs e)
        {
            await Task.Run(() =>
            {
                var Sent1 = GetLastValue(true, 3);
                var Sent2 = GetLastValue(true, 2);
                var Recv1 = GetLastValue(false, 3);
                var Recv2 = GetLastValue(false, 2);
                var AppList = ApplicationList;
                Dispatcher.Invoke(new Action(() =>
                {
                    SentValues.RemoveAt(0);
                    RecvValues.RemoveAt(0);
                    SentValues.RemoveAt(SentValues.Count - 1);
                    RecvValues.RemoveAt(RecvValues.Count - 1);
                    SentValues.Add(Sent1);
                    RecvValues.Add(Recv1);
                    SentValues.Add(Sent2);
                    RecvValues.Add(Recv2);
                    if (!ScrollChart.IsMouseCaptureWithin) {
                        if (ScrollChart.ScrollHorizontalTo > DateTime.Now.AddSeconds(-10).Ticks) {
                            ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-61).Ticks;
                            ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-1).Ticks;
                        }
                        if (ScrollChart.ScrollHorizontalFrom < DateTime.Now.AddSeconds(-duration).Ticks) {
                            ScrollChart.ScrollHorizontalFrom = From = DateTime.Now.AddSeconds(-duration).Ticks;
                            ScrollChart.ScrollHorizontalTo = To = DateTime.Now.AddSeconds(-duration + 60).Ticks;
                        }
                        SortDescription sd = AppListViewSource.View.SortDescriptions.FirstOrDefault();
                        int PrevSelectedIndex = AppListGridView.SelectedIndex;
                        AppListViewSource.Source = AppList;
                        AppListViewSource.View.SortDescriptions.Add(sd);
                        AppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
                        AppListGridView.SelectedIndex = PrevSelectedIndex;
                    }
                }));
            });
        }

        protected ObservableCollection<App> GetAppList()
        {
            using (var db = new ArgonDB()) {
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

        DateTimePoint GetLastValue(bool send, int sec)
        {
            var time = new DateTime(DateTime.Now.AddSeconds(-sec).Ticks.NextSecond());
            int data;
            using (var db = new ArgonDB()) {
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
            }
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
            public string Name { get; set; }
            public string Path { get; set; }
            public int Sent { get; set; }
            public int Recv { get; set; }
            public int Total { get; set; }
        }


        private void CartesianChart_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var s = (LiveCharts.Wpf.CartesianChart)sender;
            From = s.ScrollHorizontalFrom;
            To = s.ScrollHorizontalTo;

            SortDescription sd = AppListViewSource.View.SortDescriptions.FirstOrDefault();
            int PrevSelectedIndex = AppListGridView.SelectedIndex;
            AppListViewSource.Source = ApplicationList;
            AppListViewSource.View.SortDescriptions.Add(sd);
            AppListGridView.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;
            AppListGridView.SelectedIndex = PrevSelectedIndex;

        }
    }
}