
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
    internal enum WF_UNITS {
        SI = 0,
        US = 1,
        UK = 2,
    }

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
        internal static bool Device { get; set; }
        internal static bool Valid { get; set; }
        internal static int LogLevel { get; set; }
        internal static int Units { get; set; }
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
        public bool Device { get; set; }
        public int LogLevel { get; set; }
        public int Units { get; set; }

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
            Device = WF_Config.Device;
            LogLevel = WF_Config.LogLevel;
            Units = WF_Config.Units;
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
            WF_Config.Device = Device;
            WF_Config.LogLevel = LogLevel;
            WF_Config.Units = Units;

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
        internal static int ProfileVersion = 5;
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
            WF_Config.Device = false;
            WF_Config.Port = 8288;
            WF_Config.SI = false;
            WF_Config.UDPPort = 50222;
            WF_Config.Valid = false;
            WF_Config.WFStationInfo = new List<StationInfo>();
            WF_Config.LogLevel = 1;
            WF_Config.Units = (int)WF_UNITS.SI;

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

            WFLogging.AddListener(Console.WriteLine);
            WFLogging.Enabled = true;
            WFLogging.Level = (LOG_LEVELS)WF_Config.LogLevel;
            WFLogging.TimeStamps = true;

            WFLogging.Log("WeatherFlow Node Server " + VERSION);

            if ((WF_Config.Password != "") && (WF_Config.Username != "") && (WF_Config.Profile != 0))
                WF_Config.Valid = true;
            else 
                WF_Config.Valid = false;

            NS = new NodeServer();

            WF_WebsocketLog LogServer = new WF_WebsocketLog(8289);

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
                WFLogging.Error(ex.Message);
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
                WFLogging.Error("Failed to read configuration file wfnodeserver.json");
                return;
            }

            try {
                string json = new string(buf);
                cfgObj = serializer.Deserialize<cfgstate>(json.Substring(0, len));
                cfgObj.LoadState();
            } catch (Exception ex) {
                WFLogging.Error("Failed to import configuration.");
                WFLogging.Error(ex.Message);
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
        internal Dictionary<string, EventArgs> NodeData = new Dictionary<string, EventArgs>();
        private object profile_lock = new object();

        //internal NodeServer(string host, string user, string pass, int profile, bool si_units, bool hub_node, int port) {
        internal NodeServer() {

            // Start server to handle config and ISY queries
            WFNServer wfn = new WFNServer("/WeatherFlow", WF_Config.Port, api_key);
            WFLogging.Log("Started on port " + WF_Config.Port.ToString());

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
                WFLogging.Log("Please point your browser at http://localhost:" + WF_Config.Port.ToString() + "/config to configure the node server.");
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
            int p;

            if (!SetupRest())
                return;

            p = LookupProfile();

            if (!ProfileDetected)
                InstallNodeServerOnISY(++p);

            if (WF_Config.ProfileVersion != WeatherFlowNS.ProfileVersion)
                UpdateProfileFiles();
            ConfigureNodes();
        }

        internal void UpdateProfileFiles() {
            lock (profile_lock) {
                WFLogging.Log("Updating profile files on ISY...");
                // First remove existing profile files
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/profile/remove");

                // Install our embedded versions
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "EN_US.TXT", "nls");
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "I_EDIT.XML", "editor");
                wf_nodesetup.UploadFile(Rest, WF_Config.Profile, "I_NDEFS.XML", "nodedef");

                // This command is documented but doesn't exist yet
                //Rest.REST("ns/2/profile/reload");
                WFLogging.Log("Files uploaded, ISY admin console must be restarted for changes to take effect.");

                WF_Config.ProfileVersion = WeatherFlowNS.ProfileVersion;
                WFLogging.Log(WeatherFlowNS.SaveConfiguration());
            }
        }

        internal bool SetupRest() {
            if (WF_Config.ISY == "") {
                //ISYDetect.IsyAutoDetect();  // UPNP detection
                WF_Config.ISY = ISYDetect.FindISY();
                if (WF_Config.ISY == "") {
                    WFLogging.Error("Failed to detect an ISY on the network. Please add isy=<your isy IP Address> to the command line.");
                    //  TODO: Wait on configuration here.  
                    WeatherFlowNS.shutdown = true;
                    return false;
                }
            }
            WFLogging.Log("Using ISY at " + WF_Config.ISY);

            Rest.Base = "http://" + WF_Config.ISY + "/rest/";
            Rest.AuthRequired = true;
            Rest.Username = WF_Config.Username;
            Rest.Password = WF_Config.Password;

            if (Rest.Username == "" || Rest.Password == "")
                return false;

            // TODO: Attempt to connect to ISY

            return true;
        }

        internal int LookupProfile() {
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
                    int.TryParse(profile, out ProfileNum);
                    if (name == "WeatherFlow") {
                        WFLogging.Log("Detected profile number " + ProfileNum.ToString());
                        WF_Config.Profile = ProfileNum;
                        ProfileDetected = true;
                        return ProfileNum;
                    }
                }
            } catch (Exception ex) {
                WFLogging.Error("Parsing profiles failed: " + ex.Message);
            }
            return ProfileNum;
        }

        internal void InstallNodeServerOnISY(int profile) {
            if (profile > 25) {
                WFLogging.Error("No node server profile slots available on ISY to install node server.");
                return;
            }

            // ip=<address>
            // baseurl=/weatherflow
            // name=WeatherFlow
            // nsuser=
            // nspwd=
            // isyusernum=0
            // port=8288
            // timeout=0
            // ssl=false
            // enabled=true
            string installreq = "profiles/ns/" + profile.ToString() + "/connection/set/network/?";
            installreq += "ip=" + HttpUtility.UrlEncode(ISYDetect.GetMyIPAddress());
            installreq += "&baseurl=" + HttpUtility.UrlEncode("/WeatherFlow");
            installreq += "&name=WeatherFlow";
            installreq += "&nsuser=";
            installreq += "&nspwd=";
            installreq += "&isyusernum=0";
            installreq += "&port=" + WF_Config.Port.ToString();
            installreq += "&timeout=0";
            installreq += "&ssl=false";
            installreq += "&enabled=true";

            WFLogging.Log("Attemping to install WeatherFlow node server in slot " + profile.ToString());
            Rest.REST(installreq);
        }

        internal void ConfigureNodes() {
            NodeList.Clear();

            WFLogging.Log("Querying ISY for existing nodes");
            FindOurNodes();
            if (NodeList.Count > 0 && !ProfileDetected) {
                int Profile;
                // Parse profile from node address
                int.TryParse(NodeList.ElementAt(0).Key.Substring(1, 3), out Profile);
                WFLogging.Info("Detected profile number " + Profile.ToString() + " from nodelist");
                WF_Config.Profile = Profile;
                ProfileDetected = true;
            }

            // Fix up for pre version 4 profiles, convert SI nodes to US nodes, this should
            // only need to run if we're moving to profile version 4.
            //if (WF_Config.ProfileVersion < 4) {
            string[] keys = NodeList.Keys.ToArray<string>();
            foreach (string address in keys) {
                if (NodeList[address] == "WF_AirSI") {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_AirUS");
                    NodeList[address] = "WF_AirUS";
                    WFLogging.Log("  - Converted " + address);
                } else if (NodeList[address] == "WF_SkySI") {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_SkyUS");
                    NodeList[address] = "WF_SkyUS";
                    WFLogging.Log("  - Converted " + address);
                } else if (NodeList[address] == "WF_LightningSI") {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_LightningUS");
                    NodeList[address] = "WF_LightningUS";
                    WFLogging.Log("  - Converted " + address);
                } else if (NodeList[address] == "WF_RapidWindSI") {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWindUS");
                    NodeList[address] = "WF_RapidWindUS";
                    WFLogging.Log("  - Converted " + address);
                }
            }
                
            // Set the nodes to the proper type matching the configuration value
            foreach (string address in NodeList.Keys) {
                switch (WF_Config.Units) {
                    case (int)WF_UNITS.SI:
                        if ((NodeList[address] == "WF_AirUS") || (NodeList[address] == "WF_AirUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Air");
                        if ((NodeList[address] == "WF_SkyUS") || (NodeList[address] == "WF_SkyUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Sky");
                        if ((NodeList[address] == "WF_LightningUS") || (NodeList[address] == "WF_LightningUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_Lightning");
                        if ((NodeList[address] == "WF_RapidWindUS") || (NodeList[address] == "WF_RapidWindUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWind");
                        break;
                    case (int)WF_UNITS.US:
                        if ((NodeList[address] == "WF_Air") || (NodeList[address] == "WF_AirUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_AirUS");
                        if ((NodeList[address] == "WF_Sky") || (NodeList[address] == "WF_SkyUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_SkyUS");
                        if ((NodeList[address] == "WF_Lightning") || (NodeList[address] == "WF_LightningUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_LightningUS");
                        if ((NodeList[address] == "WF_RapidWind") || (NodeList[address] == "WF_RapidWindUK"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWindUS");
                        break;
                    case (int)WF_UNITS.UK:
                        if ((NodeList[address] == "WF_AirUS") || (NodeList[address] == "WF_Air"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_AirUK");
                        if ((NodeList[address] == "WF_SkyUS") || (NodeList[address] == "WF_Sky"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_SkyUK");
                        if ((NodeList[address] == "WF_LightningUS") || (NodeList[address] == "WF_Lightning"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_LightningUK");
                        if ((NodeList[address] == "WF_RapidWindUS") || (NodeList[address] == "WF_RapidWind"))
                            Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address + "/change/WF_RapidWindUK");
                        break;
                }
            }
        }

        internal void StartWebSocket() {
            if (wsi.Started) {
                WFLogging.Info("Shutting down web socket connection.");
                wsi.Stop();
            }

            wsi.Start();
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.remote) {
                    WFLogging.Info("    Start listening for packets from station " + s.station_id.ToString());
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
                    WFLogging.Info("AddStationAir: Found station " + id.ToString());
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
            WFLogging.Info("AddStationAir: Adding station " + id.ToString());
            WF_Config.WFStationInfo.Add(si);
        }

        internal void AddStationSky(int id, int sky, string sky_sn) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.station_id == id) {
                    WFLogging.Info("AddStationSky: Found station " + id.ToString());
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
            WFLogging.Info("AddStationSky: Adding station " + id.ToString());
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

        private string NodeSuffix() {
            if (WF_Config.Units == (int)WF_UNITS.SI) return "";
            else if (WF_Config.Units == (int)WF_UNITS.US) return "US";
            else if (WF_Config.Units == (int)WF_UNITS.UK) return "UK";
            return "";
        }

        private void SendIfDiff(string address, string gv, string prev, string curr, bool force) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            if (prev != curr || force) {
                WFLogging.Debug("Sending: " + prefix + address + "/report/status/" + gv + "/" + curr);
                Rest.REST(prefix + address + "/report/status/" + gv + "/" + curr);
            }
        }

        // Handler that is called when we receive Air data
        internal void HandleAir(object sender, AirEventArgs air) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + air.SerialNumber;
            string sec_address = "n" + WF_Config.Profile.ToString("000") + "_" + air.SerialNumber + "_d";
            DateTime start = DateTime.Now;
            AirEventArgs prev;
            bool force = false;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                WFLogging.Info("Device " + air.SerialNumber + " doesn't exist, create it.");
                if (WeatherFlowNS.Debug) {
                    WFLogging.Debug("Debug:");
                    WFLogging.Debug(air.Raw);
                }

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Air" + NodeSuffix() + "/?name=WeatherFlow%20(" + air.SerialNumber + ")");
                NodeList.Add(address, "WF_Air" + NodeSuffix());
            }

            StationInfo sinfo = wf_station.FindStationAir(air.serial_number);
            if (sinfo.station_id == -1)
                AddStationAir(0, air.DeviceID, air.serial_number);

            if (NodeData.ContainsKey(address)) {
                prev = (AirEventArgs)NodeData[address];
            } else {
                prev = air;
                force = true;
            }

            SendIfDiff(address, "GV1", prev.TemperatureUOM, air.TemperatureUOM, force);
            SendIfDiff(address, "GV2", prev.HumidityUOM, air.HumidityUOM, force);
            SendIfDiff(address, "GV3", prev.SeaLevelUOM, air.SeaLevelUOM, force);
            SendIfDiff(address, "GV4", prev.StrikesUOM, air.StrikesUOM, force);
            SendIfDiff(address, "GV5", prev.DistanceUOM, air.DistanceUOM, force);
            SendIfDiff(address, "GV6", prev.DewpointUOM, air.DewpointUOM, force);
            SendIfDiff(address, "GV7", prev.ApparentTempUOM, air.ApparentTempUOM, force);
            SendIfDiff(address, "GV8", prev.TrendUOM, air.TrendUOM, force);
            SendIfDiff(address, "GV9", prev.BatteryUOM, air.BatteryUOM, force);

            NodeData[address] = air;

            // Normally, we'd add the new node when we see a device status message
            // but device status doesn't tell us what type of device it is for so
            // create a air device node here if it's warrented.
            if (WF_Config.Device) {
                if (!NodeList.Keys.Contains(sec_address)) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + sec_address +
                        "/add/WF_AirD/?primary=" + address + "&name=WeatherFlow%20(" + air.SerialNumber + "_d)");
                    NodeList.Add(sec_address, "WF_AirD");
                    Rest.SendWSDLReqeust("SetParent", address, sec_address);
                }
            }
            WFLogging.Info("HandleAir       " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        // Handler that is called when re receive Sky data
        internal void HandleSky(object sender, SkyEventArgs sky) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + sky.SerialNumber;
            string sec_address = "n" + WF_Config.Profile.ToString("000") + "_" + sky.SerialNumber + "_d";
            DateTime start = DateTime.Now;
            SkyEventArgs prev;
            bool force = false;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                WFLogging.Info("Device " + sky.SerialNumber + " doesn't exist, create it.");
                if (WeatherFlowNS.Debug) {
                    WFLogging.Debug("Debug:");
                    WFLogging.Debug(sky.Raw);
                }

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Sky" + NodeSuffix() + "/?name=WeatherFlow%20(" + sky.SerialNumber + ")");
                NodeList.Add(address, "WF_Sky" + NodeSuffix());
            }

            StationInfo sinfo = wf_station.FindStationSky(sky.serial_number);
            if (sinfo.station_id == -1)
                AddStationSky(0, sky.DeviceID, sky.serial_number);

            if (NodeData.ContainsKey(address)) {
                prev = (SkyEventArgs)NodeData[address];
            } else {
                prev = sky;
                force = true;
            }

            SendIfDiff(address, "GV1", prev.IlluminationUOM, sky.IlluminationUOM, force);
            SendIfDiff(address, "GV2", prev.UVUOM, sky.UVUOM, force);
            SendIfDiff(address, "GV3", prev.SolarRadiationUOM, sky.SolarRadiationUOM, force);
            SendIfDiff(address, "GV4", prev.WindSpeedUOM, sky.WindSpeedUOM, force);
            SendIfDiff(address, "GV5", prev.GustSpeedUOM, sky.GustSpeedUOM, force);
            SendIfDiff(address, "GV6", prev.WindLullUOM, sky.WindLullUOM, force);
            SendIfDiff(address, "GV7", prev.WindDirectionUOM, sky.WindDirectionUOM, force);
            SendIfDiff(address, "GV8", prev.RainRateUOM, sky.RainRateUOM, force);
            SendIfDiff(address, "GV9", prev.DailyUOM, sky.DailyUOM, force);
            SendIfDiff(address, "GV10", prev.BatteryUOM, sky.BatteryUOM, force);
            SendIfDiff(address, "GV11", prev.PrecipitationTypeUOM, sky.PrecipitationTypeUOM, force);

            NodeData[address] = sky;

            // Normally, we'd add the new node when we see a device status message
            // but device status doesn't tell us what type of device it is for so
            // create a sky device node here if it's warrented.
            if (WF_Config.Device) {
                if (!NodeList.Keys.Contains(sec_address)) {
                    Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + sec_address +
                        "/add/WF_SkyD/?primary=" + address + "&name=WeatherFlow%20(" + sky.SerialNumber + "_d)");
                    NodeList.Add(sec_address, "WF_SkyD");
                    Rest.SendWSDLReqeust("SetParent", address, sec_address);
                }
            }
            WFLogging.Info("HandleSky       " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        private void SendIfDiff(string address, string gv, string prev, string curr, string uom, bool force) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            if (prev != curr || force) {
                Rest.REST(prefix + address + "/report/status/" + gv + "/" + curr + uom);
            }
        }

        internal void HandleDevice(object sender, DeviceEventArgs device) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + device.SerialNumber;
            double up;
            double secsperday = 60 * 60 * 24;
            DateTime start = DateTime.Now;
            DeviceEventArgs prev;
            bool force = false;

            // Skip if not enabled.
            if (!WF_Config.Device)
                return;

            // Somehow we need to know what type of device this is?
            if (!NodeList.Keys.Contains(address)) {
                return;
            }

            // Device uptime is in seconds.  Lets make this easier to understand and 
            // report it in days
            up = (double)device.Uptime / secsperday; // Days

            if (NodeData.ContainsKey(address)) {
                prev = (DeviceEventArgs)NodeData[address];
            } else {
                prev = device;
                force = true;
            }

            if (NodeList[address].Contains("Air")) {
                SendIfDiff(address, "GV0", prev.Voltage, device.Voltage, "/72", force);
                SendIfDiff(address, "GV1", "", up.ToString("0.##"), "/10", force);
                SendIfDiff(address, "GV2", prev.RSSI, device.RSSI, "/56", force);
                SendIfDiff(address, "GV3", prev.SensorStatus(0x001), device.SensorStatus(0x001), "/25", force);
                SendIfDiff(address, "GV4", prev.SensorStatus(0x002), device.SensorStatus(0x002), "/25", force);
                SendIfDiff(address, "GV5", prev.SensorStatus(0x004), device.SensorStatus(0x004), "/25", force);
                SendIfDiff(address, "GV6", prev.SensorStatus(0x008), device.SensorStatus(0x008), "/25", force);
                SendIfDiff(address, "GV7", prev.SensorStatus(0x010), device.SensorStatus(0x010), "/25", force);
                SendIfDiff(address, "GV8", prev.SensorStatus(0x020), device.SensorStatus(0x020), "/25", force);
                SendIfDiff(address, "GV9", prev.Firmware, device.Firmware, "/56", force);
                SendIfDiff(address, "GV10", prev.Frequency, device.Frequency, "/56", force);
            } else if (NodeList[address].Contains("Sky")) {
                SendIfDiff(address, "GV0", prev.Voltage, device.Voltage, "/72", force);
                SendIfDiff(address, "GV1", "", up.ToString("0.##"), "/10", force);
                SendIfDiff(address, "GV2", prev.RSSI, device.RSSI, "/56", force);
                SendIfDiff(address, "GV3", prev.SensorStatus(0x040), device.SensorStatus(0x040), "/25", force);
                SendIfDiff(address, "GV4", prev.SensorStatus(0x080), device.SensorStatus(0x080), "/25", force);
                SendIfDiff(address, "GV5", prev.SensorStatus(0x100), device.SensorStatus(0x100), "/25", force);
                SendIfDiff(address, "GV6", prev.Firmware, device.Firmware, "/56", force);
                SendIfDiff(address, "GV7", prev.Frequency, device.Frequency, "/56", force);
            } 

            NodeData[address] = device;

            WFLogging.Info("HandleDevice    " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        internal void HandleWind(object sender, RapidEventArgs wind) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + wind.SerialNumber;
            DateTime start = DateTime.Now;
            RapidEventArgs prev;
            bool force = false;

            if (!NodeList.Keys.Contains(address)) {
                string sky_address = "n" + WF_Config.Profile.ToString("000") + "_" + wind.Parent;

                WFLogging.Info("Device " + wind.SerialNumber + " doesn't exist, create it.");

                // Ideally, this should be a secondary node under tha matching Air node.
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_RapidWind" + NodeSuffix() + "/?primary=" + sky_address + "&name=WeatherFlow%20(" + wind.SerialNumber + ")");
                NodeList.Add(address, "WF_RapidWind");

                // Group it
                Rest.SendWSDLReqeust("SetParent", sky_address, address);
            }

            if (NodeData.ContainsKey(address)) {
                prev = (RapidEventArgs)NodeData[address];
            } else {
                prev = wind;
                force = true;
            }

            SendIfDiff(address, "GV0", prev.Direction.ToString(), wind.Direction.ToString(), "/25", force);
            SendIfDiff(address, "GV1", prev.SpeedUOM, wind.SpeedUOM, force);

            NodeData[address] = wind;

            WFLogging.Info("HandleWind      " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        internal void HandleLightning(object sender, LightningEventArgs strike) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + strike.SerialNumber;
            DateTime start = DateTime.Now;
            LightningEventArgs prev;
            bool force = false;

            if (!NodeList.Keys.Contains(address)) {
                string air_address = "n" + WF_Config.Profile.ToString("000") + "_" + strike.Parent;

                WFLogging.Info("Device " + strike.SerialNumber + " doesn't exist, create it.");

                // Ideally, this should be a secondary node under tha matching Air node.
                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Lightning" + NodeSuffix() + "/?primary=" + air_address + "&name=WeatherFlow%20(" + strike.SerialNumber + ")");
                NodeList.Add(address, "WF_Lightning");

                Rest.SendWSDLReqeust("SetParent", air_address, address);
            }

            if (NodeData.ContainsKey(address)) {
                prev = (LightningEventArgs)NodeData[address];
            } else {
                prev = strike;
                force = true;
            }

            SendIfDiff(address, "GV0", prev.TimeStamp, strike.TimeStamp, "/25", force);
            SendIfDiff(address, "GV1", prev.DistanceUOM, strike.DistanceUOM, force);
            SendIfDiff(address, "GV2", prev.Energy, strike.Energy, "/56", force);

            NodeData[address] = strike;
            WFLogging.Info("HandleLightning " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        internal void HandleRain(object sender, RainEventArgs rain) {
            //string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + rain.SerialNumber;

            if (!NodeList.Keys.Contains(address))
                return;

            WFLogging.Info("Rain Start Event at : " + rain.TimeStamp);
        }

        internal void HandleHub(object sender, HubEventArgs hub) {
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + hub.SerialNumber;
            DateTime start = DateTime.Now;
            HubEventArgs prev;
            bool force = false;

            if (!NodeList.Keys.Contains(address)) {
                // Add it
                WFLogging.Info("Device " + hub.SerialNumber + " doesn't exist, create it.");

                Rest.REST("ns/" + WF_Config.Profile.ToString() + "/nodes/" + address +
                    "/add/WF_Hub/?name=WeatherFlow%20(" + hub.SerialNumber + ")");
                NodeList.Add(address, "WF_Hub");
            }

            if (WF_Config.Hub) {
                double up = (double)hub.Uptime / (60.0 * 60.0 * 24.0); // Days

                if (NodeData.ContainsKey(address)) {
                    prev = (HubEventArgs)NodeData[address];
                } else {
                    prev = hub;
                    force = true;
                }

                SendIfDiff(address, "GV1", prev.Firmware, hub.Firmware, "/56", force);
                SendIfDiff(address, "GV2", "", up.ToString("0.##"), "/10", force);
                SendIfDiff(address, "GV3", prev.RSSI, hub.RSSI, "/56", force);
                SendIfDiff(address, "GV4", prev.Sequence, hub.Sequence, "/56", force);
            }

            NodeData[address] = hub;

            WFLogging.Debug("HUB: firmware    = " + hub.Firmware);
            WFLogging.Debug("HUB: reset flags = " + hub.ResetFlags);
            WFLogging.Debug("HUB: stack       = " + hub.Stack);
            WFLogging.Debug("HUB: fs          = " + hub.FS);
            WFLogging.Debug("HUB: rssi        = " + hub.RSSI);
            WFLogging.Debug("HUB: timestamp   = " + hub.TimeStamp);
            WFLogging.Debug("HUB: uptime      = " + hub.Uptime.ToString());
            WFLogging.Info("HandleHub       " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("#.00") + " ms");
        }

        internal void GetUpdate(object sender, UpdateEventArgs update) {
            string address = "n" + WF_Config.Profile.ToString("000") + "_" + update.SerialNumber;
            heartbeat.Updated(address, update.UpdateTime, update.type);
            //heartbeat.Updated(address);
        }

        internal void AddNode(string address) {
            WFLogging.Info("Adding " + address + " to our list.");
            NodeList.Add(address, "");
        }
        internal void RemoveNode(string address) {
            WFLogging.Info("Removing " + address + " from our list.");
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
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Air") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Air");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirUS") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirUS");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirUK") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirUK");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Sky") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Sky");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkyUS") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkyUS");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkyUK") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkyUK");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Hub") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Hub");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkyD") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkyD");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirD") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirD");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_Lightning") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_Lightning");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_LightningUS") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_LightningUS");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_LightningUK") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_LightningUK");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWind") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWind");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWindUS") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWindUS");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWindUK") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWindUK");
                            WFLogging.Info("Found: " + node.SelectSingleNode("address").InnerText);

                        // These will need to be converted!
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_AirSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_AirSI");
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_SkySI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_SkySI");
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_LightningSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_LightningSI");
                        }  else if (node.Attributes["nodeDefId"].Value == "WF_RapidWindSI") {
                            NodeList.Add(node.SelectSingleNode("address").InnerText, "WF_RapidWindSI");
                        }
                    } catch {
                        WFLogging.Error("Failed to parse nodeDefId attribute from " + node.Name);
                    }
                }
            } catch (Exception ex) {
                WFLogging.Error("XML parsing failed: " + ex.Message);
            }
        }
    }


}
