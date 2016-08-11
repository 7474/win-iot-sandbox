using bme280_repository;
using Bme280Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace bme280_service
{
    public sealed class Bme280LogService : IBackgroundTask
    {
        private TimeSpan _summaryUnit;
        private ABme280 _bme280;
        private ICollection<Bme280Data> _poolData;
        private IBme280Repository _localRepository;
        private IBme280Repository _remoteRepository;
        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection appServiceConnection;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            serviceDeferral = taskInstance.GetDeferral();
            await FileIO.AppendLinesAsync(
                            await ApplicationData.Current.LocalFolder.CreateFileAsync("service-log.txt", CreationCollisionOption.OpenIfExists),
                            new string[] { "Run, " + DateTime.Now });

            _summaryUnit = TimeSpan.FromMinutes(1);
            _poolData = new List<Bme280Data>();
            _localRepository = new Bme280LocalRepository();
            //_remoteRepository = new Bme280RemoteRepository();
            // XXX 色々パラメータに出したい
            _bme280 = new Bme280I2c("I2C1", 0x76, 1000);
            _bme280.UpdateSensorData += _bme280_UpdateSensorData;
            await _bme280.Initialize();
            _bme280.Start();

            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null &&
                appService.Name == "bme280-service")
            {
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDefferal = args.GetDeferral();
            var message = args.Request.Message;
            string command = message["Command"] as string;
            // XXX たぶん型が対応していない。JSONなんかで受けてやるのがいいのかな？
            var from = message["From"] as DateTime?;
            var to = message["To"] as DateTime?;

            switch (command)
            {
                case "Query":
                    if (from.HasValue && to.HasValue)
                    {
                        var results = _localRepository.GetList(from.Value, to.Value);
                        var responseMessage = new ValueSet();
                        responseMessage.Add("Response", results);
                        await args.Request.SendResponseAsync(responseMessage);
                    }
                    messageDefferal.Complete();
                    break;
                case "Quit":
                    //Service was asked to quit. Give us service deferral
                    //so platform can terminate the background task
                    messageDefferal.Complete();
                    serviceDeferral.Complete();
                    break;
            }
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
            if (data.Timestamp - _poolData.First().Timestamp >= _summaryUnit)
            {
                var summary = new Bme280Data(
                    _poolData.Select(x => x.Temperature).Average(),
                    _poolData.Select(x => x.Humidity).Average(),
                    _poolData.Select(x => x.Pressure).Average(),
                    _poolData.Select(x => x.Timestamp).Last()
                    );
                _poolData.Clear();
                await LogSummary(summary);
            }
        }

        private async Task LogSummary(Bme280Data summary)
        {
            await _localRepository.Add(summary);
            //await _remoteRepository.Add(summary);
        }
    }
}
