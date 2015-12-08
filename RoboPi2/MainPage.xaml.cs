using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation.Metadata;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RoboPi2
{
    public sealed partial class MainPage : Page
    {
        private bool isBlinking = false;

        private GpioPin led1;
        private GpioPin motor1A;
        private GpioPin motor1B;
        private GpioPin motor1E;
        private GpioPin motor2A;
        private GpioPin motor2B;
        private GpioPin motor2E;
        private DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        private DispatcherTimer ledTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };

        private readonly GpioController gpio;

        public MainPage()
        {
            this.InitializeComponent();

            timer.Tick += Timer_Tick;

            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioController"))
            {
                gpio = GpioController.GetDefault();

                if (gpio == null)
                {
                    TextMain.Text = "No GPIO";
                    return;
                }

                led1 = gpio.OpenPin(12);
                // Left motor
                motor1A = gpio.OpenPin(23);
                motor1B = gpio.OpenPin(24);
                motor1E = gpio.OpenPin(25);
                // Right motor
                motor2A = gpio.OpenPin(5);
                motor2B = gpio.OpenPin(6);
                motor2E = gpio.OpenPin(13);

                led1.SetDriveMode(GpioPinDriveMode.Output);
                motor1A.SetDriveMode(GpioPinDriveMode.Output);
                motor1B.SetDriveMode(GpioPinDriveMode.Output);
                motor1E.SetDriveMode(GpioPinDriveMode.Output);
                motor2A.SetDriveMode(GpioPinDriveMode.Output);
                motor2B.SetDriveMode(GpioPinDriveMode.Output);
                motor2E.SetDriveMode(GpioPinDriveMode.Output);
                TextMain.Text = "GPIO set";

                ledTimer.Tick += LedTimer_Tick;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            timer.Start();
            try
            {
                isBlinking = true;

                if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                {
                    // Do some action to signal that everything is initialized correctly
                    ledTimer.Start();

                    await Task.Delay(1000);
                    TextMain.Text = "Motor on";

                    // Turn right
                    motor1A.Write(GpioPinValue.High);
                    motor1B.Write(GpioPinValue.Low);
                    motor1E.Write(GpioPinValue.High);
                    motor2A.Write(GpioPinValue.Low);
                    motor2B.Write(GpioPinValue.High);
                    motor2E.Write(GpioPinValue.High);
                    UpdateTexts();
                    await Task.Delay(200);

                    // Turn left
                    motor1A.Write(GpioPinValue.Low);
                    motor1B.Write(GpioPinValue.High);
                    motor1E.Write(GpioPinValue.High);
                    motor2A.Write(GpioPinValue.High);
                    motor2B.Write(GpioPinValue.Low);
                    motor2E.Write(GpioPinValue.High);
                    UpdateTexts();
                    await Task.Delay(200);

                    // Stop
                    motor1E.Write(GpioPinValue.Low);
                    motor2E.Write(GpioPinValue.Low);
                    UpdateTexts();
                    TextMain.Text = "Standing by";
                }

                isBlinking = false;
            }
            catch (Exception ex)
            {
                TextMain.Text = "Error: " + ex.Message + ". " + ex.ToString();
            }
        }

        private void LedTimer_Tick(object sender, object e)
        {
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && led1 != null)
            {
                var ledState = led1.Read();

                if (!isBlinking && ledState == GpioPinValue.Low)
                {
                    // Idle state: HIGH
                    led1.Write(GpioPinValue.High);
                }
                else if (isBlinking)
                {
                    // Blinking
                    if (ledState == GpioPinValue.Low) led1.Write(GpioPinValue.High);
                    else led1.Write(GpioPinValue.Low);
                }
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            RectangleCenter.Opacity = 0.5;
            RectangleUp.Opacity = 0.5;
            RectangleDown.Opacity = 0.5;
            RectangleLeft.Opacity = 0.5;
            RectangleRight.Opacity = 0.5;

            if (Gamepad.Gamepads.Count > 0)
            {
                TextMain.Text = "Gamepad detected";

                var gamepad = Gamepad.Gamepads[0];
                var reading = gamepad.GetCurrentReading();

                var isMoving = false;
                if (reading.Buttons.HasFlag(GamepadButtons.DPadUp))
                {
                    if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                    {
                        motor1A.Write(GpioPinValue.High);
                        motor1B.Write(GpioPinValue.Low);
                        motor1E.Write(GpioPinValue.High);
                        motor2A.Write(GpioPinValue.High);
                        motor2B.Write(GpioPinValue.Low);
                        motor2E.Write(GpioPinValue.High);
                    }
                    RectangleUp.Opacity = 1;
                    isMoving = true;
                }
                else if (reading.Buttons.HasFlag(GamepadButtons.DPadDown))
                {
                    if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                    {
                        motor1A.Write(GpioPinValue.Low);
                        motor1B.Write(GpioPinValue.High);
                        motor1E.Write(GpioPinValue.High);
                        motor2A.Write(GpioPinValue.Low);
                        motor2B.Write(GpioPinValue.High);
                        motor2E.Write(GpioPinValue.High);
                    }
                    RectangleDown.Opacity = 1;
                    isMoving = true;
                }
                if (reading.Buttons.HasFlag(GamepadButtons.DPadLeft))
                {
                    if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                    {
                        motor1A.Write(GpioPinValue.Low);
                        motor1B.Write(GpioPinValue.High);
                        motor1E.Write(GpioPinValue.High);
                        motor2A.Write(GpioPinValue.High);
                        motor2B.Write(GpioPinValue.Low);
                        motor2E.Write(GpioPinValue.High);
                    }
                    RectangleLeft.Opacity = 1;
                    isMoving = true;
                }
                else if (reading.Buttons.HasFlag(GamepadButtons.DPadRight))
                {
                    if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                    {
                        motor1A.Write(GpioPinValue.High);
                        motor1B.Write(GpioPinValue.Low);
                        motor1E.Write(GpioPinValue.High);
                        motor2A.Write(GpioPinValue.Low);
                        motor2B.Write(GpioPinValue.High);
                        motor2E.Write(GpioPinValue.High);
                    }
                    RectangleRight.Opacity = 1;
                    isMoving = true;
                }

                if (!isMoving)
                {
                    isBlinking = false;
                    if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
                    {
                        motor1E.Write(GpioPinValue.Low);
                        motor2E.Write(GpioPinValue.Low);
                    }
                    RectangleCenter.Opacity = 1;
                }
                else
                {
                    isBlinking = true;
                }
            }

            UpdateTexts();
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            Debug.WriteLine("Gamepad removed");
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            Debug.WriteLine("Gamepad added");
        }

        private void ButtonA_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin"))
            {
                if (motor1A.Read() == GpioPinValue.High)
                    motor1A.Write(GpioPinValue.Low);
                else
                    motor1A.Write(GpioPinValue.High);
                UpdateTexts();
            }
        }

        private void ButtonB_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin"))
            {
                if (motor1B.Read() == GpioPinValue.High)
                    motor1B.Write(GpioPinValue.Low);
                else
                    motor1B.Write(GpioPinValue.High);
                UpdateTexts();
            }
        }

        private void ButtonE_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin"))
            {
                if (motor1E.Read() == GpioPinValue.High)
                    motor1E.Write(GpioPinValue.Low);
                else
                    motor1E.Write(GpioPinValue.High);
                UpdateTexts();
            }
        }

        private void UpdateTexts()
        {
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioPin") && gpio != null)
            {
                TextA.Text = motor1A.Read() == GpioPinValue.High ? "H" : "L";
                TextB.Text = motor1B.Read() == GpioPinValue.High ? "H" : "L";
                TextE.Text = motor1E.Read() == GpioPinValue.High ? "H" : "L";
            }
        }
    }
}