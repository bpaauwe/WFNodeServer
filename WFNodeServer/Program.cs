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

namespace WFNodeServer {
    class WeatherFlowNS {
        internal static NodeServer NS;
        static void Main(string[] args) {
             NS = new NodeServer();

            while (true) ;
        }
    }

    internal delegate void AirEvent(Object sender, AirEventArgs e);
    internal delegate void SkyEvent(Object sender, SkyEventArgs e);

    internal class AirEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.AirData data;
        internal bool si_units { get; set; }

        internal AirEventArgs(WeatherFlow_UDP.AirData d) {
            data = d;
            si_units = false;
        }

        // Might be nice if we had properties to pull out the
        // data in a formatted string.

        internal string Pressure {
            get {
                double inhg = data.obs[0][1] * 0.02952998751;
                return inhg.ToString("0.##");
            }
        }
        internal string Temperature {
            get { return data.obs[0][2].ToString(); }
        }
        internal string Humidity {
            get { return data.obs[0][3].ToString(); }
        }
        internal string Strikes {
            get { return data.obs[0][4].ToString(); }
        }
        internal string Distance {
            get { return data.obs[0][5].ToString(); }
        }
        internal string Battery {
            get { return data.obs[0][6].ToString(); }
        }
    }

    internal class SkyEventArgs : System.EventArgs {
        internal WeatherFlow_UDP.SkyData data;
        internal bool si_units { get; set; }

        internal SkyEventArgs(WeatherFlow_UDP.SkyData d) {
            data = d;
            si_units = false;
        }
        internal string Illumination {
            get { return data.obs[0][1].ToString(); }
        }
        internal string UV {
            get { return data.obs[0][2].ToString(); }
        }
        internal string Rain {
            get { return data.obs[0][3].ToString(); }
        }
        internal string WindLull {
            get { return data.obs[0][4].ToString(); }
        }
        internal string WindSpeed {
            get { return data.obs[0][5].ToString(); }
        }
        internal string GustSpeed {
            get { return data.obs[0][6].ToString(); }
        }
        internal string WindDirection {
            get { return data.obs[0][7].ToString(); }
        }
        internal string Battery {
            get { return data.obs[0][8].ToString(); }
        }
        internal string Interval {
            get { return data.obs[0][9].ToString(); }
        }
        internal string SolarRadiation {
            get { return data.obs[0][10].ToString(); }
        }
        internal string PrecipitationDay {
            get { return data.obs[0][11].ToString(); }
        }
        internal string PrecipitationType {
            get { return data.obs[0][12].ToString(); }
        }
        internal string WindSampleInterval {
            get { return data.obs[0][13].ToString(); }
        }
    }

    internal class NodeServer {
        private static rest Rest;
        internal event SkyEvent WFSkySubscribers = null;
        internal event AirEvent WFAirSubscribers = null;
        internal bool active = false;
        internal Dictionary<string, bool> NodeList = new Dictionary<string, bool>();
        internal int Profile = 0;

        internal NodeServer() {
            WeatherFlow_UDP udp_client;
            Thread udp_thread;

            //ISYDetect.IsyAutoDetect();  // UPNP detection
            string ISYIP = ISYDetect.FindISY();

            Rest = new rest("http://" + ISYIP + "/rest/");
            Rest.Username = "admin";
            Rest.Password = "Tr1ck13r";

            // Is there some way to detect what profile we're installed at?
            //  We can look at the nodes "/rest/nodes" and search the output
            // for a node with nodedef=WeatherFlow. The node address prefix
            // will tell us which profile we're using.
            FindOurNodes();
            if (NodeList.Count > 0) {
                // Parse profile from node address
                int.TryParse(NodeList.ElementAt(0).Key.Substring(1, 3), out Profile);
                Console.WriteLine("Detected profile number " + Profile.ToString());
            }
                
            // If we don't know the profile number, we shouldn't do
            // anything at this point.  Goal may be to get profile
            // number from command line so we can continue
            if (Profile == 0)
                return;

            WFNServer wfn = new WFNServer("/WeatherFlow", 8288);
            Console.WriteLine("Started on port 8288");

            WFAirSubscribers += new AirEvent(HandleAir);
            WFSkySubscribers += new SkyEvent(HandleSky);
            // Try to create our node?  This will fail if the node already
            // exist.
            Rest.REST("ns/2/nodes/n002_weather_flow/add/WeatherFlow/?name=WeatherFlow");

            // Start a thread to monitor the UDP port
            Console.WriteLine("Starting udp_thread Thread.");
            udp_client = new WeatherFlow_UDP();
            udp_thread = new Thread(new ThreadStart(udp_client.WeatherFlowThread));
            udp_thread.IsBackground = true;
            udp_thread.Start();
        }

        internal void RaiseAirEvent(Object sender, WFNodeServer.AirEventArgs e) {
            if (WFAirSubscribers != null)
                WFAirSubscribers(sender, e);
        }
        internal void RaiseSkyEvent(Object sender, WFNodeServer.SkyEventArgs e) {
            if (WFSkySubscribers != null)
                WFSkySubscribers(sender, e);
        }

        // Handler that is called when we receive Air data
        internal void HandleAir(object sender, AirEventArgs air) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";

            Console.WriteLine("In theory, we're updating the Air data now.");

            foreach (string address in NodeList.Keys) {
                report = prefix + address + "/report/status/GV1/" + air.Temperature + "/C";
                Rest.REST(report);

                report = prefix + address + "/report/status/GV2/" + air.Humidity + "/PERCENT";
                Rest.REST(report);

                report = prefix + address + "/report/status/GV3/" + air.Pressure + "/23";
                Rest.REST(report);

                report = prefix + address + "/report/status/GV4/" + air.Strikes + "/0";
                Rest.REST(report);

                report = prefix + address + "/report/status/GV5/" + air.Distance + "/KM";
                Rest.REST(report);
            }
        }

        // Handler that is called when re receive Sky data
        internal void HandleSky(object sender, SkyEventArgs sky) {
            string report;
            string prefix = "ns/" + Profile.ToString() + "/nodes/";

            foreach (string address in NodeList.Keys) {
                report = prefix + address + "/report/status/GV6/" + sky.Illumination + "/36";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV7/" + sky.UV + "/71";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV8/" + sky.SolarRadiation + "/74";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV9/" + sky.WindSpeed + "/49";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV10/" + sky.GustSpeed + "/49";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV11/" + sky.WindDirection + "/25";
                Rest.REST(report);
                report = prefix + address + "/report/status/GV12/" + sky.Rain + "/82";
                Rest.REST(report);
            }
        }

        internal void AddNode(string address) {
            Console.WriteLine("Adding " + address + " to our list.");
            NodeList.Add(address, false);
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
                            NodeList.Add(node.SelectSingleNode("address").InnerText, false);
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
