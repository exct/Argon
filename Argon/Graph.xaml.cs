using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Argon
{
    public partial class Graph : UserControl, INotifyPropertyChanged
    {
        ArgonDB db = new ArgonDB();
        int duration = 60;
        ChartValues<ObservableValue> sendVals = new ChartValues<ObservableValue>();
        ChartValues<ObservableValue> recvVals = new ChartValues<ObservableValue>();
        public SeriesCollection DataSeries { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private double _lastValue;
        public double LastValue
        {
            get { return _lastValue; }
            set {
                _lastValue = value;
                OnPropertyChanged("LastValue");
            }
        }


        public Graph()
        {
            InitializeComponent();

            DataSeries = GetValues(duration);


            Task.Run(() =>
            {
                while (true) {
                    Thread.Sleep(1000);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DataSeries[0].Values.RemoveAt(59);
                        DataSeries[1].Values.RemoveAt(59);
                        DataSeries[0].Values.RemoveAt(0);
                        DataSeries[1].Values.RemoveAt(0);
                        DataSeries[0].Values.Add(GetLastValue(true, 3));
                        DataSeries[1].Values.Add(GetLastValue(false, 3));
                        DataSeries[0].Values.Add(GetLastValue(true, 2));
                        DataSeries[1].Values.Add(GetLastValue(false, 2));
                        SetValue();
                    });
                }
            });

            DataContext = this;
        }

        public SeriesCollection GetValues(int duration)
        {
            var time = DateTime.Now.AddSeconds(-duration);
            var data = db.NetworkTraffic
                         .Where(x => x.Time > time.Ticks.NextSecond())
                         .OrderBy(x => x.Time)
                         .GroupBy(x => x.Time)
                         .Select(y => new
                         {
                             Time = y.Select(z => z.Time).First(),
                             Sent = new ObservableValue(y.Sum(z => z.Sent)),
                             Recv = new ObservableValue(y.Sum(z => z.Recv))
                         });

            var Sent = new List<ObservableValue>();
            var Recv = new List<ObservableValue>();

            for (int i = 0; i < duration; i++) {
                if (data.Where(x => x.Time == time.AddSeconds(i).Ticks.NextSecond()).Count() > 0) {
                    Sent.Add(data.Where(x => x.Time == time.AddSeconds(i).Ticks.NextSecond()).Select(x => x.Sent).First());
                    Recv.Add(data.Where(x => x.Time == time.AddSeconds(i).Ticks.NextSecond()).Select(x => x.Recv).First());
                }
                else {
                    Sent.Add(new ObservableValue(0));
                    Recv.Add(new ObservableValue(0));
                }
            }

            return new SeriesCollection
                {
                    new LineSeries { Values = new ChartValues<ObservableValue>(Sent) },
                    new LineSeries { Values = new ChartValues<ObservableValue>(Recv) }
                };
        }

        ObservableValue GetLastValue(bool send, int sec)
        {
            int data;
            if (send)
                data = db.NetworkTraffic
                         .Where(x => x.Time == DateTime.Now.AddSeconds(-sec).Ticks.NextSecond())
                         .GroupBy(x => x.Time)
                         .Select(x => x.Sum(y => y.Sent))
                         .FirstOrDefault();
            else
                data = db.NetworkTraffic
                         .Where(x => x.Time == DateTime.Now.AddSeconds(-sec).Ticks.NextSecond())
                         .GroupBy(x => x.Time)
                         .Select(x => x.Sum(y => y.Recv))
                         .FirstOrDefault();

            return new ObservableValue(data);

        }

        private void SetValue()
        {
            LastValue = ((ChartValues<ObservableValue>)DataSeries[0].Values).Last().Value +
                        ((ChartValues<ObservableValue>)DataSeries[1].Values).Last().Value;
        }


        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}