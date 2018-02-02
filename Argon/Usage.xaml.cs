using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using LinqToDB;

using LiveCharts;
using LiveCharts.Wpf;

namespace Argon
{
    public partial class Usage : UserControl
    {
        public CollectionViewSource AppListViewSource { get; set; }
        public CollectionViewSource AppDetailsViewSource { get; set; }
        public bool NetTotal { get; set; }
        public bool NetSend { get; set; }
        public bool NetRecv { get; set; }
        public bool ProcAvg { get; set; }
        public DateTime DateFrom
        {
            get {
                return _dateFrom;
            }
            set {
                _dateFrom = value;
                DateTimePicker_SelectedDateChanged();
                OnPropertyChanged("DateFrom");
            }
        }
        public DateTime DateTo
        {
            get {
                return _dateTo;
            }
            set {
                _dateTo = value;
                DateTimePicker_SelectedDateChanged();
                OnPropertyChanged("DateTo");
            }
        }
        public TimeSpan TimeFrom
        {
            get {
                return new TimeSpan(_dateFrom.TimeOfDay.Ticks);
            }
            set {
                _dateFrom = _dateFrom.Date.Add(value);
                OnPropertyChanged("TimeFrom");
            }
        }
        public TimeSpan TimeTo
        {
            get {
                return new TimeSpan(_dateTo.TimeOfDay.Ticks);
            }
            set {
                _dateTo = _dateTo.Date.Add(value);
                OnPropertyChanged("TimeTo");
            }
        }

        private MainWindow mainWindow;
        private List<AppUsage> AppList = new List<AppUsage>();
        private List<Thread> threads = new List<Thread>();
        private DateTime _dateFrom = DateTime.Today;
        private DateTime _dateTo = DateTime.Today.AddDays(1).AddSeconds(-1);

        public Usage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            AppListViewSource = new CollectionViewSource
            {
                Source = new List<AppUsage>()
            };
            AppListViewSource.View.SortDescriptions.Add(new SortDescription("Total", ListSortDirection.Descending));
            AppListViewSource.View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AppDetailsViewSource = new CollectionViewSource
            {
                Source = new List<AppDetail>()
            };
            DateTimePicker_SelectedDateChanged();
            DataContext = this;
        }

