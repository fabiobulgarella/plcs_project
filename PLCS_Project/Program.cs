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
        private Reader reader;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            // Setup sdcard            
            SDMemoryCard.SetSDcard(sdCard);

            // Setup display
            Display.SetDisplay(displayTE35);

            // Setup sensor
            sensors = new SensorsHandler(usbHost, button);

            // Setup network
            network = new Network(ethernetJ11D);

            // Setup reader
            reader = new Reader(sensors);

            // MqttHandler
            mqttHandler = new MqttHandler();
        }
    }
}
