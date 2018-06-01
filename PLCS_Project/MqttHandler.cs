using System;
using System.Threading;
using System.Net;
using Microsoft.SPOT;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PLCS_Project
{
    class MqttHandler
    {
        private const String MQTT_BROKER_ADDRESS = "192.168.137.1";
        private MqttClient mqttClient;

        public MqttHandler()
        {
            // create client instance 
            mqttClient = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

            // register to message received 
            mqttClient.MqttMsgPublishReceived += mqttClient_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            mqttClient.Connect(clientId);

            // subscribe to the topic "/home/temperature" with QoS 2 
            mqttClient.Subscribe(new string[] { "/home/temperature" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        void mqttClient_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
