
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

        internal enum DataType {
            STRIKE = 0,
            WIND,
            AIR,
            SKY,
            DEVICE,
            HUB,
        }

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
            public int device_id { get; set; }
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public List<double> ob { get; set; }
        }

        public class AirData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public int device_id { get; set; }
            public List<List<Nullable<double>>> obs { get; set; }
            public int firmware_revision { get; set; }
            public bool valid = false;

            public AirData() {
                serial_number = "";
                type = "";
                hub_sn = "";
                device_id = 0;
                firmware_revision = 0;
                obs = new List<List<Nullable<double>>>();
            }
        }

        public class SkyData {
            public string serial_number { get; set; }
            public string type { get; set; }
            public string hub_sn { get; set; }
            public int device_id { get; set; }
            public List<List<Nullable<double>>> obs { get; set; }
            public int firmware_revision { get; set; }
            public bool valid = false;

            public SkyData() {
                serial_number = "";
                type = "";
                hub_sn = "";
                device_id = 0;
                firmware_revision = 0;
                obs = new List<List<Nullable<double>>>();
            }
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
            public int hub_rssi { get; set; }  // Added in firmware 35
            public int sensor_status { get; set; }
            public int debug { get; set; }  // Added in firmware 35
            public int freq { get; set; }  // Added in firmware 40
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
            public string fs { get; set; }
        }

        public class Summary {
            public double precip_total_1h { get; set; }
            public double precip_total_24h { get; set; }
            public double precip_high_24h { get; set; }
            public int precip_high_epoch_24h { get; set; }
        }
        public class ObsData {
            public Summary summary { get; set; }
            public string serial_number { get; set; }
            public string hub_sn { get; set; }
            public string type { get; set; }
            public string source { get; set; }
            public List<List<double?>> obs { get; set; }
            public int device_id { get; set; }
            public int firmware_revision { get; set; }
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

                // Add event to update this info.
                WeatherFlowNS.NS.RaiseDeviceEvent(this, new WFNodeServer.DeviceEventArgs(DeviceObj));
                WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)DeviceObj.timestamp, DeviceObj.serial_number + "_d", DataType.DEVICE));

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
                WFLogging.Error("Deserialization of device status failed: " + ex.Message);
            }
        }

        private void HubStatus(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                HubObj = serializer.Deserialize<HubData>(json);
                WeatherFlowNS.NS.RaiseHubEvent(this, new WFNodeServer.HubEventArgs(HubObj));
                WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)HubObj.timestamp, HubObj.serial_number, DataType.HUB));

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
                WFLogging.Error("Deserialization of device status failed: " + ex.Message);
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
                double elevation = 0;

                AirObj = serializer.Deserialize<AirData>(json);
                AirObj.valid = true;

                // Look up elevation
                StationInfo si = wf_station.FindStationAir(AirObj.serial_number);
                if (si != null) {
                    elevation = si.elevation;
                }
                    
                // Do we just want to raise an event with the data object?
                AirEventArgs evnt = new AirEventArgs(AirObj);
                evnt.SetDewpoint = 0;
                evnt.SetApparentTemp = 0;
                evnt.SetTrend = 1;
                evnt.SetSeaLevel = SeaLevelPressure(AirObj.obs[0][(int)AirIndex.PRESSURE].GetValueOrDefault(), elevation);
                evnt.Raw = json;
                if (SkyObj.valid) {
                    try {
                        evnt.SetDewpoint = CalcDewPoint();
                        evnt.SetApparentTemp = FeelsLike(AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault(),
                                                         AirObj.obs[0][(int)AirIndex.HUMIDITY].GetValueOrDefault(),
                                                         SkyObj.obs[0][(int)SkyIndex.WIND_SPEED].GetValueOrDefault());
                        // Trend is -1, 0, 1 while event wants 0, 1, 2
                        evnt.SetTrend = PressureTrend() + 1;
                        // Heat index & Windchill ??
                    } catch {
                    }
                } else {
                }

                WeatherFlowNS.NS.RaiseAirEvent(this, evnt);
                WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)AirObj.obs[0][0], AirObj.serial_number, DataType.AIR));
            } catch (Exception ex) {
                WFLogging.Error("Deserialization failed for air data: " + ex.Message);
            }
		}

		private void SkyObservations(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                SkyObj = serializer.Deserialize<SkyData>(json);
                SkyObj.valid = true;

                WFNodeServer.SkyEventArgs evnt = new SkyEventArgs(SkyObj);
                evnt.SetDaily = CalcDailyPrecipitation();
                evnt.Raw = json;

                WeatherFlowNS.NS.RaiseSkyEvent(this, evnt);
                WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)SkyObj.obs[0][0], SkyObj.serial_number, DataType.SKY));
            } catch (Exception ex) {
                WFLogging.Error("Deserialization failed for sky data: " + ex.Message);
                return;
            }

		}

        internal void LigtningStrikeEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                // evt[0] = timestamp
                // evt[1] = distance (km)
                // evt[2] = energy
                StrikeObj = serializer.Deserialize<StrikeData>(json);
                WeatherFlowNS.NS.RaiseLightningEvent(this, new LightningEventArgs(StrikeObj));

            } catch (Exception ex) {
                WFLogging.Error("Failed to deserialize strike event: " + ex.Message);
            }
        }

        internal void PrecipitationEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                // evt[0] = timestamp
                PreciptObj = serializer.Deserialize<PreciptData>(json);
                WeatherFlowNS.NS.RaiseRainEvent(this, new RainEventArgs(PreciptObj));
            } catch (Exception ex) {
                WFLogging.Error("Failed to deserialize precipitation event: " + ex.Message);
            }
        }

        internal void RapidWindEvt(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            try {
                // evt[0] = timestamp
                // evt[1] = speed (m/s)
                // evt[2] = direction 
                WindObj = serializer.Deserialize<WindData>(json);

                StationInfo si = wf_station.FindStationSky(WindObj.serial_number);
                if (si.rapid) {
                    WeatherFlowNS.NS.RaiseRapidEvent(this, new RapidEventArgs(WindObj));
                    WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)WindObj.ob[0], WindObj.serial_number + "_r", DataType.WIND));
                }
            } catch (Exception ex) {
                WFLogging.Error("Failed to deserialize rapid wind event: " + ex.Message);
                WFLogging.Error(json);
            }
        }

		internal void WSObservations(string json) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            ObsData obs;
            double elevation = 0;

            try {
                try {
                    obs = serializer.Deserialize<ObsData>(json);
                } catch (Exception ex) {
                    WFLogging.Error("Deserialization failed for WebSocket data: " + ex.Message);
                    WFLogging.Error(json);
                    return;
                }
                if (json.Contains("obs_sky")) {
                    //SkyObj = new SkyData();
                    // The first websocket packet seems to be cached data and it
                    // doesn't include some things like the device serial number.
                    // Without the serial number, we can't really process it
                    if (obs.source == "cache")
                        return;
                    SkyObj.device_id = obs.device_id;
                    SkyObj.firmware_revision = obs.firmware_revision;
                    SkyObj.hub_sn = obs.hub_sn;
                    SkyObj.obs = obs.obs;
                    SkyObj.serial_number = obs.serial_number;
                    SkyObj.type = obs.type;
                    SkyObj.valid = true;

                    WFNodeServer.SkyEventArgs evnt = new SkyEventArgs(SkyObj);
                    evnt.SetDaily = CalcDailyPrecipitation();
                    evnt.Raw = json;

                    // This fails the first time, why?
                    WeatherFlowNS.NS.RaiseSkyEvent(this, evnt);
                    WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)SkyObj.obs[0][0], SkyObj.serial_number, DataType.SKY));
                } else if (json.Contains("obs_air")) {
                    //AirObj = new AirData();
                    if (obs.source == "cache")
                        return;
                    AirObj.device_id = obs.device_id;
                    AirObj.firmware_revision = obs.firmware_revision;
                    AirObj.hub_sn = obs.hub_sn;
                    AirObj.obs = obs.obs;
                    AirObj.serial_number = obs.serial_number;
                    AirObj.type = obs.type;
                    AirObj.valid = true;

                    // Look up elevation
                    StationInfo si = wf_station.FindStationAir(AirObj.serial_number);
                    if (si != null) {
                        elevation = si.elevation;
                    }

                    AirEventArgs evnt = new AirEventArgs(AirObj);
                    evnt.SetDewpoint = 0;
                    evnt.SetApparentTemp = 0;
                    evnt.SetTrend = 1;
                    evnt.SetSeaLevel = SeaLevelPressure(AirObj.obs[0][(int)AirIndex.PRESSURE].GetValueOrDefault(), elevation);
                    evnt.Raw = json;
                    if (SkyObj.valid) {
                        try {
                            evnt.SetDewpoint = CalcDewPoint();
                            evnt.SetApparentTemp = FeelsLike(AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault(),
                                                         AirObj.obs[0][(int)AirIndex.HUMIDITY].GetValueOrDefault(),
                                                         SkyObj.obs[0][(int)SkyIndex.WIND_SPEED].GetValueOrDefault());
                            // Trend is -1, 0, 1 while event wants 0, 1, 2
                            evnt.SetTrend = PressureTrend() + 1;
                            // Heat index & Windchill ??
                        } catch {
                        }
                    } else {
                    }

                    WeatherFlowNS.NS.RaiseAirEvent(this, evnt);
                    WeatherFlowNS.NS.RaiseUpdateEvent(this, new UpdateEventArgs((int)AirObj.obs[0][0], AirObj.serial_number, DataType.AIR));
                }

            } catch (Exception ex) {
                WFLogging.Error("Failed to process websocket observation data: " + ex.Message);
                return;
            }

		}


        // t is temperature C
        // w is wind speed in m/s
        // h is humidity
        // Returns temperature in C
        internal double FeelsLike(double t, double h, double w) {
            if (t >= 27.0) { // Heat index returns temperature in F
                return TempC(CalcHeatIndex(t, h));
            } else if (t <= 10.0) {
                return TempC(CalcWindChill(t, w)); // Windchill returns temperature in F
            }

            return t;
        }

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
               

			ws = SkyObj.obs[0][(int)SkyIndex.WIND_SPEED].GetValueOrDefault() / 2.2368; // convert mph to m/s
			wv = AirObj.obs[0][(int)AirIndex.HUMIDITY].GetValueOrDefault() / 100 * 6.105 * Math.Exp(17.27 * AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault() / (237.7 + AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault()));

			return AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault() + (0.33 * wv) - (0.70 * ws) - 4.0;
		}

        internal static double TempF(double tempc) {
            return Math.Round((tempc * 1.8) + 32, 1);
        }

        internal static double TempC(double tempf) {
            return Math.Round((tempf -32) / 1.8, 1);
        }

        internal static double MS2MPH(double ms) {
            //return (ms * 3600.0) / 1609.344;
            return Math.Round(ms / 0.44704, 1);
        }

        internal static double MS2KPH(double ms) {
            return Math.Round((ms * (18 / 5)), 1);
        }

        internal static double KPH2MS(double kph) {
            return Math.Round((kph * (5 / 18)), 1);
        }

        internal static double MPH2MS(double mph) {
            return Math.Round(mph * .44704, 1);
        }

        internal static double MPH2KPH(double mph) {
            return Math.Round(mph * 1.609344, 1);
        }

        internal static double KPH2MPH(double kph) {
            return Math.Round(kph / 1.609344, 1);
        }

        internal static double KM2Miles(double km) {
            return Math.Round(km / 1.609344, 1);
        }

        internal static double Miles2KM(double miles) {
            return Math.Round(miles / 0.62137119, 1);
        }

        internal static double MM2Inch(double mm) {
            return Math.Round(mm * 0.03937, 3);
        }

        internal static double Inch2MM(double inch) {
            return Math.Round(inch * 25.4, 2);
        }

        internal double ApparentTemp_F() {
            return Math.Round((ApparentTemp_C() * 1.8) + 32, 1);
        }

        internal double SeaLevelPressureApprox(double p, double h) {
            return p + h / 8.3;
        }
        internal double SeaLevelPressure(double Ps, double Alt) {
            double i = 287.05;
            double a = 9.80665;
            double r = .0065;
            double s = 1013.25; // pressure at sealeval
            double n = 288.15;  // Temperature 

            double l = a / (i * r);
            double c = i * r / a;

            double u = Math.Pow(1 + Math.Pow(s / Ps, c) * (r * Alt / n), l);

            return (Ps * u);
        }

        // Uses and returns temp in C
        internal double CalcDewPoint() {
            double t = AirObj.obs[0][(int)AirIndex.TEMPURATURE].GetValueOrDefault();
            double h = AirObj.obs[0][(int)AirIndex.HUMIDITY].GetValueOrDefault();
            double b;

            //b = (Math.Log(h / 100) + ((17.27 *  t) / (237.3 + t))) / 17.27;
            b = (17.625 * t) / (243.04 + t);
            return (243.04 * (Math.Log(h/100) + b)) / (17.625 - Math.Log(h/100) - b);
        }

        // uses and returns temp in F
        internal double CalcHeatIndex(double t_c, double h) {
            double t = TempF(t_c);
            //double h = AirObj.obs[0][(int)AirIndex.HUMIDITY].GetValueOrDefault();
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
        internal double CalcWindChill(double t_c, double w) {
            double t = TempF(t_c);
            double v = MS2MPH(w);

            if ((t <= 50.0) && (v >= 5.0))
                return 35.74 + (0.6215 * t) - (35.75 * Math.Pow(v, 0.16)) + (0.4275 * t * Math.Pow(v, 0.16));
            else
                return t;
        }

        private double CalcDailyPrecipitation() {
            if (DateTime.Now.ToShortDateString() != CurrentDate) {
                CurrentDate = DateTime.Now.ToShortDateString();
                DailyPrecipitation = 0;
            }

            DailyPrecipitation += SkyObj.obs[0][(int)SkyIndex.RAIN].GetValueOrDefault();

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
            t.pressure = AirObj.obs[0][(int)AirIndex.PRESSURE].GetValueOrDefault();

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
