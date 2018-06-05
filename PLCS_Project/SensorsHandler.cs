using System;
using System.Threading;
using Microsoft.SPOT;
using GHI.Usb.Host;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace PLCS_Project
{
    class SensorsHandler
    {
        private Mouse mouse;
        private BME280Device bme280;
        private USBHost usbHost;
        private Button button;

        public static int oldX, oldY;
        public static double tempC, pressureMb, relativeHumidity;
        public static double oldTempC, oldPressureMb, oldRelativeHumidity;

        private bool bme280Working = false;

        public SensorsHandler(USBHost usbHostObject, Button buttonObject)
        {
            usbHost = usbHostObject;
            button = buttonObject;

            // Setup USBHost patched module and get mouse if already connected
            InitMouse();

            // Setup bosch bme280 sensor
            Thread setupSensor = new Thread(InitSensor);
            setupSensor.Start();

            // Mouse timers
            GT.Timer mouseTimer = new GT.Timer(500);
            mouseTimer.Tick += mouseTimer_Tick;
            mouseTimer.Start();
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

            while (isNotActivated)
            {
                try
                {
                    bme280 = new BME280Device(0x76)
                    {
                        AltitudeInMeters = 239
                    };

                    Utils.TurnLedOn(0);
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
            Utils.TurnLedOn(1);
        }

        void mouse_Disconnected(BaseDevice sender, EventArgs e)
        {
            Debug.Print("Mouse Disconnected");
            Debug.Print("Connected devices -> " + Controller.GetConnectedDevices().Length);
            Utils.TurnLedOff(1);
        }

        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (mouse != null)
            {
                mouse.ResetPosition();
                mouse.HasMoved = true;
            }
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
                    Utils.TurnLedOn(0);
                    Debug.Print("Sensor connected");
                }
            }
            catch (BME280Exception)
            {
                bme280Working = false;
                Utils.TurnLedOff(0);
                Debug.Print("Unable to read sensor data");
                Debug.Print("Sensor disconnected");
            }
        }
    }
}
