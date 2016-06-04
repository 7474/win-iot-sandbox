using Bme280Uwp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace bme280_repository
{
    public interface IBme280Repository
    {
        Task Add(Bme280Data data);
        Task<IEnumerable<Bme280Data>> GetList(DateTime fron, DateTime to);
    }
}
