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

        public static bool IsCardMounted { get { return sdCard.IsCardMounted; } }
        public static bool IsCardInserted { get { return sdCard.IsCardInserted; } }
        public static bool IsInitialized = false;

        public static void SetSDcard(SDCard sdCardObject)
        {
            sdCard = sdCardObject;
            sdCard.Mounted += sdCard_Mounted;
            sdCard.Unmounted += sdCard_Unmounted;
        }

        public static bool Mount()
        {
            try
            {
                return sdCard.Mount();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return false;
            }
        }

        public static bool Unmount()
        {
            try
            {
                return sdCard.Unmount();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return false;
            }
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
                return sdCard.StorageDevice.ReadFile(fileName);
            }
            catch (Exception)
            {
                Debug.Print("File not present");
                return null;
            }
        }

        public static void WriteFile(string fileName, byte[] data, bool flush = false)
        {
            try
            {
                sdCard.StorageDevice.WriteFile(fileName, data);
                
                if (flush)
                    sdCard.StorageDevice.Volume.FlushAll();
            }
            catch (Exception)
            {
                Debug.Print("File not written");
            }
        }

        public static void DeleteFile(string filePath, bool flush = false)
        {
            try
            {
                sdCard.StorageDevice.Delete(filePath);

                if (flush)
                    sdCard.StorageDevice.Volume.FlushAll();
            }
            catch (Exception)
            {
                Debug.Print("File not deleted");
            }
        }

        private static void FlushAll()
        {
            try
            {
                sdCard.StorageDevice.Volume.FlushAll();
            }
            catch (Exception)
            {
                Debug.Print("Error during FlushAll!");
            }
        }

        public static bool CheckSdCard()
        {
            if (sdCard.IsCardInserted)
            {
                if (sdCard.IsCardMounted)
                    return true;
                else
                    return Mount();
            }
            else if (sdCard.IsCardMounted)
            {
                Unmount();
            }

            return false;
        }

        public static string RenameUnsynchedFile(string fileName)
        {
            try
            {
                if (Time.IsTimeSynchronized)
                {
                    long notSynchDate = long.Parse(fileName.Split('_')[1]);
                    long synchDate = notSynchDate + Time.FirstSyncTimeOffset;
                    string newFileName = new DateTime(synchDate).ToString("yyyy-MM-ddTHH\\:mm\\:ss");
                    byte[] unsynchFile = sdCard.StorageDevice.ReadFile(fileName);
                    byte[] synchFile = Json.ChangeTimestamps(unsynchFile, synchDate);
                    sdCard.StorageDevice.WriteFile(newFileName, synchFile);
                    sdCard.StorageDevice.Delete(fileName);
                    sdCard.StorageDevice.Volume.FlushAll();
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

        public static string[] GetFileList()
        {
            try
            {
                return sdCard.StorageDevice.ListRootDirectoryFiles();
            }
            catch (Exception e)
            {
                Debug.Print("An error occurred getting root file list: " + e.Message);
                return null;
            }
        }

        private static void RemoveUnsynchedFiles()
        {
            int counter = 0;
            String[] fileList = sdCard.StorageDevice.ListRootDirectoryFiles();

            foreach (String fileName in fileList)
            {
                if (fileName.IndexOf("_") != -1)
                {
                    DeleteFile(fileName);
                    counter++;
                }
            }

            FlushAll();
            unsynchedToBeRemoved = false;
            IsInitialized = true;
            Debug.Print("Removed " + counter + " unsynched files not recoverable.");
        }
    }
}
