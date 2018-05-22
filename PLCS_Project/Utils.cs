using System;
using Microsoft.SPOT.IO;
using Microsoft.SPOT;
using System.Text;

namespace PLCS_Project
{
    static class Utils
    {
        private static const String device_id = "FEZ_49";

        public static void PrintVolumeInfo(VolumeInfo volume)
        {
            Debug.Print("Volume Name: " + volume.Name);
            Debug.Print("File System: " + volume.FileSystem);
            Debug.Print("Serial Number: " + volume.SerialNumber);
            Debug.Print("Volume ID: " + volume.VolumeID);
            Debug.Print("Volume Label: " + volume.VolumeLabel);
            Debug.Print("Total Size: " + volume.TotalSize);
            Debug.Print("Free Space: " + volume.TotalFreeSpace);
        }

        public static String CreateJsonMeasurements(int x, int y, double tempC, double pressureMb, double relativeHumidity)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"device_id\": \"");
            jsonString.Append(device_id);
            jsonString.Append("\", \"measurements\": [");
            
            return jsonString.ToString();
        }
    }
}
