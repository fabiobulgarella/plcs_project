using System;
using System.Threading;
using Microsoft.SPOT;

using GHI.Networking;
using Gadgeteer.Modules.GHIElectronics;
using GT = Gadgeteer;

namespace PLCS_Project
{
    class Network
    {
        private const string SSID = "Tu Wi.Fi l'americano";
        private const string KEY = "132ADEC38C28";

        private const string IP = "192.168.137.2";
        private const string NETMASK = "255.255.255.0";
        private const string GATEWAY = "192.168.137.1";

        private static string[] DNS = new string[] { "208.67.222.222", "208.67.220.220" };

        private static bool ethernetConnected;
        private static bool wifiConnected;

        public static bool IsConnected { get { return ethernetConnected || wifiConnected; } }

        private EthernetJ11D ethernet;
        private WiFiRS21 wifi;

        private Thread wifiScanThread;
        private bool wifiEnabled;

        public Network(EthernetJ11D ethernet, WiFiRS21 wifi)
        {
            this.ethernet = ethernet;
            this.wifi = wifi;
            wifiEnabled = true;
            ethernetConnected = wifiConnected = false;

            InitEthernet();
            //InitWifi();
        }

        /*
        private void ToggleNetwork()
        {
            if (wifiEnabled)
            {
                wifiEnabled = false;
                if (wifi.IsNetworkConnected)
                    wifi.NetworkInterface.Disconnect();
                if (wifiScanThread.IsAlive)
                    wifiScanThread.Join();

                ethernet.UseThisNetworkInterface();
            }
            else
            {
                wifiEnabled = true;
                wifi.UseThisNetworkInterface();
                wifiScanThread.Start();
            }
        }
        */

        private void WaitForDhcp(bool forWifi)
        {
            Debug.Print("Waiting for DHCP...");

            if (forWifi)
            {
                while (wifi.NetworkInterface.IPAddress == "0.0.0.0")
                {
                    Thread.Sleep(2000);
                }
            }
            else
            {
                while (ethernet.NetworkInterface.IPAddress == "0.0.0.0")
                {
                    Thread.Sleep(2000);
                }
            }

            PrintWifiConfiguration();
            //Debug.Print("IP Address obtained -> " + wifi.NetworkInterface.IPAddress);
        }

        /**
         * WIFI SECTION
         */
        private void InitWifi()
        {
            wifi.NetworkDown += wifi_NetworkDown;
            wifi.NetworkUp += wifi_NetworkUp;
            wifi.UseThisNetworkInterface();
            wifi.NetworkInterface.EnableDhcp();
            wifi.NetworkInterface.EnableStaticDns(DNS);
            wifiScanThread = new Thread(WifiScan);
        }

        private void WifiScan()
        {
            while (wifiEnabled && !wifi.IsNetworkConnected)
            {
                WiFiRS9110.NetworkParameters[] networks = null;
                try
                {
                    networks = wifi.NetworkInterface.Scan(SSID);
                    Debug.Print("Found " + networks.Length + " network with SSID \"" + SSID + "\"");
                }
                catch (Exception)
                {
                    Debug.Print("Error during Wifi scan");
                }
                
                if (networks != null && networks.Length > 0)
                {
                    try
                    {
                        wifi.NetworkInterface.Join(SSID, KEY);
                        WaitForDhcp(true);
                        Time.InitService();
                    }
                    catch (Exception)
                    {
                        Debug.Print("Unable to connect to this network!");
                    }
                }

                // Riprova tra 30 secondi
                Thread.Sleep(30000);
            }
        }

        private void StartScan()
        {
            if (!wifiScanThread.IsAlive)
                wifiScanThread.Start();
        }

        void wifi_NetworkUp(GT.Modules.Module.NetworkModule sender, GT.Modules.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Wifi network is up");
            wifiConnected = true;

            Display.UpdateWifiStatus(true);
        }

        void wifi_NetworkDown(GT.Modules.Module.NetworkModule sender, GT.Modules.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Wifi network is down");
            wifiConnected = false;

            Display.UpdateWifiStatus(false);
            StartScan();
        }

        public void PrintWifiConfiguration()
        {
            Debug.Print("WIFI Configuration INTERFACE -> " + wifi.NetworkInterface.IPAddress + "  " + wifi.NetworkInterface.SubnetMask + "   " + wifi.NetworkInterface.GatewayAddress + "   " + wifi.NetworkInterface.DnsAddresses[0] + ", " + ethernet.NetworkInterface.DnsAddresses[1]);
        }

        /**
         * ETHERNET SECTION
         */
        private void InitEthernet()
        {
            ethernet.NetworkDown += ethernet_NetworkDown;
            ethernet.NetworkUp += ethernet_NetworkUp;
            ethernet.UseStaticIP(IP, NETMASK, GATEWAY);
            ethernet.UseThisNetworkInterface();
        }

        void ethernet_NetworkUp(GT.Modules.Module.NetworkModule sender, GT.Modules.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Ethernet network is up");
            PrintEthConfiguration();
            ethernetConnected = true;

            Display.UpdateEtherStatus(true);
            Time.InitService();
        }

        void ethernet_NetworkDown(GT.Modules.Module.NetworkModule sender, GT.Modules.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Ethernet network is down");
            ethernetConnected = false;

            Display.UpdateEtherStatus(false);
        }

        public void PrintEthConfiguration()
        {
            Debug.Print("ETH Configuration INTERFACE -> " + ethernet.NetworkInterface.IPAddress + "  " + ethernet.NetworkInterface.SubnetMask + "   " + ethernet.NetworkInterface.GatewayAddress + "   " + ethernet.NetworkInterface.DnsAddresses[0] + ", " + ethernet.NetworkInterface.DnsAddresses[1]);
        }
    }
}
