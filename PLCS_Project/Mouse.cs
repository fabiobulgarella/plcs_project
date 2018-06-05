using System;
using System.Reflection;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.Usb.Host;
using GHI.Usb.Descriptors;
using System.Collections;


namespace PLCS_Project
{
    public class Mouse : RawDevice
    {
        private const double dotMillimeters = 322.83464566929138;
        
        private const int classCode = 0x03;
        private const int subclassCode = 0x01;
        private const int protocolCode = 0x02;
        private const int inputPipeEndpoint = 0;

        private RawDevice.Pipe inputPipe;
        private byte[] inputData;

        public static bool neverConnected = true;
        public static int X { get; private set; }
        public static int Y { get; private set; }
        public int ExceptionCounter { get; private set; }
        public bool HasMoved { get; set; }

        private MethodInfo transferMethod;

        public Mouse(uint id, byte interfaceIndex, ushort vendorId, ushort productId, byte portNumber, DeviceType deviceType) : base(id, interfaceIndex, vendorId, productId, portNumber, deviceType)
        {
            if (neverConnected)
            {
                X = Y = 0;
                neverConnected = false;
            }
            ExceptionCounter = 0;
            HasMoved = true;
            transferMethod = typeof(RawDevice.Pipe).GetMethod("NativeTransfer", BindingFlags.NonPublic | BindingFlags.Instance);
            
            Configuration configuration = GetConfigurationDescriptor(0);

            foreach (Interface i in configuration.Interfaces)
            {
                if (i.ClassCode == classCode && i.SubclassCode == subclassCode && i.ProtocolCode == protocolCode)
                {
                    inputPipe = OpenPipe(i.Endpoints[inputPipeEndpoint]);
                    inputPipe.TransferTimeout = 0;
                    ushort maxPacketSize = inputPipe.Endpoint.MaximumPacketSize;
                    inputData = new byte[maxPacketSize];
                    WorkerInterval = 8;

                    break;
                }
            }
        }

        protected override void CheckEvents(object sender)
        {
            if (base.CheckObjectState(false))
            {
                int bytesReceived = 0;
                try
                {
                    bytesReceived = (int)transferMethod.Invoke(inputPipe, new object[] { inputData, 0, inputData.Length, 0 });
                }
                catch (Exception)
                {
                    ExceptionCounter++;
                    //Debug.Print("Input Error: " + Controller.GetLastError().ToString());
                }

                if (bytesReceived == 7)
                {
                    X += BitConverter.ToInt16(inputData, 1);
                    Y += BitConverter.ToInt16(inputData, 3);
                    HasMoved = true;
                }
            }
        }

        public string GetMillimetersX()
        {
            double millimetersX = X / dotMillimeters;
            return millimetersX.ToString("F2");
        }

        public string GetMillimetersY()
        {
            double millimetersY = Y / dotMillimeters;
            return millimetersY.ToString("F2");
        }

        public string GetStringPosition()
        {
            return "DPI ->  X: " + X.ToString() + "   Y: " + Y.ToString();
        }

        public string GetStringMillimetersPosition()
        {
            return "mm ->  X: " + GetMillimetersX() + "   Y: " + GetMillimetersY();
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void ResetPosition()
        {
            X = Y = 0;
        }

        public static void CleanGhiMouse()
        {
            FieldInfo devices = typeof(Controller).GetField("devices", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo listLock = typeof(Controller).GetField("listLock", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo onDisconnected = typeof(BaseDevice).GetMethod("OnDisconnected", BindingFlags.NonPublic | BindingFlags.Instance);

            lock (listLock.GetValue(null))
            {
                foreach (BaseDevice device in (ArrayList)devices.GetValue(null))
                {
                    onDisconnected.Invoke(device, null);
                    device.Dispose();
                }
                ((ArrayList)devices.GetValue(null)).Clear();
            }
        }
    }
}
