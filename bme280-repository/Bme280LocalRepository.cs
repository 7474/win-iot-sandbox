using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bme280Uwp;
using Windows.Storage;
using System.IO;

namespace bme280_repository
{
    public class Bme280LocalRepository : IBme280Repository
    {
        public string LogFolder { get; private set; }

        private StorageFolder _folder;

        public Bme280LocalRepository(string logFolder)
        {
            LogFolder = logFolder;
            _folder = ApplicationData.Current.LocalFolder;
        }

        public async void Log(Bme280Data data)
        {
            var fileName = "bme280-" + data.Timestamp.ToString("yyyyMMdd") + ".csv";
            try
            {
                // XXX なんでもかんでも非同期にすればいいってものでもないと思う、通常のシーケンシャルな処理にした方がいいか？
                var file = await _folder.CreateFileAsync(
                    Path.Combine(LogFolder, fileName), CreationCollisionOption.OpenIfExists);
                await FileIO.AppendLinesAsync(file, new string[] { Format(data) });
            }
            catch (Exception ex)
            {
                // XXX I/O に失敗したとおぼわしき時にTMPファイルが残っていたがリカバリ可能なのか？
                // XXX ログを吐くのが結構面倒くさい
                // https://code.msdn.microsoft.com/Logging-Sample-for-Windows-ecd3622f
                Debug.WriteLine(ex);
            }
        }

        public Task Add(Bme280Data data)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Bme280Data>> GetList(DateTime from, DateTime to)
        {
            // XXX むしろ見づらい気がする
            return Enumerable.Range(0, to.Subtract(from).Days + 1)
                .Select(d => from.AddDays(d))
                .Select(async d =>
                {
                    // XXX ファイルレベルで死んだら捨ててもいい気はする
                    try
                    {
                        var file = await GetFile(d);
                        var stream = await FileIO.ReadLinesAsync(file);

                        return stream.Select(x =>
                            {
                                try
                                {

                                    return FromCsv(x);
                                }
                                catch (Exception)
                                {
                                    return new Bme280Data(0, 0, 0, DateTime.MinValue);
                                }
                            })
                            .Where(x => x.Timestamp >= from && x.Timestamp < to);
                    }
                    catch (Exception)
                    {
                        return new List<Bme280Data>();
                    }
                })
                // XXX SelectMany で同期化しないとこける
                .SelectMany(x => x.Result);
        }

        private async Task<StorageFile> GetFile(DateTime target)
        {
            var fileName = "bme280-" + target.ToString("yyyyMMdd") + ".csv";
            var file = await _folder.CreateFileAsync(
                Path.Combine(LogFolder, fileName), CreationCollisionOption.OpenIfExists);
            return file;
        }

        public static string ToCsv(Bme280Data data)
        {
            // XXX To, From で順番が違うのは筋が悪い
            return string.Format("{0},{1},{2},{3}",
                data.Timestamp.ToString("s"),
                data.Temperature,
                data.Humidity,
                data.Pressure);
        }

        public static Bme280Data FromCsv(string record)
        {
            var fields = record.Split(new string[] { "," }, StringSplitOptions.None);
            return new Bme280Data(
                    Convert.ToDouble(fields[1]),
                    Convert.ToDouble(fields[2]),
                    Convert.ToDouble(fields[3]),
                    Convert.ToDateTime(fields[0])
                );
        }
    }
}
