//PLCS project
//Authors: Antonietta Simone Domenico, Bulgarella Fabio, Loro Matteo

using System;
using System.Threading;
using System.Reflection;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using GHI.Networking;
using GHI.Usb.Host;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using System.Collections;

namespace PLCS_Project
{
    public partial class Program
    {
        private Mouse mouse;
        private BME280Device bme280;
        private Network network;
        
        private bool sdCardMounted = false;
        private bool bme280Working = false;

        double tempC, pressureMb, relativeHumidity;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            // Setup leds            
            this.ledStrip.TurnAllLedsOff();

            // Setup display
            Display.setDisplay(displayTE35);

            // Setup USBHost patched module and get mouse if already connected
            InitMouse();

            // Setup bosch bme280 sensor
            Thread setupSensor = new Thread(InitSensor);
            setupSensor.Start();

            // Setup network
            network = new Network(ethernetJ11D, wifiRS21, button2);

            // Setup sdcard
            this.sdCard.Mounted += sdCard_Mounted;
            this.sdCard.Unmounted += sdCard_Unmounted;
            
            // Mouse timers
            GT.Timer mouseTimer = new GT.Timer(500);
            mouseTimer.Tick += mouseTimer_Tick;
            mouseTimer.Start();

            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(10000);
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();
        }

        private void InitMouse()
        {
            if (Controller.GetConnectedDevices().Length > 0)
            {
                GHI.Usb.Host.Mouse mouse = (GHI.Usb.Host.Mouse)Controller.GetConnectedDevices()[0];
                usbHost_MouseConnected(usbHost, mouse);
            }
            usbHost.MouseConnected += usbHost_MouseConnected;

            // Setup mouse position reset button
            button.Mode = Button.LedMode.OnWhilePressed;
            button.ButtonPressed += button_ButtonPressed;
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

        void usbHost_MouseConnected(USBHost sender, GHI.Usb.Host.Mouse mouse)
        {
            // Get Mouse information
            uint id = mouse.Id;
            byte interfaceIndex = mouse.InterfaceIndex;
            ushort vendorId = mouse.VendorId;
            ushort productId = mouse.ProductId;
            byte portNumber = mouse.PortNumber;
            BaseDevice.DeviceType type = mouse.Type;

            // Remove ghi mouse object
            Mouse.CleanGhiMouse();

            // Create new PLCS Mouse Object
            this.mouse = new Mouse(id, interfaceIndex, vendorId, productId, portNumber, type);
            this.mouse.Disconnected += mouse_Disconnected;

            Debug.Print("Mouse Connected");
            ledStrip.TurnLedOn(1);
        }

        void mouse_Disconnected(BaseDevice sender, EventArgs e)
        {
            Debug.Print("Mouse Disconnected");
            Debug.Print("Connected devices -> " + Controller.GetConnectedDevices().Length);
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
                Display.UpdateMouseData(mouse.ExceptionCounter, mouse.GetStringPosition(), mouse.GetStringMillimetersPosition());
                this.mouse.HasMoved = false;
            }
        }

        void sensorTimer_Tick(GT.Timer timer)
        {
            try
            {
                bme280.Measure(out tempC, out pressureMb, out relativeHumidity);
                Display.UpdateSensorData(tempC, pressureMb, relativeHumidity);
                
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

                if (this.sdCard.StorageDevice.ListDirectories(this.sdCard.StorageDevice.RootDirectory).Length == 1)
                {
                    this.sdCard.StorageDevice.CreateDirectory("00_ToSend");
                    this.sdCard.StorageDevice.CreateDirectory("01_Sent");
                }

                byte[] data = Json.CreateJsonMeasurements(mouse.GetMillimetersX(), mouse.GetMillimetersY(), tempC, pressureMb, relativeHumidity);
                string filePath = "00_ToSend\\" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss"+"+00:00") + ".json";                
                this.displayTE35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 140, 320, 18);
                this.displayTE35.SimpleGraphics.DisplayText(filePath, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 140);
                this.displayTE35.SimpleGraphics.Redraw();
                this.sdCard.StorageDevice.WriteFile(filePath, data);
            }
        }
    }
}
