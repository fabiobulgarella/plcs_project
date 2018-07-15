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
        private const string IP = "192.168.137.2";
        private const string NETMASK = "255.255.255.0";
        private const string GATEWAY = "192.168.137.1";

        private static string[] DNS = new string[] { "208.67.222.222", "208.67.220.220" };

        private static bool ethernetConnected;

        public static bool IsConnected { get { return ethernetConnected; } }

        private EthernetJ11D ethernet;

        public Network(EthernetJ11D ethernet)
        {
            this.ethernet = ethernet;
            ethernetConnected = false;

            InitEthernet();
        }

        /**
         * ETHERNET SECTION
         */
        private void InitEthernet()
        {
            ethernet.NetworkDown += ethernet_NetworkDown;
            ethernet.NetworkUp += ethernet_NetworkUp;
            ethernet.UseStaticIP(IP, NETMASK, GATEWAY);
            if (!ethernet.NetworkInterface.Opened)
                ethernet.NetworkInterface.Open();
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
