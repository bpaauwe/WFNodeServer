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
using System.Threading;
using System.Web;

namespace WFNodeServer {
    internal delegate void AirEvent(Object sender, AirEventArgs e);
    internal delegate void SkyEvent(Object sender, SkyEventArgs e);
    internal delegate void DeviceEvent(Object sender, DeviceEventArgs e);
    internal delegate void UpdateEvent(Object sender, UpdateEventArgs e);
    internal delegate void HubEvent(Object sender, HubEventArgs e);
    internal delegate void RapidEvent(Object sender, RapidEventArgs e);
    internal delegate void LightningEvent(Object sender, LightningEventArgs e);
    internal delegate void RainEvent(Object sender, RainEventArgs e);

    internal class RapidEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.WindData data;
        internal bool si_units { get; set; }
        internal RapidEventArgs(WeatherFlow_UDP.WindData d) {
            data = d;
        }
        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                d += "_r";
                return d.ToLower();
            }
        }
        internal string Parent {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }

        internal string TimeStamp {
            get { return data.ob[0].ToString(); }
        }
        internal string SpeedUOM {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return WeatherFlow_UDP.MS2KPH(data.ob[1]).ToString("0.#") + "/32";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MS2MPH(data.ob[1]).ToString("0.#") + "/48";
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.MS2MPH(data.ob[1]).ToString("0.#") + "/48";
                    default: return data.ob[1].ToString("0.#") + "/49";
                }
            }
        }

        internal string Speed {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return WeatherFlow_UDP.MS2KPH(data.ob[1]).ToString("0.#");
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MS2MPH(data.ob[1]).ToString("0.#");
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.MS2MPH(data.ob[1]).ToString("0.#");
                    default: return data.ob[1].ToString("0.#");
                }
            }
        }
        // Cardinal directions
        internal int Direction {
            get {
                return ((int)Math.Floor((data.ob[2] + 11.25) / 22.5)) % 16;
            }
        }
    }

    internal class LightningEventArgs : System.EventArgs {
        internal bool si_units { get; set; }
        internal WeatherFlow_UDP.StrikeData data;
        internal LightningEventArgs(WeatherFlow_UDP.StrikeData d) {
            data = d;
        }
        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                d += "_l";
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }
        internal string Parent {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string TimeStamp {
            get { return data.evt[0].ToString(); }
        }
        internal string DistanceUOM {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return data.evt[1].ToString("0.#") + "/83";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.KM2Miles(data.evt[1]).ToString("0.#") + "/0";
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.KM2Miles(data.evt[1]).ToString("0.#") + "/0";
                    default: return data.evt[1].ToString("0.#") + "/83";
                }
            }
        }

        internal string Distance {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return data.evt[1].ToString("0.#");
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.KM2Miles(data.evt[1]).ToString("0.#");
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.KM2Miles(data.evt[1]).ToString("0.#");
                    default: return data.evt[1].ToString("0.#");
                }
            }
        }
        internal string Energy {
            get { return data.evt[2].ToString(); }
        }
    }

    internal class RainEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.PreciptData data;
        internal RainEventArgs(WeatherFlow_UDP.PreciptData d) {
            data = d;
        }
        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }
        internal string TimeStamp {
            get { return data.evt[0].ToString(); }
        }
    }

    internal class HubEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.HubData data;

        internal HubEventArgs(WeatherFlow_UDP.HubData d) {
            data = d;
        }

        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }
        internal string ResetFlags {
            get { return data.reset_flags; }
        }
        internal string Firmware {
            get { return data.firmware_revision; }
        }
        internal string FS {
            get { return data.fs; }
        }
        internal string RSSI {
            get { return data.rssi.ToString(); }
        }
        internal string Stack {
            get { return data.stack; }
        }
        internal string Sequence {
            get { return data.seq.ToString(); }
        }
        internal string TimeStamp {
            get { return data.timestamp.ToString(); }
        }
        internal int Uptime {
            get { return data.uptime; }
        }
        
    }

    internal class UpdateEventArgs : System.EventArgs {
        internal int update_time;
        internal string serial_number;
        internal WeatherFlow_UDP.DataType type;

        internal UpdateEventArgs(int u, string s, WeatherFlow_UDP.DataType t) {
            update_time = u;
            serial_number = s;
            type = t;
        }
        internal UpdateEventArgs(int u, string s) {
            update_time = u;
            serial_number = s;
        }
        internal string SerialNumber {
            get {
                string d = serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal int UpdateTime {
            get { return update_time; }
        }
    }

    internal class DeviceEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.DeviceData data;

        internal DeviceEventArgs(WeatherFlow_UDP.DeviceData d) {
            data = d;
        }

        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                d += "_d";
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }

        internal string Type {
            get { return data.type; }
        }

        internal int Uptime {
            get { return data.uptime; }
        }
        internal string UpTime {
            get { return data.uptime.ToString(); }
        }

        internal string Voltage {
            get { return data.voltage.ToString("0.##"); }
        }

        internal string RSSI {
            get { return data.rssi.ToString("0.##"); }
        }

        internal string SensorStatus(int sensor) {
            if ((data.sensor_status & sensor) > 0)
                return "1";
            else
                return "0";
        }

        internal string Firmware {
            get { return data.firmware_revision.ToString(); }
        }

        internal string Debug {
            get { return data.debug.ToString(); }
        }

        internal string Frequency {
            get { return data.freq.ToString(); }
        }
    }

    internal class AirEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.AirData data;
        internal bool si_units { get; set; }
        private double dewpoint;
        private double apparent_temp;
        private int trend;
        private double sealevel;
        private string raw_packet;

        internal AirEventArgs(WeatherFlow_UDP.AirData d) {
            data = d;
            si_units = false;
        }

        // Might be nice if we had properties to pull out the
        // data in a formatted string.

        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }

        internal string TimeStamp {
            get {
                DateTime d = WeatherFlow_UDP.UnixTimeStampToDateTime(data.obs[0][0].GetValueOrDefault());
                //return d.ToString();
                return HttpUtility.UrlEncode(d.ToShortTimeString());
            }
        }
        internal string TS {
            get { return data.obs[0][0].ToString(); }
        }

        private double MB2InHg(double mb) {
            return mb * 0.02952998751;
        }

        internal string Pressure {
            get {
                double mb = data.obs[0][1].GetValueOrDefault();
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return mb.ToString("0.#") + "/0";
                    case (int)WF_UNITS.US: return MB2InHg(mb).ToString("0.###") + "/23";
                    case (int)WF_UNITS.UK: return mb.ToString("0.#") + "/0";
                    default: return mb.ToString("0.#") + "/0";
                }
            }
        }
        internal double SetSeaLevel {
            set { sealevel = value; }
        }
        internal string SeaLevelUOM {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return sealevel.ToString("0.#") + "/0";
                    case (int)WF_UNITS.US: return MB2InHg(sealevel).ToString("0.###") + "/23";
                    case (int)WF_UNITS.UK: return sealevel.ToString("0.#") + "/0";
                    default: return sealevel.ToString("0.#") + "/0";
                }
            }
        }
        internal string SeaLevel {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return sealevel.ToString("0.#");
                    case (int)WF_UNITS.US: return MB2InHg(sealevel).ToString("0.###");
                    case (int)WF_UNITS.UK: return sealevel.ToString("0.#");
                    default: return sealevel.ToString("0.#");
                }
            }
        }

        internal string TemperatureValue(double t) {
            switch (WF_Config.Units) {
                case (int)WF_UNITS.SI: return t.ToString("0.##");
                case (int)WF_UNITS.US: return WeatherFlow_UDP.TempF(t).ToString("0.##");
                case (int)WF_UNITS.UK: return t.ToString("0.##");
                default: return t.ToString("0.##");
            }
        }

        internal string TemperatureUOM {
            get {
                string t = TemperatureValue(data.obs[0][2].GetValueOrDefault());
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return t + "/4";
                    case (int)WF_UNITS.US: return t + "/17";
                    case (int)WF_UNITS.UK: return t + "/4";
                    default: return t + "/t";
                }
            }
        }

        internal string Temperature {
            get {
                return TemperatureValue(data.obs[0][2].GetValueOrDefault());
            }
        }

        internal string HumidityUOM {
            get { return data.obs[0][3].GetValueOrDefault().ToString() + "/51"; }
        }
        internal string Humidity {
            get { return data.obs[0][3].GetValueOrDefault().ToString(); }
        }
        internal string StrikesUOM {
            get { return data.obs[0][4].GetValueOrDefault().ToString() + "/56"; }
        }
        internal string Strikes {
            get { return data.obs[0][4].GetValueOrDefault().ToString(); }
        }
        internal string DistanceUOM {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return data.obs[0][5].GetValueOrDefault().ToString("0.#") + "/83";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.KM2Miles(data.obs[0][5].GetValueOrDefault()).ToString("0.#") + "/0";
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.KM2Miles(data.obs[0][5].GetValueOrDefault()).ToString("0.#") + "/0";
                    default: return data.obs[0][5].GetValueOrDefault().ToString("0.#") + "/83";
                }
            }
        }

        internal string Distance {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return data.obs[0][5].GetValueOrDefault().ToString("0.#");
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.KM2Miles(data.obs[0][5].GetValueOrDefault()).ToString("0.#");
                    case (int)WF_UNITS.UK: return WeatherFlow_UDP.KM2Miles(data.obs[0][5].GetValueOrDefault()).ToString("0.#");
                    default: return data.obs[0][5].GetValueOrDefault().ToString("0.#");
                }
            }
        }
        internal string BatteryUOM {
            get { return data.obs[0][6].GetValueOrDefault().ToString() + "/72"; }
        }
        internal string Battery {
            get { return data.obs[0][6].GetValueOrDefault().ToString(); }
        }

        internal double SetDewpoint {
            set { dewpoint = value; }
        }
        internal string DewpointUOM {
            get {
                string t = TemperatureValue(dewpoint);
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return t + "/4";
                    case (int)WF_UNITS.US: return t + "/17";
                    case (int)WF_UNITS.UK: return t + "/4";
                    default: return t + "/t";
                }
            }
        }
        internal string Dewpoint {
            get {
                return TemperatureValue(dewpoint);
            }
        }

        internal double SetApparentTemp {
            set { apparent_temp = value; }
        }
        internal string ApparentTempUOM {
            get {
                string t = TemperatureValue(apparent_temp);
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return t + "/4";
                    case (int)WF_UNITS.US: return t + "/17";
                    case (int)WF_UNITS.UK: return t + "/4";
                    default: return t + "/4";
                }
            }
        }
        internal string ApparentTemp {
            get {
                return TemperatureValue(apparent_temp);
            }
        }

        internal int SetTrend {
            set { trend = value; }
        }
        internal string TrendUOM {
            get { return trend.ToString() + "/25"; }
        }
        internal string Trend {
            get { return trend.ToString(); }
        }

        internal int DeviceID {
            get { return data.device_id; }
        }

        internal string Raw {
            get { return raw_packet; }
            set { raw_packet = value; }
        }
    }

    internal class SkyEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.SkyData data;
        internal bool si_units { get; set; }
        private double daily;
        private string raw_packet;

        internal SkyEventArgs(WeatherFlow_UDP.SkyData d) {
            data = d;
            si_units = false;
        }
        internal string SerialNumber {
            get {
                string d = data.serial_number.Replace('-', '_');
                return d.ToLower();
            }
        }
        internal string serial_number {
            get { return data.serial_number; }
        }
        internal string TimeStamp {
            get {
                DateTime d = WeatherFlow_UDP.UnixTimeStampToDateTime(data.obs[0][0].GetValueOrDefault());
                //return d.ToString();
                return HttpUtility.UrlEncode(d.ToShortTimeString());
            }
        }
        internal string TS {
            get { return data.obs[0][0].ToString(); }
        }
        internal string IlluminationUOM {
            get { return data.obs[0][1].GetValueOrDefault().ToString() + "/36"; }
        }
        internal string Illumination {
            get { return data.obs[0][1].GetValueOrDefault().ToString(); }
        }
        internal string UVUOM {
            get { return data.obs[0][2].GetValueOrDefault().ToString() + "/71"; }
        }
        internal string UV {
            get { return data.obs[0][2].GetValueOrDefault().ToString(); }
        }
        internal string RainUOM {
            get {
                double r = data.obs[0][3].GetValueOrDefault();
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return r.ToString("0.#") + "/82";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MM2Inch(r).ToString("0.###") + "/105";
                    case (int)WF_UNITS.UK: return r.ToString("0.###") + "/82";
                    default: return r.ToString("0.#") + "/82";
                }
            }
        }

        internal string Rain {
            get {
                double r = data.obs[0][3].GetValueOrDefault();
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return r.ToString("0.#");
                    case (int)WF_UNITS.US: return r.ToString("0.###");
                    case (int)WF_UNITS.UK: return r.ToString("0.###");
                    default: return r.ToString("0.#");
                }
            }
        }
        internal string RainRateUOM {
            get {
                double interval = data.obs[0][9].GetValueOrDefault();
                double rate = data.obs[0][3].GetValueOrDefault() * 60;

                if (interval > 0)
                    rate = rate / interval;

                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return rate.ToString("0.#") + "/46";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MM2Inch(rate).ToString("0.###") + "/24";
                    case (int)WF_UNITS.UK: return rate.ToString("0.###") + "/46";
                    default: return rate.ToString("0.#") + "/46";
                }
            }
        }
        internal string RainRate {
            get {
                double interval = data.obs[0][9].GetValueOrDefault();
                double rate = data.obs[0][3].GetValueOrDefault() * 60;

                if (interval > 0)
                    rate = rate / interval;

                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return rate.ToString("0.#");
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MM2Inch(rate).ToString("0.###");
                    case (int)WF_UNITS.UK: return rate.ToString("0.###");
                    default: return rate.ToString("0.#");
                }
            }
        }
        private string speedstr(double sp) {
            switch (WF_Config.Units) {
                case (int)WF_UNITS.SI: return WeatherFlow_UDP.MS2KPH(sp).ToString("0.#");
                case (int)WF_UNITS.US: return WeatherFlow_UDP.MS2MPH(sp).ToString("0.#");
                case (int)WF_UNITS.UK: return WeatherFlow_UDP.MS2MPH(sp).ToString("0.#");
                default: return WeatherFlow_UDP.MS2KPH(sp).ToString("0.#");
            }
        }

        internal string WindLullUOM {
            get {
                string s = speedstr(data.obs[0][4].GetValueOrDefault());
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return s + "/32";
                    case (int)WF_UNITS.US: return s + "/48";
                    case (int)WF_UNITS.UK: return s + "/48";
                    default: return s + "/32";
                }
            }
        }
        internal string WindLull {
            get { return speedstr(data.obs[0][4].GetValueOrDefault()); }
        }
        internal string WindSpeedUOM {
            get {
                string s = speedstr(data.obs[0][5].GetValueOrDefault());
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return s + "/32";
                    case (int)WF_UNITS.US: return s + "/48";
                    case (int)WF_UNITS.UK: return s + "/48";
                    default: return s + "/32";
                }
            }
        }
        internal string WindSpeed {
            get { return speedstr(data.obs[0][5].GetValueOrDefault()); }
        }
        internal string GustSpeedUOM {
            get {
                string s = speedstr(data.obs[0][6].GetValueOrDefault());
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return s + "/32";
                    case (int)WF_UNITS.US: return s + "/48";
                    case (int)WF_UNITS.UK: return s + "/48";
                    default: return s + "/32";
                }
            }
        }
        internal string GustSpeed {
            get { return speedstr(data.obs[0][6].GetValueOrDefault()); }
        }
        internal string WindDirectionUOM {
            get { return data.obs[0][7].GetValueOrDefault().ToString() + "/14"; }
        }
        internal string WindDirection {
            get { return data.obs[0][7].GetValueOrDefault().ToString(); }
        }
        internal string BatteryUOM {
            get { return data.obs[0][8].GetValueOrDefault().ToString() + "/72"; }
        }
        internal string Battery {
            get { return data.obs[0][8].GetValueOrDefault().ToString(); }
        }
        internal string Interval {
            get { return data.obs[0][9].GetValueOrDefault().ToString(); }
        }
        internal string SolarRadiationUOM {
            get { return data.obs[0][10].GetValueOrDefault().ToString() + "/74"; }
        }
        internal string SolarRadiation {
            get { return data.obs[0][10].GetValueOrDefault().ToString(); }
        }
        internal string PrecipitationDay {
            get { return data.obs[0][11].GetValueOrDefault().ToString(); }
        }
        internal string PrecipitationTypeUOM {
            get { return data.obs[0][12].GetValueOrDefault().ToString() + "/25"; }
        }
        internal string PrecipitationType {
            get { return data.obs[0][12].GetValueOrDefault().ToString(); }
        }
        internal string WindSampleInterval {
            get { return data.obs[0][13].GetValueOrDefault().ToString(); }
        }
        internal double SetDaily {
            set { daily = value; }
        }
        internal string DailyUOM {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return daily.ToString("0.#") + "/82";
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MM2Inch(daily).ToString("0.###") + "/105";
                    case (int)WF_UNITS.UK: return daily.ToString("0.#") + "/82";
                    default: return daily.ToString("0.#") + "/82";
                }
            }
        }
        internal string Daily {
            get {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI: return daily.ToString("0.#");
                    case (int)WF_UNITS.US: return WeatherFlow_UDP.MM2Inch(daily).ToString("0.###");
                    case (int)WF_UNITS.UK: return daily.ToString("0.#");
                    default: return daily.ToString("0.#");
                }
            }
        }

        internal int DeviceID {
            get { return data.device_id; }
        }

        internal string Raw {
            get { return raw_packet; }
            set { raw_packet = value; }
        }
    }
}
