using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bme280Uwp;
using Windows.Storage;
using System.IO;
using System.Diagnostics;

namespace bme280_repository
{
    public class Bme280LocalRepository : IBme280Repository
    {
        public string LogFolder { get; private set; }
        private StorageFolder _folder;

        public Bme280LocalRepository(string logFolder = "bme280log")
        {
            LogFolder = logFolder;
            // XXX UWP だと複数のアプリケーションが同一のファイルを見るのは難しい？
            //_folder = KnownFolders.DocumentsLibrary;
            //_folder = ApplicationData.Current.SharedLocalFolder;
            _folder = ApplicationData.Current.LocalFolder;
        }

        public async Task Add(Bme280Data data)
        {
            try
            {
                var file = await GetFile(data.Timestamp);
                await FileIO.AppendLinesAsync(file, new string[] { ToCsv(data) });
            }
            catch (Exception ex)
            {
                // XXX ログを吐くのが結構面倒くさい
                Debug.WriteLine(ex);
            }
        }

        public async Task<IEnumerable<Bme280Data>> GetList(DateTime from, DateTime to)
        {
            // XXX ひたすらダサい
            var results = new List<Bme280Data>();
            foreach (var d in Enumerable.Range(0, to.Subtract(from).Days + 1).Select(d => from.AddDays(d)))
            {
                IEnumerable<string> stream;
                try
                {
                    var file = await GetFile(d);
                    // XXX これ、IEnumerableでなくIListなの？
                    stream = await FileIO.ReadLinesAsync(file);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    continue;
                }
                foreach (var item in stream.Select(x =>
                    {
                        try
                        {
                            return FromCsv(x);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            return new Bme280Data(0, 0, 0, DateTime.MinValue);
                        }
                    }).Where(x => x.Timestamp >= from && x.Timestamp < to))
                {
                    results.Add(item);
                }
            }
            return results;
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
