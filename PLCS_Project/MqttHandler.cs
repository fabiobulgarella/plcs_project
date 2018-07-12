using System;
using System.Threading;
using System.Net;
using Microsoft.SPOT;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

namespace PLCS_Project
{
    class MqttHandler
    {
        private const String MQTT_BROKER_ADDRESS = "192.168.137.1";
        private const String USERNAME = "fez49";
        private const String PASSWORD = "abcd12345";
        
        private MqttClient mqttClient;
        private Thread connectionThread;

        public MqttHandler()
        {
            InitClient();

            // Start trying to connect
            connectionThread = new Thread(Connect);
            connectionThread.Start();
        }

        private void InitClient()
        {
            // create client instance 
            mqttClient = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

            // register to message received 
            mqttClient.MqttMsgPublishReceived += mqttClient_MqttMsgPublishReceived;
            mqttClient.MqttMsgPublished += mqttClient_MqttMsgPublished;
            mqttClient.ConnectionClosed += mqttClient_ConnectionClosed;
        }

        private void Connect()
        {
            while (true)
            {
                if (Network.IsConnected)
                {
                    string clientId = Guid.NewGuid().ToString();

                    try
                    {
                        byte result = mqttClient.Connect(clientId, USERNAME, PASSWORD);

                        // Check if successfully connected
                        if (result == 0)
                        {
                            mqttClient.Subscribe(new string[] { "/FEZ49/acknowledgments" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

                            Debug.Print("Mqtt connection successfully established!");
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.Print("An error occurred during mqtt connection. Trying again in few seconds...");
                    }
                }
                else
                    Debug.Print("Cannot connect mqtt client to the gateway: connection unavailable...");

                Thread.Sleep(10000);
            }
        }

        void mqttClient_ConnectionClosed(object sender, EventArgs e)
        {
            Debug.Print("Mqtt connection closed. Trying to reconnect...");
            if (!connectionThread.IsAlive)
                connectionThread.Start();
        }

        void mqttClient_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Debug.Print("Message published -> " + e.IsPublished + " ID -> " + e.MessageId);
        }

        void mqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.Print("Message Received: " + e.Message.ToString());
        }

        private void PublishMessage(byte[] data)
        {
            if (mqttClient.IsConnected)
            {
                // publish a message on "/home/measurements" topic with QoS 2
                mqttClient.Publish("/FEZ49/measurements", data, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
        }
    }
}
