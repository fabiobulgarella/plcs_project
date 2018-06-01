using System;
using Microsoft.SPOT;
using System.Text;

namespace PLCS_Project
{
    static class Json
    {
        private const String device_id = "FEZ_49";
        private const String project_name = "Fissure monitoring";
        private const String project_type = "fissure";
        private const String project_description = "Sensors to measure fissure movements in both direction";
        private const String project_location = "Politecnico di Torino";
        private const double project_latitude = 45.058120;
        private const double project_longitude = 7.691776;
        private const bool project_internal = true;

        private const String temperature = "temperature";
        private const String humidity = "humidity";
        private const String pressure = "pressure";
        private const String x_fissure_size = "x_fissure_size";
        private const String y_fissure_size = "y_fissure_size";

        public static byte[] CreateJsonConfiguration()
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"version\": 1, \"id\": \"");
            jsonString.Append(device_id);
            jsonString.Append("\", \"name\": \"");
            jsonString.Append(project_name);
            jsonString.Append("\", \"group\": \"");
            jsonString.Append(device_id);
            jsonString.Append("\", \"type\": \"");
            jsonString.Append(project_type);
            jsonString.Append("\", \"sensors\": [\"");
            jsonString.Append(temperature);
            jsonString.Append("\", \"");
            jsonString.Append(humidity);
            jsonString.Append("\", \"");
            jsonString.Append(pressure);
            jsonString.Append("\", \"");
            jsonString.Append(x_fissure_size);
            jsonString.Append("\", \"");
            jsonString.Append(y_fissure_size);
            jsonString.Append("\"], \"description\": \"");
            jsonString.Append(project_description);
            jsonString.Append("\", \"location\": \"");
            jsonString.Append(project_location);
            jsonString.Append("\", \"latitude\": ");
            jsonString.Append(project_latitude);
            jsonString.Append(", \"longitude\": ");
            jsonString.Append(project_longitude);
            jsonString.Append(", \"internal\": ");
            jsonString.Append(project_internal);
            jsonString.Append(" }");

            return Encoding.UTF8.GetBytes(jsonString.ToString());
        }
        
        public static byte[] CreateJsonMeasurements(String x, String y, double tempC, double pressureMb, double relativeHumidity)
        {
            String tempJson = CreateSingleJsonMeasurement(temperature, tempC.ToString("F2"));
            String humidityJson = CreateSingleJsonMeasurement(humidity, relativeHumidity.ToString("F2"));
            String pressureJson = CreateSingleJsonMeasurement(pressure, pressureMb.ToString("F2"));
            String xJson = CreateSingleJsonMeasurement(x_fissure_size, x);
            String yJson = CreateSingleJsonMeasurement(y_fissure_size, y);
                        
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"device_id\": \"");
            jsonString.Append(device_id);
            jsonString.Append("\", \"measurements\": [");
            jsonString.Append(tempJson + ",");
            jsonString.Append(humidityJson + ", ");
            jsonString.Append(pressureJson + ", ");
            jsonString.Append(xJson + ", ");
            jsonString.Append(yJson);
            jsonString.Append("]}");            

            return Encoding.UTF8.GetBytes(jsonString.ToString());
        }

        private static String CreateSingleJsonMeasurement(String sensorType, String value)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"iso_timestamp\": \"");
            jsonString.Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss" + "+00:00"));
            jsonString.Append("\", \"sensor\": \"");
            jsonString.Append(sensorType);
            jsonString.Append("\", \"value\": ");
            jsonString.Append(value);
            jsonString.Append(" }");
            
            return jsonString.ToString();
        }

        public static byte[] ChangeTimestamps(byte[] data, long newTimestamp)
        {
            StringBuilder newJson = new StringBuilder();

            String oldJson = new String(Encoding.UTF8.GetChars(data));
            int index = 0;
            int nowIndex = 0;
            while (true)
            {
                index = oldJson.IndexOf("iso_timestamp", index);
                if (index == -1) break;

                newJson.Append(oldJson, nowIndex, index - nowIndex + 17);
                newJson.Append(new DateTime(newTimestamp).ToString("yyyy-MM-ddTHH\\:mm\\:ss" + "+00:00") + "\"");
                nowIndex += index + 43;
            }

            newJson.Append(oldJson, nowIndex, oldJson.Length - nowIndex);

            return Encoding.UTF8.GetBytes(newJson.ToString());
        }
    }
}
