using bme280_repository;
using Bme280Uwp;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;

namespace my_home_pi
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
        public TimeSpan RefreshUnit { get; private set; }
        public TimeSpan DisplayUnit { get; private set; }

        private Bme280Data _bme280Data;
        private PlotModel _plot;
        private ICollection<Bme280PlotData> _plotLogs;

        private AppServiceConnection appServiceConnection;
        public Bme280VisualizeModel(TimeSpan refreshUnit, TimeSpan displayUnit)
        {
            SensorData = new Bme280Data(0, 0, 0, DateTime.Now);
            RefreshUnit = refreshUnit;
            DisplayUnit = displayUnit;
            //
            _plotLogs = new List<Bme280PlotData>();
            var plot = new PlotModel()
            {
                Title = "SensorData"
            };
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Temperature",
                Title = "Temperature(℃)",
                Position = AxisPosition.Left,
                //Minimum = 0,
                //Maximum = 50
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Humidity",
                Title = "Humidity(%)",
                Position = AxisPosition.Left,
                //Minimum = 0,
                //Maximum = 100
            });
            plot.Axes.Add(new LinearAxis()
            {
                Key = "Pressure",
                Title = "Pressure(Pascal)",
                Position = AxisPosition.Right,
                //Minimum = 90000,
                //Maximum = 110000
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

            Task.Run(async () =>
            {
                // Initialize the AppServiceConnection
                appServiceConnection = new AppServiceConnection();
                appServiceConnection.PackageFamilyName = "my-home-pi_pkqjh7cxnkz54";
                appServiceConnection.AppServiceName = "bme280-service";

                // Send a initialize request 
                var res = await appServiceConnection.OpenAsync();
                if (res != AppServiceConnectionStatus.Success)
                {
                    throw new Exception("Failed to connect to the AppService");
                }
            });
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                // XXX ValueSet が対応するのは Serializable じゃないといけない予感
                //var message = new ValueSet();
                //message.Add("Command", "Query");
                //message.Add("From", (DateTime?)DateTime.UtcNow.AddDays(-1));
                //message.Add("To", (DateTime?)DateTime.UtcNow);
                //var response = await appServiceConnection.SendMessageAsync(message);
                //var responseData = response.Message["Response"] as ICollection<Bme280Data>;
                var responseData = await new Bme280LocalRepository().GetList(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
                if (responseData != null && responseData.Any())
                {
                    // XXX ださみ
                    lock (_plotLogs)
                    {
                        _plotLogs.Clear();
                        foreach (var item in responseData.Select(x => new Bme280PlotData(x)))
                        {
                            _plotLogs.Add(item);
                        }
                    }
                    SensorData = responseData.Last();
                    Plot.InvalidatePlot(true);
                }
            }, RefreshUnit);
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
