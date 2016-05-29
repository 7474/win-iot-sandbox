using Microsoft.IoT.DeviceCore.Pwm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;

namespace l_chika_pwm
{
    public class LedPwmModel : ViewModels.NotificationBase
    {
        /// <summary>
        /// 0.0 ~ 1.0
        /// </summary>
        public double DutyRatio
        {
            get { return DutyCyclePercentage / 100.0; }
        }

        /// <summary>
        /// 0.0 ~ 100.0
        /// </summary>
        public double DutyCyclePercentage
        {
            get { return _ledPwm.DutyCyclePercentage; }
            set
            {
                SetProperty(_ledPwm.DutyCyclePercentage, value, () => _ledPwm.DutyCyclePercentage = value);
                RaisePropertyChanged("DutyRatio");
            }
        }
        private LedPwm _ledPwm;

        public LedPwmModel()
        {
            // XXX 取り方の整理（引数で渡すなど）
            //var provider = PwmSoftware.PwmProviderSoftware.GetPwmProvider();
            var provider = new PwmProviderManager();
            var controllers = PwmController.GetControllersAsync(provider).AsTask().Result;
            var controller = controllers.FirstOrDefault();
            controller.SetDesiredFrequency(100.0);
            _ledPwm = new LedPwm(5, controller);
        }
    }

    // XXX IDisposable
    public class LedPwm
    {
        public double DutyCyclePercentage
        {
            get { return LedPin.GetActiveDutyCyclePercentage() * 100.0; }
            set { LedPin.SetActiveDutyCyclePercentage(value / 100.0); }
        }

        private PwmPin LedPin;
        private PwmController PwmController;
        private int PinNumber;

        public LedPwm(int pinNumber, PwmController pwmController)
        {
            PinNumber = pinNumber;
            PwmController = pwmController;
            LedPin = PwmController.OpenPin(pinNumber);
            LedPin.Start();
            LedPin.SetActiveDutyCyclePercentage(0.0);
        }
    }
}
