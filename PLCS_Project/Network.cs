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
        private const string SSID = "Fabio HS";
        private const string KEY = "abcd12345";

        private const string IP = "192.168.137.2";
        private const string NETMASK = "255.255.255.0";
        private const string GATEWAY = "192.168.137.1";

        private static string[] DNS = new string[] { "208.67.222.222", "208.67.220.220" };

        private EthernetJ11D ethernet;
        private WiFiRS21 wifi;

        private Thread wifiScanThread;
        private bool wifiEnabled;

        private bool ethernetConnected;
        private bool wifiConnected;

        public bool IsConnected { get { return ethernetConnected || wifiConnected; } }

        public Network(EthernetJ11D ethernet, WiFiRS21 wifi, Button button)
        {
            this.ethernet = ethernet;
            this.wifi = wifi;
            wifiEnabled = true;
            ethernetConnected = false;

            //InitEthernet();
            InitWifi();

            // Configure network toggle button
            //button.Mode = Button.LedMode.OnWhilePressed;
            //button.ButtonPressed += button_ButtonPressed;
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

        void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            if (state == Button.ButtonState.Pressed)
                ToggleNetwork();
        }
        */

        /**
         * WIFI SECTION
         */
        private void InitWifi()
        {
            wifiConnected = false;
            wifi.NetworkDown += wifi_NetworkDown;
            wifi.NetworkUp += wifi_NetworkUp;
            wifi.NetworkInterface.Open();
            wifi.NetworkInterface.EnableDhcp();
            wifi.NetworkInterface.EnableStaticDns(DNS);
            wifiScanThread = new Thread(WifiScan);
        }

        private void WifiScan()
        {
            Debug.Print("Inside wifi scan");
            while (wifiEnabled && !wifi.IsNetworkConnected)
            {
                WiFiRS9110.NetworkParameters[] networks = null;
                try
                {
                    networks = wifi.NetworkInterface.Scan(SSID);
                    Debug.Print("Found " + networks.Length + " network with this SSID");
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
                    }
                    catch (Exception)
                    {
                        Debug.Print("Unable to connecet to this nwtwork!");
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
            Time.InitService();
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
        /*
        private void InitEthernet()
        {
            ethernetConnected = false;
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
        */
    }
}
