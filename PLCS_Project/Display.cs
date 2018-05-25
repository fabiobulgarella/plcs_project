using System;
using Microsoft.SPOT;
using Gadgeteer.Modules.GHIElectronics;
using GT = Gadgeteer;

namespace PLCS_Project
{
    class Display
    {
        private DisplayTE35 display;

        public Display(DisplayTE35 display)
        {
            this.display = display;
        }

        public void UpdateSensorData(double tempC, double pressureMb, double relativeHumidity)
        {
            string temp = "Temperature: " + tempC.ToString("F2") + " C°";
            string pressure = "Pressure: " + pressureMb.ToString("F2") + " mBar";
            string humidity = "Relative Humidity: " + relativeHumidity.ToString("F2") + " %";
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 0, 240, 36);
            display.SimpleGraphics.DisplayText(temp, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 0);
            display.SimpleGraphics.DisplayText(pressure, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 18);
            display.SimpleGraphics.DisplayText(humidity, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 36);
            display.SimpleGraphics.Redraw();
        }

        public void UpdateMouseData(int exceptionCounter, string position, string millimetersPosition)
        {
            string toPrint = "Exceptions raised: " + exceptionCounter;
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 54, 320, 36);                        
            display.SimpleGraphics.DisplayText(position, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 54);
            display.SimpleGraphics.DisplayText(millimetersPosition, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 72);
            display.SimpleGraphics.DisplayText(toPrint, Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 90);
            display.SimpleGraphics.Redraw();
        }

        public void UpdateDateTime()
        {
            display.SimpleGraphics.DisplayRectangle(GT.Color.Black, 0, GT.Color.Black, 0, 124, 320, 18);
            display.SimpleGraphics.DisplayText(DateTime.UtcNow.ToString(), Resources.GetFont(Resources.FontResources.NinaB), GT.Color.LightGray, 0, 124);
            display.SimpleGraphics.Redraw();
        }

    }
}
