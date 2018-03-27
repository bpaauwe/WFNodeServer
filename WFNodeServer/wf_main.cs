
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
using System.IO;
using System.Web;
using System.Reflection;
using System.Web.Script.Serialization;

namespace WFNodeServer {
    static class WF_Config {
        internal static string Username { get; set; }
        internal static string Password { get; set; }
        internal static string ISY { get; set; }
        internal static int Profile { get; set; }
        internal static int Port { get; set; }
        internal static int UDPPort { get; set; }
        internal static bool Hub { get; set; }
        internal static bool SI { get; set; }
        internal static List<StationInfo> WFStationInfo { get; set; }
        internal static int ProfileVersion { get; set; }
        internal static bool Valid { get; set; }
    }

    public class cfgstate {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ISY { get; set; }
        public int Profile { get; set; }
        public int Port { get; set; }
        public int UDPPort { get; set; }
        public bool Hub { get; set; }
        public bool SI { get; set; }
        public List<StationInfo> WFStationInfo { get; set; }
        public int ProfileVersion { get; set; }

        public cfgstate() {
            Username = WF_Config.Username;
            Password = WF_Config.Password;
            ISY = WF_Config.ISY;
            Profile = WF_Config.Profile;
            Port = WF_Config.Port;
            UDPPort = WF_Config.UDPPort;
            Hub = WF_Config.Hub;
            SI = WF_Config.SI;
            WFStationInfo = WF_Config.WFStationInfo;
            ProfileVersion = WF_Config.ProfileVersion;
        }

        internal void LoadState() {
            WF_Config.Username = Username;
            WF_Config.Password = Password;
            WF_Config.ISY = ISY;
            WF_Config.Profile = Profile;
            WF_Config.Port = Port;
            WF_Config.UDPPort = UDPPort;
            WF_Config.Hub = Hub;
            WF_Config.SI = SI;
            WF_Config.WFStationInfo = WFStationInfo;
            WF_Config.ProfileVersion = ProfileVersion;

            if ((Password != "") && (Username != "") && (Profile != 0) && (WFStationInfo.Count > 0))
                WF_Config.Valid = true;
        }
    }

    public class StationInfo {
        public int station_id { get; set; }
        public int air_id { get; set; }
        public int sky_id { get; set; }
        public string air_sn { get; set; }
        public string sky_sn { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double elevation { get; set; }
        public bool remote { get; set; }
        public bool rapid { get; set; }

        public StationInfo() {
            station_id = -1;
            elevation = 0;
            remote = false;
            rapid = true;
            air_sn = "";
            air_id = -1;
            sky_sn = "";
            sky_id = -1;
        }

        public StationInfo(int id) {
            station_id = id;
        }
    }

    partial class WeatherFlowNS {
        internal static NodeServer NS;
        internal static bool shutdown = false;
        internal static double Elevation = 0;
        internal static string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        internal static int ProfileVersion = 2;
        internal static bool Debug = false;
        internal static string ConfigFile = "wfnodeserver.json";

        static void Main(string[] args) {
            string username = "";
            string password = "";
            string isy_host = "";
            int profile = 0;
            int port = 50222;
            bool once = false;

            WF_Config.ISY = "";
            WF_Config.Username = "admin";
            WF_Config.Password = "";
            WF_Config.Profile = 0;
            WF_Config.Hub = false;
            WF_Config.Port = 8288;
            WF_Config.SI = false;
            WF_Config.UDPPort = 50222;
            WF_Config.Valid = false;
            WF_Config.WFStationInfo = new List<StationInfo>();

            // First check for command line config file path
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Contains("config"))
                    ConfigFile = args[i].Split('=')[1];
                if (args[i] == "-f")
                    ConfigFile = args[++i];
            }

            ReadConfiguration();

