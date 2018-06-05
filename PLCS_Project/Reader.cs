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
            GT.Timer writingTimer = new GT.Timer(20000);
            writingTimer.Tick += writingTimer_Tick;
            writingTimer.Start();
        }

        void writingTimer_Tick(GT.Timer timer)
        {            
            bool[] toForce = new bool[5];

            if (!SDMemoryCard.IsMounted || !SDMemoryCard.IsCardInserted)
            {
                SDMemoryCard.Unmount();
                if (!SDMemoryCard.Mount())
                    return;
            }
            // Get measurements from sensors handler
            SensorsHandler.Measurements m = sensors.GetMeasurements();

            for (int i = 0; i < 5; i++)
            {
                if (!m.changed[i])
                {
                    if (measuresNotChanged[i]==9)
                    {
                        toForce[i] = true;
                        measuresNotChanged[i] = 0;
                        continue;
                    }
                    measuresNotChanged[i]++;                                    
                }                    
            }

            foreach (bool force in toForce)
            {
                if (force)
                {
                    m = sensors.GetForcedMeasurements(toForce);
                    break;
                }                    
            }

            byte[] data = Json.CreateJsonMeasurements(m.x, m.y, m.temperature, m.pressure, m.humidity);
            long numberOfTicks = Json.measureTimeTicks;
            string fileName = "" + numberOfTicks;           
            if (!Time.IsTimeSynchronized)
                fileName += "_notSynch";
            SDMemoryCard.writeFile(fileName, data);
            Debug.Print("The file: " + fileName + " has been written");          
        }
    }
}
