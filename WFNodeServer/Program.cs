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
        private static string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        static void Main(string[] args) {
            string username = "";
            string password = "";
            string isy_host = "";
            int profile = 0;
            bool si_units = false;

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
                    default:
                        Console.WriteLine("Usage: WFNodeServer username=<isy user> password=<isy password> profile=<profile number>");
                        Console.WriteLine("                    [isy=<is ip address/hostname>] [si]");
                        break;
                }
            }

            Console.WriteLine("WeatherFlow Node Server " + VERSION);

            NS = new NodeServer(isy_host, username, password, profile, si_units);

            while (!shutdown) {
                Thread.Sleep(30000);
            }
        }
    }

    internal delegate void AirEvent(Object sender, AirEventArgs e);
    internal delegate void SkyEvent(Object sender, SkyEventArgs e);
    internal delegate void DeviceEvent(Object sender, DeviceEventArgs e);
    internal delegate void UpdateEvent(Object sender, UpdateEventArgs e);

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
    }

    internal class SkyEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.SkyData data;
        internal bool si_units { get; set; }

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
    }

    internal class NodeServer {
        internal rest Rest;
        internal event SkyEvent WFSkySubscribers = null;
        internal event AirEvent WFAirSubscribers = null;
        internal event DeviceEvent WFDeviceSubscribers = null;
        internal event UpdateEvent WFUpdateSubscribers = null;
        internal bool active = false;
        internal Dictionary<string, string> NodeList = new Dictionary<string, string>();
        internal Dictionary<string, int> MinutsSinceUpdate = new Dictionary<string, int>();
        internal int Profile = 0;
        internal WeatherFlow_UDP udp_client;
        internal bool SIUnits { get; set; }

        internal NodeServer(string host, string user, string pass, int profile, bool si_units) {
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

            // Start a thread to monitor the UDP port
            Console.WriteLine("Starting WeatherFlow data collection thread.");
            udp_client = new WeatherFlow_UDP();
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

            report = prefix + address + "/report/status/GV3/" + air.Pressure + "/23";
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
            unit = (SIUnits) ? "/105" : "/82";
            report = prefix + address + "/report/status/GV7/" + sky.Rain + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV8/" + sky.Battery + "/72";
            Rest.REST(report);
        }

        internal void HandleDevice(object sender, DeviceEventArgs device) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";
            string address = "n" + Profile.ToString("000") + "_" + device.SerialNumber;

            // Somehow we need to know what type of device this is?
            if (!NodeList.Keys.Contains(address)) {
                return;
            }

            if (NodeList[address].Contains("Air")) {
                report = prefix + address + "/report/status/GV10/" + device.UpTime + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV11/" + device.RSSI + "/25";
                Rest.REST(report);
            } else if (NodeList[address].Contains("Sky")) {
                report = prefix + address + "/report/status/GV9/" + device.UpTime + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV10/" + device.RSSI + "/25";
                Rest.REST(report);
            }
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

            xml = Rest.REST(query);
            try {
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
