//PLCS project
//Authors: Antonietta Simone Domenico, Bulgarella Fabio, Loro Matteo

using System;
using System.Collections;
using System.Net;
using System.Threading;
using System.Reflection;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using Microsoft.SPOT.Time;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Usb.Host;

namespace PLCS_Project
{
    public partial class Program
    {
        private BME280Device bme280;
        private Mouse mouse;
        private bool[] isLightedLed;

        double tempC, pressureMb, relativeHumidity;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            // Setup leds
            this.isLightedLed = new bool[7];
            this.ledStrip.TurnAllLedsOff();

            // Setup mouse position reset button button
            button.Mode = Button.LedMode.OnWhilePressed;
            button.ButtonPressed += button_ButtonPressed;

            // Setup display
            this.displayTE35.SimpleGraphics.AutoRedraw = false;

            // Setup newtork
            this.ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;
            this.ethernetJ11D.NetworkDown += ethernetJ11D_NetworkDown;

            // Setup sdcard
            this.sdCard.Mounted += sdCard_Mounted;
            this.sdCard.Unmounted += sdCard_Unmounted;

            // Setup bosch bme280 sensor
            bme280 = new BME280Device(0x76)
            {
                AltitudeInMeters = 239
            };

            // Setup mouse
            this.RemoveMouseDefaultDelegate();
            BaseDevice dev = Controller.GetConnectedDevices()[0];
            this.usbHost.ConnectedMouse.Dispose();
            this.mouse = new Mouse(dev.Id, dev.InterfaceIndex, dev.VendorId, dev.ProductId, dev.PortNumber, dev.Type);

            // Setup timers
            GT.Timer mouseTimer = new GT.Timer(500);
            mouseTimer.Tick += mouseTimer_Tick;
            mouseTimer.Start();

            // Sensor Timer
            GT.Timer sensorTimer = new GT.Timer(30000);
            sensorTimer.Tick += sensorTimer_Tick;
            sensorTimer.Start();

            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(120000);
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();

            // Show first bme280 reading
            sensorTimer_Tick(null);
        }

        private void RemoveMouseDefaultDelegate()
        {
            FieldInfo eventMouseConnected = typeof(Controller).GetField("MouseConnected", BindingFlags.Static | BindingFlags.NonPublic);
            eventMouseConnected.SetValue(null, null);
        }

        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (mouse.Connected)
            {
                mouse.ResetPosition();
                mouse.HasMoved = true;
            }
        }

        void ethernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {/*
            Debug.Print("Network is Up");

            TimeServiceSettings settings = new TimeServiceSettings();
            settings.RefreshTime = 10; // every 10 seconds
            settings.ForceSyncAtWakeUp = true;

            TimeService.SystemTimeChanged += TimeService_SystemTimeChanged;
            TimeService.TimeSyncFailed += TimeService_TimeSyncFailed;
            TimeService.SetTimeZoneOffset(60);

            IPHostEntry hostEntry = Dns.GetHostEntry("time.nist.gov");
            IPAddress[] address = hostEntry.AddressList;

            if (address != null)
                settings.PrimaryServer = address[0].GetAddressBytes();

            hostEntry = Dns.GetHostEntry("time.windows.com");
            address = hostEntry.AddressList;

            if (address != null)
                settings.AlternateServer = address[0].GetAddressBytes();

            TimeService.Settings = settings;
            TimeService.Start();*/
        }

        void ethernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network is Down");
        }

        void TimeService_TimeSyncFailed(object sender, TimeSyncFailedEventArgs e)
        {
            Debug.Print("DateTime Sync Failed");
        }

        void TimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
            Debug.Print("DateTime = " + DateTime.Now.ToString());
        }

        void sdCard_Mounted(SDCard sender, GT.StorageDevice device)
        {
            Debug.Print("SDCard has been Mounted");
            Utils.PrintVolumeInfo(this.sdCard.StorageDevice.Volume);
        }

        void sdCard_Unmounted(SDCard sender, EventArgs e)
        {
            Debug.Print("SDCard has been Unmounted");
        }

        void mouseTimer_Tick(GT.Timer timer)
        {
            if (mouse.HasMoved)
            {
                string toPrint = "Exceptions raised: " + mouse.ExceptionCounter;
                this.displayTE35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 0, 240, 71);
                this.displayTE35.SimpleGraphics.DisplayText("Cursor Position", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 0);
                this.displayTE35.SimpleGraphics.DisplayText(mouse.GetPosition(), Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 18);
                this.displayTE35.SimpleGraphics.DisplayText(mouse.GetMillimeterPosition(), Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 36);
                this.displayTE35.SimpleGraphics.DisplayText(toPrint, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 54);
                this.displayTE35.SimpleGraphics.Redraw();
                this.mouse.HasMoved = false;
            }
        }

        void sensorTimer_Tick(GT.Timer timer)
        {
            bme280.Measure(out tempC, out pressureMb, out relativeHumidity);
            string temp = "Temperature: " + tempC.ToString("F2") + " C°";
            string pressure = "Pressure: " + pressureMb.ToString("F2") + " mBar";
            string humidity = "Relative Humidity: " + relativeHumidity.ToString("F2") + " %";
            this.displayTE35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 72, 240, 54);
            this.displayTE35.SimpleGraphics.DisplayText(temp, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 72);
            this.displayTE35.SimpleGraphics.DisplayText(pressure, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 90);
            this.displayTE35.SimpleGraphics.DisplayText(humidity, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 107);
            this.displayTE35.SimpleGraphics.Redraw();
        }

        void writingTimer_Tick(GT.Timer timer)
        {

        }
    }
}