            foreach (string Cmd in args) {
                string[] parts;
                char[] sep = { '=' };
                parts = Cmd.Split(sep);
                switch (parts[0].ToLower()) {
                    case "username":
                        username = parts[1];
                        WF_Config.Username = parts[1];
                        break;
                    case "password":
                        password = parts[1];
                        WF_Config.Password = parts[1];
                        break;
                    case "profile":
                        int.TryParse(parts[1], out profile);
                        WF_Config.Profile = profile;
                        break;
                    case "si":
                        WF_Config.SI = true;
                        break;
                    case "isy":
                        isy_host = parts[1];
                        WF_Config.ISY = parts[1];
                        break;
                    case "hub":
                        WF_Config.Hub = true;
                        break;
                    case "udp_port":
                        int.TryParse(parts[1], out port);
                        WF_Config.UDPPort = port;
                        break;
                    case "debug":
                        Debug = true;
                        break;
                    case "config":
                        break;
                    default:
                        if (!once) {
                            Console.WriteLine("Usage: WFNodeServer username=<isy user> password=<isy password>");
                            Console.WriteLine("                    profile=<profile number>");
                            Console.WriteLine("                    [config=<confuration path/file]");
                            Console.WriteLine("                    [isy=<is ip address/hostname>] [si] [hub]");
                            once = true;
                        }
                        break;
                }
            }

            Console.WriteLine("WeatherFlow Node Server " + VERSION);

            if ((WF_Config.Password != "") && (WF_Config.Username != "") && (WF_Config.Profile != 0))
                WF_Config.Valid = true;
            else 
                WF_Config.Valid = false;

            NS = new NodeServer();

            while (!shutdown) {
                Thread.Sleep(30000);
            }
        }

        internal static string SaveConfiguration() {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            cfgstate s = new cfgstate();

            try {
                using (StreamWriter sw = new StreamWriter(ConfigFile)) {
                    sw.Write(serializer.Serialize(s));
                }
                //sw.Close();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
            return "Configuration Saved";
        }

        internal static void ReadConfiguration() {
            cfgstate cfgObj;
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            char[] buf = new char[2048];
            int len;

            try {
                using (StreamReader sr = new StreamReader(ConfigFile)) {
                    len = sr.Read(buf, 0, 2048);
                }
                //sr.Close();
            } catch {
                Console.WriteLine("Failed to read configuration file wfnodeserver.json");
                return;
            }

            try {
                string json = new string(buf);
                cfgObj = serializer.Deserialize<cfgstate>(json.Substring(0, len));
                cfgObj.LoadState();
            } catch (Exception ex) {
                Console.WriteLine("Failed to import configuration.");
                Console.WriteLine(ex.Message);
            }
        }
    }

    internal partial class NodeServer {
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
        internal wf_websocket wsi = new wf_websocket("ws.weatherflow.com", 80, "/swd/data?api_key=" + api_key);
        private System.Timers.Timer UpdateTimer = new System.Timers.Timer();
        private bool ProfileDetected = false;
        internal Heartbeat heartbeat = new Heartbeat();
        internal WeatherFlow_UDP udp_client = new WeatherFlow_UDP();

        //internal NodeServer(string host, string user, string pass, int profile, bool si_units, bool hub_node, int port) {
        internal NodeServer() {

            // Start server to handle config and ISY queries
            WFNServer wfn = new WFNServer("/WeatherFlow", WF_Config.Port, api_key);
            Console.WriteLine("Started on port " + WF_Config.Port.ToString());

            Rest = new rest();

            WFAirSubscribers += new AirEvent(HandleAir);
            WFSkySubscribers += new SkyEvent(HandleSky);
            WFDeviceSubscribers += new DeviceEvent(HandleDevice);
            WFUpdateSubscribers += new UpdateEvent(GetUpdate);
            WFRapidSubscribers += new RapidEvent(HandleWind);
            WFRainSubscribers += new RainEvent(HandleRain);
            WFLightningSubscribers += new LightningEvent(HandleLightning);
            WFHubSubscribers += new HubEvent(HandleHub);


            // At this point we should check to see if we have a valid 
            // configuration and can continue to initialize things

            if (WF_Config.Valid) {
                udp_client.Port = WF_Config.UDPPort;
                InitializeISY();
                udp_client.Start();
                heartbeat.Start();
            } else {
                Console.WriteLine("Please point your browser at http://localhost:" + WF_Config.Port.ToString() + "/config to configure the node server.");
            }
        }

        //
        // Initiailize communication with the ISY.
        //
        // Set up authentication, Copy over any new profile
        // files, query the ISY for our nodes.
        // 
        // We can attemp this as soon as we have a password
        // and username for the ISY.
        internal void InitializeISY() {
            if (!SetupRest())
                return;
            LookupProfile();
            UpdateProfileFiles();
            ConfigureNodes();
        }

