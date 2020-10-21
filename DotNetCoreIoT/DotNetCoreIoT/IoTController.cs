using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace DotNetCoreIoT
{
    public class IoTController
    {

        #region Constants

        //private static bool timerDelayChanged = false;
        private static object timerDelayChangedLock = new object();
        private static int currentPin = -1;
        //private static GpioPinValue _currentPushButtonValue;
        private const int GpioLebBlue = 5;
        private const int GpioLedRed = 6;
        private const int GpioPushButton = 13;

        private static readonly int[] GpioPinNumbers = new int[] { GpioLebBlue, GpioLedRed };


        private static readonly int[] Speeds = new int[] { 1000, 500, 250, 50, 25 };
        private static int _currentSpeedIndex = 0;

        //private static Dictionary<int, GpioPin> pins;
        //private static GpioPin pushButtonPin;

        private const int MaxLedIntervalTime = 1000;
        private static int _currentLedIntervalTime = MaxLedIntervalTime;

        private static GpioController _controller;
        //private static DispatcherTimer timer;

        private Timer _timer;

        #endregion

        #region Singleton Instance

        private static readonly Lazy<IoTController> _instance = new Lazy<IoTController>(() => new IoTController());

        /// <summary>
        /// Gets the singleton instance of the IoTController.
        /// </summary>
        public static IoTController Instance => _instance.Value;


        private IoTController()
        {
            Initialize();
        }
        #endregion


        private void Initialize()
        {
            _controller = new GpioController();

            InitializePins();
        }

        public bool Started {get; private set;}

        public void Start(){
            InitializeTimer(_currentLedIntervalTime);    
            Started = true;           
        }

        private void InitializePins()
        {
            foreach (var gpioPinNumber in GpioPinNumbers)
            {
                _controller.OpenPin(gpioPinNumber, PinMode.Output);

            }

            _controller.OpenPin(GpioPushButton, PinMode.InputPullUp);
            _controller.RegisterCallbackForPinValueChangedEvent(GpioPushButton, PinEventTypes.Falling, OnButtonValueChanged);
       }

        private static readonly object _timerDelayChangedLock = new object();

        private void OnButtonValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            lock (_timerDelayChangedLock)
            {
                if (pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling)
                {
                    _currentSpeedIndex = _currentSpeedIndex == (Speeds.Length - 1) ? 0 : _currentSpeedIndex + 1;
                    _currentLedIntervalTime = Speeds[_currentSpeedIndex];
                    Console.WriteLine($"Changing led speed to: {_currentLedIntervalTime} ms");
                    InitializeTimer(_currentLedIntervalTime);
                }
            }
        }

        private void InitializeTimer(int ledDelayTime)
        {
            if (_timer == null)
            {
                _timer = new Timer(ProcessLeds);
            }
            else
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            _timer.Change(ledDelayTime, ledDelayTime);

        }

        private static void ProcessLeds(object state)
        {
            currentPin++;
            currentPin = currentPin >= GpioPinNumbers.Length ? 0 : currentPin;

            int count = 0;
            foreach (var pinNumber in GpioPinNumbers)
            {
                var pinValue = count == currentPin ? PinValue.Low : PinValue.High;

                _controller.Write(pinNumber, pinValue);

                count++;
            }

        }
    }
}
