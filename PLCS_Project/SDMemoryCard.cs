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

        public static void writeFile(string nameFile, byte[] data)
        {
            string filePath = nameFile + ".json";
            sdCard.StorageDevice.WriteFile(filePath, data);
        }

        public static void deleteFile(string nameFile)
        {
            string filePath = nameFile + ".json";
            sdCard.StorageDevice.Delete(filePath);
        }

    }
}
