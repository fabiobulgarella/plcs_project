using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace PLCS_Project
{

    /// <summary>
    /// <para>Class BME280Device sets up the Bosch BME28 temperature/pressure/humidity sensor for a </para>
    /// <para>weather monitoring application.  Other modes are possible but not covered by this class.</para>
    /// <para>See the documentation for the Bosch BME280 devuce.</para>
    /// <para>.</para>
    /// <para>Sensor mode:  forced mode, 1 sample per minute</para>
    /// <para>Oversampling settings:  pressure x 1, temperature x 1, humidity x 1</para>
    /// <para>IIR filter settings:  filter off</para>
    /// <para>.</para>
    /// <para>Performance for weather setting:</para>
    /// <para>    Current consumption: 0.16uA</para>
    /// <para>    RMS Noise:           3.3Pa/30cm, 0.07% RH</para>
    /// <para>    Data oputput rate:   1/60 secs</para>
    /// </summary>
    public class BME280Device : IDisposable
    {

        #region StatusFlags enumeration (PUBLIC)
        /// <summary>
        /// <para>Enumeration StatusFlags represents the states of statis that the BME280</para>
        /// <para>device reports.</para>
        /// </summary>
        [Flags]
        public enum StatusFlags { Idle = 0, NVRamUpdate = 1, Measuring = 8 };
        #endregion

        #region PowerMode enumeration (PUBLIC)
        /// <summary>
        /// <para>Enumeration PowerMode represents the states of currently power mode set on BME280.</para>
        /// </summary>
        [Flags]
        public enum PowerMode { Sleep = 0, Forced = 1, Normal = 3 };
        #endregion



        #region Constants

        /// <summary>kFeetIncrementForMillibarLookup = 200</summary>
        private const int kFeetIncrementForMillibarLookup = 200;

        /// <summary>kMetersPerFoot =  0.3048</summary>
        private const double kMetersPerFoot = 0.3048F;

        /// <summary>kFeetPerMeter = 1.0F / kMetersPerFoot</summary>
        private const double kFeetPerMeter = 1.0F / kMetersPerFoot;

        /// <summary>kMinAltitudeMeters = -308F</summary>
        private const double kMinAltitudeMeters = -308F;
        /// <summary>kMaxAltitudeMeters = 3080F</summary>
        private const double kMaxAltitudeMeters = 3080F;
        /// <summary>kMinAltitudeFeet = -1000F</summary>
        private const double kMinAltitudeFeet = -1000F;
        /// <summary>kMaxAltitudeFeet = 10000F</summary>
        private const double kMaxAltitudeFeet = 10000F;
        /// <summary>kDefaltAltitudeFeet = 0F</summary>
        private const double kDefaltAltitudeFeet = 0F;






        // http://www.engineeringtoolbox.com/barometers-elevation-compensation-d_1812.html
        private readonly short[] kMillibarCorrections = new short[]  { 
            /* Altitude    Corrextion */
            /*-1000 */     -37, 
            /*-800*/       -30, 
            /*-600 */      -22, 
            /*-400 */      -15,
            /*-200 */       -7,
            /* 0 */          0, 
            /* 200 */        7,
            /* 400 */       15,
            /* 600 */       22,
            /* 800 */       29,
            /* 1000 */      36, 
            /* 1200 */      43, 
            /* 1400 */      50,
            /* 1600 */      57,
            /* 1800 */      64,
            /* 2000 */      71,
            /* 2200 */      78,
            /* 2400 */      85,
            /* 2600 */      92,
            /* 2800 */      98,
            /* 3000 */     105,
            /* 3200 */     112,
            /* 3400 */     118,
            /* 3600 */     125,
            /* 3800 */     132,
            /* 4000 */     138,
            /* 4200 */     145,
            /* 4400 */     151,
            /* 4600 */     157,
            /* 4800 */     164,
            /* 5000 */     170,
            /* 5200 */     176,
            /* 5400 */     183,
            /* 5600 */     189,
            /* 5800 */     195,
            /* 6000 */     201,
            /* 6200 */     207,
            /* 6400 */     213,
            /* 6600 */     219,
            /* 6800 */     225,
            /* 7000 */     231,
            /* 7200 */     237,
            /* 7400 */     243,
            /* 7600 */     249,
            /* 7800 */     255,
            /* 8000 */     261,
            /* 8200 */     266, 
            /* 8400 */     272,
            /* 8600 */     278,
            /* 8800 */     283,
            /* 9000 */     289,
            /* 9200 */     295,
            /* 9400 */     300,
            /* 9600 */     306,
            /* 9800 */     311,
            /*10000 */     316 };



        /// <summary>kKpaToInchesHg = 0.2953F</summary>
        private const float kKpaToInchesHg = 0.2953F;

        /// <summary>kMillibarsPerPa = 100F</summary>
        private const float kMillibarsPerPa = 100F;

        /// <summary>kMillibarsPerKpa = 10F</summary>
        private const float kMillibarsPerKpa = 0.1F;

        /// <summary>kRegisterIDAddress = 0xD0</summary>
        private const byte kRegisterIDAddress = 0xD0;
        /// <summary>kValidIDByte = 0x60</summary>
        private const byte kValidIDByte = 0x60;

        /// <summary>kRegisterReset = 0xE0</summary>
        private const byte kRegisterReset = 0xE0;
        /// <summary>kResetValue = 0xB6</summary>
        private const byte kResetValue = 0xB6;

        /// <summary>kRegisterStatus = 0xF3</summary>
        private const byte kRegisterStatus = 0xF3;
        /// <summary>byte kStatusMask = 0x09</summary>
        private const byte kStatusMask = 0x09;
        /// <summary>byte kPowerModeMask = 0x03</summary>
        private const byte kPowerModeMask = 0x03;

        /// <summary>kRegisterHumidityControl = 0xF2</summary>
        private const byte kRegisterHumidityControl = 0xF2;
        /// <summary>kHumidityOversample1X = 0x01</summary>
        private const byte kHumidityOversample1X = 0x01;
        /// <summary>kHumidityControlValue = kHumidityOversample1X</summary>
        private const byte kHumidityControlValue = kHumidityOversample1X;

        /// <summary>kRegisterMeasurementControl = 0xF4</summary>
        private const byte kRegisterMeasurementControl = 0xF4;

        // Temperature Oversampling
        // 0   0   0                    No sample
        // 0   0   1                    x1
        // 0   1   0                    x2
        // 0   1   1                    x4
        // 1   0   0                    x8
        // 1   0   1                    x16
        // 1   1   1                    x16 <- not a mustake
        //
        // Pressure Oversamoliuing
        //              0   0   0                    No sample
        //              0   0   1                    x1
        //              0   1   0                    x2
        //              0   1   1                    x4
        //              1   0   0                    x8
        //              1   0   1                    x16
        //              1   1   1                    x16 <- not a mustake
        //
        // Mode
        //                          0    0           Sleep mode
        //                          0    1           Forced mode
        //                          1    0           Forced mode <- not a mistake
        //                          1    1           Normal mode
        // ================================
        // 0   0   1   0   0   1    0    1           Temperature x1, pressure x 1, forced mode (weather monitoring)

        /// <summary>kWeatherMonitoringConfig = 0x25</summary>
        private const byte kWeatherMonitoringConfig = 0x25;

        /// <summary>kRegisterConfig = 0xF5</summary>
        private const byte kRegisterConfig = 0xF5;

        /// <summary>kConfigData = 0x00</summary>
        private const byte kConfigData = 0x00;  // Default for weather monitoring.

        /// <summary>kRegisterADCDataStart = 0xF7</summary>
        private const byte kRegisterADCDataStart = 0xF7;

        /// <summary>kRegisterAdcDataEnd = 0xFE</summary>
        private const byte kRegisterAdcDataEnd = 0xFE;

        /// <summary>kADCDataCount = kRegisterAdcDataEnd - kRegisterADCDataStart + 1</summary>
        private const byte kADCDataCount = kRegisterAdcDataEnd - kRegisterADCDataStart + 1;
        /// <summary>kRegisterCalDataSection88Start = 0x88</summary>
        private const byte kRegisterCalDataSection88Start = 0x88;
        /// <summary>kRegisterCalDataSection88End = 0xA1</summary>
        private const byte kRegisterCalDataSection88End = 0xA1;
        /// <summary>kCalDataSection88Length = kRegisterCalDataSection88End - kRegisterCalDataSection88Start +1</summary>
        private const byte kCalDataSection88Length = kRegisterCalDataSection88End - kRegisterCalDataSection88Start + 1;
        /// <summary>kRegisterCalDataSectionE1Start = 0xE1</summary>
        private const byte kRegisterCalDataSectionE1Start = 0xE1;
        /// <summary>kRegisterCalDataSectionE1End = 0xE7</summary>
        private const byte kRegisterCalDataSectionE1End = 0xE7;
        /// <summary>kCalDataSectionE1Length = kRegisterCalDataSectionE1End - kRegisterCalDataSectionE1Start + 1</summary>
        private const byte kCalDataSectionE1Length = kRegisterCalDataSectionE1End - kRegisterCalDataSectionE1Start + 1; // 6 +1
        /// <summary>kMaxFrequencyKhz = 400</summary>
        private const ushort kMaxFrequencyKhz = 400;
        /// <summary>kMinFrequencyKhz = 10</summary>
        private const ushort kMinFrequencyKhz = 10;
        /// <summary>kminValidAddress = 0x76</summary>
        private const ushort kMinValidAddress = 0x76;
        /// <summary>kMaxValidAddress = 0x77</summary>
        private const ushort kMaxValidAddress = 0x77;

        /// <summary>kMaxExecuteTry = 3</summary>
        private const int kMaxExecuteTry = 3;

        /// <summary>kI2cBusTransactionFailed = "I2C bus transaction failed."</summary>
        private const string kI2cBusTransactionFailed = "I2C bus transaction failed.";

        #endregion

        #region AltitudeInFeet property (PUBLIC)
        /// <summary>
        /// Property AltitudeInFeet sets the altitude of the BME280 Device in feet.
        /// </summary>
        public double AltitudeInFeet
        {
            get { return _altitudeInFeet; }
            set
            {
                if (value < kMinAltitudeFeet || value > kMaxAltitudeFeet)
                {
                    throw new BME280Exception(
                        "Altitude (feet) is outside range of " +
                        kMinAltitudeFeet.ToString() +
                        " to " +
                        kMinAltitudeFeet.ToString() + ".");
                }
                _altitudeInFeet = value;
                _altitudeInMeters = _altitudeInFeet * kMetersPerFoot;
            }
        }
        #endregion

        #region AltitudeInMeters property (PUBLIC)
        /// <summary>
        /// Property AltitudeInMeters gets/sets the altitude (in meters) of the BME280 Device.
        /// </summary>
        public double AltitudeInMeters
        {
            get { return _altitudeInMeters; }
            set
            {
                if (value < kMinAltitudeMeters || value > kMaxAltitudeMeters)
                {
                    throw new BME280Exception(
                        "Altitude (meters) is outside range of " +
                        kMinAltitudeMeters.ToString() +
                        " to " +
                        kMinAltitudeMeters.ToString() + ".");
                }
                _altitudeInMeters = value;
                _altitudeInFeet = _altitudeInMeters * kFeetPerMeter;
            }
        }
        #endregion

        #region RawTemperatureADC property (private)
        /// <summary>
        /// <para>Property RawTemperatureADC gets/sets the internal representation of the </para>
        /// <para>temperature data stored at registers 0xFA, oxFB, and 0xFC in the BME280</para>
        /// <para>device.</para>
        /// <para>.</para>
        /// <para>o Register 0xFA (MSB) bits [7:0] contain bits [19:12] of RawTemperatureADC</para>
        /// <para>o Register 0xFB (LSB) bits [7:0] contain bits [11:4] of RawTemperatureADC</para>
        /// <para>o Register 0xFC (XLSB) bits [7:4] contian bits [3:0] of RawTemperatureADC</para>
        /// </summary>
        private Int32 RawTemperatureADC { get; set; }
        #endregion

        #region RawPressureADC property (private)
        /// <summary>
        /// <para>Property RawPressureADC gets/sets the internal representation of the </para>
        /// <para>pressure data stored at registers 0xf7, 0xF8, and 0xF9 in the BMD280</para>
        /// <para>device.</para>
        /// <para>.</para>
        /// <para>o Register 0xF7 (MSB) bits [7:0] contain bits [19:12] of RawPressureADC</para>
        /// <para>o Register 0xF8 (LSB) bits [7:0] contain bits [11:4] of RawPressureADC</para>
        /// <para>o Register 0xF9 (XLSB) bits [7:4] contian bits [3:0] of RawPressureADC</para> 
        /// </summary>
        private Int32 RawPressureADC { get; set; }
        #endregion

        #region RawHumidityADC property (private)
        /// <summary>
        /// <para>Property RawHumidityADC gets/sets the internal representation of the </para>
        /// <para>humidity data stored at registers 0xf7, 0xF8, and 0xF9 in the BMD280</para>
        /// <para>device.</para>
        /// <para>.</para>
        /// <para>o Register 0xFD (MSB) bits [7:0] contain bits [15:8] of RawHumidityADC</para>
        /// <para>o Register 0xFE (LSB) bits [7:0] contain bits [7:0] of RawHumidityADC</para>
        /// </summary>
        private Int32 RawHumidityADC { get; set; }
        #endregion

        #region Device property (private)
        /// <summary>
        /// <para>Property Device represents the underlying I2CDevice that represents the BME280Device </para>
        /// <para>object.</para>
        /// </summary>
        private I2CDevice Device { get; set; }
        #endregion

        #region TemperatureFine property (private)
        /// <summary>
        /// <para>Property TemperatureFine represents a temperature value used in the calculations </para>
        /// <para>pf pressure and humidity.</para>
        /// </summary>
        private int TemperatureFine { get; set; }
        #endregion

        #region ReportPressureInMillibars property (PUBLIC)
        /// <summary>
        /// <para>Property ReportPressureInMillibars gets/sets the pressure reporting mode. Value </para>
        /// <para>true indicates a millibar unit selection; otherwise inches of Hg unit selection.</para>
        /// </summary>
        public bool ReportPressureInMillibars { get; set; }
        #endregion

        #region ReportTemperatureInCentigrade property (PUBLIC)
        /// <summary>
        /// <para>Property ReportTemperatureInCentigrade gets/sets the temperature reporting mode. Value</para>
        /// <para>true represents degrees Centigrade units; otherwise degrees Farenheit unit selection.</para>
        /// </summary>
        public bool ReportTemperatureInCentigrade { get; set; }
        #endregion

        #region Member Variables
        ushort dig_T1;
        short dig_T2, dig_T3;
        ushort dig_P1;
        short dig_P2, dig_P3, dig_P4, dig_P5, dig_P6, dig_P7, dig_P8, dig_P9;
        byte dig_H1;
        short dig_H2;
        byte dig_H3;
        short dig_H4, dig_H5;
        sbyte dig_H6;

        private double _altitudeInFeet;
        private double _altitudeInMeters;

        #endregion

        #region #ctor (PUBLIC)
        /// <summary>
        /// <para>Constructor BME280Device creates a BME280Device object that represents a single</para>
        /// <para>Bosch BME280 environmental sensor.</para>
        /// <para>.</para>
        /// <para>The constructor does the following:</para>
        /// <para>o Validates the 7-bit slave address</para>
        /// <para>o Validates the clock frequency</para>
        /// <para>o Creates the underlying I2CDevice object</para>
        /// <para>o Communicates with the device to validate the device ID</para>
        /// <para>o Gets and stores the device unique calibration data for this BME280Device</para>
        /// </summary>
        /// <param name="slaveAddress">The 7-bit slave address</param>
        /// <param name="frequencykHz">The I2C communication frequency in kHz</param>
        /// <exception cref="BME280Exception"></exception>
        public BME280Device(ushort slaveAddress, int frequencykHz = 100)
        {
            Device = null;

            if (slaveAddress < kMinValidAddress || slaveAddress > kMaxValidAddress)
                throw new BME280Exception("Slave address out of range of 0x76-0x77.");
            if (frequencykHz < kMinFrequencyKhz || frequencykHz > kMaxFrequencyKhz)
                throw new BME280Exception("Clock frequency out of range.");

            try
            { 
                Device = new I2CDevice(new I2CDevice.Configuration(slaveAddress, frequencykHz));

                // Validate that this is a BME280 chip
                ValidateChipID();
                // Reset Chip
                ChipSoftReset();
                // Make a one-time read of the calibration data
                GetCalibrationData();
            }
            catch (Exception ex)
            {
                if (Device != null)
                    Device.Dispose();

                if (ex is BME280Exception)
                    throw ex;
                else
                    throw new BME280Exception("I2CDevice constructor failed.", ex);
            }

            // Initialize the altitude (through feet)
            AltitudeInFeet = 0F;

            // Initialize the reporting to report pressure in millibars and temperature in Centigrade
            ReportPressureInMillibars = true;
            ReportTemperatureInCentigrade = true;
        }
        #endregion

        #region ValidateChipID method (private)
        /// <summary>
        /// <para>Method ValidateChipID validates that the BME280 device is </para>
        /// <para>present, responding, and reports the correct chip ID. </para>
        /// </summary>
        /// <exception cref="BME280Exception"></exception>
        private void ValidateChipID()
        {
            byte[] chipId = { 0x00 };
            byte[] chipIDRegister = { kRegisterIDAddress };
            I2CDevice.I2CTransaction[] transactions = 
            { 
                // Write the chipID register 
                I2CDevice.CreateWriteTransaction(chipIDRegister),  
                // Read the 1-byte chipID   
                I2CDevice.CreateReadTransaction(chipId)             
            };
            try { Device.Transact(transactions, 1000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }
            if (chipId[0] != kValidIDByte)
                throw new BME280Exception("Invalid BME280 chip ID.");
        }
        #endregion

        #region ChipSoftReset method (private)
        /// <summary>
        /// <para>Method ChipSoftReset reset BME280 device</para>
        /// </summary>
        /// <exception cref="BME280Exception"></exception>
        private void ChipSoftReset()
        {
            byte[] reset = { kRegisterReset, kResetValue };
            I2CDevice.I2CTransaction[] transactions = 
            { 
                // Reset BME280 chip
                I2CDevice.CreateWriteTransaction(reset)  
            };
            try { Device.Transact(transactions, 1000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }
        }
        #endregion

        #region GetCalibrationData method (private)
        /// <summary>
        /// <para>Method GetCalibrationData gets the device-unique factory-set calibration data</para>
        /// <para>for values for temperature, pressure, and humidity.</para>
        /// <para>.</para>
        /// <para>The calibration values are read-only and do not change for the life of the device.</para>
        /// </summary>
        /// <remarks>
        /// <para>The calibration values are stored as member variables in the BME280Device class.</para>
        /// </remarks>
        /// <exception cref="BME280Exception"></exception>
        private void GetCalibrationData()
        {
            byte[] calDataPart1Start = { kRegisterCalDataSection88Start };
            byte[] calDataPart2Start = { kRegisterCalDataSectionE1Start };
            byte[] calDataPart1 = new Byte[kCalDataSection88Length];
            byte[] calDataPart2 = new Byte[kCalDataSectionE1Length];
            I2CDevice.I2CTransaction[] transactions = 
            {
                // Set register start address for a read at calDataPart1Start
                I2CDevice.CreateWriteTransaction(calDataPart1Start),
                // Read sizeof(calDataPart1) 
                I2CDevice.CreateReadTransaction(calDataPart1), 

                // Set register start address for a read at calDataPart2Start
                I2CDevice.CreateWriteTransaction(calDataPart2Start), 
                // Read sizeof(calDataPart2) 
                I2CDevice.CreateReadTransaction(calDataPart2)       
            };
            try { Device.Transact(transactions, 2000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }

            // Initialize index for the start of the calDataPart1 buffer
            int index = 0;
            // Capture temperature calibration variables
            dig_T1 = calDataPart1.ExtractUShort(ref index);  // data at register 0x88[7:0]/0x89[15:8] to dig_T1            
            dig_T2 = calDataPart1.ExtractShort(ref index);   // data at register 0x8A[7:0]/0x8B[15:8] to dig_T2              
            dig_T3 = calDataPart1.ExtractShort(ref index);   // data at register 0x8C[7:0]/0x8D[15:8] to dig_T2
            // Capture pressure calibration variables
            dig_P1 = calDataPart1.ExtractUShort(ref index);  // data at register 0x8E[7:0]/0x8F[15:8] to dig_P1
            dig_P2 = calDataPart1.ExtractShort(ref index);   // data at register 0x90[7:0]/0x91[15:8] to dig_P2   
            dig_P3 = calDataPart1.ExtractShort(ref index);   // data at register 0x92[7:0]/0x93[15:8] to dig_P3
            dig_P4 = calDataPart1.ExtractShort(ref index);   // data at register 0x94[7:0]/0x95[15:8] to dig_P4
            dig_P5 = calDataPart1.ExtractShort(ref index);   // data at register 0x96[7:0]/0x97[15:8] to dig_P5
            dig_P6 = calDataPart1.ExtractShort(ref index);   // data at register 0x98[7:0]/0x99[15:8] to dig_P6
            dig_P7 = calDataPart1.ExtractShort(ref index);   // data at register 0x9A[7:0]/0x9B[15:8] to dig_P7
            dig_P8 = calDataPart1.ExtractShort(ref index);   // data at register 0x9C[7:0]/0x9D[15:8] to dig_P8
            dig_P9 = calDataPart1.ExtractShort(ref index);   // data at register 0x9E[7:0]/0x9F[15:8] to dig_P9   
            index += sizeof(byte);                           // skip the data at register 0xA0 (unused)

            // Capture humidity calibration variables
            dig_H1 = calDataPart1[index++];                  // data at register 0xA1[7:0]

            // reset the index to use the calDataPart2 buffer
            index = 0;                                          // reseet index for calDataPart2 buffer
            dig_H2 = calDataPart2.ExtractShort(ref index);      // data at register 0xE1[7:0]/0xE2[15:8]
            dig_H3 = calDataPart2[index++];                     // data at register 0xE3[7:0]


            // Data at registers 0xE4-0xE6 are spread between two calibration 
            // variables so get the values separately
            var E4 = (UInt32)calDataPart2[index++];            // data at register 0xE4[7:0]
            var E5 = (UInt32)calDataPart2[index++];            // data at register 0xE5[7:0]
            var E6 = (UInt32)calDataPart2[index++];            // data at register 0xE6[7:0]

            // 12-bit Calibration variable dig_H4 exists in the following bytes and bits:
            // E4 [11:4] and E5[0:3] 
            // Mapping:
            // 15   14   13   12   11   10   09   08   07   06   06   04   03   02   01   00
            // X    X    X    X    E4-7 E4-6 E4-5 E4-4 E4-3 E4-2 E4-1 E4-0 E5-3 E5-2 E5-1 E5-0  
            dig_H4 = (short)(E4 << 4);                        // E4[7:0] to dig_H4[11:4]
            dig_H4 |= (short)(E5 & 0x0F);                     // E5[3:0] to dig_H4[3:0] 

            // 12-bit Calibration variable dig_H5 exists in the in the following bytes and bits:
            // TODO
            // E5[7:4] and E6 [11:4] comprise the dig_h4 variable
            // Mapping:
            // 15   14   13   12   11   10   09   08   07   06   06   04   03   02   01   00
            // X    X    X    X    E6-7 E6-6 E6-5 E6-4 E6-3 E6-2 E5-1 E6-0 E5-7 E5-6 E5-5 E5-4   
            dig_H5 = (short)(E6 << 4);                       // E6[7:0] to dig_H5[11:4]
            dig_H5 |= (short)(E5 >> 4);                      // E5[7:4] to dig_H5[3:0]
            dig_H6 = (sbyte)calDataPart2[index++];           // data at register 0xE7[7:0] to dig_H6
        }
        #endregion

        #region GetRawADCData method (private)
        /// <summary>
        /// <para>Method GetRawADCData gets the raw data from the device int </para> 
        /// <para>the RawPressureADC, RawTemperatureADC, and the RawHumidityADC</para>
        /// <para>properties.</para>
        /// </summary>
        /// <exception cref="BME280Exception"></exception>
        private void GetRawADCData()
        {
            // The raw data for pressure, temperature and humidity are mapped 
            // into the following BME 280 registers
            // 0xF7 pressure      MSB[7:0]
            // 0xF8 pressure      LSB[7:0]  
            // 0xF9 pressure     XLSB[7:4] bits [3:0] are zero
            // 0xFA temperature   MSB[7:0]
            // 0xFB temperature   LSB[7:0]
            // 0xFC temperature  XLSB[7:4] bits [3:0] are zero
            // 0xFD humidity      MSB[7:0] 
            // 0xFE humitidy      LSB[7:0] 

            // Raw temperature and raw pressure values are 20 bit precision in 24 bits
            // Raw temperatiure and raw pressure values must be shifted right 4 places 
            // after reading and converting from big endian to little endian.
            // Raw humidity values are 16 bit precision.

            // Buffer for command (register address)
            byte[] rawDataStart = { kRegisterADCDataStart };
            // Buffer for data
            byte[] rawDataBuffer = new Byte[kADCDataCount];

            // Create the I2C transaction sequence
            I2CDevice.I2CTransaction[] transactions = 
            {
                // Write the register start address for a read at kRegisterADCDataStart
                I2CDevice.CreateWriteTransaction(rawDataStart),
                // Read rawDataBuffer.Length bytes
                I2CDevice.CreateReadTransaction(rawDataBuffer), 
            };
            try { Device.Transact(transactions, 2000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }

            // ###########################################################
            // Get raw pressure
            // ###########################################################
            int index = 0;
            // Read 3 bytes, convert to little endian and shift result right by 4
            RawPressureADC = rawDataBuffer.ExtractIntFromBigEndianBuffer(ref index, 3, 4);

            // ###########################################################
            // Get raw temperature
            // ###########################################################
            // Read 3 bytes, convert to little endian and shift result right by 4
            RawTemperatureADC = rawDataBuffer.ExtractIntFromBigEndianBuffer(ref index, 3, 4);

            // ###########################################################
            // Get raw humidity
            // ###########################################################
            // Read 2 bytes, convert to little endian do not shift result
            RawHumidityADC = rawDataBuffer.ExtractIntFromBigEndianBuffer(ref index, 2, 0);

        }
        #endregion

        #region Measure method (PUBLIC)
        /// <summary>
        /// <para>Method Measure returns the temperature, pressure, and relitive humidity percent.</para>
        /// <para>The units of the temperature and pressure measurement depend on the settings of </para>
        /// <para>the ReportPressureInMillibars property and the ReportTemperatureInCentigrade property.</para>
        /// <para>The options for pressure consist of Millibars or inches Hg.  The options for temperature</para>
        /// <para>consist of Centigrade or Farenheight.</para>
        /// </summary>
        /// <param name="temperature">The returned temperature value</param>
        /// <param name="pressure">The returned pressure value</param>
        /// <param name="relativeHumidityPercent">The returned humidity value</param>
        /// <exception cref="BME280Exception"></exception>
        public void Measure(out double temperature, out double pressure, out double relativeHumidityPercent)
        {
            TriggerMeasurements();
            while (GetStatus() != StatusFlags.Idle) Thread.Sleep(40);
            GetRawADCData();
            temperature = ReportTemperatureInCentigrade ?
                GetCompensatedTemperatureC() :
                GetCompensatedTemperatureF();
            pressure = ReportPressureInMillibars ?
                GetCompensatedPressureInMillibars() :
                GetCompensatedPressureInInchesHg();
            relativeHumidityPercent = GetCompensatedHumidity();
        }
        #endregion

        #region TriggerMeasurements method (private)
        /// <summary>
        /// <para>Method TriggerMeasurements configures the BME280 device to make a forced single</para>
        /// <para>measurement cycle for temperature, pressure and humidity, suitable for weather</para>
        /// <para>monitoring. The measurements results are posted by the DME280 device from shadow</para>
        /// <para>registers to the raw ADC registers readable by the I2C interface.</para>
        /// </summary>
        /// <exception cref="BME280Exception"></exception>
        private void TriggerMeasurements()
        {
            byte[] config = { kRegisterConfig, kConfigData };
            byte[] ctrl_hum = { kRegisterHumidityControl, kHumidityOversample1X };
            byte[] ctrl_meas = { kRegisterMeasurementControl, kWeatherMonitoringConfig };
            I2CDevice.I2CTransaction[] transactions = 
            {
                // Write the register kRegisterConfig to set confiuguration
                // 1 byte transfer
                I2CDevice.CreateWriteTransaction(config),
                // Write the register kRegisterHumidityControl to set humidty oversampling
                // 1 byte transfer
                I2CDevice.CreateWriteTransaction(ctrl_hum),
                // Write the register kRegisterMeasurementControl to set pressure and temperature oversampling and forced mode
                // 1 byte transfer
                I2CDevice.CreateWriteTransaction(ctrl_meas),
            };
            try { Device.Transact(transactions, 1000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }
        }
        #endregion

        #region GetStatus method (private)
        /// <summary>
        /// <para>Method GetStatus reads the BME280 device status register and returns the status as a </para>
        /// <para>StatusFlags enumeration.</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetStatus reads the BME280 device status register and returns the status as a </para>
        /// <para>StatusFlags enumeration.</para>
        /// </returns>
        /// <exception cref="BME280Exception"></exception>
        private StatusFlags GetStatus()
        {
            byte[] statusStart = { kRegisterStatus };
            byte[] status = { 0xFF };
            I2CDevice.I2CTransaction[] transactions = 
            {
                // Write the register start address for a read at kRegisterStatus 
                I2CDevice.CreateWriteTransaction(statusStart),
                // Read the value of the status
                I2CDevice.CreateReadTransaction(status),
            };
            try { Device.Transact(transactions, 1000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }
            status[0] = (byte)(status[0] & kStatusMask);
            return (StatusFlags)status[0];
        }
        #endregion

        #region GetPowerMode method (private)
        /// <summary>
        /// <para>Method GetPowerMode reads the BME280 device ctrl_meas register and returns power mode</para>
        /// </summary>
        /// <returns>
        /// <para>PowerMode enumeration</para>
        /// </returns>
        /// <exception cref="BME280Exception"></exception>
        private PowerMode GetPowerMode()
        {
            byte[] powerRegister = { kRegisterMeasurementControl };
            byte[] powerMode = new byte[1];
            I2CDevice.I2CTransaction[] transactions = 
            {
                // Write the ctrl_meas register address for reading it's value 
                I2CDevice.CreateWriteTransaction(powerRegister),
                // Read the value of power mode
                I2CDevice.CreateReadTransaction(powerMode)
            };
            try { Device.Transact(transactions, 1000); }
            catch (Exception ex) { throw new BME280Exception(kI2cBusTransactionFailed, ex); }
            powerMode[0] = (byte)(powerMode[0] & kPowerModeMask);
            return (PowerMode)powerMode[0];
        }
        #endregion

        #region Dispose method (PUBLIC)
        /// <summary>
        /// <para>Method Dispose disposes the underlying I2C device, Device.</para>
        /// </summary>
        public void Dispose()
        {
            if (Device != null) Device.Dispose();
            Device = null;
        }
        #endregion

        #region GetCompensatedTemperatureF method (private)
        /// <summary>
        /// <para>Method GetCompensatedTemperatureF returns the compensated temperature</para> 
        /// <para>in Degrees Farenheit.</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedTemperatureF returns the compensated temperature</para> 
        /// <para>in Degrees Farenheit.</para>
        /// </returns>
        private double GetCompensatedTemperatureF()
        {
            var temp = (GetCompensatedTemperatureC() * 9F / 5F) + 32F;
            return temp;
        }
        #endregion

        #region GetCompensatedTemperatureC method (private)
        /// <summary>
        /// <para>Method GetCompensatedTemperatureC uses the dig_T* temperature compensation factors</para>
        /// <para>and the RawTemperatureADC value to return a compensated temperature in Centigrade.</para>
        /// <para>. </para>
        /// <para>It also sets the TemperatureFine property used in the calculation of pressure and humidity.</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedTemperatureC uses the dig_T* temperature compensation factors</para>
        /// <para>and the RawTemperatureADC value to return a compensated temperature in Centigrade.</para>
        /// </returns>
        /// <remarks>
        /// <para>Always call GetCompensatedTemperatureC before calling any method that returns pressure</para>
        /// <para>or humidity.</para>
        /// </remarks>
        private double GetCompensatedTemperatureC()
        {
            double var1;
            double var2;
            double temperature;
            double temperature_min = -40;
            double temperature_max = 85;

            var1 = ((double)RawTemperatureADC) / 16384.0 - ((double)dig_T1) / 1024.0;
            var1 = var1 * ((double)dig_T2);
            var2 = (((double)RawTemperatureADC) / 131072.0 - ((double)dig_T1) / 8192.0);
            var2 = (var2 * var2) * ((double)dig_T3);
            TemperatureFine = (int)(var1 + var2);
            temperature = (var1 + var2) / 5120.0;

            if (temperature < temperature_min)
                temperature = temperature_min;
            else if (temperature > temperature_max)
                temperature = temperature_max;

            return temperature;
        }
        #endregion

        #region GetCompensatedPressureInPa method (private)
        /// <summary>
        /// <para>Method GetCompensatedPressureInPa uses the dig_P* compensation factors, the TemperaturFine</para>
        /// <para>property value, and the RawPressureADC value to calculate an return a compensated pressure </para>
        /// <para>in Pa.</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedPressureInPa uses the dig_P* compensation factors, the TemperaturFine</para>
        /// <para>property value, and the RawPressureADC value to calculate an return a compensated pressure </para>
        /// <para>in Pa.</para>
        /// </returns>
        private double GetCompensatedPressureInPa()
        {
            double var1;
            double var2;
            double var3;
            double pressure;
            double pressure_min = 30000.0;
            double pressure_max = 110000.0;

            var1 = ((double)TemperatureFine / 2.0) - 64000.0;
            var2 = var1 * var1 * ((double)dig_P6) / 32768.0;
            var2 = var2 + var1 * ((double)dig_P5) * 2.0;
            var2 = (var2 / 4.0) + (((double)dig_P4) * 65536.0);
            var3 = ((double)dig_P3) * var1 * var1 / 524288.0;
            var1 = (var3 + ((double)dig_P2) * var1) / 524288.0;
            var1 = (1.0 + var1 / 32768.0) * ((double)dig_P1);
            /* avoid exception caused by division by zero */
            if (var1 != 0.0)
            {
                pressure = 1048576.0 - (double)RawPressureADC;
                pressure = (pressure - (var2 / 4096.0)) * 6250.0 / var1;
                var1 = ((double)dig_P9) * pressure * pressure / 2147483648.0;
                var2 = pressure * ((double)dig_P8) / 32768.0;
                pressure = pressure + (var1 + var2 + ((double)dig_P7)) / 16.0;

                if (pressure < pressure_min)
                    pressure = pressure_min;
                else if (pressure > pressure_max)
                    pressure = pressure_max;
            }
            else
            { /* Invalid case */
                pressure = pressure_min;
            }

            return pressure;
        }
        #endregion



        #region GetCompensatedPressureInMillibars method (private)
        /// <summary>
        /// <para>Method GetCompensatedPressureInMillibars returns the compensated pressure</para>
        /// <para>in millibars.</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedPressureInMillibars returns the compensated pressure</para>
        /// <para>in millibars.</para>
        /// </returns>
        private double GetCompensatedPressureInMillibars()
        {
            var result = GetCompensatedPressureInPa() / kMillibarsPerPa;
            result += (double)GetMillibarCorrectionForAltitude();
            return result;
        }
        #endregion

        #region GetCompensatedPressureInInchesHg method (private)
        /// <summary>
        /// <para>Method GetCompensatedPressureInInchesHg returns the compensated pressure</para>
        /// <para>im mm of Hg/</para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedPressureInInchesHg returns the compensated pressure</para>
        /// <para>im mm of Hg/</para>
        /// </returns>
        double GetCompensatedPressureInInchesHg()
        {
            var result = GetCompensatedPressureInMillibars() * kMillibarsPerKpa * kKpaToInchesHg;
            return result;
        }
        #endregion

        #region GetCompensatedHumidity method (private)
        /// <summary>
        /// <para>Method GetCompensatedHumidity uses the dig_H* compensation parameters, the TemperatureFine</para>
        /// <para>property value and the RawHumidityADC value to calculate and return a compensated relative </para>
        /// <para>humidity. </para>
        /// </summary>
        /// <returns>
        /// <para>Method GetCompensatedHumidity uses the dig_H* compensation parameters, the TemperatureFine</para>
        /// <para>property value and the RawHumidityADC value to calculate and return a compensated relative </para>
        /// <para>humidity. </para>
        /// </returns>
        double GetCompensatedHumidity()
        {
            double humidity;
            double humidity_min = 0.0;
            double humidity_max = 100.0;
            double var1;
            double var2;
            double var3;
            double var4;
            double var5;
            double var6;

            var1 = ((double)TemperatureFine) - 76800.0;
            var2 = (((double)dig_H4) * 64.0 + (((double)dig_H5) / 16384.0) * var1);
            var3 = RawHumidityADC - var2;
            var4 = ((double)dig_H2) / 65536.0;
            var5 = (1.0 + (((double)dig_H3) / 67108864.0) * var1);
            var6 = 1.0 + (((double)dig_H6) / 67108864.0) * var1 * var5;
            var6 = var3 * var4 * (var5 * var6);
            humidity = var6 * (1.0 - ((double)dig_H1) * var6 / 524288.0);

            if (humidity > humidity_max)
                humidity = humidity_max;
            else if (humidity < humidity_min)
                humidity = humidity_min;

            return humidity;
        }
        #endregion

        private int GetMillibarCorrectionForAltitude()
        {
            // Shift to positive area.
            var altitudeInFeet = (int)AltitudeInFeet;
            var negativeLookupOffset = (int)(kMinAltitudeFeet);
            altitudeInFeet += System.Math.Abs(negativeLookupOffset);
            var altitudeLookupIndex = altitudeInFeet / kFeetIncrementForMillibarLookup;
            var wholeMillibars = kMillibarCorrections[altitudeLookupIndex];
            if (altitudeLookupIndex < kMillibarCorrections.Length - 1)
            {
                var extrapolationDistance = altitudeInFeet % kFeetIncrementForMillibarLookup;
                var nextWholeMillibars = kMillibarCorrections[altitudeLookupIndex + 1];
                var diffMillibars = (double)nextWholeMillibars - (double)wholeMillibars;
                diffMillibars = (double)extrapolationDistance / (double)kFeetIncrementForMillibarLookup * diffMillibars;
                wholeMillibars += (short)System.Math.Round(diffMillibars);
            }
            return wholeMillibars;
        }

    }


    /// <summary>
    /// A BME280 specific exception.
    /// </summary>
    public class BME280Exception : Exception
    {
        public BME280Exception() { }
        public BME280Exception(string message) : base(message) { }
        public BME280Exception(string message, Exception inner) : base(message, inner) { }
    }

    public class I2CException : Exception
    {
        public I2CException() { }
        public I2CException(string message) : base(message) { }
        public I2CException(string message, Exception inner) : base(message, inner) { }
    }

    public static class I2DeviceExtensions
    {
        /// <summary>
        /// Object I2CBusLock consists of a locking object for the I2C Bus.
        /// </summary>
        private static object I2CBusLock = new object();

        #region Transact extension method of I2CDevice (PUBLIC)
        /// <summary>
        /// <para>Transact extension method of I2CDevice accepts I2C trnasactions and a timeout</para>
        /// <para>value and calls upon I2CDevice to execute the I2C bus transactions in a thread</para>
        /// <para>save manner.</para>
        /// </summary>
        /// <param name="this">blins this parameter</param>
        /// <param name="transactions">An array of I2CTransaction to perform</param>
        /// <param name="timeout">A timeout value to perform the transactions</param>
        /// <exception cref="I2CException"></exception>
        public static void Transact(
            this I2CDevice @this,
            I2CDevice.I2CTransaction[] transactions,
            int timeout)
        {

            int bytesToTransfer = 0;
            int bytesTransferred = 0;
            // Calculate bytesToTransfer
            foreach (var transaction in transactions)
                bytesToTransfer += transaction.Buffer.Length;

            // Execute the transactions in a thread save manner
            lock (I2CBusLock)
            {
                bytesTransferred = @this.Execute(transactions, timeout);
            }

            // Validate successful completion of transaction
            if (bytesToTransfer != bytesTransferred)
                throw new I2CException(
                    "Error:  " +
                    "I2C transaction of " + bytesToTransfer.ToString() +
                    " byte(s) transferred " + bytesTransferred.ToString() +
                    " byte(s).");
        }
        #endregion
    }

    /// <summary>
    /// Class for extension methods
    /// </summary>
    public static class BME280Ext
    {


        #region Reverse extension method of byte[] (PUBLIC)
        /// <summary>
        /// <para>Extension method Reverse of byte[] reverses and returns the bytes in the targeted array.</para>
        /// </summary>
        /// <param name="this">blind this parameter</param>
        /// <returns>
        /// <para>Extension method Reverse of byte[] reverses and returns the bytes in the targeted array.</para>
        /// </returns>
        public static byte[] Reverse(this byte[] @this)
        {
            var buffer = new Stack();
            foreach (var byteVal in @this)
            {
                buffer.Push(byteVal);
            }
            var revBuffer = buffer.ToArray();
            for (int i = 0; i < @this.Length; i++)
            {
                @this[i] = (byte)revBuffer[i];
            }
            return @this;
        }
        #endregion

        #region ExtractUShort extension method of byte[] (PUBLIC)
        /// <summary>
        /// <para>Extension method ExtractUShort of byte[] returns a ushort value represented by </para>
        /// <para>the two bytes starting at the designated offset in the target byte array.</para>
        /// </summary>
        /// <param name="this">blind this parameter</param>
        /// <param name="index">The designated start index to parse in the array</param>
        /// <returns>
        /// <para>Extension method ExtractUShort of byte[] returns a ushort value represented by </para>
        /// <para>the two bytes starting at the designated offset in the target byte array.</para>
        /// </returns>
        public static ushort ExtractUShort(this byte[] @this, ref int index)
        {

            if (index < 0 || index > @this.Length - sizeof(ushort))
                throw new BME280Exception(
                    "Index value out of range in method " +
                    "byte[].ExtractUShort().");
            ushort value = (ushort)(@this[index + 1] << 8);
            value += (ushort)@this[index];
            index += sizeof(ushort);
            return value;
        }
        #endregion

        #region ExtractShort extension method of byte[] (PUBLIC)
        /// <summary>
        /// <para>Extension method ExtractShort of byte[] returns a short value represented by </para>
        /// <para>the two bytes starting at the designated offset in the target byte array.</para>
        /// </summary>
        /// <param name="this">blind this parameter</param>
        /// <param name="index">The designated start index to parse in the array</param>
        /// <returns>
        /// <para>Extension method ExtractShort of byte[] returns a short value represented by </para>
        /// <para>the two bytes starting at the designated offset in the target byte array.</para>
        /// </returns>
        public static short ExtractShort(this byte[] @this, ref int index)
        {
            if (index < 0 || index > @this.Length - sizeof(short))
                throw new BME280Exception(
                    "Index value out of range in method " +
                    "byte[].ExtractShort().");
            ushort value = (ushort)(@this[index + 1] << 8);
            value += (ushort)@this[index];
            index += sizeof(ushort);
            return (short)value;
        }
        #endregion

        #region ExtractIntFromBigEndianBuffer extension method of byte[] (PUBLIC)
        /// <summary>
        /// <para>Extension method ExtractIntFromBigEndianBuffer of byte[] returns an int value</para>
        /// <para>parsed from a buffer in big endian format. The number of bytes to parse at the</para>
        /// <para>index configurable, as is the option to shift the result, and by how many bits.</para>
        /// </summary>
        /// <param name="this">blind this parameter</param>
        /// <param name="index">The index to start parsing</param>
        /// <param name="validByteCount">The count of bytes to parse</param>
        /// <param name="shiftRightBits">The number of bits to right shift the result</param>
        /// <returns>
        /// <para>Extension method ExtractIntFromBigEndianBuffer of byte[] returns an int value</para>
        /// <para>parsed from a buffer in big endian format. The number of bytes to parse at the</para>
        /// <para>index configurable, as is the option to shift the result, and by how many bits.</para>
        /// </returns>
        /// <exception cref="BME280Exception"></exception>
        public static int ExtractIntFromBigEndianBuffer(this byte[] @this, ref int index, int validByteCount, int shiftRightBits)
        {

            // Validate index parameter
            if (index < 0 || index > @this.Length - validByteCount)
                throw new BME280Exception(
                    "Variable 'index' value out of range in " +
                    "method byte[].ExtractIntFromBigEndianBuffer().");
            // Validate validByteCount parameter
            if (validByteCount < 1 || validByteCount > sizeof(int))
                throw new BME280Exception(
                    "Variable 'validByteCount' value out of range in " +
                    "method ExtractIntFromBigEndian().");
            // Validate shiftRightBits parameter
            if (shiftRightBits > 32)
                throw new BME280Exception(
                    "Varialbe 'shiftRightBits' value out of range in " +
                    "method ExtractIntFromBigEndian().");

            // Build the array to parse
            var buffer = new byte[] { 0, 0, 0, 0 };
            // Adjust the destination index basedd in the validByteCount
            var destinationIndex = sizeof(int) - validByteCount;
            // destinationIndex   validByteCount
            //       3                1
            //       2                2
            //       1                3
            //       0                4

            // Copy data into the buffer in the correct byte location
            for (var i = 0; i < validByteCount; i++) buffer[destinationIndex++] = @this[index++];
            // Convert to little endian
            buffer = buffer.Reverse();
            // Convert to uint
            UInt32 temp = 0;
            temp = (temp | buffer[3]) << 8;
            temp = (temp |= buffer[2]) << 8;
            temp = (temp |= buffer[1]) << 8;
            temp = (temp |= buffer[1]);
            int result = (int)temp;
            // Shift if necessary
            if (shiftRightBits > 0)
                result = result >> shiftRightBits;
            return result;

        }
        #endregion

    }
}