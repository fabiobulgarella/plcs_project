using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using GT = Gadgeteer;

namespace PLCS_Project
{
    static class Display
    {
        private static DisplayTE35 display;

        public static void SetDisplay(DisplayTE35 displayObject)
        {
            display = displayObject;
            display.SimpleGraphics.AutoRedraw = false;
            PrintHeader();
            UpdateMouseState(false);
            UpdateTPHState(false);
            UpdateSDState(false);
        }

        private static void PrintHeader()
        {
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 0, 320, 18);
            display.SimpleGraphics.DisplayText("FEZ_49 Group", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 0);
            display.SimpleGraphics.DisplayText("TS", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 265, 0);
            display.SimpleGraphics.DisplayText("E", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 290, 0);
            display.SimpleGraphics.DisplayText("W", Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 305, 0);            
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateTimeStatus(bool synchronized)
        {
            GT.Color color = synchronized ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 265, 0, 25, 25);
            display.SimpleGraphics.DisplayText("TS", Resources.GetFont(Resources.FontResources.NinaB), color, 265, 0);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateEtherStatus(bool active)
        {
            GT.Color color = active ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 290, 0, 15, 15);
            display.SimpleGraphics.DisplayText("E", Resources.GetFont(Resources.FontResources.NinaB), color, 290, 0);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateWifiStatus(bool active)
        {
            GT.Color color = active ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 305, 0, 15, 15);
            display.SimpleGraphics.DisplayText("W", Resources.GetFont(Resources.FontResources.NinaB), color, 305, 0);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateSensorData(double tempC, double pressureMb, double relativeHumidity)
        {
            string temp = "Temperature: " + tempC.ToString("F1") + " C°";
            string pressure = "Pressure: " + pressureMb.ToString("F1") + " mBar";
            string humidity = "Relative Humidity: " + relativeHumidity.ToString("F1") + " %";
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 18, 320, 54);
            display.SimpleGraphics.DisplayText(temp, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 18);
            display.SimpleGraphics.DisplayText(pressure, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 36);
            display.SimpleGraphics.DisplayText(humidity, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 54);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateMouseData(int exceptionCounter, string position, string millimetersPosition)
        {
            string toPrint = "Exceptions raised: " + exceptionCounter;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 72, 320, 54);
            display.SimpleGraphics.DisplayText(position, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 72);
            display.SimpleGraphics.DisplayText(millimetersPosition, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 90);
            display.SimpleGraphics.DisplayText(toPrint, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 108);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateTPHState(bool activeTPH)
        {
            GT.Color colorTPH = activeTPH ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 265, 225, 35, 35);
            display.SimpleGraphics.DisplayText("TPH", Resources.GetFont(Resources.FontResources.NinaB), colorTPH, 265, 225);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateSDState(bool activeSD)
        {
            GT.Color colorSD = activeSD ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 300, 225, 20, 20);
            display.SimpleGraphics.DisplayText("SD", Resources.GetFont(Resources.FontResources.NinaB), colorSD, 300, 225);
            display.SimpleGraphics.Redraw();
        }

        public static void UpdateMouseState(bool activeMouse)
        {
            GT.Color colorMouse = activeMouse ? GT.Color.Green : GT.Color.Red;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 240, 225, 15, 15);
            display.SimpleGraphics.DisplayText("XY", Resources.GetFont(Resources.FontResources.NinaB), colorMouse, 240, 225);
            display.SimpleGraphics.Redraw();
        }
    }
}
