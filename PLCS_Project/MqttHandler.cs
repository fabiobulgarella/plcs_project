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

        public MqttHandler()
        {
            Thread mqttThread = new Thread(InitClient);
            mqttThread.Start();
        }

        private void InitClient()
        {
            while (!Network.IsConnected)
            {
                Thread.Sleep(5000);
            }

            Thread.Sleep(10000);

            // create client instance 
            mqttClient = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

            // register to message received 
            mqttClient.MqttMsgPublishReceived += mqttClient_MqttMsgPublishReceived;
            mqttClient.MqttMsgPublished += mqttClient_MqttMsgPublished;

            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId, USERNAME, PASSWORD);

            // subscribe to the topic "/FEZ49/" with QoS 2 
            mqttClient.Subscribe(new string[] { "/FEZ49/acknowledgments" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
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
            // publish a message on "/home/measurements" topic with QoS 2
            mqttClient.Publish("/FEZ49/measurements", data, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }
    }
}
