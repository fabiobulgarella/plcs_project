using System;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using GHI.Usb.Descriptors;
using Gadgeteer.Modules.GHIElectronics;
using GT = Gadgeteer;

namespace PLCS_Project
{
    static class Utils
    {
        private static LEDStrip ledStrip;

        public static void SetLedStrip(LEDStrip ledStripObject)
        {
            ledStrip = ledStripObject;
            ledStrip.TurnAllLedsOff();
        }

        public static void TurnLedOn(int led)
        {
            ledStrip.TurnLedOn(led);
        }

        public static void TurnLedOff(int led)
        {
            ledStrip.TurnLedOff(led);
        }

        public static void TurnAllLedsOn()
        {
            ledStrip.TurnAllLedsOn();
        }

        public static void TurnAllLedsOff()
        {
            ledStrip.TurnAllLedsOff();
        }

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

        public static void PrintInterfaceInfo(Interface i)
        {
            Debug.Print("");
            Debug.Print("Interface Index:  " + i.Index);
            Debug.Print("Interface Number:  " + i.Number);
            Debug.Print("Class Code: " + i.ClassCode);
            Debug.Print("Subclass Code: " + i.SubclassCode);
            Debug.Print("Number Endpoints: " + i.NumberEndpoints);
            Debug.Print("Protocol Code:  " + i.ProtocolCode);
        }

        public static void PrintAuxiliaryDescriptors(Auxiliary[] auxiliaryDescriptors)
        {
            foreach (Auxiliary aux in auxiliaryDescriptors)
            {
                Debug.Print("");
                Debug.Print("Auxiliary Type: " + aux.Type);
                Debug.Print("Auxiliary Payload: " + aux.Payload.ToString());
            }
        }
    }
}
