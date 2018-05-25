using System;
using System.Net;
using Microsoft.SPOT.IO;
using Microsoft.SPOT;
using Microsoft.SPOT.Time;

namespace PLCS_Project
{
    static class Utils
    {
        public static bool timeConfigured = false;

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

        public static void SetupTimeService()
        {
            if (!timeConfigured)
            {
                TimeServiceSettings settings = new TimeServiceSettings();
                settings.RefreshTime = 3600; // every 10 seconds
                settings.ForceSyncAtWakeUp = true;

                TimeService.SystemTimeChanged += TimeService_SystemTimeChanged;
                TimeService.TimeSyncFailed += TimeService_TimeSyncFailed;
                TimeService.SetTimeZoneOffset(60);

                IPHostEntry hostEntry = Dns.GetHostEntry("time.nist.gov");
                IPAddress[] address = hostEntry.AddressList;

                if (address != null)
                    settings.PrimaryServer = address[0].GetAddressBytes();

                hostEntry = Dns.GetHostEntry("time.windows.com");
                address = hostEntry.AddressList;

                if (address != null)
                    settings.AlternateServer = address[0].GetAddressBytes();

                TimeService.Settings = settings;
                TimeService.Start();
                timeConfigured = true;
            }
        }

        private static void TimeService_TimeSyncFailed(object sender, TimeSyncFailedEventArgs e)
        {
            Debug.Print("DateTime Sync Failed");
        }

        private static void TimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
            Debug.Print("DateTime = " + DateTime.Now.ToString());
        }
    }
}
