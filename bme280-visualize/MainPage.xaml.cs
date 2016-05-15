using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace bme280_visualize
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // XXX コンストラクタで非同期処理ってどうするのが普通なの？
            // XXX 初期化とかもVMの責務？
            var bme280i2c = new Bme280I2c("I2C1", 0x76, 1000);
            Bme280 = new Bme280VisualizeModel(bme280i2c);
            Initialize(bme280i2c);
        }

        private async void Initialize(Bme280I2c bme280i2c)
        {
            await bme280i2c.Initialize();
            bme280i2c.Start();
        }

        public Bme280VisualizeModel Bme280 { get; private set; }
    }
}
