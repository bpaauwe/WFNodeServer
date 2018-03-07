
//
// WFNodeServer - ISY Node Server for Weather Flow weather station data
//
// Copyright (C) 2018 Robert Paauwe
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace WFNodeServer {
    partial class WeatherFlow_UDP {
                internal PreciptData PreciptObj = new PreciptData();
        internal StrikeData StrikeObj = new StrikeData();
        internal WindData WindObj = new WindData();
        internal AirData AirObj = new AirData();
        internal SkyData SkyObj = new SkyData();
        internal DeviceData DeviceObj = new DeviceData();
        internal HubData HubObj = new HubData();
        internal Dictionary<string, DeviceData> Sensors = new Dictionary<string, DeviceData>();
        internal Boolean ValidHub = false;
        private string CurrentDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)).ToShortDateString();
        private double DailyPrecipitation = 0;

        internal enum WF {
            ROOT = 0,
            TEMPERATURE,
            HUMIDITY,
            PRESSURE,
            DEWPOINT,
            WINDSPEED,
            GUSTSPEED,
            WINDDIRECTION,
            PRECIPITATION,
            DAILY_PRECIPITATION,
            UV,
            SOLAR,
            ILLUMINACE,
            STRIKES,
            DISTANCE,
            APPARENT_TEMPERATURE,
            HEATINDEX,
            WINDCHILL,
            PRESSURE_TREND,
        }

        public class PreciptData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public List<int> evt { get; set; }
        }

        public class StrikeData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public int device_id { get; set; }
            public string hub_sn { get; set; }
            public List<double> evt { get; set; }
        }

        public class WindData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public int device_id { get; set; }
            public string hub_sn { get; set; }
            public List<double> ob { get; set; }
        }

        public class AirData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public int device_id { get; set; }
            public List<List<double>> obs { get; set; }
            public int firmware_revision { get; set; }
        }

        public class SkyData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public int device_id { get; set; }
            public List<List<double>> obs { get; set; }
            public int firmware_revision { get; set; }
        }

        public class DeviceData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public int timestamp { get; set; }
            public int uptime { get; set; }
            public double voltage { get; set; }
            public int firmware_revision { get; set; }
            public int rssi { get; set; }
            public int sensor_status { get; set; }
        }

        public class HubData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string firmware_revision { get; set; }
            public int uptime { get; set; }
            public int rssi { get; set; }
            public int timestamp { get; set; }
            public string reset_flags { get; set; }
            public string stack { get; set; }
            public int seq { get; set; }
            public int fs { get; set; }
        }

        private enum AirIndex {
            TS = 0,
            PRESSURE,
            TEMPURATURE,
            HUMIDITY,
            STRIKES,
            DISTANCE,
            BATTERY,
            INTERVAL,
        }

        private enum SkyIndex {
            TS = 0,
            ILLUMINATION,
            UV,
            RAIN,
            WIND_LULL,
            WIND_SPEED,
            GUST_SPEED,
            WIND_DIRECTION,
            BATTERY,
            INTERVAL,
            SOlAR_RADIATION,
            PRECIPITATINO_DAY,
        }

        // TODO:
        //   How do we want to handle the device status?  There should
        //   be at least 2 devices present (Air & Sky). Do we create
        //   a HS record for each and use that as a place to store the
        //   device specific data?  Or do we just create an internal
        //   list of devivces?
        //
        //   Whatever we do here we should probably do for the hub
        //   as well (treat it as a third device).
        private void DeviceStatus(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                DeviceObj = serializer.Deserialize<DeviceData>(json);

                //Console.WriteLine("Serial Number:     " + DeviceObj.serial_number);
                //Console.WriteLine("Device Type:       " + DeviceObj.type);
                //Console.WriteLine("Hub Serial Number: " + DeviceObj.hub_sn);
                //Console.WriteLine("timestamp:         " + DeviceObj.timestamp.ToString());
                //Console.WriteLine("uptime:            " + DeviceObj.uptime.ToString());
                //Console.WriteLine("Voltage:           " + DeviceObj.voltage.ToString());
                //Console.WriteLine("Firmware:          " + DeviceObj.firmware_revision.ToString());
                //Console.WriteLine("RSSI:              " + DeviceObj.rssi.ToString());
                //Console.WriteLine("Sensor status:     " + DeviceObj.sensor_status.ToString());

                if (Sensors.ContainsKey(DeviceObj.serial_number))
                    Sensors[DeviceObj.serial_number] = DeviceObj;
                else
                    Sensors.Add(DeviceObj.serial_number, DeviceObj);

            } catch (Exception ex) {
                Console.WriteLine("Deserialization of device status failed: " + ex.Message);
            }
        }

        private void HubStatus(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                HubObj = serializer.Deserialize<HubData>(json);

                //Console.WriteLine("Serial Number:     " + HubObj.serial_number);
                //Console.WriteLine("Device Type:       " + HubObj.type);
                //Console.WriteLine("Firmware:          " + HubObj.firmware_revision.ToString());
                //Console.WriteLine("uptime:            " + HubObj.uptime.ToString());
                //Console.WriteLine("RSSI:              " + HubObj.rssi.ToString());
                //Console.WriteLine("timestamp:         " + HubObj.timestamp.ToString());
                //Console.WriteLine("Reset Flags:       " + HubObj.reset_flags);
                //Console.WriteLine("Stack:             " + HubObj.stack);
                //Console.WriteLine("Sequence:          " + HubObj.seq.ToString());
                //Console.WriteLine("External File:     " + HubObj.fs.ToString());
                ValidHub = true;
            } catch (Exception ex) {
                Console.WriteLine("Deserialization of device status failed: " + ex.Message);
            }
        }

     	private void AirObservations(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

			// obs[0][0] = time (seconds)
			// obs[0][1] = station pressure (MB)
			// obs[0][2] = air temp (c)
			// obs[0][3] = humidity (%)
			// obs[0][4] = lightning count
			// obs[0][5] = avg lightning dist (km)
			// obs[0][6] = battery
			// obs[0][7] = interval (minutes)
            try {
                AirObj = serializer.Deserialize<AirData>(json);

                Console.WriteLine("Processing Air observations");

                // Do we just want to raise an event with the data object?
                WeatherFlowNS.NS.RaiseAirEvent(this, new WFNodeServer.AirEventArgs(AirObj));

                try {
                    //WFDeviceList[WF.DEWPOINT].SetValue(CalcDewPoint());
                    //WFDeviceList[WF.APPARENT_TEMPERATURE].SetValue(ApparentTemp_C());
                    //WFDeviceList[WF.HEATINDEX].SetValue(CalcHeatIndex());
                    //WFDeviceList[WF.PRESSURE_TREND].SetValue(PressureTrend());
                } catch {
                    Console.WriteLine("Skiping calculations, missing sky data.");
                }
            } catch (Exception ex) {
                Console.WriteLine("Deserialization failed for air data: " + ex.Message);
            }
		}

		private void SkyObservations(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                SkyObj = serializer.Deserialize<SkyData>(json);

                Console.WriteLine("Processing Sky observations");
                WeatherFlowNS.NS.RaiseSkyEvent(this, new WFNodeServer.SkyEventArgs(SkyObj));
                //WFDeviceList[WF.WINDCHILL].SetValue(CalcWindChill());
                //WFDeviceList[WF.DAILY_PRECIPITATION].SetValue(CalcDailyPrecipitation());
            } catch (Exception ex) {
                Console.WriteLine("Deserialization failed for sky data: " + ex.Message);
                return;
            }

		}

