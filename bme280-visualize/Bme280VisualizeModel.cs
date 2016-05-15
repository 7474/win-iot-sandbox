using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace bme280_visualize
{
    public class Bme280VisualizeModel : ViewModels.NotificationBase
    {
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
        private ICollection<Bme280Data> _logs;

        public Bme280VisualizeModel(Bme280I2c bme280)
        {
            SensorData = new Bme280Data(0, 0, 0, DateTime.Now);
            _logs = new ObservableCollection<Bme280Data>();
            _bme280 = bme280;
            _bme280.UpdateSensorData += new Bme280UpdateEventHaldler(SensorUpdated);
        }

        private void SensorUpdated(object sendor, Bme280DataUpdateEventArgs e)
        {
            SensorData = e.Data;
            _logs.Add(e.Data);
        }
    }
}
