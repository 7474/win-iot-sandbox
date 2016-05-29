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
        public Bme280Data SensorData
        {
            get { return _bme280Data; }
            private set
            {
                SetProperty(_bme280Data, value, () => _bme280Data = value);
            }
        }
        public TimeSpan SummaryUnit { get; private set; }
        public TimeSpan DisplayUnit { get; private set; }

        private Bme280I2c _bme280;
        private Bme280Data _bme280Data;
        private PlotModel _plot;
        private ICollection<Bme280PlotData> _plotLogs;
        private ICollection<Bme280Data> _plotTargets;

        private IBme280Logger _logger;

        public Bme280VisualizeModel(Bme280I2c bme280, TimeSpan summaryUnit, TimeSpan displayUnit, IBme280Logger logger)
        {
            SensorData = new Bme280Data(0, 0, 0, DateTime.Now);
            SummaryUnit = summaryUnit;
            DisplayUnit = displayUnit;
            _logger = logger;
            _bme280 = bme280;
            _bme280.UpdateSensorData += new Bme280UpdateEventHaldler(SensorUpdated);
            //
            _plotLogs = new List<Bme280PlotData>();
            _plotTargets = new List<Bme280Data>();
            var plot = new PlotModel()
            {
                Title = "SensorData"
            };
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Temperature",
                Title = "Temperature(℃)",
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 50
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Humidity",
                Title = "Humidity(%)",
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 100
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Pressure",
                Title = "Pressure(Pascal)",
                Position = AxisPosition.Right,
                Minimum = 90000,
                Maximum = 110000
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
                DataFieldX = "PlotTimestamp",
                DataFieldY = "Temperature",
                ItemsSource = _plotLogs,
                YAxisKey = "Temperature"
            });
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
                DataFieldX = "PlotTimestamp",
                DataFieldY = "Pressure",
                ItemsSource = _plotLogs,
                YAxisKey = "Pressure"
            });
            Plot = plot;
        }

        private void SensorUpdated(object sendor, Bme280DataUpdateEventArgs e)
        {
            SensorData = e.Data;
            _logger.Log(e.Data);
            _plotTargets.Add(e.Data);

            if (_plotTargets.First().Timestamp + SummaryUnit <= e.Data.Timestamp)
            {
                _plotLogs.Add(
                    new Bme280PlotData(
                    _plotTargets.Select(x => x.Temperature).Average(),
                    _plotTargets.Select(x => x.Humidity).Average(),
                    _plotTargets.Select(x => x.Pressure).Average(),
                    _plotTargets.Select(x => x.Timestamp).Last()
                    ));
                _plotTargets.Clear();

                while (_plotLogs.Last().Timestamp - _plotLogs.First().Timestamp > DisplayUnit)
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
