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
        internal string Speed {
            get {
                if (si_units)
                    return WeatherFlow_UDP.MS2MPH(data.ob[1]).ToString("0.#");
                else
                    return data.ob[1].ToString();
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
        internal string Distance {
            get { return data.evt[1].ToString(); }
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

        internal string Pressure {
            get {
                double inhg = data.obs[0][1].GetValueOrDefault() * 0.02952998751;
                return inhg.ToString("0.###");
            }
        }
        internal double SetSeaLevel {
            set { sealevel = value; }
        }
        internal string SeaLevel {
            get {
                double inhg = sealevel * 0.02952998751;
                return inhg.ToString("0.###");
            }
        }

        internal string Temperature {
            get {
                if (si_units)
                    return WeatherFlow_UDP.TempF(data.obs[0][2].GetValueOrDefault()).ToString("0.#");
                else
                    return data.obs[0][2].GetValueOrDefault().ToString();
            }
        }
        internal string Humidity {
            get { return data.obs[0][3].GetValueOrDefault().ToString(); }
        }
        internal string Strikes {
            get { return data.obs[0][4].GetValueOrDefault().ToString(); }
        }
        internal string Distance {
            get {
                if (si_units)
                    return WeatherFlow_UDP.KM2Miles(data.obs[0][5].GetValueOrDefault()).ToString("0.#");
                else
                    return data.obs[0][5].GetValueOrDefault().ToString();
                }
        }
        internal string Battery {
            get { return data.obs[0][6].GetValueOrDefault().ToString(); }
        }

        internal double SetDewpoint {
            set { dewpoint = value; }
        }
        internal string Dewpoint {
            get {
                if (si_units)
                    return WeatherFlow_UDP.TempF(dewpoint).ToString("0.#");
                else
                    return dewpoint.ToString("0.#");
            }
        }

        internal double SetApparentTemp {
            set { apparent_temp = value; }
        }
        internal string ApparentTemp {
            get {
                if (si_units)
                    return WeatherFlow_UDP.TempF(apparent_temp).ToString("0.#");
                else
                    return apparent_temp.ToString("0.#");
            }
        }

        internal int SetTrend {
            set { trend = value; }
        }
        internal string Trend {
            get {
                    return trend.ToString();
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
        internal string Illumination {
            get { return data.obs[0][1].GetValueOrDefault().ToString(); }
        }
        internal string UV {
            get { return data.obs[0][2].GetValueOrDefault().ToString(); }
        }
        internal string Rain {
            get {
                if (si_units)
                    return WeatherFlow_UDP.MM2Inch(data.obs[0][3].GetValueOrDefault()).ToString("0.###");
                else
                    return data.obs[0][3].GetValueOrDefault().ToString("0.#");
            }
        }
        internal string RainRate {
            get {
                double interval = data.obs[0][9].GetValueOrDefault();
                double rate = data.obs[0][3].GetValueOrDefault() * 60;

                if (interval > 0)
                    rate = rate / interval;

                if (si_units)
                    return WeatherFlow_UDP.MM2Inch(rate).ToString("0.###");
                else
                    return rate.ToString("0.#");
            }
        }
        internal string WindLull {
            get { return data.obs[0][4].GetValueOrDefault().ToString(); }
        }
        internal string WindSpeed {
            get {
                if (si_units)
                    return WeatherFlow_UDP.MS2MPH(data.obs[0][5].GetValueOrDefault()).ToString("0.#");
                else
                    return data.obs[0][5].GetValueOrDefault().ToString();
            }
        }
        internal string GustSpeed {
            get {
                if (si_units)
                    return WeatherFlow_UDP.MS2MPH(data.obs[0][6].GetValueOrDefault()).ToString("0.#");
                else
                    return data.obs[0][5].GetValueOrDefault().ToString();
            }
        }
        internal string WindDirection {
            get { return data.obs[0][7].GetValueOrDefault().ToString(); }
        }
        internal string Battery {
            get { return data.obs[0][8].GetValueOrDefault().ToString(); }
        }
        internal string Interval {
            get { return data.obs[0][9].GetValueOrDefault().ToString(); }
        }
        internal string SolarRadiation {
            get { return data.obs[0][10].GetValueOrDefault().ToString(); }
        }
        internal string PrecipitationDay {
            get { return data.obs[0][11].GetValueOrDefault().ToString(); }
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
        internal string Daily {
            get {
                if (si_units)
                    return WeatherFlow_UDP.MM2Inch(daily).ToString("0.###");
                else
                    return daily.ToString("0.#");
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
