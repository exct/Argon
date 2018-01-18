using System;
using System.Collections.Generic;
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
    public partial class ProcessorGraph : UserControl, INotifyPropertyChanged
    {
        public ChartValues<DateTimePoint> ProcLoadValues { get; set; }
        public CollectionViewSource AppListViewSource { get; set; }

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

        public ProcessorGraph()
        {
            InitializeComponent();
            From = DateTime.Now.AddSeconds(-61).Ticks.NextSecond();
            To = DateTime.Now.AddSeconds(-1).Ticks.NextSecond();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            ProcLoadValues = new ChartValues<DateTimePoint>();
            GetValues(duration);
            Formatter = x => new DateTime((long)x).ToString("hh:mm:ss tt");
            AppListViewSource = new CollectionViewSource
            {
                Source = GetAppList(),
            };
            AppListViewSource.View.SortDescriptions.Add(new SortDescription("Processor", ListSortDirection.Descending));

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
                bool IsScrolling, AtStart, AtEnd, IsActive;
                IsScrolling = AtStart = AtEnd = IsActive = false;

                Dispatcher.Invoke(new Action(() =>
                {
                    IsActive = mainWindow.MainTabControl.SelectedIndex == 0 && mainWindow.GraphTabControl.SelectedIndex == 1;
                    IsScrolling = ScrollChart.IsMouseCaptureWithin;
                    AtStart = ScrollChart.ScrollHorizontalTo > DateTime.Now.AddSeconds(-10).Ticks;
                    AtEnd = ScrollChart.ScrollHorizontalFrom < DateTime.Now.AddSeconds(-duration).Ticks;
                }));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProcLoadValues.RemoveAt(ProcLoadValues.Count - 1);
                    ProcLoadValues.RemoveAt(0);
                    ProcLoadValues.AddRange(GraphValues);
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
            var AppList = GetAppList();

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

        protected List<App> GetAppList()
        {
            using (var db = new ArgonDB()) {
                return db.ProcessCounters
                         .OrderByDescending(x => x.Time)
                         .Take(6000)
                         .Where(x => ((double)x.Time).Between(From, To))
                         .GroupBy(x => x.Name)
                         .Select(y => new App
                         {
                             Icon = y.First().Path.GetIcon(),
                             Name = y.First().Name,
                             Path = y.First().Path,
                             Processor = Math.Round(y.Sum(z => z.ProcessorLoadPercent) / 60, 2)
                         }).ToList();
            }
        }

        public void GetValues(int duration)
        {
            using (var db = new ArgonDB()) {
                var time = new DateTime(DateTime.Now.AddSeconds(-duration).Ticks.NextSecond());
                var data = db.ProcessCounters
                             .OrderBy(x => x.Time)
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

        List<DateTimePoint> GetLast2Values()
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
                             Value = (double)y.Sum(z => z.ProcessorLoadPercent)
                         })
                         .ToList();
            }
        }

        private void SetValue()
        {
            LastValue = ProcLoadValues.Last().Value;
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
            public decimal Processor { get; set; }
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

