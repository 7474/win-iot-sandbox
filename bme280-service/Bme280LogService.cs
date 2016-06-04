using Bme280Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace bme280_service
{
    public sealed class Bme280LogService : IBackgroundTask
    {
        public TimeSpan SummaryUnit { get; private set; }
        private ABme280 _bme280;
        private ICollection<Bme280Data> _poolData;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _poolData = new List<Bme280Data>();
            _bme280.UpdateSensorData += _bme280_UpdateSensorData;
        }

        private async void _bme280_UpdateSensorData(object sender, Bme280DataUpdateEventArgs e)
        {
            try
            {
                await ReceiveBme280Data(e.Data);
            }
            catch (Exception ex)
            {
                // XXX
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async Task ReceiveBme280Data(Bme280Data data)
        {
            _poolData.Add(data);
            if (data.Timestamp - _poolData.First().Timestamp >= SummaryUnit)
            {
                var summary = new Bme280Data(
                    _poolData.Select(x => x.Temperature).Average(),
                    _poolData.Select(x => x.Humidity).Average(),
                    _poolData.Select(x => x.Pressure).Average(),
                    _poolData.Select(x => x.Timestamp).Last()
                    );
                _poolData.Clear();
            }
        }

        private async Task LogSummary(Bme280Data summary)
        {
            //
        }
    }
}
