using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Argon
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : UserControl, INotifyPropertyChanged
    {
        const int shift = 10000000;
        private double _lastValue;

        public SeriesCollection NetworkDataSeries { get; set; }
        ArgonDB db = new ArgonDB();

        public Graph()
        {
            InitializeComponent();

            //Get last 60s from db
            DateTime dt = new DateTime(DateTime.Now.Ticks.NextSecond()); 
            var data = from x in db.NetworkTraffic
                       where x.Time > dt.AddSeconds(-60).Ticks
                       select new NetworkTraffic
                       {
                           Time = x.Time,
                           Sent = x.Sent,
                           Recv = x.Recv
                       };

            var data2 = data.GroupBy(x => x.Time).Select(y => new { Value = y.Sum(z => z.Sent + z.Recv) });
            var ChartVals = new ChartValues<ObservableValue>();
            for (int i = 60; i > 0; i--)
            {
                var time = dt.AddSeconds(-i).Ticks;

                ChartVals.Add(new ObservableValue(0 + data.Where(x => x.Time == time).GroupBy(x => x.Time).Select(y => y.Sum(z => z.Sent + z.Recv)).FirstOrDefault()));
            }



            NetworkDataSeries = new SeriesCollection
            {
                new LineSeries
                {
                    AreaLimit = -10,
                    Values = ChartVals
                }
            };

            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(1000);
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            NetworkDataSeries[0].Values.Add(new ObservableValue(0 +
            //                db.NetworkTraffic.Where(x => x.Time > DateTime.Now.AddSeconds(-1).Ticks).GroupBy(x => x.Time > 1).Select(y => y.Sum(z => z.Sent + z.Recv)).FirstOrDefault()));
            //            NetworkDataSeries[0].Values.RemoveAt(0);
            //            SetValue();
            //        });
            //    }
            //});
            //DataContext = this;
        }

        public double LastValue
        {
            get { return _lastValue; }
            set
            {
                _lastValue = value;
                OnPropertyChanged("LastValue");
            }
        }

        private void SetValue()
        {
            LastValue = ((ChartValues<ObservableValue>)NetworkDataSeries[0].Values).Last().Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}