#if false
        internal void LigtningStrikeEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            IPlugInAPI.strTrigActInfo[] HSTriggers;

            try {
                // evt[0] = timestamp
                // evt[1] = distance (km)
                // evt[2] = energy
                StrikeObj = serializer.Deserialize<StrikeData>(json);

                // Get list of matching triggers to check
                HSTriggers = WeatherFlow.callback.TriggerMatches(WeatherFlow.IFACE_NAME, 1, -1);
                foreach (IPlugInAPI.strTrigActInfo tinfo in HSTriggers) {
                    TriggerData d = TriggerFromData(tinfo.DataIn);

                    switch (d.Condition) {
                        case 1: // equal
                            if (StrikeObj.evt[1] == d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 2: // not equal
                            if (StrikeObj.evt[1] != d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 3: // less than
                            if (StrikeObj.evt[1] < d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 4: // greater than
                            if (StrikeObj.evt[1] > d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Failed to deserialize strike event: " + ex.Message);
            }
        }

        internal void PrecipitationEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            IPlugInAPI.strTrigActInfo[] HSTriggers;

            try {
                // evt[0] = timestamp
                StrikeObj = serializer.Deserialize<StrikeData>(json);

                // Get list of matching triggers to check
                HSTriggers = WeatherFlow.callback.TriggerMatches(WeatherFlow.IFACE_NAME, 2, -1);
                foreach (IPlugInAPI.strTrigActInfo tinfo in HSTriggers) {
                    WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                }
            } catch (Exception ex) {
                Console.WriteLine("Failed to deserialize precipitation event: " + ex.Message);
            }
        }

        internal void RapidWindEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            IPlugInAPI.strTrigActInfo[] HSTriggers;

            try {
                // evt[0] = timestamp
                // evt[1] = speed (m/s)
                // evt[2] = direction 
                WindObj = serializer.Deserialize<WindData>(json);

                // Get list of matching wind speed triggers to check
                HSTriggers = WeatherFlow.callback.TriggerMatches(WeatherFlow.IFACE_NAME, 3, -1);
                foreach (IPlugInAPI.strTrigActInfo tinfo in HSTriggers) {
                    TriggerData d = TriggerFromData(tinfo.DataIn);

                    switch (d.Condition) {
                        case 1: // equal
                            if (WindObj.ob[1] == d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 2: // not equal
                            if (WindObj.ob[1] != d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 3: // less than
                            if (WindObj.ob[1] < d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 4: // greater than
                            if (WindObj.ob[1] > d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                    }
                    WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                }

                // Get list of matching wind direction triggers to check
                HSTriggers = WeatherFlow.callback.TriggerMatches(WeatherFlow.IFACE_NAME, 4, -1);
                foreach (IPlugInAPI.strTrigActInfo tinfo in HSTriggers) {
                    TriggerData d = TriggerFromData(tinfo.DataIn);

                    switch (d.Condition) {
                        case 1: // equal
                            if (WindObj.ob[2] == d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 2: // not equal
                            if (WindObj.ob[2] != d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 3: // less than
                            if (WindObj.ob[2] < d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                        case 4: // greater than
                            if (WindObj.ob[2] > d.Value)
                                WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                            break;
                    }
                    WeatherFlow.callback.TriggerFire(WeatherFlow.IFACE_NAME, tinfo);
                }
            } catch (Exception ex) {
                Console.WriteLine("Failed to deserialize rapid wind event: " + ex.Message);
            }
        }
#endif

		//
		// Formula:
		//   water_vapor_pressure = relative_humidity / 100 * 6.105 * math.exp(17.27 * temp_c / (237.7 + temp_c))
		//   at = temp_c + (0.33 * water_vapor_pressure) - (0.70 * wind speed) - 4
		// wind speed is in meter/s
		//
		internal double ApparentTemp_C() {
			double wv;
			double ws;

            if (AirObj.obs[0][0] == 0 || SkyObj.obs[0][0] == 0)
                return 0.0;
               

			ws = SkyObj.obs[0][(int)SkyIndex.WIND_SPEED] / 2.2368; // convert mph to m/s
			wv = AirObj.obs[0][(int)AirIndex.HUMIDITY] / 100 * 6.105 * Math.Exp(17.27 * AirObj.obs[0][(int)AirIndex.TEMPURATURE] / (237.7 + AirObj.obs[0][(int)AirIndex.TEMPURATURE]));

			return AirObj.obs[0][(int)AirIndex.TEMPURATURE] + (0.33 * wv) - (0.70 * ws) - 4.0;
		}

        internal static double TempF(double tempc) {
            return Math.Round((tempc * 1.8) + 32, 1);
        }

        private static double TempC(double tempf) {
            return Math.Round((tempf -32) / 1.8, 1);
        }

        private static double MS2MPH(double ms) {
            //return (ms * 3600.0) / 1609.344;
            return Math.Round(ms / 0.44704, 1);
        }

        private static double MS2KPH(double ms) {
            return Math.Round((ms * (18 / 5)), 1);
        }

        private static double KPH2MS(double kph) {
            return Math.Round((kph * (5 / 18)), 1);
        }

        private static double MPH2MS(double mph) {
            return Math.Round(mph * .44704, 1);
        }

        private static double MPH2KPH(double mph) {
            return Math.Round(mph * 1.609344, 1);
        }

        private static double KPH2MPH(double kph) {
            return Math.Round(kph / 1.609344, 1);
        }

        private static double KM2Miles(double km) {
            return Math.Round(km / 1.609344, 1);
        }

        private static double Miles2KM(double miles) {
            return Math.Round(miles / 0.62137119, 1);
        }

        private static double MM2Inch(double mm) {
            return Math.Round(mm * 0.03937, 1);
        }

        private static double Inch2MM(double inch) {
            return Math.Round(inch * 25.4, 2);
        }

        internal double ApparentTemp_F() {
            return Math.Round((ApparentTemp_C() * 1.8) + 32, 1);
        }

        // Uses and returns temp in C
        internal double CalcDewPoint() {
            double t = AirObj.obs[0][(int)AirIndex.TEMPURATURE];
            double h = AirObj.obs[0][(int)AirIndex.HUMIDITY];
            double b;

            //b = (Math.Log(h / 100) + ((17.27 *  t) / (237.3 + t))) / 17.27;
            b = (17.625 * t) / (243.04 + t);
            return (243.04 * (Math.Log(h/100) + b)) / (17.625 - Math.Log(h/100) - b);
        }

        // uses and returns temp in F
        internal double CalcHeatIndex() {
            double t = TempF(AirObj.obs[0][(int)AirIndex.TEMPURATURE]);
            double h = AirObj.obs[0][(int)AirIndex.HUMIDITY];
            double c1 = -42.379;
            double c2 = 2.04901523;
            double c3 = 10.14333127;
            double c4 = -0.22475541;
            double c5 = -6.83783 * Math.Pow(10, -3);
            double c6 = -5.481717 * Math.Pow(10, -2);
            double c7 = 1.22874 * Math.Pow(10, -3);
            double c8 = 8.5282 * Math.Pow(10, -4);
            double c9 = -1.99 * Math.Pow(10, -6);

            if ((t < 80.0) || (h < 40.0))
                return t;
            else
                return (c1 + (c2 * t) + (c3 * h) + (c4 * t * h) + (c5 * t * t) + (c6 * h * h) + (c7 * t * t * h) + (c8 * t * h * h) + (c9 * t * t * h * h));
        }

        // uses and returns temp in F
        internal double CalcWindChill() {
            double t = TempF(AirObj.obs[0][(int)AirIndex.TEMPURATURE]);
            double v = MS2MPH(SkyObj.obs[0][(int)SkyIndex.WIND_SPEED]);

            if ((t < 50.0) && (v > 5.0))
                return 35.74 + (0.6215 * t) - (35.75 * Math.Pow(v, 0.16)) + (0.4275 * t * Math.Pow(v, 0.16));
            else
                return t;
        }

        private double CalcDailyPrecipitation() {
            if (DateTime.Now.ToShortDateString() != CurrentDate) {
                CurrentDate = DateTime.Now.ToShortDateString();
                DailyPrecipitation = 0;
            }

            DailyPrecipitation += SkyObj.obs[0][(int)SkyIndex.RAIN];

            return DailyPrecipitation;
        }


        // Pressure trend is measured over a 3 hour period
        private class TrendData {
            internal DateTime time { get; set; }
            internal double pressure { get; set; }
        }

        private Queue<TrendData> Trend = new Queue<TrendData>();

        internal int PressureTrend() {
            int trend = 0;
            TrendData t = new TrendData();

            t.time = DateTime.Now;
            t.pressure = AirObj.obs[0][(int)AirIndex.PRESSURE];

            Trend.Enqueue(t);

            t = Trend.Peek();
            if (t.time.AddHours(3) < DateTime.Now) {
                t = Trend.Dequeue();

                if (t.pressure < (AirObj.obs[0][(int)AirIndex.PRESSURE] + 1))
                    trend = 1;
                else if (t.pressure > (AirObj.obs[0][(int)AirIndex.PRESSURE] - 1))
                    trend = -1;
            }

            return trend;
        }

        internal static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
