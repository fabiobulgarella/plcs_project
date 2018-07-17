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

        private int oldX, oldY;
        private double tempC, pressureMb, relativeHumidity;
        private double oldTempC, oldPressureMb, oldRelativeHumidity;
        private bool wasMouseFailed;
        private bool wasSensorFailed;
        private bool isMouseConnected;

        private bool bme280Working;
        private int lastWrittenX;
        private int lastWrittenY;

        public SensorsHandler(USBHost usbHostObject, Button buttonObject)
        {
            // Set objects
            usbHost = usbHostObject;
            button = buttonObject;

            // Initialize variables
            oldX = oldY = 0;
            oldTempC = oldPressureMb = oldPressureMb = 0;
            lastWrittenX = lastWrittenY = 0;
            wasMouseFailed = wasSensorFailed = true;
            bme280Working = false;
            isMouseConnected = false;

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
            GT.Timer persistenceTimer = new GT.Timer(6000);
            persistenceTimer.Tick += persistenceTimer_Tick;
            persistenceTimer.Start();
        }

        public Measurements GetMeasurements(bool[] toForce)
        {
            // Update Bosch data
            sensorTimer_Tick(null);

            Measurements measurements;
            measurements.changed = new bool[5];

            if (mouse != null && isMouseConnected)
            {
                // Get X position
                int newX = Mouse.X;
                if (newX != oldX || toForce[0] || wasMouseFailed)
                {
                    oldX = newX;
                    measurements.x = Mouse.GetMillimetersX(newX);
                    measurements.changed[0] = true;
                }
                else
                    measurements.x = null;

                // Get Y position
                int newY = Mouse.Y;
                if (newY != oldY || toForce[1] || wasMouseFailed)
                {
                    oldY = newY;
                    measurements.y = Mouse.GetMillimetersY(newY);
                    measurements.changed[1] = true;
                }
                else
                    measurements.y = null;

                wasMouseFailed = false;
            }
            else
            {
                if (toForce[0] || !wasMouseFailed)
                {
                    measurements.x = "FAIL";
                    measurements.y = "FAIL";
                    measurements.changed[0] = true;
                    measurements.changed[1] = true;
                }
                else
                {
                    measurements.x = null;
                    measurements.y = null;
                }
                
                wasMouseFailed = true;
            }

            if (bme280Working)
            {
                // Get Temperature
                if (tempC.ToString("F1") != oldTempC.ToString("F1") || toForce[2] || wasSensorFailed)
                {
                    oldTempC = tempC;
                    measurements.temperature = tempC;
                    measurements.changed[2] = true;
                }
                else
                    measurements.temperature = -100;

                // Get Pressure
                if (pressureMb.ToString("F1") != oldPressureMb.ToString("F1") || toForce[3] || wasSensorFailed)
                {
                    oldPressureMb = pressureMb;
                    measurements.pressure = pressureMb;
                    measurements.changed[3] = true;
                }
                else
                    measurements.pressure = -100;

                // Get Relative Humidity
                if (relativeHumidity.ToString("F1") != oldRelativeHumidity.ToString("F1") || toForce[4] || wasSensorFailed)
                {
                    oldRelativeHumidity = relativeHumidity;
                    measurements.humidity = relativeHumidity;
                    measurements.changed[4] = true;
                }
                else
                    measurements.humidity = -100;

                wasSensorFailed = false;
            }
            else
            {
                if (toForce[2] || !wasSensorFailed)
                {
                    measurements.temperature = -102;
                    measurements.pressure = -102;
                    measurements.humidity = -102;
                    measurements.changed[2] = true;
                    measurements.changed[3] = true;
                    measurements.changed[4] = true;
                }
                else
                {
                    measurements.temperature = -100;
                    measurements.pressure = -100;
                    measurements.humidity = -100;
                }

                wasSensorFailed = true;
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
                if (SDMemoryCard.IsCardInserted && SDMemoryCard.IsCardMounted)
                {
                    data = SDMemoryCard.ReadFile("MouseData");
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
                    lastWrittenX = Int32.Parse(cordinates[0]);
                    lastWrittenY = Int32.Parse(cordinates[1]);
                    mouse.SetPosition(lastWrittenX, lastWrittenY);
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
                LoadMouseData();

            isMouseConnected = true;

            Debug.Print("Mouse Connected");
            Display.UpdateMouseState(true);
        }

        void mouse_Disconnected(BaseDevice sender, EventArgs e)
        {
            isMouseConnected = false;

            Debug.Print("Mouse Disconnected");
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
                int x = Mouse.X;
                int y = Mouse.Y;

                if (x != lastWrittenX || y != lastWrittenY)
                {
                    String mouseData = x + " " + y;

                    if (!SDMemoryCard.WriteFile("MouseData", Encoding.UTF8.GetBytes(mouseData), true))
                    {
                        Debug.Print("ERROR while saving MouseData!");
                    }
                    else
                    {
                        lastWrittenX = x;
                        lastWrittenY = y;
                    }
                }
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

                    isNotActivated = false;
                }
                catch (BME280Exception)
                {
                    Debug.Print("Error during BME280 initiliazation");
                }

                Thread.Sleep(5000);
            }

            // Sensor Timer
            GT.Timer sensorTimer = new GT.Timer(20000);
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
                    Display.UpdateTPHState(true);
                    Debug.Print("Sensor connected");                    
                }
                
            }
            catch (BME280Exception)
            {
                bme280Working = false;
                Display.UpdateTPHState(false);
                Debug.Print("Unable to read sensor data");
                Debug.Print("Sensor disconnected");
            }
        }
    }
}
