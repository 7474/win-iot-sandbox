using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace winiot_i2c_checker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void AddOutput(string value)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Output.Text += value + Environment.NewLine;
            });
        }
        private async void PutStatus(string value)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Status.Text = value;
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(new Action(async () =>
            {
                var startAddr = 0x00;
                var endAddr = 0x7f;

                var i2cDeviceSelector = I2cDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(i2cDeviceSelector);
                foreach (var di in dis)
                {
                    AddOutput("---- " + di.Id + " ----");
                    AddOutput("\t" + di.ToString());
                    foreach (var addr in Enumerable.Range(startAddr, endAddr - startAddr + 1))
                    {
                        try
                        {
                            PutStatus("checking " + addr.ToString("x2"));
                            var i2cSetting = new I2cConnectionSettings(addr);
                            i2cSetting.BusSpeed = I2cBusSpeed.FastMode;
                            var i2cDevice = await I2cDevice.FromIdAsync(di.Id, i2cSetting);
                            // XXX Write して大丈夫か？
                            var result = i2cDevice.WritePartial(new byte[] { 0 });
                            if (result.Status != I2cTransferStatus.SlaveAddressNotAcknowledged)
                            {
                                AddOutput(addr.ToString("x2") + " : " + i2cDevice.DeviceId);
                                AddOutput("\t" + i2cDevice.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
            }));
        }
    }
}
