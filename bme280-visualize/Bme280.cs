using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace bme280_visualize
{
    using Windows.Devices.Enumeration;    // https://ae-bst.resource.bosch.com/media/_tech/media/datasheets/BST-BME280_DS001-11.pdf
    // 6.2.3 Compensation formulas
    // The data type “BME280_S32_t” should define a 32 bit signed integer variable type and can usually be defined as “long signed int”. 
    using BME280_S32_t = Int32;

    /// <summary>
    /// I2C接続設定（CSB LOW）にしたBME280からの読み取りを行う。
    /// </summary>
    public class Bme280I2c
    {
        public Bme280Data Data { get; private set; }
        public int UpdatePeriodMillis { get; private set; }

        public event Bme280UpdateEventHaldler UpdateSensorData;

        private I2cDevice _bme280Ic2;
        private Timer _updateTimer;

        private readonly string I2C_CONTROLLER_NAME;
        private readonly int I2C_ADDR;

        #region For calibration data
        private int dig_T1;
        private int dig_T2;
        private int dig_T3;
        private int dig_P1;
        private int dig_P2;
        private int dig_P3;
        private int dig_P4;
        private int dig_P5;
        private int dig_P6;
        private int dig_P7;
        private int dig_P8;
        private int dig_P9;
        private int dig_H1;
        private int dig_H2;
        private int dig_H3;
        private int dig_H4;
        private int dig_H5;
        private int dig_H6;
        #endregion

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="i2cControllerName">Raspberry Pi: "I2C1"</param>
        /// <param name="i2cAddress">SDO LOW: 0x76, SDO HIGH: 0x77</param>
        /// <param name="updatePeriodMillis">XXX BME280向けに最適化する</param>
        public Bme280I2c(string i2cControllerName, int i2cAddress, int updatePeriodMillis)
        {
            I2C_CONTROLLER_NAME = i2cControllerName;
            I2C_ADDR = i2cAddress;
            UpdatePeriodMillis = updatePeriodMillis;
        }

        public async Task Initialize()
        {
            try
            {
                string aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var dis = await DeviceInformation.FindAllAsync(aqs);

                var settings = new I2cConnectionSettings(I2C_ADDR);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                _bme280Ic2 = await I2cDevice.FromIdAsync(dis[0].Id, settings);
                if (_bme280Ic2 != null)
                {
                    Configure();
                    ReadTrim();
                    Data = ReadData();
                    _updateTimer = new Timer(ReadTimerCallback, null, -1, UpdatePeriodMillis);
                }
                else
                {
                    throw new Exception(string.Format("Device was not found. {0}, {1}", I2C_CONTROLLER_NAME, I2C_ADDR));
                }
            }
            catch (Exception)
            {
                // 処理しない
                throw;
            }
        }

        public void Start()
        {
            _updateTimer.Change(0, UpdatePeriodMillis);
        }

        public void Stop()
        {
            _updateTimer.Change(-1, UpdatePeriodMillis);
        }

        private void Configure()
        {
            // TODO 任意の設定を行えるようにする
            // http://trac.switch-science.com/wiki/BME280
            byte osrs_t = 1;             //Temperature oversampling x 1
            byte osrs_p = 1;             //Pressure oversampling x 1
            byte osrs_h = 1;             //Humidity oversampling x 1
            byte mode = 3;               //Normal mode
            byte t_sb = 5;               //Tstandby 1000ms
            byte filter = 0;             //Filter off 
            byte spi3w_en = 0;           //3-wire SPI Disable

            byte ctrl_meas_reg = (byte)((osrs_t << 5) | (osrs_p << 2) | mode);
            byte config_reg = (byte)((t_sb << 5) | (filter << 2) | spi3w_en);
            byte ctrl_hum_reg = osrs_h;

            WriteRegister(0xF2, ctrl_hum_reg);
            WriteRegister(0xF4, ctrl_meas_reg);
            WriteRegister(0xF5, config_reg);
        }

        private void WriteRegister(byte address, byte data)
        {
            byte[] writeBuf = new byte[] { address, data };
            _bme280Ic2.Write(writeBuf);
        }

        private void ReadTrim()
        {
            byte[] writeBuf;
            var from0x88 = new byte[26];
            var from0xe1 = new byte[16];

            writeBuf = new byte[] { 0x88 };
            _bme280Ic2.WriteRead(writeBuf, from0x88);

            writeBuf = new byte[] { 0xe1 };
            _bme280Ic2.WriteRead(writeBuf, from0xe1);

            var calib = new Bme280CompenstionParameter(from0x88, from0xe1);

            dig_T1 = calib.dig_T1;
            dig_T2 = calib.dig_T2;
            dig_T3 = calib.dig_T3;
            dig_P1 = calib.dig_P1;
            dig_P2 = calib.dig_P2;
            dig_P3 = calib.dig_P3;
            dig_P4 = calib.dig_P4;
            dig_P5 = calib.dig_P5;
            dig_P6 = calib.dig_P6;
            dig_P7 = calib.dig_P7;
            dig_P8 = calib.dig_P8;
            dig_P9 = calib.dig_P9;
            dig_H1 = calib.dig_H1;
            dig_H2 = calib.dig_H2;
            dig_H3 = calib.dig_H3;
            dig_H4 = calib.dig_H4;
            dig_H5 = calib.dig_H5;
            dig_H6 = calib.dig_H6;
        }

        private Bme280Data ReadData()
        {
            byte[] writeBuf = new byte[] { 0xF7 };
            byte[] readBuf = new byte[8];
            _bme280Ic2.WriteRead(writeBuf, readBuf);

            var pres_raw = (readBuf[0] << 12) | (readBuf[1] << 4) | (readBuf[2] >> 4);
            var temp_raw = (readBuf[3] << 12) | (readBuf[4] << 4) | (readBuf[5] >> 4);
            var hum_raw = (readBuf[6] << 8) | readBuf[7];

            var temp_act = calibration_T(temp_raw);
            var press_act = calibration_P(pres_raw);
            var hum_act = calibration_H(hum_raw);

            return new Bme280Data(temp_act, hum_act, press_act, DateTime.UtcNow);
        }

        private BME280_S32_t t_fine;
        private double calibration_T(int adc_T)
        {
            double var1, var2, T;
            var1 = (((double)adc_T) / 16384.0 - ((double)dig_T1) / 1024.0) * ((double)dig_T2);
            var2 = ((((double)adc_T) / 131072.0 - ((double)dig_T1) / 8192.0) * (((double)adc_T) / 131072.0 - ((double)dig_T1) / 8192.0)) * ((double)dig_T3);
            t_fine = (BME280_S32_t)(var1 + var2);
            T = (var1 + var2) / 5120.0;
            return T;
        }

        private double calibration_P(int adc_P)
        {
            double var1, var2, p;
            var1 = ((double)t_fine / 2.0) - 64000.0;
            var2 = var1 * var1 * ((double)dig_P6) / 32768.0;
            var2 = var2 + var1 * ((double)dig_P5) * 2.0;
            var2 = (var2 / 4.0) + (((double)dig_P4) * 65536.0);
            var1 = (((double)dig_P3) * var1 * var1 / 524288.0 + ((double)dig_P2) * var1) / 524288.0;
            var1 = (1.0 + var1 / 32768.0) * ((double)dig_P1);
            if (var1 == 0.0)
            {
                return 0.0;
                // avoid exception caused by division by zero  
            }
            p = 1048576.0 - (double)adc_P;
            p = (p - (var2 / 4096.0)) * 6250.0 / var1;
            var1 = ((double)dig_P9) * p * p / 2147483648.0;
            var2 = p * ((double)dig_P8) / 32768.0;
            p = p + (var1 + var2 + ((double)dig_P7)) / 16.0;
            return p;
        }

        private double calibration_H(int adc_H)
        {
            double var_H;

            var_H = (((double)t_fine) - 76800.0);
            var_H = (adc_H - (((double)dig_H4) * 64.0 + ((double)dig_H5) / 16384.0 * var_H)) * (((double)dig_H2) / 65536.0 * (1.0 + ((double)dig_H6) / 67108864.0 * var_H * (1.0 + ((double)dig_H3) / 67108864.0 * var_H)));
            var_H = var_H * (1.0 - ((double)dig_H1) * var_H / 524288.0);
            if (var_H > 100.0) var_H = 100.0;
            else if (var_H < 0.0) var_H = 0.0;

            return var_H;
        }

        private void ReadTimerCallback(object state)
        {
            var data = ReadData();
            Data = data;
            var callbacks = UpdateSensorData;
            if (callbacks != null)
            {
                callbacks(this, new Bme280DataUpdateEventArgs(data));
            }
        }

    }

    public delegate void Bme280UpdateEventHaldler(object sender, Bme280DataUpdateEventArgs e);

    public class Bme280DataUpdateEventArgs : EventArgs
    {
        public Bme280Data Data { get; private set; }

        public Bme280DataUpdateEventArgs(Bme280Data data)
        {
            Data = data;
        }
    }

    public class Bme280Data
    {
        public double Temperature { get; private set; }
        public double Humidity { get; private set; }
        public double Pressure { get; private set; }
        public DateTime Timestamp { get; private set; }

        public Bme280Data(double temperature, double humidity, double pressure, DateTime timestamp)
        {
            Temperature = temperature;
            Humidity = humidity;
            Pressure = pressure;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return string.Format("Temperature={0}, Humidity={1}, Pressure={2}, Timestamp={3}", Temperature, Humidity, Pressure, Timestamp);
        }
    }

    /// <summary>
    /// https://ae-bst.resource.bosch.com/media/_tech/media/datasheets/BST-BME280_DS001-11.pdf
    /// 6.2.2 Trimming parameter readout 
    /// Table 16: Compensation parameter storage, naming and data type 
    /// </summary>
    public class Bme280CompenstionParameter
    {
        public ushort dig_T1 { get { return (ushort)((GetData(0x89) << 8) | GetData(0x88)); } }
        public short dig_T2 { get { return (short)((GetData(0x8b) << 8) | GetData(0x8a)); } }
        public short dig_T3 { get { return (short)((GetData(0x8d) << 8) | GetData(0x8c)); } }
        public ushort dig_P1 { get { return (ushort)((GetData(0x8f) << 8) | GetData(0x8e)); } }
        public short dig_P2 { get { return (short)((GetData(0x91) << 8) | GetData(0x90)); } }
        public short dig_P3 { get { return (short)((GetData(0x93) << 8) | GetData(0x92)); } }
        public short dig_P4 { get { return (short)((GetData(0x95) << 8) | GetData(0x94)); } }
        public short dig_P5 { get { return (short)((GetData(0x97) << 8) | GetData(0x96)); } }
        public short dig_P6 { get { return (short)((GetData(0x99) << 8) | GetData(0x98)); } }
        public short dig_P7 { get { return (short)((GetData(0x9b) << 8) | GetData(0x9a)); } }
        public short dig_P8 { get { return (short)((GetData(0x9d) << 8) | GetData(0x9c)); } }
        public short dig_P9 { get { return (short)((GetData(0x9f) << 8) | GetData(0x9e)); } }
        public byte dig_H1 { get { return (byte)(GetData(0xa1)); } }
        public short dig_H2 { get { return (short)((GetData(0xe2) << 8) | GetData(0xe1)); } }
        public byte dig_H3 { get { return (byte)(GetData(0xe3)); } }
        public short dig_H4 { get { return (short)((GetData(0xe4) << 4) | (GetData(0xe5) & 0x0f)); } }
        public short dig_H5 { get { return (short)((GetData(0xe6) << 4) | ((GetData(0xe5) >> 4) & 0x0f)); } }
        public sbyte dig_H6 { get { return (sbyte)(GetData(0xe7)); } }

        /// <summary>
        /// 0x88 ~ 26バイト分の生データ
        /// </summary>
        private byte[] _from0x88;
        /// <summary>
        /// 0xe1 ~ 16バイト分の生データ
        /// </summary>
        private byte[] _from0xe1;

        public Bme280CompenstionParameter(byte[] from0x88, byte[] from0xe1)
        {
            _from0x88 = from0x88;
            _from0xe1 = from0xe1;
        }

        public byte GetData(int address)
        {
            if (address < 0xe1)
            {
                return _from0x88[address - 0x88];
            }
            else
            {
                return _from0xe1[address - 0xe1];
            }
        }
    }
}
