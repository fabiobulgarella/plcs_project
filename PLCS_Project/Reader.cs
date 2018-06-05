using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace PLCS_Project
{
    class Reader
    {
        private SensorsHandler sensors;

        public Reader(SensorsHandler sensorsObject)
        {
            sensors = sensorsObject;

            // Writing Timer
            GT.Timer writingTimer = new GT.Timer(20000);
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();
        }

        void writingTimer_Tick(GT.Timer timer)
        {
            if (!SDMemoryCard.IsMounted || !SDMemoryCard.IsCardInserted)
            {
                SDMemoryCard.Unmount();
                if (!SDMemoryCard.Mount())
                    return;
            }
            // Get measurements from sensors handler
            SensorsHandler.Measurements m = sensors.getMeasurements();

            byte[] data = Json.CreateJsonMeasurements(m.x, m.y, m.temperature, m.pressure, m.humidity);
            long numberOfTicks = DateTime.UtcNow.Ticks;
            string fileName = "" + numberOfTicks;           
            if (!Time.IsTimeSynchronized)
                fileName += "_notSynch";
            SDMemoryCard.writeFile(fileName, data);
            Debug.Print("The file: " + fileName + " has been written");          
        }
    }
}
