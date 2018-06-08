using System.Threading;
using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using GT = Gadgeteer;

namespace PLCS_Project
{
    static class SDMemoryCard
    {
        private static SDCard sdCard;

        public static bool IsMounted { get { return sdCard.IsCardMounted; } }
        public static bool IsCardInserted { get { return sdCard.IsCardInserted; } }

        public static void setSDcard(SDCard sdCardObject)
        {
            sdCard = sdCardObject;
            sdCard.Mounted += sdCard_Mounted;
            sdCard.Unmounted += sdCard_Unmounted;
        }

        public static bool Mount()
        {
            return sdCard.Mount();
        }

        public static bool Unmount()
        {
            return sdCard.Unmount();
        }

        private static void sdCard_Mounted(SDCard sender, GT.StorageDevice device)
        {
            Debug.Print("SDCard has been Mounted");
            Utils.TurnLedOn(2);
            Display.UpdateSDState(true);
        }

        private static void sdCard_Unmounted(SDCard sender, EventArgs e)
        {
            Debug.Print("SDCard has been Unmounted");
            Utils.TurnLedOff(2);
            Display.UpdateSDState(false);
        }

        public static byte[] readFile(string fileName)
        {
            string filePath = fileName + ".json";
            return sdCard.StorageDevice.ReadFile(filePath);
        }

        public static void writeFile(string fileName, byte[] data)
        {
            string filePath = fileName + ".json";
            sdCard.StorageDevice.WriteFile(filePath, data);
        }

        public static void deleteFile(string fileName)
        {
            string filePath = fileName + ".json";
            sdCard.StorageDevice.Delete(filePath);
        }

        public static string renameUnsynchFile(string fileName)
        {
            if (Time.IsTimeSynchronized)
            {
                long notSynchDate = long.Parse(fileName.Split('_')[0]);
                long synchDate = notSynchDate + Time.FirstSyncTimeOffset;;
                string newFileName = synchDate.ToString() + ".json";
                byte[] unsynchFile = sdCard.StorageDevice.ReadFile(fileName + ".json");
                byte[] synchFile = Json.ChangeTimestamps(unsynchFile, synchDate);
                sdCard.StorageDevice.WriteFile(newFileName, synchFile);
                sdCard.StorageDevice.Delete(fileName + ".json");
                return newFileName;
            }

            return null;
        }
    }
}
