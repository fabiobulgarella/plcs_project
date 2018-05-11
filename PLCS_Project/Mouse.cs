using System;
using Microsoft.SPOT;

using GHI.Usb.Host;
using GHI.Usb.Descriptors;
using Microsoft.SPOT.Hardware;
using System.Reflection;

namespace PLCS_Project
{
    class Mouse : RawDevice
    {
        private const double dotMillimeters = 322.83464566929138;
        
        private const int classCode = 0x03;
        private const int subclassCode = 0x01;
        private const int protocolCode = 0x02;
        private const int inputPipeEndpoint = 0;

        private RawDevice.Pipe inputPipe;
        private byte[] inputData;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int ExceptionCounter { get; private set; }
        public bool HasMoved { get; set; }

        private MethodInfo transferMethod;

        public Mouse(uint id, byte interfaceIndex, ushort vendorId, ushort productId, byte portNumber, DeviceType deviceType) : base(id, interfaceIndex, vendorId, productId, portNumber, deviceType)
        {
            X = Y = 0;
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
                    Debug.Print("Input Error: " + Controller.GetLastError().ToString());
                }

                if (bytesReceived == 7)
                {
                    X += BitConverter.ToInt16(inputData, 1);
                    Y += BitConverter.ToInt16(inputData, 3);
                    HasMoved = true;
                }
            }
        }

        public string GetMillimeterX()
        {
            double millimeterX = X / dotMillimeters;
            return millimeterX.ToString("F3");
        }

        public string GetMillimeterY()
        {
            double millimeterY = Y / dotMillimeters;
            return millimeterY.ToString("F3");
        }

        public string GetPosition()
        {
            return "DPI ->  X: " + X.ToString() + "   Y: " + Y.ToString();
        }

        public string GetMillimeterPosition()
        {
            return "mm ->  X: " + GetMillimeterX() + "   Y: " + GetMillimeterY();
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

        private void PrintInterfaceInfo(Interface i)
        {
            Debug.Print("");
            Debug.Print("Interface Index:  " + i.Index);
            Debug.Print("Interface Number:  " + i.Number);
            Debug.Print("Class Code: " + i.ClassCode);
            Debug.Print("Subclass Code: " + i.SubclassCode);
            Debug.Print("Number Endpoints: " + i.NumberEndpoints);
            Debug.Print("Protocol Code:  " + i.ProtocolCode);
        }

        private void PrintAuxiliaryDescriptors(Auxiliary[] auxiliaryDescriptors)
        {
            foreach (Auxiliary aux in auxiliaryDescriptors)
            {
                Debug.Print("");
                Debug.Print("Auxiliary Type: " + aux.Type);
                Debug.Print("Auxiliary Payload: " + aux.Payload.ToString());
            }
        }
    }
}
