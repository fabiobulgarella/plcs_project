//PLCS project
//Authors: Antonietta Simone Domenico, Bulgarella Fabio, Loro Matteo

using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using GHI.Usb.Host;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace PLCS_Project
{
    public partial class Program
    {
        private Mouse mouse;
        private BME280Device bme280;
        private Display display;
        
        private bool sdCardMounted = false;
        private bool bme280Working = false;

        double tempC, pressureMb, relativeHumidity;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            // Setup display
            display = new Display(displayTE35);

            // Setup leds            
            this.ledStrip.TurnAllLedsOff();

            // Setup mouse position reset button
            button.Mode = Button.LedMode.OnWhilePressed;
            button.ButtonPressed += button_ButtonPressed;

            // Setup display
            this.displayTE35.SimpleGraphics.AutoRedraw = false;

            // Setup newtork
            InitNetwork();

            // Setup sdcard
            this.sdCard.Mounted += sdCard_Mounted;
            this.sdCard.Unmounted += sdCard_Unmounted;

            // Setup USBHost patched module and get mouse if already connected
            InitMouse();

            // Setup bosch bme280 sensor
            Thread setupSensor = new Thread(InitSensor);
            setupSensor.Start();
            
            // Mouse timers
            GT.Timer mouseTimer = new GT.Timer(500);
            mouseTimer.Tick += mouseTimer_Tick;
            mouseTimer.Start();

            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(10000);   //120000
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();
        }

        private void InitMouse()
        {
            if (Controller.GetConnectedDevices().Length > 0)
            {
                GHI.Usb.Host.Mouse baseMouse = (GHI.Usb.Host.Mouse)Controller.GetConnectedDevices()[0];
                usbHost_MouseConnected(usbHost, baseMouse);
            }
            usbHost.MouseConnected += usbHost_MouseConnected;
        }

        private void InitSensor()
        {
            bool isNotActivated = true;

            while(isNotActivated)
            {
                try
                {
                    bme280 = new BME280Device(0x76)
                    {
                        AltitudeInMeters = 239
                    };

                    ledStrip.TurnLedOn(0);
                    isNotActivated = false;
                }
                catch (BME280Exception)
                {
                    Debug.Print("Error during BME280 initiliazation");
                }

                Thread.Sleep(5000);
            }

            // Sensor Timer
            GT.Timer sensorTimer = new GT.Timer(10000);
            sensorTimer.Tick += sensorTimer_Tick;
            sensorTimer.Start();
            
            // Show first bme280 reading
            sensorTimer_Tick(null);
        }

        private void InitNetwork()
        {
            this.ethernetJ11D.NetworkSettings.EnableDhcp();
            this.ethernetJ11D.NetworkSettings.EnableDynamicDns();
            this.ethernetJ11D.UseDHCP();
            this.ethernetJ11D.UseThisNetworkInterface();
            this.ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;
            this.ethernetJ11D.NetworkDown += ethernetJ11D_NetworkDown;
        }

        void usbHost_MouseConnected(USBHost sender, GHI.Usb.Host.Mouse mouse)
        {
            Debug.Print("Mouse Connected");

            // Deactivate GHI.Usb.Host.Mouse internal worker
            mouse.WorkerInterval = -1;

            // Create new PLCS Mouse Object
            this.mouse = new Mouse(mouse.Id, mouse.InterfaceIndex, mouse.VendorId, mouse.ProductId, mouse.PortNumber, mouse.Type);
            this.mouse.Disconnected += mouse_Disconnected;
            ledStrip.TurnLedOn(1);
        }

        void mouse_Disconnected(BaseDevice sender, EventArgs e)
        {
            Debug.Print("Mouse Disconnected");
            ledStrip.TurnLedOff(1);
        }

        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (mouse != null)
            {
                mouse.ResetPosition();
                mouse.HasMoved = true;
            }
        }

        void ethernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network is Up");
            Utils.SetupTimeService();
        }

        void ethernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network is Down");
        }

        void sdCard_Mounted(SDCard sender, GT.StorageDevice device)
        {
            Debug.Print("SDCard has been Mounted");
            Utils.PrintVolumeInfo(this.sdCard.StorageDevice.Volume);
            sdCardMounted = true;
            this.ledStrip.TurnLedOn(2);
        }

        void sdCard_Unmounted(SDCard sender, EventArgs e)
        {
            Debug.Print("SDCard has been Unmounted");
            sdCardMounted = false;
            this.ledStrip.TurnLedOff(2);
        }

        void mouseTimer_Tick(GT.Timer timer)
        {
            if (mouse != null && mouse.HasMoved)
            {
                display.UpdateMouseData(mouse.ExceptionCounter, mouse.GetStringPosition(), mouse.GetStringMillimetersPosition());
                this.mouse.HasMoved = false;
            }
        }

        void sensorTimer_Tick(GT.Timer timer)
        {
            try
            {
                bme280.Measure(out tempC, out pressureMb, out relativeHumidity);
                display.UpdateSensorData(tempC, pressureMb, relativeHumidity);
                
                if (!bme280Working)
                {
                    bme280Working = true;
                    ledStrip.TurnLedOn(0);
                    Debug.Print("Sensor connected");
                }
            }
            catch (BME280Exception)
            {
                bme280Working = false;
                ledStrip.TurnLedOff(0);
                Debug.Print("Unable to read sensor data");
                Debug.Print("Sensor disconnected");
            }
        }

        void writingTimer_Tick(GT.Timer timer)
        {
            if (this.sdCard.IsCardMounted)
            {
                sdCardMounted = true;
                this.ledStrip.TurnLedOn(2);
                string test1 = "Number of element into the SD:" + this.sdCard.StorageDevice.ListDirectories(this.sdCard.StorageDevice.RootDirectory).Length.ToString();
                string test2 = "Root string:" + this.sdCard.StorageDevice.RootDirectory;
                this.displayTE35.SimpleGraphics.DisplayText(test1, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 140);
                this.displayTE35.SimpleGraphics.DisplayText(test2, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 160);

                if (this.sdCard.StorageDevice.ListDirectories(this.sdCard.StorageDevice.RootDirectory).Length == 1)
                {
                    this.sdCard.StorageDevice.CreateDirectory("00_ToSend");
                    this.sdCard.StorageDevice.CreateDirectory("01_Sent");
                }

                byte[] data = Json.CreateJsonMeasurements(mouse.GetMillimetersX(), mouse.GetMillimetersY(), tempC, pressureMb, relativeHumidity);
                this.sdCard.StorageDevice.WriteFile("00_ToSend\\test1.json", data);
            }
        }
    }
}
