﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.HumanInterfaceDevice;
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

namespace RoboPi2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GpioPin motor1A;
        private GpioPin motor1B;
        private GpioPin motor1E;
        private GpioPin motor2A;
        private GpioPin motor2B;
        private GpioPin motor2E;
        private DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };

        public MainPage()
        {
            this.InitializeComponent();

            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                TextMain.Text = "No GPIO";
                return;
            }


            motor1A = gpio.OpenPin(23);
            motor1B = gpio.OpenPin(24);
            motor1E = gpio.OpenPin(25);
            motor2A = gpio.OpenPin(0);
            motor2B = gpio.OpenPin(5);
            motor2E = gpio.OpenPin(6);

            motor1A.SetDriveMode(GpioPinDriveMode.Output);
            motor1B.SetDriveMode(GpioPinDriveMode.Output);
            motor1E.SetDriveMode(GpioPinDriveMode.Output);
            motor2A.SetDriveMode(GpioPinDriveMode.Output);
            motor2B.SetDriveMode(GpioPinDriveMode.Output);
            motor2E.SetDriveMode(GpioPinDriveMode.Output);
            TextMain.Text = "GPIO set";

            timer.Tick += Timer_Tick;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                timer.Start();

                await Task.Delay(1000);

                TextMain.Text = "Motor on";
                motor1A.Write(GpioPinValue.High);
                motor1B.Write(GpioPinValue.Low);
                motor1E.Write(GpioPinValue.High);
                motor2A.Write(GpioPinValue.Low);
                motor2B.Write(GpioPinValue.High);
                motor2E.Write(GpioPinValue.High);
                UpdateTexts();

                await Task.Delay(200);

                motor1A.Write(GpioPinValue.Low);
                motor1B.Write(GpioPinValue.High);
                motor1E.Write(GpioPinValue.High);
                motor2A.Write(GpioPinValue.High);
                motor2B.Write(GpioPinValue.Low);
                motor2E.Write(GpioPinValue.High);
                UpdateTexts();

                await Task.Delay(200);

                motor1E.Write(GpioPinValue.Low);
                motor2E.Write(GpioPinValue.Low);
                UpdateTexts();
                TextMain.Text = "Standing by";
            }
            catch (Exception ex)
            {
                TextMain.Text = "Error: " + ex.Message + ". " + ex.ToString();
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            timer.Stop();
            if (!await XboxJoystickInit())
            {
                timer.Start();
            }
            else
            {
                TextXbox.Visibility = Visibility.Collapsed;
            }

        }

        private void ButtonA_Click(object sender, RoutedEventArgs e)
        {
            if (motor1A.Read() == GpioPinValue.High)
                motor1A.Write(GpioPinValue.Low);
            else
                motor1A.Write(GpioPinValue.High);
            UpdateTexts();
        }

        private void ButtonB_Click(object sender, RoutedEventArgs e)
        {
            if (motor1B.Read() == GpioPinValue.High)
                motor1B.Write(GpioPinValue.Low);
            else
                motor1B.Write(GpioPinValue.High);
            UpdateTexts();
        }

        private void ButtonE_Click(object sender, RoutedEventArgs e)
        {
            if (motor1E.Read() == GpioPinValue.High)
                motor1E.Write(GpioPinValue.Low);
            else
                motor1E.Write(GpioPinValue.High);
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            TextA.Text = motor1A.Read() == GpioPinValue.High ? "H" : "L";
            TextB.Text = motor1B.Read() == GpioPinValue.High ? "H" : "L";
            TextE.Text = motor1E.Read() == GpioPinValue.High ? "H" : "L";
        }

        private static XboxHidController controller;
        public async Task<bool> XboxJoystickInit()
        {
            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            var deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);

            if (deviceInformationCollection.Count == 0)
            {
                return false;
            }

            Debug.WriteLine($"Found {deviceInformationCollection.Count} devices");
            foreach (DeviceInformation d in deviceInformationCollection)
            {
                Debug.WriteLine("Device ID: " + d.Id);

                HidDevice hidDevice = await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read);

                if (hidDevice == null)
                {
                    try
                    {
                        var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;

                        if (!deviceAccessStatus.Equals(DeviceAccessStatus.Allowed))
                        {
                            Debug.WriteLine("DeviceAccess: " + deviceAccessStatus.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Xbox init - " + e.Message);
                    }

                    Debug.WriteLine("Failed to connect to the controller!");
                }
                else
                {
                    var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;
                    Debug.WriteLine("DeviceAccess: " + deviceAccessStatus.ToString());

                    controller = new XboxHidController(hidDevice);
                    controller.DirectionChanged += Controller_DirectionChanged;
                    return true;
                }
            }
            return false;
        }

        private async void Controller_DirectionChanged(ControllerVector sender)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                RectangleCenter.Opacity = 0.5;
                RectangleUp.Opacity = 0.5;
                RectangleDown.Opacity = 0.5;
                RectangleLeft.Opacity = 0.5;
                RectangleRight.Opacity = 0.5;
                if (sender.Magnitude > 5000)
                {
                    switch (sender.Direction)
                    {
                        case ControllerDirection.UpLeft:
                        case ControllerDirection.UpRight:
                        case ControllerDirection.Up:
                            motor1A.Write(GpioPinValue.High);
                            motor1B.Write(GpioPinValue.Low);
                            motor1E.Write(GpioPinValue.High);
                            motor2A.Write(GpioPinValue.High);
                            motor2B.Write(GpioPinValue.Low);
                            motor2E.Write(GpioPinValue.High);
                            RectangleUp.Opacity = 1;
                            break;
                        case ControllerDirection.DownLeft:
                        case ControllerDirection.DownRight:
                        case ControllerDirection.Down:
                            motor1A.Write(GpioPinValue.Low);
                            motor1B.Write(GpioPinValue.High);
                            motor1E.Write(GpioPinValue.High);
                            motor2A.Write(GpioPinValue.Low);
                            motor2B.Write(GpioPinValue.High);
                            motor2E.Write(GpioPinValue.High);
                            RectangleDown.Opacity = 1;
                            break;
                        case ControllerDirection.Left:
                            motor1A.Write(GpioPinValue.Low);
                            motor1B.Write(GpioPinValue.High);
                            motor1E.Write(GpioPinValue.High);
                            motor2A.Write(GpioPinValue.High);
                            motor2B.Write(GpioPinValue.Low);
                            motor2E.Write(GpioPinValue.High);
                            RectangleLeft.Opacity = 1;
                            break;
                        case ControllerDirection.Right:
                            motor1A.Write(GpioPinValue.High);
                            motor1B.Write(GpioPinValue.Low);
                            motor1E.Write(GpioPinValue.High);
                            motor2A.Write(GpioPinValue.Low);
                            motor2B.Write(GpioPinValue.High);
                            motor2E.Write(GpioPinValue.High);
                            RectangleRight.Opacity = 1;
                            break;
                        case ControllerDirection.None:
                            motor1E.Write(GpioPinValue.Low);
                            motor2E.Write(GpioPinValue.Low);
                            RectangleCenter.Opacity = 1;
                            break;
                    }
                }
                else
                {
                    motor1E.Write(GpioPinValue.Low);
                    motor2E.Write(GpioPinValue.Low);
                    RectangleCenter.Opacity = 1;
                }
                UpdateTexts();
            });
            
        }
    }
}