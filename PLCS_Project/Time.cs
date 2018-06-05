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
        private static long firstSyncTimeOffset;

        public static bool IsTimeSynchronized { get { return timeSynchronized; } }
        public static long FirstSyncTimeOffset { get { return firstSyncTimeOffset; } }

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
            UpdateNow();
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

                Thread.Sleep(30000);
            }

            if (address != null)
                return address[0];

            return null;
        }

        private static void UpdateNow()
        {
            while (true)
            {
                DateTime beforeTime = DateTime.UtcNow;
                TimeServiceStatus status = TimeService.UpdateNow(TimeService.Settings.Tolerance);

                if (status.Flags == TimeServiceStatus.TimeServiceStatusFlags.SyncSucceeded)
                {
                    if (!timeSynchronized)
                    {
                        TimeSpan offset = DateTime.UtcNow - beforeTime;
                        firstSyncTimeOffset = offset.Ticks;
                        timeSynchronized = true;
                    }

                    break;
                }

                Thread.Sleep(30000);
            }
        }

        private static void TimeService_TimeSyncFailed(object sender, TimeSyncFailedEventArgs e)
        {
            Debug.Print("DateTime Sync Failed: " + e.ErrorCode + " -> " + e.ToString());
        }

        private static void TimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
            Debug.Print("Time Synchronized -> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            Display.UpdateTimeStatus(true);
        }
    }
}
