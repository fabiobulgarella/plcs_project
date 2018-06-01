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

        public static void setSDcard(SDCard sdCardObject)
        {
            sdCard = sdCardObject;
            if(!sdCard.IsCardMounted && sdCard.IsCardInserted)
                sdCard.Mount();
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
            Thread.Sleep(5000);
            long offset = 0;
            if(Time.IsTimeSynchronized)
                            offset = Time.SyncTimeOffset;
            Debug.Print(offset.ToString());
            long notSynchDate = long.Parse(fileName.Split('_')[0]);
            long synchDate = notSynchDate + offset;                                      
            string newFileName = synchDate.ToString() + ".json";
            byte[] unsynchFile = sdCard.StorageDevice.ReadFile(fileName + ".json");
            byte[] synchFile = Json.ChangeTimestamps(unsynchFile,synchDate);                                             
            sdCard.StorageDevice.WriteFile(newFileName, synchFile);
            sdCard.StorageDevice.Delete(fileName + ".json");
            return newFileName;
        }
    }
}
