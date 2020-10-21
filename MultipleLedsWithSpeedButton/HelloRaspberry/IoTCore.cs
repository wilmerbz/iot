using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

namespace HelloRaspberry
{
    public static class IoTCore
    {
        private static bool timerDelayChanged = false;
        private static object timerDelayChangedLock = new object();
        private static int currentPin = -1;
        private static GpioPinValue _currentPushButtonValue;
        private const int GPIO_LED_BLUE = 5;
        private const int GPIO_LED_RED = 6;
        private const int GPIO_PUSH_BUTTON = 13;

        private static readonly int[] GpioPinNumbers = new int[] { GPIO_LED_BLUE, GPIO_LED_RED };


        private static readonly int[] Speeds = new int[] { 1000, 500, 250, 50, 25 };
        private static int _currentSpeedIndex = 0;

        private static Dictionary<int, GpioPin> pins;
        private static GpioPin pushButtonPin;

        private const int MAX_LED_INTERVAL_TIME = 1000;
        private const int LED_INTERVAL_TIME_CHANGE = 100;
        private static int _currentLedIntervalTime = MAX_LED_INTERVAL_TIME;

        private static GpioController gpio;
        private static DispatcherTimer timer;
        public static void StartBlinking()
        {

            gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                System.Diagnostics.Debug.WriteLine("No GPIO controller found on this device.");
                return;
            }

            System.Diagnostics.Debug.WriteLine("GPIO controller found.");

            InitializePins();
            InitializeTimer(_currentLedIntervalTime);
        }

        private static void InitializeTimer(int ledDelayTime)
        {
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            else
            {
                timer.Stop();
            }

            timer.Interval = TimeSpan.FromMilliseconds(ledDelayTime);
            timer.Start();

        }

        private static void Timer_Tick(object sender, object e)
        {
            if (timerDelayChanged)
            {
                timerDelayChanged = false;
                InitializeTimer(_currentLedIntervalTime);
            }

            currentPin++;
            currentPin = currentPin >= GpioPinNumbers.Length ? 0 : currentPin;

            int count = 0;
            foreach (var pinNumber in GpioPinNumbers)
            {
                var pinValue = count == currentPin ? GpioPinValue.Low : GpioPinValue.High;
                pins[pinNumber].Write(pinValue);
                count++;
            }

        }


        private static void InitializePins()
        {
            pins = new Dictionary<int, GpioPin>();

            foreach (var gpioPinNumber in GpioPinNumbers)
            {
                var gpioPin = InitializePin(gpioPinNumber, GpioPinValue.High, GpioPinDriveMode.Output);
                pins.Add(gpioPinNumber, gpioPin);
            }

            _currentPushButtonValue = GpioPinValue.Low;
            pushButtonPin = InitializePin(GPIO_PUSH_BUTTON, _currentPushButtonValue, GpioPinDriveMode.InputPullUp);
            pushButtonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            pushButtonPin.ValueChanged += PushButtonPin_ValueChanged;
        }

        private static void PushButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            lock (timerDelayChangedLock)
            {
                if (args.Edge == GpioPinEdge.FallingEdge)
                {
                    _currentSpeedIndex = _currentSpeedIndex == (Speeds.Length - 1) ? 0 : _currentSpeedIndex + 1;
                    _currentLedIntervalTime = Speeds[_currentSpeedIndex];
                    
                    timerDelayChanged = true;
                }

            }
        }

        private static GpioPin InitializePin(int pinNumber, GpioPinValue value, GpioPinDriveMode driveMode)
        {
            if (gpio == null) return null;

            var pin = gpio.OpenPin(pinNumber);
            pin.Write(value);

            if (pin.IsDriveModeSupported(driveMode))
            {
                pin.SetDriveMode(driveMode);
            }

            return pin;
        }
    }
}
