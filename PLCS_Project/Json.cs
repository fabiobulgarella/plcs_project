using System;
using Microsoft.SPOT;
using System.Text;

namespace PLCS_Project
{
    static class Json
    {
        private const String version = "2";
        private const String device_id = "FEZ_49";
        private const String project_name = "Fissure monitoring";
        private const String project_group = "FEZ 49";
        private const String project_type = "fissure";
        private const String project_description = "Sensors to measure fissure movements in both direction";
        private const String project_location = "Politecnico di Torino";
        private const double project_latitude = 45.058120;
        private const double project_longitude = 7.691776;
        private const bool project_internal = true;

        private static String measureTime;
        public static long measureTimeTicks;

        public static byte[] CreateJsonConfiguration()
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"version\": " + version);
            jsonString.Append(", \"id\": \"" + device_id);
            jsonString.Append("\", \"name\": \"" + project_name);
            jsonString.Append("\", \"group\": \"" + project_group);
            jsonString.Append("\", \"type\": \"" + project_type);
            jsonString.Append("\", \"sensors\": [ ");
            jsonString.Append("{ \"id\": 1, \"name\": \"displacement x\", \"type\": \"displacement\" }, ");
            jsonString.Append("{ \"id\": 2, \"name\": \"displacement y\", \"type\": \"displacement\" }, ");
            jsonString.Append("{ \"id\": 3, \"name\": \"temperature\", \"type\": \"temperature\" }, ");
            jsonString.Append("{ \"id\": 4, \"name\": \"pressure\", \"type\": \"pressure\" }, ");
            jsonString.Append("{ \"id\": 5, \"name\": \"humidity\", \"type\": \"humidity\" } ");
            jsonString.Append("], \"description\": \"" + project_description);
            jsonString.Append("\", \"location\": \"" + project_location);
            jsonString.Append("\", \"latitude\": " + project_latitude);
            jsonString.Append(", \"longitude\": " + project_longitude);
            jsonString.Append(", \"internal\": true");
            jsonString.Append(" }");

            return Encoding.UTF8.GetBytes(jsonString.ToString());
        }
        
        public static byte[] CreateJsonMeasurements(String x, String y, double tempC, double pressureMb, double relativeHumidity)
        {
            measureTimeTicks = DateTime.UtcNow.Ticks;
            measureTime = new DateTime(measureTimeTicks).ToString("yyyy-MM-ddTHH:mm:ss" + "+00:00");

            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"version\": " + version);
            jsonString.Append(", \"device_id\": \"" + device_id);
            jsonString.Append("\", \"iso_timestamp\": \"" + measureTime);
            jsonString.Append("\", \"measurements\": [ ");

            // Displacement X measurement
            if (x != null)
            {
                String xJson;

                if (x == "FAIL")
                    xJson = CreateSingleJsonMeasurement(1, "0", "FAIL");
                else
                    xJson = CreateSingleJsonMeasurement(1, x, "OK");
                
                jsonString.Append(xJson);
            }

            // Displacement Y measurement
            if (y != null)
            {
                String yJson;

                if (y == "FAIL")
                    yJson = CreateSingleJsonMeasurement(2, "0", "FAIL");
                else
                    yJson = CreateSingleJsonMeasurement(2, y, "OK");

                jsonString.Append(yJson);
            }

            // Temerature (C°) measurement
            if (tempC != -100)
            {
                String tempJson;

                if (tempC == -101)
                    tempJson = CreateSingleJsonMeasurement(3, "0", "OUTOFRANGE");
                else if (tempC == -102)
                    tempJson = CreateSingleJsonMeasurement(3, "0", "FAIL");
                else
                    tempJson = CreateSingleJsonMeasurement(3, tempC.ToString("F1"), "OK");
                
                jsonString.Append(tempJson);
            }

            // Pressure (Millibars) measurement
            if (pressureMb != -100)
            {
                String pressureJson;

                if (pressureMb == -101)
                    pressureJson = CreateSingleJsonMeasurement(4, "0", "OUTOFRANGE");
                else if (pressureMb == -102)
                    pressureJson = CreateSingleJsonMeasurement(4, "0", "FAIL");
                else
                    pressureJson = CreateSingleJsonMeasurement(4, pressureMb.ToString("F1"), "OK");

                jsonString.Append(pressureJson);
            }

            // Relative Humidity measurement
            if (relativeHumidity != -100)
            {
                String humidityJson;

                if (relativeHumidity == -101)
                    humidityJson = CreateSingleJsonMeasurement(5, "0", "OUTOFRANGE");
                else if (relativeHumidity == -102)
                    humidityJson = CreateSingleJsonMeasurement(5, "0", "FAIL");
                else
                    humidityJson = CreateSingleJsonMeasurement(5, relativeHumidity.ToString("F1"), "OK");

                jsonString.Append(humidityJson);
            }

            jsonString.Remove(jsonString.Length - 2, 2);
            jsonString.Append(" ] }");

            return Encoding.UTF8.GetBytes(jsonString.ToString());
        }

        private static String CreateSingleJsonMeasurement(int sensorId, String value, String status)
        {
            StringBuilder jsonString = new StringBuilder();
            jsonString.Append("{ \"sensor_id\": " + sensorId);
            jsonString.Append(", \"iso_timestamp\": \"" + measureTime);
            jsonString.Append("\", \"value\": " + value);
            jsonString.Append(", \"status\": \"" + status + "\" }, ");
            
            return jsonString.ToString();
        }

        public static byte[] ChangeTimestamps(byte[] data, long newTimestamp)
        {
            String newMeasureTime = new DateTime(newTimestamp).ToString("yyyy-MM-ddTHH:mm:ss" + "+00:00");
            StringBuilder newJson = new StringBuilder();
            String oldJson = new String(Encoding.UTF8.GetChars(data));

            int index = 0;
            int nowIndex = 0;
            while (true)
            {
                index = oldJson.IndexOf("iso_timestamp", index + 1);
                if (index == -1) break;

                newJson.Append(oldJson, nowIndex, index - nowIndex + 17);
                newJson.Append(newMeasureTime + "\"");
                nowIndex = index + 43;
            }

            newJson.Append(oldJson, nowIndex, oldJson.Length - nowIndex);

            return Encoding.UTF8.GetBytes(newJson.ToString());
        }
    }
}
