using System;
using System.Net;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Time;

namespace PLCS_Project
{
    static class Time
    {
        private static bool timeConfigured = false;
        private static bool timeSynchronized = false;

        public static bool IsTimeSynchronized
        {
            get
            {
                return timeSynchronized;
            }
        }

        public static long SyncTimeOffset { get; private set; }

        public static void InitService()
        {
            if (!timeConfigured)
            {
                Debug.Print("Configuring TimeService...");
                Thread timeThread = new Thread(SetupTimeService);
                timeThread.Start();
                timeConfigured = true;
            }
        }

        private static void SetupTimeService()
        {
            TimeServiceSettings settings = new TimeServiceSettings();
            settings.RefreshTime = 20;
            settings.ForceSyncAtWakeUp = true;

            TimeService.SystemTimeChanged += TimeService_SystemTimeChanged;
            TimeService.TimeSyncFailed += TimeService_TimeSyncFailed;
            TimeService.SetTimeZoneOffset(120);


            IPAddress address = GetNtpAddress("0.it.pool.ntp.org");
            if (address != null)
                settings.PrimaryServer = address.GetAddressBytes();

            address = GetNtpAddress("1.it.pool.ntp.org");
            if (address != null)
                settings.AlternateServer = address.GetAddressBytes();

            TimeService.Settings = settings;
            TimeService.Start();
        }

        private static IPAddress GetNtpAddress(string dn)
        {
            IPAddress[] address = null;

            while (true)
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(dn);
                    address = hostEntry.AddressList;
                    break;
                }
                catch (Exception)
                {
                    Debug.Print("Ntp DNS resolving error");
                }

                Thread.Sleep(60000);
            }

            if (address != null)
                return address[0];

            return null;
        }

        private static void TimeService_TimeSyncFailed(object sender, TimeSyncFailedEventArgs e)
        {
            Debug.Print("DateTime Sync Failed: " + e.ErrorCode + " -> " + e.ToString());
        }

        private static void TimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
            Debug.Print("Time Synchronized");

            if (!timeSynchronized)
            {
                SyncTimeOffset = TimeService.LastSyncStatus.SyncTimeOffset;
                Debug.Print(SyncTimeOffset.ToString());
            }

            timeSynchronized = true;
            TimeService.Stop();
            Display.UpdateTimeStatus(true);
        }
    }
}
