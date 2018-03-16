﻿
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
using System.Threading;
using System.Xml;
using System.Web;
using System.Reflection;

namespace WFNodeServer {
    class WeatherFlowNS {
        internal static NodeServer NS;
        internal static bool shutdown = false;
        internal static double Elevation = 0;
        private static string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        static void Main(string[] args) {
            string username = "";
            string password = "";
            string isy_host = "";
            int profile = 0;
            bool si_units = false;
            bool hub_node = false;
            int port = 50222;

            foreach (string Cmd in args) {
                string[] parts;
                char[] sep = { '=' };
                parts = Cmd.Split(sep);
                switch (parts[0].ToLower()) {
                    case "username":
                        username = parts[1];
                        break;
                    case "password":
                        password = parts[1];
                        break;
                    case "profile":
                        int.TryParse(parts[1], out profile);
                        break;
                    case "si":
                        si_units = true;
                        break;
                    case "isy":
                        isy_host = parts[1];
                        break;
                    case "elevation":
                        double.TryParse(parts[1], out Elevation);
                        break;
                    case "hub":
                        hub_node = true;
                        break;
                    case "udp_port":
                        int.TryParse(parts[1], out port);
                        break;
                    default:
                        Console.WriteLine("Usage: WFNodeServer username=<isy user> password=<isy password> profile=<profile number>");
                        Console.WriteLine("                    [isy=<is ip address/hostname>] [si]");
                        break;
                }
            }

            Console.WriteLine("WeatherFlow Node Server " + VERSION);

            NS = new NodeServer(isy_host, username, password, profile, si_units, hub_node, port);

            while (!shutdown) {
                Thread.Sleep(30000);
            }
        }
    }

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
                return d.ToLower();
            }
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
                if (data.ob[2] >= 348.75 || data.ob[2] < 11.25)
                    return 0;
                else if (data.ob[2] >= 11.25 && data.ob[2] < 33.75)
                    return 1;
                else if (data.ob[2] >= 33.75 && data.ob[2] < 56.25)
                    return 2;
                else if (data.ob[2] >= 56.25 && data.ob[2] < 78.75)
                    return 3;
                else if (data.ob[2] >= 78.75 && data.ob[2] < 101.25)
                    return 4;
                else if (data.ob[2] >= 101.25 && data.ob[2] < 123.75)
                    return 5;
                else if (data.ob[2] >= 123.75 && data.ob[2] < 146.25)
                    return 6;
                else if (data.ob[2] >= 146.25 && data.ob[2] < 168.75)
                    return 7;
                else if (data.ob[2] >= 168.75 && data.ob[2] < 191.25)
                    return 8;
                else if (data.ob[2] >= 191.25 && data.ob[2] < 213.75)
                    return 9;
                else if (data.ob[2] >= 213.75 && data.ob[2] < 236.25)
                    return 10;
                else if (data.ob[2] >= 236.25 && data.ob[2] < 258.75)
                    return 11;
                else if (data.ob[2] >= 258.75 && data.ob[2] < 281.25)
                    return 12;
                else if (data.ob[2] >= 281.25 && data.ob[2] < 303.75)
                    return 13;
                else if (data.ob[2] >= 303.75 && data.ob[2] < 326.25)
                    return 14;
                else if (data.ob[2] >= 326.25 && data.ob[2] < 348.75)
                    return 15;
            return (int)data.ob[2];
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
                d += "l";
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
        internal string TimeStamp {
            get { return data.timestamp.ToString(); }
        }
        internal string Uptime {
            get { return data.uptime.ToString(); }
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
                return d.ToLower();
            }
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
                return inhg.ToString("0.##");
            }
        }
        internal double SetSeaLevel {
            set { sealevel = value; }
        }
        internal string SeaLevel {
            get {
                double inhg = sealevel * 0.02952998751;
                return inhg.ToString("0.##");
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
                    return WeatherFlow_UDP.MM2Inch(data.obs[0][3].GetValueOrDefault()).ToString("0.##");
                else
                    return data.obs[0][3].GetValueOrDefault().ToString("0.#");
                }
        }
        internal string RainRate {
            get {
                double rate = data.obs[0][4].GetValueOrDefault() * 60;
                if (si_units)
                    return WeatherFlow_UDP.MM2Inch(rate).ToString("0.##");
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
                    return WeatherFlow_UDP.MM2Inch(daily).ToString("0.##");
                else
                    return daily.ToString("0.#");
            }
        }

        internal string Raw {
            get { return raw_packet; }
            set { raw_packet = value; }
        }
    }

    internal class NodeServer {
        internal rest Rest;
        internal event SkyEvent WFSkySubscribers = null;
        internal event AirEvent WFAirSubscribers = null;
        internal event DeviceEvent WFDeviceSubscribers = null;
        internal event UpdateEvent WFUpdateSubscribers = null;
        internal event HubEvent WFHubSubscribers = null;
        internal event RapidEvent WFRapidSubscribers = null;
        internal event LightningEvent WFLightningSubscribers = null;
        internal event RainEvent WFRainSubscribers = null;
        internal bool active = false;
        internal Dictionary<string, string> NodeList = new Dictionary<string, string>();
        internal Dictionary<string, int> MinutsSinceUpdate = new Dictionary<string, int>();
        internal int Profile = 0;
        internal WeatherFlow_UDP udp_client;
        internal bool SIUnits { get; set; }

        internal NodeServer(string host, string user, string pass, int profile, bool si_units, bool hub_node, int port) {
            Thread udp_thread;

            SIUnits = si_units;
            Profile = profile;

            if (host == "") {
                //ISYDetect.IsyAutoDetect();  // UPNP detection
                host = ISYDetect.FindISY();
                if (host == "") {
                    Console.WriteLine("Failed to detect an ISY on the network. Please add isy=<your isy IP Address> to the command line.");
                    WeatherFlowNS.shutdown = true;
                    return;
                }
            } else {
                Console.WriteLine("Using ISY at " + host);
            }


            Rest = new rest("http://" + host + "/rest/");
            Rest.Username = user;
            Rest.Password = pass;

            // Is there some way to detect what profile we're installed at?
            //  We can look at the nodes "/rest/nodes" and search the output
            // for a node with nodedef=WeatherFlow. The node address prefix
            // will tell us which profile we're using.
            FindOurNodes();
            if (NodeList.Count > 0) {
                // Parse profile from node address
                int.TryParse(NodeList.ElementAt(0).Key.Substring(1, 3), out Profile);
                Console.WriteLine("Detected profile number " + Profile.ToString());
            } else {
                // Should we try and create a node?  No, let's do this in the event handlers
                // so we can use the serial number as the node address
                //string address = "n" + profile.ToString("000") + "_weatherflow1";

                //Rest.REST("ns/" + profile.ToString() + "/nodes/" + address +
                //    "/add/WeatherFlow/?name=WeatherFlow");
                //NodeList.Add(address, si_units);
            }
                
            // If we don't know the profile number, we shouldn't do
            // anything at this point.  Goal may be to get profile
            // number from command line so we can continue
            if (Profile == 0)
                return;

            // If si units, switch the nodedef 
            foreach (string address in NodeList.Keys) {
                if (si_units && (NodeList[address] == "WF_Air")) {
                    Rest.REST("ns/" + profile.ToString() + "/nodes/" + address + "/change/WF_AirSI");
                } else if (!si_units && (NodeList[address] == "WF_AirSI")) {
                    Rest.REST("ns/" + profile.ToString() + "/nodes/" + address + "/change/WF_Air");
                } else if (si_units && (NodeList[address] == "WF_Sky")) {
                    Rest.REST("ns/" + profile.ToString() + "/nodes/" + address + "/change/WF_SkySI");
                } else if (!si_units && (NodeList[address] == "WF_SkySI")) {
                    Rest.REST("ns/" + profile.ToString() + "/nodes/" + address + "/change/WF_Sky");
                } else if (!si_units && (NodeList[address] == "WF_Sky")) {
                } else if (!si_units && (NodeList[address] == "WF_Air")) {
                } else if (si_units && (NodeList[address] == "WF_SkySI")) {
                } else if (si_units && (NodeList[address] == "WF_AirSI")) {
                } else {
                    Console.WriteLine("Node with address " + address + " has unknown type " + NodeList[address]);
                }

                MinutsSinceUpdate.Add(address, 0);
            }

            WFNServer wfn = new WFNServer("/WeatherFlow", 8288, profile);
            Console.WriteLine("Started on port 8288");

            WFAirSubscribers += new AirEvent(HandleAir);
            WFSkySubscribers += new SkyEvent(HandleSky);
            WFDeviceSubscribers += new DeviceEvent(HandleDevice);
            WFUpdateSubscribers += new UpdateEvent(GetUpdate);
            WFRapidSubscribers += new RapidEvent(HandleWind);
            WFRainSubscribers += new RainEvent(HandleRain);
            WFLightningSubscribers += new LightningEvent(HandleLightning);

            // TODO: Make this configuraboe
            if (hub_node)
                WFHubSubscribers += new HubEvent(HandleHub);

            // Start a thread to monitor the UDP port
            Console.WriteLine("Starting WeatherFlow data collection thread.");
            udp_client = new WeatherFlow_UDP(port);
            udp_thread = new Thread(new ThreadStart(udp_client.WeatherFlowThread));
            udp_thread.IsBackground = true;
            udp_thread.Start();

            System.Timers.Timer UpdateTimer = new System.Timers.Timer();
            UpdateTimer.AutoReset = true;
            UpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(UpdateTimer_Elapsed);
            UpdateTimer.Interval = 60000;
            UpdateTimer.Start();
        }

        void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";

            foreach (string address in NodeList.Keys) {
                report = prefix + address + "/report/status/GV0/" + MinutsSinceUpdate[address].ToString() + "/45";
                Rest.REST(report);
                MinutsSinceUpdate[address]++;
            }
        }

        internal void RaiseAirEvent(Object sender, WFNodeServer.AirEventArgs e) {
            if (WFAirSubscribers != null)
                WFAirSubscribers(sender, e);
        }
        internal void RaiseSkyEvent(Object sender, WFNodeServer.SkyEventArgs e) {
            if (WFSkySubscribers != null)
                WFSkySubscribers(sender, e);
        }
        internal void RaiseDeviceEvent(Object sender, WFNodeServer.DeviceEventArgs e) {
            if (WFDeviceSubscribers != null)
                WFDeviceSubscribers(sender, e);
        }
        internal void RaiseUpdateEvent(Object sender, WFNodeServer.UpdateEventArgs e) {
            if (WFUpdateSubscribers != null)
                WFUpdateSubscribers(sender, e);
        }
        internal void RaiseHubEvent(Object sender, WFNodeServer.HubEventArgs e) {
            if (WFHubSubscribers != null)
                WFHubSubscribers(sender, e);
        }
        internal void RaiseRapidEvent(Object sender, WFNodeServer.RapidEventArgs e) {
            if (WFRapidSubscribers != null)
                WFRapidSubscribers(sender, e);
        }
        internal void RaiseLightningEvent(Object sender, WFNodeServer.LightningEventArgs e) {
            if (WFLightningSubscribers != null)
                WFLightningSubscribers(sender, e);
        }
        internal void RaiseRainEvent(Object sender, WFNodeServer.RainEventArgs e) {
            if (WFRainSubscribers != null)
                WFRainSubscribers(sender, e);
        }

        // Handler that is called when we receive Air data
        internal void HandleAir(object sender, AirEventArgs air) {
            string report;
            string unit;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + air.SerialNumber;

            air.si_units = SIUnits;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + air.SerialNumber + " doesn't exist, create it.");
                Console.WriteLine("Debug");
                Console.WriteLine(air.Raw);

                Rest.REST("ns/" + Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Air" + ((SIUnits) ? "SI" : "") + "/?name=WeatherFlow%20(" + air.SerialNumber + ")");
                NodeList.Add(address, "WF_Air" + ((SIUnits) ? "SI" : ""));
                MinutsSinceUpdate.Add(address, 0);
            }

            //report = prefix + address + "/report/status/GV0/" + air.TS + "/25";
            //Rest.REST(report);

            unit = (SIUnits) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV1/" + air.Temperature + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV2/" + air.Humidity + "/PERCENT";
            Rest.REST(report);

            report = prefix + address + "/report/status/GV3/" + air.SeaLevel + "/23";
            Rest.REST(report);

            report = prefix + address + "/report/status/GV4/" + air.Strikes + "/0";
            Rest.REST(report);

            unit = (SIUnits) ? "/0" : "/KM";
            report = prefix + address + "/report/status/GV5/" + air.Distance + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV6/" + air.Battery + "/72";
            Rest.REST(report);

            unit = (SIUnits) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV7/" + air.Dewpoint + unit;
            Rest.REST(report);

            unit = (SIUnits) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV8/" + air.ApparentTemp + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV9/" + air.Trend + "/25";
            Rest.REST(report);
        }

        // Handler that is called when re receive Sky data
        internal void HandleSky(object sender, SkyEventArgs sky) {
            string report;
            string unit;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + sky.SerialNumber;

            sky.si_units = SIUnits;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + sky.SerialNumber + " doesn't exist, create it.");
                Console.WriteLine("Debug");
                Console.WriteLine(sky.Raw);

                Rest.REST("ns/" + Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Sky" + ((SIUnits) ? "SI" : "") + "/?name=WeatherFlow%20(" + sky.SerialNumber + ")");
                NodeList.Add(address, "WF_Sky" + ((SIUnits) ? "SI" : ""));
                MinutsSinceUpdate.Add(address, 0);
            }

            //report = prefix + address + "/report/status/GV0/" + sky.TS + "/25";
            //Rest.REST(report);
            report = prefix + address + "/report/status/GV1/" + sky.Illumination + "/36";
            Rest.REST(report);
            report = prefix + address + "/report/status/GV2/" + sky.UV + "/71";
            Rest.REST(report);
            report = prefix + address + "/report/status/GV3/" + sky.SolarRadiation + "/74";
            Rest.REST(report);
            unit = (SIUnits) ? "/48" : "/49";
            report = prefix + address + "/report/status/GV4/" + sky.WindSpeed + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV5/" + sky.GustSpeed + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV6/" + sky.WindDirection + "/25";
            Rest.REST(report);

            // Currently we just report the rain over 1 minute. If we want the rate
            // this number should be multiplied by 60 to get inches/hour (which is UOM 24)
            // mm/hour is uom 46
            //unit = (SIUnits) ? "/105" : "/82";
            //report = prefix + address + "/report/status/GV7/" + sky.Rain + unit;
            //Rest.REST(report);
            unit = (SIUnits) ? "/24" : "/46";
            report = prefix + address + "/report/status/GV7/" + sky.RainRate + unit;
            Rest.REST(report);

            unit = (SIUnits) ? "/105" : "/82";
            report = prefix + address + "/report/status/GV8/" + sky.Daily + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV9/" + sky.Battery + "/72";
            Rest.REST(report);
        }

        internal void HandleDevice(object sender, DeviceEventArgs device) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + device.SerialNumber;
            string units;
            int up;

            // Somehow we need to know what type of device this is?
            if (!NodeList.Keys.Contains(address)) {
                return;
            }

            // Device uptime is in seconds.  Lets make this easier to understand as
            // the time goes up.
            if (device.Uptime < 120) {
                up = device.Uptime;
                units = "/57";
            } else if (device.Uptime < (60 * 60 * 2)) {
                up = device.Uptime / 60;  // Minutes
                units = "/45";
            } else if (device.Uptime < (60 * 60 * 24 * 2)) {
                up = device.Uptime / (60 * 60); // Hours
                units = "/20";
            } else {
                up = device.Uptime / (60 * 60 * 24); // Days
                units = "/10";
            }


            if (NodeList[address].Contains("Air")) {
                report = prefix + address + "/report/status/GV10/" + up.ToString("0.#") + units;
                Rest.REST(report);
                report = prefix + address + "/report/status/GV11/" + device.RSSI + "/25";
                Rest.REST(report);
            } else if (NodeList[address].Contains("Sky")) {
                report = prefix + address + "/report/status/GV10/" + up.ToString("0.#") + units;
                Rest.REST(report);
                report = prefix + address + "/report/status/GV11/" + device.RSSI + "/25";
                Rest.REST(report);
            }
        }

        internal void HandleWind(object sender, RapidEventArgs wind) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + wind.SerialNumber;
            string unit;

            if (!NodeList.Keys.Contains(address))
                return;

            wind.si_units = SIUnits;

            unit = (SIUnits) ? "/48" : "/49";
            report = prefix + address + "/report/status/GV13/" + wind.Speed + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV12/" + wind.Direction.ToString() + "/25";
            Rest.REST(report);
        }

        internal void HandleLightning(object sender, LightningEventArgs strike) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + strike.SerialNumber;

            strike.si_units = SIUnits;

            if (!NodeList.Keys.Contains(address)) {
                Console.WriteLine("Device " + strike.SerialNumber + " doesn't exist, create it.");

                Rest.REST("ns/" + Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Lightning/?name=WeatherFlow%20(" + strike.SerialNumber + ")");
                NodeList.Add(address, "WF_Lightning");
                //MinutsSinceUpdate.Add(address, 0);
            }
            report = prefix + address + "/report/status/GV0/" + strike.TimeStamp + "/25";
            Rest.REST(report);

            string unit = (SIUnits) ? "/0" : "/KM";
            report = prefix + address + "/report/status/GV1/" + strike.Distance + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV2/" + strike.Energy + "/0";
            Rest.REST(report);

        }

        internal void HandleRain(object sender, RainEventArgs rain) {
            //string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + rain.SerialNumber;

            if (!NodeList.Keys.Contains(address))
                return;

            Console.WriteLine("Rain Start Event at : " + rain.TimeStamp);
        }

        internal void HandleHub(object sender, HubEventArgs hub) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + hub.SerialNumber;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + hub.SerialNumber + " doesn't exist, create it.");

                Rest.REST("ns/" + Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Hub/?name=WeatherFlow%20(" + hub.SerialNumber + ")");
                NodeList.Add(address, "WF_Hub");
            }

            Console.WriteLine("HUB: firmware    = " + hub.Firmware);
            Console.WriteLine("HUB: reset flags = " + hub.ResetFlags);
            Console.WriteLine("HUB: stack       = " + hub.Stack);
            Console.WriteLine("HUB: fs          = " + hub.FS);
            Console.WriteLine("HUB: rssi        = " + hub.RSSI);
            Console.WriteLine("HUB: timestamp   = " + hub.TimeStamp);
            Console.WriteLine("HUB: uptime      = " + hub.Uptime);
        }

        internal void GetUpdate(object sender, UpdateEventArgs update) {
            string address = "n" + Profile.ToString("000") + "_" + update.SerialNumber;

            if (MinutsSinceUpdate.Keys.Contains(address)) {
                MinutsSinceUpdate[address] = 0;
            }
        }

        internal void AddNode(string address) {
            Console.WriteLine("Adding " + address + " to our list.");
            NodeList.Add(address, "");
        }
        internal void RemoveNode(string address) {
            Console.WriteLine("Removing " + address + " from our list.");
            NodeList.Remove(address);
        }

        // Query the ISY nodelist and find the addresses of all our nodes.
        private void FindOurNodes() {
            string query = "nodes";
            string xml;
            XmlDocument xmld;
            XmlNode root;
            XmlNodeList list;

            try {
                xml = Rest.REST(query);
                xmld = new XmlDocument();
                xmld.LoadXml(xml);

                root = xmld.FirstChild;
                root = xmld.SelectSingleNode("nodes");
                list = root.ChildNodes;

                foreach (XmlNode node in list) {
                    if (node.Name != "node")
                        continue;

                    try {
                        if (node.Attributes["nodeDefId"].Value == "WeatherFlow") {
                            // Found one. 
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WeatherFlow");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Air") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Air");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirSI");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Sky") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Sky");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkySI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkySI");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }
                    } catch {
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("XML parsing failed: " + ex.Message);
            }
        }
    }


}
