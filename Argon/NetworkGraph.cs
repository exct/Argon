using System;
using System.Linq;

using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Argon
{
    class NetworkGraph
    {
        ArgonDB db = new ArgonDB();

        public SeriesCollection GetValues(int duration)
        {
            var sendVals = new ChartValues<ObservableValue>();
            var recvVals = new ChartValues<ObservableValue>();

            long dt = DateTime.Now.AddSeconds(-duration).Ticks.NextSecond();
            IQueryable<int> data = db.NetworkTraffic
                                     .Where(x => x.Time > dt)
                                     .OrderBy(x => x.Time)
                                     .GroupBy(x => x.Time)
                                     .Select(y => y.Sum(z => z.Sent));

            foreach (var x in data)
                sendVals.Add(new ObservableValue(x));

            return new SeriesCollection
                {
                    new LineSeries { Values = sendVals },
                    new LineSeries { Values = recvVals }
                };
        }
    }
}
