using System;
using Microsoft.SPOT.IO;
using Microsoft.SPOT;

namespace PLCS_Project
{
    static class Utils
    {
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
    }
}
