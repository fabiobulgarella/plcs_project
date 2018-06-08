using System;
using System.Threading;
using Microsoft.SPOT;
using GHI.Usb.Host;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using System.Text;

namespace PLCS_Project
{
    class SensorsHandler
    {
        public struct Measurements
        {
            public String x;
            public String y;
            public double temperature;
            public double pressure;
            public double humidity;
            public bool[] changed;
        }

        private Mouse mouse;
        private BME280Device bme280;
        private USBHost usbHost;
        private Button button;
        private bool mouseFirstConnect;

        public static int oldX, oldY;
        public static double tempC, pressureMb, relativeHumidity;
        public static double oldTempC, oldPressureMb, oldRelativeHumidity;

        private bool bme280Working = false;

        public SensorsHandler(USBHost usbHostObject, Button buttonObject)
        {
            // Set objects
            usbHost = usbHostObject;
            button = buttonObject;

            // Initialize old variables
            oldX = oldY = 0;
            oldTempC = oldPressureMb = oldPressureMb = 0;

            // Setup USBHost patched module and get mouse if already connected
            InitMouse();

            // Setup bosch bme280 sensor
            Thread setupSensor = new Thread(InitSensor);
            setupSensor.Start();

            // Mouse timers
            GT.Timer mouseTimer = new GT.Timer(500);
            mouseTimer.Tick += mouseTimer_Tick;
            mouseTimer.Start();

            // Mouse persistence timer
            GT.Timer persistenceTimer = new GT.Timer(5000);
            persistenceTimer.Tick += persistenceTimer_Tick;
            persistenceTimer.Start();
        }

        public Measurements GetMeasurements(bool[] toForce)
        {
            // Update Bosch data
            sensorTimer_Tick(null);

            Measurements measurements;
            measurements.changed = new bool[5];

            if (mouse != null)
            {
                // Get X position
                int newX = Mouse.X;
                if (newX != oldX || toForce[0])
                {
                    oldX = newX;
                    measurements.x = Mouse.GetMillimetersX(newX);
                    measurements.changed[0] = true;
                }
                else
                    measurements.x = null;

                // Get Y position
                int newY = Mouse.Y;
                if (newY != oldY || toForce[1])
                {
                    oldY = newY;
                    measurements.y = Mouse.GetMillimetersY(newY);
                    measurements.changed[1] = true;
                }
                else
                    measurements.y = null;
            }
            else
            {
                measurements.x = null;
                measurements.y = null;
            }

            if (bme280Working)
            {
                // Get Temperature
                if (tempC != oldTempC || toForce[2])
                {
                    oldTempC = tempC;
                    measurements.temperature = tempC;
                    measurements.changed[2] = true;
                }
                else
                    measurements.temperature = -100;

                // Get Pressure
                if (pressureMb != oldPressureMb || toForce[3])
                {
                    oldPressureMb = pressureMb;
                    measurements.pressure = pressureMb;
                    measurements.changed[3] = true;
                }
                else
                    measurements.pressure = -100;

                // Get Relative Humidity
                if (relativeHumidity != oldRelativeHumidity || toForce[4])
                {
                    oldRelativeHumidity = relativeHumidity;
                    measurements.humidity = relativeHumidity;
                    measurements.changed[4] = true;
                }
                else
                    measurements.humidity = -100;
            }
            else
            {
                measurements.temperature = -100;
                measurements.humidity = -100;
                measurements.pressure = -100;
            }

            return measurements;
        }

        /*
         * MOUSE SECTION
         */
        private void InitMouse()
        {
            mouseFirstConnect = true;

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

        private void LoadMouseData()
        {
            byte[] data = null;

            while (true)
            {
                if (SDMemoryCard.IsCardInserted && SDMemoryCard.IsMounted)
                {
                    data = SDMemoryCard.readFile("MouseData");
                    break;
                }

                Thread.Sleep(2000);
            }

            if (data != null)
            {
                String mouseData = new String(Encoding.UTF8.GetChars(data));
                String[] cordinates = mouseData.Split(new char[] { ' ' }, 2);
                Debug.Print("Lette X -> " + cordinates[0] + "  Y -> " + cordinates[1]);

                if (mouse != null)
                {
                    mouse.SetPosition(Int32.Parse(cordinates[0]), Int32.Parse(cordinates[1]));
                }
            }

            mouseFirstConnect = false;
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

            if (mouseFirstConnect)
            {
                LoadMouseData();
            }

            Debug.Print("Mouse Connected");
            Utils.TurnLedOn(1);
            Display.UpdateMouseState(true);
        }

        void mouse_Disconnected(BaseDevice sender, EventArgs e)
        {
            Debug.Print("Mouse Disconnected");
            Debug.Print("Connected devices -> " + Controller.GetConnectedDevices().Length);
            Utils.TurnLedOff(1);
            Display.UpdateMouseState(false);
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

        void persistenceTimer_Tick(GT.Timer timer)
        {
            if (mouse != null)
            {
                String mouseData = Mouse.X + " " + Mouse.Y;
                SDMemoryCard.writeFile("MouseData", Encoding.UTF8.GetBytes(mouseData));
            }
        }

        /*
         * BOSCH SECTION
         */
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
                    Display.UpdateTPHState(true);
                    Debug.Print("Sensor connected");                    
                }
                
            }
            catch (BME280Exception)
            {
                bme280Working = false;
                Utils.TurnLedOff(0);
                Display.UpdateTPHState(false);
                Debug.Print("Unable to read sensor data");
                Debug.Print("Sensor disconnected");
            }
        }
    }
}
