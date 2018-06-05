//PLCS project
//Authors: Antonietta Simone Domenico, Bulgarella Fabio, Loro Matteo

using System;
using System.Collections;
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

namespace PLCS_Project
{
    public partial class Program
    {
        private SensorsHandler sensors;
        private Network network;
        private MqttHandler mqttHandler;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            // Setup leds            
            Utils.SetLedStrip(ledStrip);

            // Setup display
            Display.setDisplay(displayTE35);

            // Setup sensor
            sensors = new SensorsHandler(usbHost, button);

            // Setup network
            network = new Network(ethernetJ11D, wifiRS21);

            // Setup sdcard            
            SDMemoryCard.setSDcard(sdCard);
            
            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(20000);
            writingTimer.Tick += writingTimer_Tick;
            //writingTimer.Start();

            // MqttHandler test
            //mqttHandler = new MqttHandler();
        }

        void writingTimer_Tick(GT.Timer timer)
        {
            if (SDMemoryCard.IsMounted)
            {
                this.sdCard.Mount();                
            }

            //byte[] data = Json.CreateJsonMeasurements(mouse.GetMillimetersX(), mouse.GetMillimetersY(), tempC, pressureMb, relativeHumidity);
            long numberOfTicks = DateTime.UtcNow.Ticks;
            string fileName = "" + numberOfTicks;            
            //Debug.Print("The file: " + fileName + " has been written");          
            if (!Time.IsTimeSynchronized)
                fileName += "_notSynch";            
            //SDMemoryCard.writeFile(fileName, data);

            SDMemoryCard.renameUnsynchFile("129513600287878872_notSynch");
            
            /*SDMemoryCard.deleteFile(fileName);
            Debug.Print("The file: " + fileName + " has been deleted");   */
            this.sdCard.Unmount();          
           
        }
    }
}