        internal void UpdateProfileFiles() {

            if (WF_Config.ProfileVersion != WeatherFlowNS.ProfileVersion) {
                Console.WriteLine("Updating profile files on ISY...");
                // First remove existing profile files
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/profile/remove");

                // Install our embedded versions
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "EN_US.TXT", "nls");
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "I_EDIT.XML", "editor");
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "I_NDEFS.XML", "nodedef");

                // This command is documented but doesn't exist yet
                //Rest.REST("ns/2/profile/reload");
                Console.WriteLine("Files uploaded, ISY must be rebooted for changes to take effect.");

                WF_Config.ProfileVersion = WeatherFlowNS.ProfileVersion;
                Console.WriteLine(WeatherFlowNS.SaveConfiguration());
                
            }
        }

        internal bool SetupRest() {
            if (WF_Config.ISY == "") {
                //ISYDetect.IsyAutoDetect();  // UPNP detection
                WF_Config.ISY = ISYDetect.FindISY();
                if (WF_Config.ISY == "") {
                    Console.WriteLine("Failed to detect an ISY on the network. Please add isy=<your isy IP Address> to the command line.");
                    //  TODO: Wait on configuration here.  
                    WeatherFlowNS.shutdown = true;
                    return false;
                }
            }
            Console.WriteLine("Using ISY at " + WF_Config.ISY);

            Rest.Base = "http://" + WF_Config.ISY + "/rest/";
            Rest.AuthRequired = true;
            Rest.Username = WF_Config.Username;
            Rest.Password = WF_Config.Password;
            return true;
        }

        internal void LookupProfile() {
            XmlDocument xmld;
            XmlNode root;
            int ProfileNum = 0;
            // Look up the node server's currently installed to see if we're
            // actually installed.
            string profiles = Rest.REST("profiles/ns/0/connection");

            // parse profiles looking for connections->connection->name fields
            try {
                xmld = new XmlDocument();
                xmld.LoadXml(profiles);

                root = xmld.FirstChild;
                root = xmld.SelectSingleNode("connections");
                foreach (XmlNode node in root.ChildNodes) {
                    string profile = node.Attributes["profile"].Value;
                    string name = node.SelectSingleNode("name").InnerText;
                    if (name == "WeatherFlow") {
                        int.TryParse(profile, out ProfileNum);
                        Console.WriteLine("Detected profile number " + ProfileNum.ToString());
                        WF_Config.Profile = ProfileNum;
                        ProfileDetected = true;
                        return;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Parsing profiles failed: " + ex.Message);
            }
        }

        internal void ConfigureNodes() {
            NodeList.Clear();

            FindOurNodes();
            if (NodeList.Count > 0 && !ProfileDetected) {
                int Profile;
                // Parse profile from node address
                int.TryParse(NodeList.ElementAt(0).Key.Substring(1, 3), out Profile);
                Console.WriteLine("Detected profile number " + Profile.ToString() + " from nodelist");
                WF_Config.Profile = Profile;
                ProfileDetected = true;
            }
                
            // If si units, switch the nodedef 
            foreach (string address in NodeList.Keys) {
                if (WF_Config.SI && (NodeList[address] == "WF_Air")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_AirSI");
                } else if (!WF_Config.SI && (NodeList[address] == "WF_AirSI")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Air");
                } else if (WF_Config.SI && (NodeList[address] == "WF_Sky")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_SkySI");
                } else if (!WF_Config.SI && (NodeList[address] == "WF_SkySI")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Sky");
                } else if (!WF_Config.SI && (NodeList[address] == "WF_Sky")) {
                } else if (!WF_Config.SI && (NodeList[address] == "WF_Air")) {
                } else if (WF_Config.SI && (NodeList[address] == "WF_SkySI")) {
                } else if (WF_Config.SI && (NodeList[address] == "WF_AirSI")) {
                } else if (WF_Config.SI && (NodeList[address] == "WF_Lightning")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_LightningSI");
                } else if (!WF_Config.SI && (NodeList[address] == "WF_LightningSI")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Lightning");
                } else if (WF_Config.SI && NodeList[address] == "WF_LightningSI") {
                } else if (!WF_Config.SI && NodeList[address] == "WF_Lightning") {
                } else if (WF_Config.SI && (NodeList[address] == "WF_RapidWind")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWindSI");
                } else if (!WF_Config.SI && (NodeList[address] == "WF_RapidWindSI")) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWind");
                } else if (WF_Config.SI && NodeList[address] == "WF_RapidWindSI") {
                } else if (!WF_Config.SI && NodeList[address] == "WF_RapidWind") {
                } else if (NodeList[address] == "WF_SkyD") {
                } else if (NodeList[address] == "WF_AirD") {
                } else if (NodeList[address] == "WF_Hub") {
                } else {
                    Console.WriteLine("Node with address " + address + " has unknown type " + NodeList[address]);
                }
            }
        }

        internal void StartWebSocket() {
            if (wsi.Started) {
                Console.WriteLine("Shutting down web socket connection.");
                wsi.Stop();
            }

            wsi.Start();
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.remote) {
                    Console.WriteLine("    Start listening for packets from station " + s.station_id.ToString());
                    if (s.air_id > 0)
                        wsi.StartListen(s.air_id.ToString());
                    if (s.sky_id > 0) {
                        wsi.StartListen(s.sky_id.ToString());
                        if (s.rapid)
                            wsi.StartListenRapid(s.sky_id.ToString());
                    }
                }
            }
            //wsi.StartListenRapid("4286");
        }

        internal void DeleteStation(int id) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.station_id == id) {
                    WF_Config.WFStationInfo.Remove(s);
                    return;
                }
            }
        }

        internal void AddStation(int id, double elevation, int air, int sky, bool remote, string air_sn, string sky_sn, bool rapid) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.station_id == id) {
                    s.remote = remote;
                    s.elevation = elevation;
                    s.air_id = air;
                    s.sky_id = sky;
                    if (sky_sn != "")
                        s.sky_sn = sky_sn;
                    if (air_sn != "")
                        s.air_sn = air_sn;
                    s.rapid = rapid;
                    return;
                }
            }

            // Doesn't exist yet, add it
            StationInfo si = new StationInfo(id);
            si.remote = remote;
            si.elevation = elevation;
            si.air_id = air;
            si.sky_id = sky;
            si.sky_sn = sky_sn;
            si.air_sn = air_sn;
            si.rapid = rapid;
            WF_Config.WFStationInfo.Add(si);
        }

        internal void AddStationAir(int id, int air, string air_sn) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.station_id == id) {
                    Console.WriteLine("AddStationAir: Found station " + id.ToString());
                    s.air_id = air;
                    s.air_sn = air_sn;
                    return;
                }
            }

            StationInfo si = new StationInfo(id);
            si.remote = false;
            si.elevation = 0;
            si.air_id = air;
            si.sky_id = -1;
            si.sky_sn = "";
            si.air_sn = air_sn;
            si.rapid = false;
            Console.WriteLine("AddStationAir: Adding station " + id.ToString());
            WF_Config.WFStationInfo.Add(si);
        }

        internal void AddStationSky(int id, int sky, string sky_sn) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.station_id == id) {
                    Console.WriteLine("AddStationSky: Found station " + id.ToString());
                    s.sky_id = sky;
                    s.sky_sn = sky_sn;
                    return;
                }
            }

            StationInfo si = new StationInfo(id);
            si.remote = false;
            si.elevation = 0;
            si.air_id = -1;
            si.sky_id = sky;
            si.sky_sn = sky_sn;
            si.air_sn = "";
            si.rapid = false;
            Console.WriteLine("AddStationSky: Adding station " + id.ToString());
            WF_Config.WFStationInfo.Add(si);
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
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + air.SerialNumber;
            string sec_address = "n" + WF_Config.Profile.ToString("000") + "_" + air.SerialNumber + "_d";

            air.si_units = WF_Config.SI;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + air.SerialNumber + " doesn't exist, create it.");
                if (WeatherFlowNS.Debug) {
                    Console.WriteLine("Debug:");
                    Console.WriteLine(air.Raw);
                }

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Air" + ((WF_Config.SI) ? "SI" : "") + "/?name=WeatherFlow%20(" + air.SerialNumber + ")");
                NodeList.Add(address, "WF_Air" + ((WF_Config.SI) ? "SI" : ""));
            }

            StationInfo sinfo = wf_station.FindStationAir(air.serial_number);
            if (sinfo.station_id == -1)
                AddStationAir(0, air.DeviceID, air.serial_number);

            //report = prefix + address + "/report/status/GV0/" + air.TS + "/25";
            //Rest.REST(report);

            unit = (WF_Config.SI) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV1/" + air.Temperature + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV2/" + air.Humidity + "/PERCENT";
            Rest.REST(report);

            report = prefix + address + "/report/status/GV3/" + air.SeaLevel + "/23";
            Rest.REST(report);

            report = prefix + address + "/report/status/GV4/" + air.Strikes + "/25";
            Rest.REST(report);

            unit = (WF_Config.SI) ? "/0" : "/KM";
            report = prefix + address + "/report/status/GV5/" + air.Distance + unit;
            Rest.REST(report);

            unit = (WF_Config.SI) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV6/" + air.Dewpoint + unit;
            Rest.REST(report);

            unit = (WF_Config.SI) ? "/F" : "/C";
            report = prefix + address + "/report/status/GV7/" + air.ApparentTemp + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV8/" + air.Trend + "/25";
            Rest.REST(report);

            if (!NodeList.Keys.Contains(sec_address)) {
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + sec_address +
                    "/add/WF_AirD/?primary=" + address + "&name=WeatherFlow%20(" + air.SerialNumber + "_d)");
                NodeList.Add(sec_address, "WF_AirD");
            }
            report = prefix + sec_address + "/report/status/GV0/" + air.Battery + "/72";
            Rest.REST(report);
        }

        // Handler that is called when re receive Sky data
        internal void HandleSky(object sender, SkyEventArgs sky) {
            string report;
            string unit;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + sky.SerialNumber;
            string sec_address = "n" + WF_Config.Profile.ToString("000") + "_" + sky.SerialNumber + "_d";

            sky.si_units = WF_Config.SI;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + sky.SerialNumber + " doesn't exist, create it.");
                if (WeatherFlowNS.Debug) {
                    Console.WriteLine("Debug:");
                    Console.WriteLine(sky.Raw);
                }

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Sky" + ((WF_Config.SI) ? "SI" : "") + "/?name=WeatherFlow%20(" + sky.SerialNumber + ")");
                NodeList.Add(address, "WF_Sky" + ((WF_Config.SI) ? "SI" : ""));

                // Do we want to add a secondary diagnostic node?
            }

            StationInfo sinfo = wf_station.FindStationSky(sky.serial_number);
            if (sinfo.station_id == -1)
                AddStationSky(0, sky.DeviceID, sky.serial_number);

            //report = prefix + address + "/report/status/GV0/" + sky.TS + "/25";
            //Rest.REST(report);
            report = prefix + address + "/report/status/GV1/" + sky.Illumination + "/36";
            Rest.REST(report);
            report = prefix + address + "/report/status/GV2/" + sky.UV + "/71";
            Rest.REST(report);
            report = prefix + address + "/report/status/GV3/" + sky.SolarRadiation + "/74";
            Rest.REST(report);
            unit = (WF_Config.SI) ? "/48" : "/49";
            report = prefix + address + "/report/status/GV4/" + sky.WindSpeed + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV5/" + sky.GustSpeed + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV6/" + sky.WindLull + unit;
            Rest.REST(report);
            report = prefix + address + "/report/status/GV7/" + sky.WindDirection + "/76";
            Rest.REST(report);

            // Currently we just report the rain over 1 minute. If we want the rate
            // this number should be multiplied by 60 to get inches/hour (which is UOM 24)
            // mm/hour is uom 46
            //unit = (WF_Config.SI) ? "/105" : "/82";
            //report = prefix + address + "/report/status/GV7/" + sky.Rain + unit;
            //Rest.REST(report);
            unit = (WF_Config.SI) ? "/24" : "/46";
            report = prefix + address + "/report/status/GV8/" + sky.RainRate + unit;
            Rest.REST(report);

            unit = (WF_Config.SI) ? "/105" : "/82";
            report = prefix + address + "/report/status/GV9/" + sky.Daily + unit;
            Rest.REST(report);

            if (!NodeList.Keys.Contains(sec_address)) {
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + sec_address +
                    "/add/WF_SkyD/?primary=" + address + "&name=WeatherFlow%20(" + sky.SerialNumber + "_d)");
                NodeList.Add(sec_address, "WF_SkyD");
            }
            report = prefix + sec_address + "/report/status/GV0/" + sky.Battery + "/72";
            Rest.REST(report);
        }

        internal void HandleDevice(object sender, DeviceEventArgs device) {
            string report;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + device.SerialNumber;
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
                report = prefix + address + "/report/status/GV1/" + up.ToString("0.#") + units;
                Rest.REST(report);
                report = prefix + address + "/report/status/GV2/" + device.RSSI + "/56";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV3/" + device.SensorStatus(0x001) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV4/" + device.SensorStatus(0x002) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV5/" + device.SensorStatus(0x004) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV6/" + device.SensorStatus(0x008) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV7/" + device.SensorStatus(0x010) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV8/" + device.SensorStatus(0x020) + "/25";
                Rest.REST(report);

            } else if (NodeList[address].Contains("Sky")) {
                report = prefix + address + "/report/status/GV1/" + up.ToString("0.#") + units;
                Rest.REST(report);
                report = prefix + address + "/report/status/GV2/" + device.RSSI + "/56";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV3/" + device.SensorStatus(0x040) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV4/" + device.SensorStatus(0x080) + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV5/" + device.SensorStatus(0x100) + "/25";
                Rest.REST(report);
            }
        }

        internal void HandleWind(object sender, RapidEventArgs wind) {
            string report;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + wind.SerialNumber;
            string unit;

            if (!NodeList.Keys.Contains(address)) {
                string sky_address = "n" + WF_Config.Profile.ToString("000") + "_" + wind.Parent;

                Console.WriteLine("Device " + wind.SerialNumber + " doesn't exist, create it.");

                // Ideally, this should be a secondary node under tha matching Air node.
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_RapidWind/?primary=" + sky_address + "&name=WeatherFlow%20(" + wind.SerialNumber + ")");
                NodeList.Add(address, "WF_RapidWind");
            }

            wind.si_units = WF_Config.SI;

            unit = (WF_Config.SI) ? "/48" : "/49";
            //report = prefix + address + "/report/status/GV11/" + wind.Speed + unit;
            report = prefix + address + "/report/status/GV1/" + wind.Speed + unit;
            Rest.REST(report);
            //report = prefix + address + "/report/status/GV10/" + wind.Direction.ToString() + "/25";
            report = prefix + address + "/report/status/GV0/" + wind.Direction.ToString() + "/25";
            Rest.REST(report);
        }

        internal void HandleLightning(object sender, LightningEventArgs strike) {
            string report;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + strike.SerialNumber;

            strike.si_units = WF_Config.SI;

            if (!NodeList.Keys.Contains(address)) {
                string air_address = "n" + WF_Config.Profile.ToString("000") + "_" + strike.Parent;

                Console.WriteLine("Device " + strike.SerialNumber + " doesn't exist, create it.");

                // Ideally, this should be a secondary node under tha matching Air node.
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Lightning/?primary=" + air_address + "&name=WeatherFlow%20(" + strike.SerialNumber + ")");
                NodeList.Add(address, "WF_Lightning");
            }
            report = prefix + address + "/report/status/GV0/" + strike.TimeStamp + "/25";
            Rest.REST(report);

            string unit = (WF_Config.SI) ? "/56" : "/KM";
            report = prefix + address + "/report/status/GV1/" + strike.Distance + unit;
            Rest.REST(report);

            report = prefix + address + "/report/status/GV2/" + strike.Energy + "/56";
            Rest.REST(report);

        }

        internal void HandleRain(object sender, RainEventArgs rain) {
            //string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + rain.SerialNumber;

            if (!NodeList.Keys.Contains(address))
                return;

            Console.WriteLine("Rain Start Event at : " + rain.TimeStamp);
        }

        internal void HandleHub(object sender, HubEventArgs hub) {
            string report;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + hub.SerialNumber;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                Console.WriteLine("Device " + hub.SerialNumber + " doesn't exist, create it.");

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Hub/?name=WeatherFlow%20(" + hub.SerialNumber + ")");
                NodeList.Add(address, "WF_Hub");
            }

            if (WF_Config.Hub) {
                report = prefix + address + "/report/status/GV1/" + hub.Firmware + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV2/" + hub.Uptime + "/58";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV3/" + hub.RSSI + "/56";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV4/" + hub.Sequence + "/25";
                Rest.REST(report);
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
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + update.SerialNumber;
            heartbeat.Updated(address);
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
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Hub") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Hub");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkyD") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkyD");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirD") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirD");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Lightning") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Lightning");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_LightningSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_LightningSI");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWind") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWind");
                            Console.WriteLine("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWindSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWindSI");
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
