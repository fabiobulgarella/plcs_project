using System;
using System.Threading;
using System.Net;
using Microsoft.SPOT;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Collections;

namespace PLCS_Project
{
    class MqttHandler
    {
        private const String MQTT_BROKER_ADDRESS = "192.168.137.1";
        private const String USERNAME = "fez49";
        private const String PASSWORD = "abcd12345";
        
        private MqttClient mqttClient;
        private Thread connectionThread;
        private Thread transmissionThread;

        private Queue fileQueue;
        private Hashtable messageIdToFileMap;
        private ArrayList waitingGatewayAckFiles;
        private ArrayList waitingAmazonAckFiles;

        public MqttHandler()
        {
            InitClient();

            // Start trying to connect
            connectionThread = new Thread(Connect);
            connectionThread.Start();

            // Start transmission thread
            transmissionThread = new Thread(SenderWorker);
            transmissionThread.Start();
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
                            mqttClient.Subscribe(new string[] { "FEZ49/acknowledgments" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                            mqttClient.Publish("FEZ49/configuration", Json.CreateJsonConfiguration(), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                            Debug.Print("Mqtt connection successfully established!");
                            break;
                        }
                        else
                        {
                            Debug.Print("MQTT Client configuration error: broker rejected connection request!");
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

            // Clear waiting gateway ack tables
            messageIdToFileMap.Clear();
            waitingGatewayAckFiles.Clear();

            if (connectionThread == null || !connectionThread.IsAlive)
            {
                connectionThread = new Thread(Connect);
                connectionThread.Start();
            }
        }

        void mqttClient_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            String fileName = (String)messageIdToFileMap[e.MessageId];
            messageIdToFileMap.Remove(e.MessageId);
            waitingGatewayAckFiles.Remove(fileName);

            if (e.IsPublished)
            {
                waitingAmazonAckFiles.Add(fileName);
                Debug.Print("Message \"" + fileName + "\" published with ID -> " + e.MessageId);
            }
        }

        void mqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            String fileName = new String(Encoding.UTF8.GetChars(e.Message));

            // Delete correctly sent message (measurements set)
            SDMemoryCard.DeleteFile(fileName, true);
            waitingAmazonAckFiles.Remove(fileName);
            Debug.Print("Message \"" + fileName + "\" correctly saved on Amazon S3");
        }

        private ushort PublishMessage(byte[] data)
        {
            if (mqttClient.IsConnected && Network.IsConnected)
            {
                try
                {
                    // publish a message on "FEZ49/measurements" topic with QoS 2
                    return mqttClient.Publish("FEZ49/measurements", data, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }
                catch (Exception)
                {
                    Debug.Print("An error occurred publishing a message!");
                }
            }

            return 0;
        }

        private void CreateFileQueue()
        {
            string[] fileList = SDMemoryCard.GetFileList();

            if (fileList != null)
                foreach (string fileName in fileList)
                    fileQueue.Enqueue(fileName);
        }

        private void SenderWorker()
        {
            fileQueue = new Queue();
            messageIdToFileMap = new Hashtable();
            waitingGatewayAckFiles = new ArrayList();
            waitingAmazonAckFiles = new ArrayList();

            while (!SDMemoryCard.IsInitialized)
            {
                Thread.Sleep(3000);
            }

            while (true)
            {
                try
                {
                    String fileName = (String)fileQueue.Dequeue();

                    // Check if current file is "MouseData"
                    if (fileName.Length == 9) continue;

                    if (fileName.IndexOf("_") != -1)
                    {
                        String newFileName = SDMemoryCard.RenameUnsynchedFile(fileName);
                        if (newFileName != null)
                            fileQueue.Enqueue(newFileName);
                        else
                            fileQueue.Enqueue(fileName);

                        Thread.Sleep(200);
                        continue;
                    }

                    if (waitingAmazonAckFiles.Contains(fileName) || waitingGatewayAckFiles.Contains(fileName))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    byte[] fileData = SDMemoryCard.ReadFile(fileName);

                    Thread.Sleep(200);

                    if (fileData != null)
                    {
                        ushort messageId = PublishMessage(fileData);
                        
                        if (messageId == 0)
                            fileQueue.Enqueue(fileName);
                        else
                        {
                            messageIdToFileMap.Add(messageId, fileName);
                            waitingGatewayAckFiles.Add(fileName);
                        }
                    }
                }
                catch (Exception)
                {
                    Thread.Sleep(13000);
                    CreateFileQueue();
                }

                Thread.Sleep(4500);
            }
        }
    }
}
