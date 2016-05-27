using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace bme280_visualize
{
    public class Bme280VisualizeModel : ViewModels.NotificationBase
    {
        public PlotModel Plot
        {
            get { return _plot; }
            private set
            {
                SetProperty(_plot, value, () => _plot = value);
            }
        }
        public ICollection<Bme280Data> Logs
        {
            get { return _logs; }
            private set
            {
                SetProperty(_logs, value, () => _logs = value);
            }
        }
        public Bme280Data SensorData
        {
            get { return _bme280Data; }
            private set
            {
                SetProperty(_bme280Data, value, () => _bme280Data = value);
            }
        }

        private Bme280I2c _bme280;
        private Bme280Data _bme280Data;
        // XXX グラフ表示設定は色々いじる
        private PlotModel _plot;
        private ICollection<Bme280PlotData> _plotLogs;
        //private ICollection<DataPoint> _plotTemperatures;
        private ICollection<Bme280Data> _plotTargets;
        private ICollection<Bme280Data> _logs;

        public Bme280VisualizeModel(Bme280I2c bme280)
        {
            SensorData = new Bme280Data(0, 0, 0, DateTime.Now);
            _logs = new List<Bme280Data>();
            _bme280 = bme280;
            _bme280.UpdateSensorData += new Bme280UpdateEventHaldler(SensorUpdated);
            //
            _plotLogs = new List<Bme280PlotData>();
            //Enumerable.Range(10, 20).ToList().ForEach(i =>
            //{
            //    _plotLogs.Add(
            //        new Bme280PlotData(i, i + 20, i * 10, DateTime.Now.AddMinutes(i)));
            //});
            _plotTargets = new List<Bme280Data>();
            var plot = new PlotModel()
            {
                Title = "SensorData"
            };
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Temperature",
                Title = "Temperature",
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 50
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Humidity",
                Title = "Humidity",
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 100
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Pressure",
                Title = "Pressure",
                Position = AxisPosition.Right,
                Minimum = 900,
                Maximum = 1100
            });
            plot.Axes.Add(new DateTimeAxis()
            {
                Position = AxisPosition.Bottom,
                LabelFormatter = (x) => DateTimeAxis.ToDateTime(x).ToString("HH:mm:ss"),
                //Minimum = DateTimeAxis.ToDouble(DateTime.UtcNow),
                //Maximum = DateTimeAxis.ToDouble(DateTime.UtcNow.AddHours(0.5))
            });
            // https://github.com/oxyplot/docs/blob/master/models/series/LineSeries.rst
            plot.Series.Add(new LineSeries()
            {
                Title = "Temperature",
                Mapping = (x) =>
                {
                    var data = x as Bme280PlotData;
                    return new DataPoint(data.PlotTimestamp, data.Temperature);
                },
                ItemsSource = _plotLogs,
                YAxisKey = "Temperature"
            });
            //plot.Series.Add(new LineSeries()
            //{
            //    Title = "Temperature-DataField",
            //    DataFieldX = "PlotTimestamp",
            //    DataFieldY = "Temperature",
            //    ItemsSource = _plotLogs,
            //    YAxisKey = "To100"
            //});
            plot.Series.Add(new LineSeries()
            {
                Title = "Humidity",
                DataFieldX = "PlotTimestamp",
                DataFieldY = "Humidity",
                ItemsSource = _plotLogs,
                YAxisKey = "Humidity"
            });
            plot.Series.Add(new LineSeries()
            {
                Title = "Pressure",
                Mapping = (x) =>
                {
                    var data = x as Bme280PlotData;
                    // パスカル -> ヘクトパスカル
                    return new DataPoint(data.PlotTimestamp, data.Pressure / 100);
                },
                //DataFieldX = "PlotTimestamp",
                //DataFieldY = "Pressure",
                ItemsSource = _plotLogs,
                YAxisKey = "Pressure"
            });
            Plot = plot;
        }

        private void SensorUpdated(object sendor, Bme280DataUpdateEventArgs e)
        {
            SensorData = e.Data;
            // XXX 生ログをどうにかする
            //_logs.Add(e.Data);
            _plotTargets.Add(e.Data);
            // XXX タイムスライスにする
            //if (_plotTargets.Count >= 60)
            if (_plotTargets.Count >= 5)
            {
                _plotLogs.Add(
                    new Bme280PlotData(
                    _plotTargets.Select(x => x.Temperature).Average(),
                    _plotTargets.Select(x => x.Humidity).Average(),
                    _plotTargets.Select(x => x.Pressure).Average(),
                    _plotTargets.Select(x => x.Timestamp).Last()
                    ));
                _plotTargets.Clear();
                if (_plotLogs.Count > 600)
                {
                    _plotLogs.Remove(_plotLogs.First());
                }
                // http://oxyplot.codeplex.com/wikipage?title=WpfExample2
                // XXX Note that the Plot control is not observing changes in your dataset.
                //RaisePropertyChanged("Plot");
                Plot.InvalidatePlot(true);
            }
        }
    }

    class Bme280PlotData : Bme280Data
    {
        public double PlotTimestamp { get { return DateTimeAxis.ToDouble(Timestamp); } }

        public Bme280PlotData(Bme280Data data)
            : base(data.Temperature, data.Humidity, data.Pressure, data.Timestamp)
        {
        }
        public Bme280PlotData(double temperature, double humidity, double pressure, DateTime timestamp)
            : base(temperature, humidity, pressure, timestamp)
        {
        }
    }
}
