using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace PLCS_Project
{
    class Reader
    {
        private int[] measuresNotChanged;
        private SensorsHandler sensors;

        public Reader(SensorsHandler sensorsObject)
        {
            sensors = sensorsObject;
            measuresNotChanged = new int[5];
            
            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(30000);
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();
        }

        void writingTimer_Tick(GT.Timer timer)
        {            
            // Check if Sdcard is present
            if (!SDMemoryCard.CheckSdCard())
                return;

            bool[] toForce = new bool[5];
            
            // Check what measures have to be forced
            for (int i = 0; i < 5; i++)
            {
                if (measuresNotChanged[i] == 14)
                {
                    toForce[i] = true;
                    measuresNotChanged[i] = 0;
                }              
            }

            // Get measurements from sensors handler
            SensorsHandler.Measurements m = sensors.GetMeasurements(toForce);

            // Update measuresNotChanged values
            for (int i = 0; i < 5; i++)
            {
                if (!m.changed[i])
                    measuresNotChanged[i]++;
            }

            // Produce JSON and write it on a file
            byte[] data = Json.CreateJsonMeasurements(m.x, m.y, m.temperature, m.pressure, m.humidity);
            long numberOfTicks = Json.measureTimeTicks;
            string fileName = numberOfTicks.ToString();
            
            if (!Time.IsTimeSynchronized)
                fileName += "_notSynch";
            
            SDMemoryCard.WriteFile(fileName, data);
            Debug.Print("The file: " + fileName + " has been written");
        }
    }
}
