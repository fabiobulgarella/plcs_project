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
        private static bool unsynchedToBeRemoved = true;

        public static bool IsMounted { get { return sdCard.IsCardMounted; } }
        public static bool IsCardInserted { get { return sdCard.IsCardInserted; } }

        public static void SetSDcard(SDCard sdCardObject)
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

            // Delete unsynched files written before reboot
            if (unsynchedToBeRemoved)
                RemoveUnsynchedFiles();
        }

        private static void sdCard_Unmounted(SDCard sender, EventArgs e)
        {
            Debug.Print("SDCard has been Unmounted");
            Utils.TurnLedOff(2);
            Display.UpdateSDState(false);
        }

        public static byte[] ReadFile(string fileName)
        {
            try
            {
                string filePath = fileName + ".json";
                return sdCard.StorageDevice.ReadFile(filePath);
            }
            catch (Exception)
            {
                Debug.Print("File not present");
                return null;
            }
        }

        public static void WriteFile(string fileName, byte[] data)
        {
            try
            {
                string filePath = fileName + ".json";
                sdCard.StorageDevice.WriteFile(filePath, data);
            }
            catch (Exception)
            {
                Debug.Print("File not written");
            }
        }

        public static void DeleteFile(string filePath)
        {
            try
            {
                sdCard.StorageDevice.Delete(filePath);
            }
            catch (Exception)
            {
                Debug.Print("File not deleted");
            }
        }

        public static string RenameUnsynchedFile(string fileName)
        {
            try
            {
                if (Time.IsTimeSynchronized)
                {
                    long notSynchDate = long.Parse(fileName.Split('_')[0]);
                    long synchDate = notSynchDate + Time.FirstSyncTimeOffset; ;
                    string newFileName = synchDate.ToString() + ".json";
                    byte[] unsynchFile = sdCard.StorageDevice.ReadFile(fileName + ".json");
                    byte[] synchFile = Json.ChangeTimestamps(unsynchFile, synchDate);
                    sdCard.StorageDevice.WriteFile(newFileName, synchFile);
                    sdCard.StorageDevice.Delete(fileName + ".json");
                    return newFileName;
                }
                return null;
            }
            catch (Exception)
            {
                Debug.Print("File not renamed");
                return null;
            }            
        }

        private static void RemoveUnsynchedFiles()
        {
            String[] fileList = sdCard.StorageDevice.ListFiles("\\");
            foreach (String fileName in fileList)
            {
                if (fileName.IndexOf("_notSynch") != -1)
                    DeleteFile(fileName);
            }

            unsynchedToBeRemoved = false;
        }
    }
}