        protected List<AppUsage> GetApplicationList()
        {
            var proc = new List<AppUsage>();
            var net = new List<AppUsage>();

            Task readProc = Task.Factory.StartNew(() =>
            {
                threads.Add(Thread.CurrentThread);
                using (var db = new ArgonDB()) {
                    proc = db.ProcessCounters
                             .AsParallel()
                             .Where(x => ((double)x.Time).Between(DateFrom.Ticks, DateTo.Ticks))
                             .GroupBy(x => new { x.Name, x.Path })
                             .Select(y => new AppUsage
                             {
                                 Icon = y.First().Path.GetIcon(),
                                 Name = y.First().Name,
                                 Path = y.First().Path,
                                 CPU = Math.Round(y.Average(z => z.ProcessorLoadPercent), 2)
                             }).ToList();
                }
            });

            Task readNet = Task.Factory.StartNew(() =>
            {
                threads.Add(Thread.CurrentThread);
                using (var db = new ArgonDB()) {
                    net = db.NetworkTraffic
                            .Where(x => ((double)x.Time).Between(DateFrom.Ticks, DateTo.Ticks))
                            .GroupBy(x => new { x.ApplicationName, x.FilePath })
                            .Select(y => new AppUsage
                            {
                                Name = y.First().ApplicationName,
                                Path = y.First().FilePath,
                                Sent = y.Sum(z => z.Sent),
                                Recv = y.Sum(z => z.Recv),
                                Total = y.Sum(z => z.Sent) + y.Sum(z => z.Recv)
                            }).ToList();
                }
            });

            var net2 = new List<AppUsage>();
            readNet.Wait();
            readProc.Wait();

            Parallel.ForEach(net, (n) =>
            {
                var p = proc.Where(x => x.Path == n.Path && x.Name == n.Name).FirstOrDefault();
                if (p == null) {
                    net2.Add(new AppUsage
                    {
                        Icon = n.Path.GetIcon(),
                        Name = n.Name,
                        Path = n.Path,
                        Sent = n.Sent,
                        Recv = n.Recv,
                        Total = n.Total
                    });
                }
                else {
                    p.Sent = n.Sent;
                    p.Recv = n.Recv;
                    p.Total = n.Total;
                }
            });

            proc.AddRange(net2);
            return proc;

        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void KillRunningThreads()
        {
            if (threads.Count > 0) {
                Parallel.ForEach(threads, (t) =>
                {
                    try {
                        t.Abort();
                    }
                    catch { }
                });
                threads.Clear();
            }
        }

        private void DateTimePicker_SelectedDateChanged()
        {
            KillRunningThreads();
            //bool isActive = mainWindow.MainTabControl.SelectedIndex == 1;

            SortDescription sd;
            if (AppListViewSource.View.SortDescriptions.Any())
                sd = AppListViewSource.View.SortDescriptions.First();
            else
                sd = new SortDescription("Name", ListSortDirection.Ascending);

            AppList = null;
            AppListViewSource.Source = new List<AppUsage>();
            AppDetailsViewSource.Source = new List<AppDetail>();
            UsagePieChart.Series = null;

            if (_dateFrom > _dateTo || _dateFrom > DateTime.Now) {
                ProgressRing1.Visibility = Visibility.Collapsed;
                ErrorInvalidDateRange.Visibility = Visibility.Visible;
                return;
            }
            else
                ErrorInvalidDateRange.Visibility = Visibility.Collapsed;

            ProgressRing1.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                threads.Add(Thread.CurrentThread);
                //if (!isActive)
                //    Thread.Sleep(3000);

                AppList = GetApplicationList();

                Dispatcher.BeginInvoke(new Action(() =>
                {

                    ProgressRing1.Visibility = Visibility.Collapsed;
                    AppListViewSource.Source = AppList;
                    AppListViewSource.View.SortDescriptions.Add(sd);
                    AppListViewSource.View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    AppListDataGrid.Columns.First(x => x.Header.ToString() == sd.PropertyName).SortDirection = sd.Direction;

                    GetChartSeries();
                    AppListDataGrid.SelectedIndex = 0;
                    var selectedItem = (AppUsage)AppListDataGrid.SelectedItem;
                    if (selectedItem != null)
                        UsageChartPushOut(selectedItem.Name, selectedItem.Path);
                }));

                threads.Clear();
            });
        }

        private void GetChartSeries()
        {
            if (AppList == null) return;

            Task.Run(() =>
            {
                var series = new List<PieSeries>();
                Dispatcher.Invoke(new Action(() =>
                {
                    series = AppList.OrderByDescending(x => NetTotal ? x.Total :
                                                            NetSend ? x.Sent :
                                                            NetRecv ? x.Recv :
                                                            x.CPU)
                                    .Select(x => new PieSeries
                                    {
                                        Title = x.Name,
                                        Tag = x.Path,
                                        Values = new ChartValues<double> { NetTotal ? x.Total :
                                                                           NetSend ? x.Sent :
                                                                           NetRecv ? x.Recv :
                                                                           x.CPU },
                                        DataLabels = false
                                    }).ToList();

                }));

                var seriesCollection = new SeriesCollection();
                foreach (var s in series)
                    seriesCollection.Add(s);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UsagePieChart.Series = seriesCollection;
                }));
            });
        }

        private void UsagePieChart_DataClick(object sender, ChartPoint chartPoint)
        {
            var selectedSeries = (PieSeries)chartPoint.SeriesView;
            UsageChartPushOut(selectedSeries);

            AppListDataGrid.SelectedItem = AppList.Where(x => x.Path == selectedSeries.Tag.ToString() && x.Name == selectedSeries.Title).First();
            AppListDataGrid.ScrollIntoView(AppListDataGrid.SelectedItem);
        }

        private void AppListGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (e.AddedItems.Count == 0) return;
            var item = (AppUsage)(e.AddedItems[0]);

            UsageChartPushOut(item.Name, item.Path);
            GetAppDetails(item);
        }

        protected void UsageChartPushOut(string name, string path)
        {
            if (UsagePieChart.Series == null) return;
            UsageChartPushOut((PieSeries)UsagePieChart.Series.Where(x => ((PieSeries)x).Tag.ToString() == path && x.Title == name).FirstOrDefault());
        }
        protected void UsageChartPushOut(PieSeries series)
        {
            foreach (PieSeries s in UsagePieChart.Series)
                s.PushOut = 0;

            if (series != null)
                series.PushOut = 15;
        }

        protected void GetAppDetails(AppUsage a)
        {
            Task.Run(() =>
            {
                threads.Add(Thread.CurrentThread);

                List<AppDetail> details;
                using (var db = new ArgonDB()) {
                    details = db.NetworkTraffic
                                .Where(x => x.Time.Between(DateFrom.Ticks, DateTo.Ticks) && x.FilePath == a.Path && x.ApplicationName == a.Name)
                                .Select(x => new AppDetail
                                {
                                    SourceIP = x.SourceAddr,
                                    SourcePort = x.SourcePort,
                                    DestinationIP = x.DestAddr,
                                    DestinationPort = x.DestPort
                                }).Distinct().ToList();
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AppDetailsViewSource.Source = details;
                }));

                threads.Clear();
            });
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            GetChartSeries();
        }

        public class AppUsage
        {
            public BitmapSource Icon { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public double Sent { get; set; }
            public double Recv { get; set; }
            public double Total { get; set; }
            public double CPU { get; set; }
        }


        public class AppDetail
        {
            public string SourceIP { get; set; }
            public int SourcePort { get; set; }
            public string DestinationIP { get; set; }
            public int DestinationPort { get; set; }
        }
    }
}
