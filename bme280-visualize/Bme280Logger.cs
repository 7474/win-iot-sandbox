using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace bme280_visualize
{
    public interface IBme280Logger
    {
        // XXX UWP 周りのAPIを見ていると、同期的に1つずつデータを渡すのがイケていない感あり
        void Log(Bme280Data data);
    }

    public class Bme280LocalLogger : IBme280Logger
    {
        public string LogFolder { get; private set; }

        private StorageFolder _folder;

        public Bme280LocalLogger(string logFolder)
        {
            LogFolder = logFolder;
            _folder = ApplicationData.Current.LocalFolder;
        }

        public async void Log(Bme280Data data)
        {
            var fileName = "bme280-" + data.Timestamp.ToString("yyyyMMdd") + ".csv";
            var file = await _folder.CreateFileAsync(
                Path.Combine(LogFolder, fileName), CreationCollisionOption.OpenIfExists);
            await FileIO.AppendLinesAsync(file, new string[] { Format(data) });
        }

        public static string Format(Bme280Data data)
        {
            return string.Format("{0},{1},{2},{3}",
                data.Timestamp.ToString("s"),
                data.Temperature,
                data.Humidity,
                data.Pressure);
        }
    }
}
