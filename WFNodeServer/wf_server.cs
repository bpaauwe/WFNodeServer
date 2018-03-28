
//
// WFNodeServer - ISY Node Server for WeatherFlow weather station data
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
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Reflection;

namespace WFNodeServer {
    internal class WFNServer {
        private Thread server_thread;
        private string root_directory;
        private HttpListener listener;
        private int port;
        private string api_key;
        private string cfg_file_status = "";

        private Dictionary<string, string> NodeDefs = new Dictionary<string, string> {
            {"WF_Sky", "Sky Sensor - metric"},
            {"WF_Air", "Air Sensor - metric"},
            {"WF_SkySI", "Sky Sensor - imperial"},
            {"WF_AirSI", "Air Sensor - imperial"},
            {"WF_SkyD", "Sky Sensor - device data"},
            {"WF_AirD", "Air Sensor - device data"},
            {"WF_Hub", "Hub data / heartbeat"},
            {"WF_Lightning", "Lightning event data"},
            {"WF_LightningSI", "Lightning event data"},
            {"WF_RapidWind", "Rapid Wind speed/direction data"},
            {"WF_RapidWindSI", "Rapid Wind speed/direction data"},
        };

        internal int Port {
            get {
                return port;
            }
            private set { }
        }

        internal WFNServer(string path, int port, string api_key) {
            this.api_key = api_key;
            this.Initialize(path, port);
        }

        // Auto assign port. Can probably remove this
        internal WFNServer(string path) {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(path, port);
        }

        internal void Stop() {
            server_thread.Abort();
            listener.Stop();
        }

        private void Listen() {
            using (listener = new HttpListener()) {
                try {
                    listener.Prefixes.Add("http://*:" + port.ToString() + "/");
                    listener.Start();
                } catch (Exception ex) {
                    Console.WriteLine("Failed to start web server on port " + port.ToString());
                    Console.WriteLine("  Error was: " + ex.Message);
                    return;
                }
                while (true) {
                    try {
                        HttpListenerContext context = listener.GetContext();
                        Process(context);
                    } catch (Exception ex) {
                        Console.WriteLine("Failed to process connection: " + ex.Message);
                    }
                    //Thread.Sleep(5000);
                }
            }
        }

        private void Process(HttpListenerContext context) {
            string filename = context.Request.Url.AbsolutePath;
            //Console.WriteLine(" request = " + filename);

            if (filename.Contains("config") || filename == "/") {
                // Handle configuration
                ConfigPage(context);
                return;
            }

            // WeatherFlow/install  - install
            // Weatherflow/nodes/<address>/query - Query the node and report status
            // WeatherFlow/nodes/<address>/status - report current status
            // WeatherFlow/add/nodes - Add all nodes
            //  These could have a ?requestid=<requestid> at the end
            //  that means we need to send a message after completing the task
            // WeatherFlow/nodes/<address>/report/add
            // WeatherFlow/nodes/<address>/report/remove
            // WeatherFlow/nodes/<address>/report/rename
            // WeatherFlow/nodes/<address>/report/enable
            // WeatherFlow/nodes/<address>/report/disable

            // TODO: Parse out the request id if present
            if (filename.Contains("install")) {
                Console.WriteLine("Recieved request to install the profile files.");
            } else if (filename.Contains("query")) {
                string[] parts;
                parts = filename.Split('/');

                Console.WriteLine("Query node " + parts[3]);
            } else if (filename.Contains("status")) {
                string[] parts;
                parts = filename.Split('/');

                Console.WriteLine("Get status of node " + parts[3]);
                NodeStatus();
            } else if (filename.Contains("add")) {
                Console.WriteLine("Add our node.  How is this different from report/Add?");
                AddNodes();

            // the report API is not yet implemented on the ISY so we'll
            // never get anything of these until it is.
            } else if (filename.Contains("report/add")) {
                Console.WriteLine("Report that a node was added?");
            } else if (filename.Contains("report/rename")) {
            } else if (filename.Contains("report/remove")) {
            } else if (filename.Contains("report/enable")) {
            } else if (filename.Contains("report/disable")) {
            } else if (filename.Contains("favicon.ico")) {
            } else { 
                Console.WriteLine("Unknown Request: " + filename);
            }

            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = 0;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port) {
            this.root_directory = path;
            this.port = port;

            server_thread = new Thread(this.Listen);
            server_thread.IsBackground = true;
            server_thread.Name = "WFNodeServer REST API";
            server_thread.Priority = ThreadPriority.Normal;
            server_thread.Start();
        }

        private void AddNodes() {
            string str;

            if (WF_Config.Profile > 0) {
                str = "ns/" + WF_Config.Profile.ToString() + "/nodes/n" + WF_Config.Profile.ToString("000") + "_weather_flow/add/WeatherFlow/?name=WeatherFlow";
                WeatherFlowNS.NS.Rest.REST(str);
            }
        }

        private void NodeStatus() {
            WeatherFlowNS.NS.RaiseAirEvent(this, new WFNodeServer.AirEventArgs(WeatherFlowNS.NS.udp_client.AirObj));
            WeatherFlowNS.NS.RaiseSkyEvent(this, new WFNodeServer.SkyEventArgs(WeatherFlowNS.NS.udp_client.SkyObj));
        }

        struct station_form {
            internal string action;
            internal int station_id;
            internal int air_id;
            internal int sky_id;
            internal double elevation;
            internal bool remote;
            internal bool rapid;
        };

        private station_form GetStationInfo(string resp) {
            string[] list;
            station_form sf = new station_form();

            sf.remote = false;
            sf.rapid = false;

            list = resp.Split('&');
            foreach (string p in list) {
                string[] pr = p.Split('=');
                switch (pr[0]) {
                    case "station_id": int.TryParse(pr[1], out sf.station_id); break;
                    case "air_id": int.TryParse(pr[1], out sf.air_id); break;
                    case "sky_id": int.TryParse(pr[1], out sf.sky_id); break;
                    case "elevation": double.TryParse(pr[1], out sf.elevation); break;
                    case "remote": sf.remote = true; break;
                    case "rapid": sf.rapid = true; break;
                    case "stationcfg": sf.action = pr[1]; break;
                }
            }
            return sf;
        }

        private bool AddStation(station_form a_form) {
            wf_station station = new wf_station(api_key);

            if (station.GetStationMeta(a_form.station_id.ToString()) || (a_form.station_id == 0)) {
                a_form.air_id = (a_form.air_id == 0) ? station.Air : a_form.air_id;
                a_form.sky_id = (a_form.sky_id == 0) ? station.Sky : a_form.sky_id;
                a_form.elevation = (a_form.elevation == 0) ? station.Elevation : a_form.elevation;
                WeatherFlowNS.NS.AddStation(a_form.station_id, a_form.elevation, a_form.air_id, a_form.sky_id, a_form.remote, station.AirSN, station.SkySN, a_form.rapid);
                return true;
            } else {
                Console.WriteLine("Failed to find station " + a_form.station_id.ToString());
                cfg_file_status = "Station " + a_form.station_id.ToString() + " lookup failed.";
            }
            return false;
        }

        private void ConfigPage(HttpListenerContext context) {
            string cfg_page;
            byte[] page;
            byte[] post = new byte[1024];

            //Console.WriteLine("content length = " + context.Request.ContentLength64.ToString());
            if (context.Request.ContentLength64 > 0) {
                string[] list;
                string[] pair;
                int len = (int)context.Request.ContentLength64;
                bool initISY = false;
                bool saveCfg = false;

                context.Request.InputStream.Read(post, 0, len);
                string resp = Encoding.Default.GetString(post);
                resp = resp.Substring(0, len);

                //Console.WriteLine("Response = " + resp);

                cfg_file_status = "";

                if (resp.Contains("stationcfg")) {
                    station_form a_form = GetStationInfo(resp);
                    switch (a_form.action) {
                        case "Delete":
                            Console.WriteLine("Delete station " + a_form.station_id.ToString());
                            WeatherFlowNS.NS.DeleteStation(a_form.station_id);
                            saveCfg = true;
                            break;
                        case "++Add++":
                            bool exists = false;
                            Console.WriteLine("Add station " + a_form.station_id.ToString());
                            // Check if station already exists
                            foreach (StationInfo s in WF_Config.WFStationInfo) {
                                if (s.station_id == a_form.station_id) {
                                    cfg_file_status = "Station " + a_form.station_id.ToString() + " already exists.";
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                                saveCfg = AddStation(a_form);
                            break;
                        case "Update":
                            Console.WriteLine("Update station " + a_form.station_id.ToString());
                            saveCfg = AddStation(a_form);
                            break;
                        default:
                            Console.WriteLine("Unknown action [" + a_form.action + "]");
                            break;
                    }
                } else {
                    list = resp.Split('&');
                    foreach (string item in list) {
                        pair = item.Split('=');
                        switch (pair[0]) {
                            case "sAddress":
                                if (pair[1] != WF_Config.ISY) {
                                    WF_Config.ISY = pair[1];
                                    initISY = true;
                                    saveCfg = true;
                                }
                                break;
                            case "sUsername":
                                if (pair[1] != WF_Config.Username) {
                                    WF_Config.Username = pair[1];
                                    initISY = true;
                                    saveCfg = true;
                                }
                                break;
                            case "sPassword":
                                if (pair[1] != WF_Config.Password) {
                                    WF_Config.Password = pair[1];
                                    initISY = true;
                                    saveCfg = true;
                                }
                                break;
                            case "sProfile":
                                int p = 0; ;
                                int.TryParse(pair[1], out p);
                                if (p != WF_Config.Profile) {
                                    WF_Config.Profile = p;
                                    initISY = true;
                                    saveCfg = true;
                                }
                                break;
                            case "webPort":
                                int w = 0; ;
                                int.TryParse(pair[1], out w);
                                if (w != WF_Config.Port) {
                                    WF_Config.Port = w;
                                    saveCfg = true;
                                }
                                break;
                            case "sSI":
                                bool imperial = (pair[1] == "1");
                                if (imperial != WF_Config.SI) {
                                    WF_Config.SI = (pair[1] == "1");
                                    saveCfg = true;
                                }
                                break;
                            case "sHub":
                                bool hub = (pair[1] == "1");
                                if (hub != WF_Config.Hub) {
                                    WF_Config.Hub = (pair[1] == "1");
                                    saveCfg = true;
                                }
                                break;
                            case "sDevice":
                                bool device = (pair[1] == "1");
                                if (device != WF_Config.Device) {
                                    WF_Config.Device = (pair[1] == "1");
                                    saveCfg = true;
                                }
                                break;
                            case "serverctl":
                                if (pair[1].Contains("Restart")) {
                                    WeatherFlowNS.NS.udp_client.Start();
                                    WeatherFlowNS.NS.heartbeat.Start();
                                    cfg_file_status = "Server Started";
                                    Thread.Sleep(400);
                                } else if (pair[1].Contains("Pause")) {
                                    WeatherFlowNS.NS.heartbeat.Stop();
                                    WeatherFlowNS.NS.udp_client.Stop();
                                    cfg_file_status = "Server Paused";
                                }
                                break;
                            case "websocket":
                                if (pair[1].Contains("Start")) {
                                    try {
                                        WeatherFlowNS.NS.StartWebSocket();
                                    } catch (Exception ex) {
                                        Console.WriteLine("Starting websocket client failed: " + ex.Message);
                                    }
                                    cfg_file_status = "Websocket Client Started";
                                    Thread.Sleep(400);
                                } else if (pair[1].Contains("Stop")) {
                                    WeatherFlowNS.NS.wsi.Stop();
                                    cfg_file_status = "Websocket Client Stopped";
                                    Thread.Sleep(800);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (saveCfg)
                    cfg_file_status += WeatherFlowNS.SaveConfiguration();
                if (initISY)
                    WeatherFlowNS.NS.InitializeISY();
            }

            try {
                cfg_page = MakeConfigPage();
            } catch (Exception ex) {
                Console.WriteLine("Failed to make configuration web page.");
                Console.WriteLine(ex.Message);
                context.Response.Close();
                return;
            }
            // How can we substitute values into the page?  May need to dynamically
            // generate the page instead of storing it as a resource.  That would
            // be a bit of a pain.

            page = ASCIIEncoding.ASCII.GetBytes(cfg_page);

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = page.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(page, 0, page.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }

        private string ConfigBoolItem(string title, string varname, bool flag) {
            string item;
            string v = (flag) ? "1" : "0";
            string check = (flag) ? "checked" : "unchecked";

            item = "<tr>\n";
            item += "<input type=\"hidden\" name=\"" + varname + "\" value=\"" + "0" + "\">\n";
            item += "<td><b>" + title + "</b></td>\n";
            item += "<td width=\"5%\"><input " + check + " type=\"checkbox\" name=\"" + varname + "\" value=\"" + "1" + "\" onClick=\"this.form.submit();\"></td>\n";

            return item;
        }

        private string ConfigItem(string title, string varname, string varvalue, int type) {
            string item;

            item = "<tr>\n";
            item += "<td width=\"50%\">" + title + "</td>\n";
            item += "<td width=\"40%\"><input style=\"width:250px\" type=\"";
            if (type == 0)
                item += "number\" step=\"any\"";
            else if (type == 1)
                item += "text\"";
            else if (type == 2)
                item += "password\"";
            else if (type == 3)
                item += "required pattern=\"(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}:\\d{1,5})|(^\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})\"";
            item += " name=\"" + varname + "\" value=\"" + varvalue +"\"></td>\n";
            item += "<td width=\"10%\"><input type=\"submit\" value=\"  Set  \"></td>\n";
            item += "<td></td>\n";
            item += "</tr>\n";

            return item;
        }

        private string MakeConfigPage() {
            string page;
            bool remote_configured = false;
            bool nodeserver_configured = false;

            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.remote && (s.air_id != 0 || s.sky_id != 0))
                    remote_configured = true;
            }

            if ((WF_Config.Profile > 0) &&
                (WF_Config.Password != "") &&
                (WF_Config.Username != "") &&
                (WF_Config.Port > 0) &&
                (WF_Config.ISY != "")) {
                nodeserver_configured = true;
            }

            page = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html\">\n";
            page += "<meta http-equiv=\"cache-control\" content=\"no-cache\">\n";
            page += "<meta http-equiv=\"expires\" content=\"0\">\n";
            page += "<meta http-equiv=\"pragma\" content=\"no-cache\">\n";
            page += "<meta http-equiv=\"Content-Language\" content=\"en\">\n";
            page += "<meta charset=\"UTF-8\">\n";
            page += "<meta name=\"google\" content=\"notranslate\">\n";
            page += "<style>\n";
            page += "	body { font-family: Sans-Serif; }\n";
            page += "</style>\n";
            page += "<title>WeatherFlow Nodeserver Web Interface</title>\n";
            page += "</head><body>\n";
            page += "<form name=\"root\" action=\"/config\" enctype=\"application/x-www-form-urlencoded\" method=\"post\">\n";
            page += "<table border=\"0\" width=\"100%\">\n";
            page += "<tr><td align=\"center\"><h1>WeatherFlow Nodeserver v" + WeatherFlowNS.VERSION + "</h1></td></tr>\n";
            page += "<div align=\"center\">\n";
            page += "<table border=\"0\" width=\"600\" id=\"tblBody\" style=\"padding-left: 4px; padding-right: 4px; padding-top: 1px; padding-bottom: 1px\">\n";
            page += "<tr><td colspan=\"3\" Class=\"sectionTitle\"><br><h2>Configuration</h2><br></td></tr>\n";

            page += ConfigItem("Port", "webPort", WF_Config.Port.ToString(), 0);
            page += ConfigItem("ISY Address", "sAddress", WF_Config.ISY, 3);
            page += ConfigItem("ISY Username", "sUsername", WF_Config.Username, 1);
            page += ConfigItem("ISY Password", "sPassword", WF_Config.Password, 2);
            page += ConfigItem("Profile Number", "sProfile", WF_Config.Profile.ToString(), 0);
            page += ConfigBoolItem("Use SI Units", "sSI", WF_Config.SI);
            page += ConfigBoolItem("Include Hub data", "sHub", WF_Config.Hub);
            page += ConfigBoolItem("Include Device data", "sDevice", WF_Config.Device);

            page += "<tr>\n";
            page += "<td colspan=\"3\">";
            if (nodeserver_configured) {
                page += "<form method=\"post\">";
                page += "<input type=\"submit\" name=\"serverctl\" value=\" Restart Node Server \">";
                page += "&nbsp;";
                page += "<input type=\"submit\" name=\"serverctl\" value=\" Pause Node Server \">";
                page += "&nbsp;";
                page += "<input style=\"width: 120px; text-align: center; background-color: #e8e8e8;\" type=\"text\" name=\"status\" value=\"";
                page += (WeatherFlowNS.NS.udp_client.Active) ? "Running" : "Paused";
                page += "\" readonly>";
                page += "</form>";
            }
            page += "</td>\n";
            page += "</tr>\n";

            page += "<tr><th colspan=\"3\">&nbsp;</th></tr>\n";
            page += "</table> </div> </table> </form>\n";

            page += "<div style=\"padding-left: 4px; padding-right: 4px; padding-top: 20px; padding-bottom: 1px\">\n";
            page += "<table border=\"0\">\n";
            page += "<tr><td colspan=\"8\"><h2>Station Configuration</h2></td></tr>\n";
            page += "<tr><th>Station ID</th><th>Sky ID</th><th>Air ID</th><th>Elevation (meters)</th><th>Remote</th><th>Rapid</th><th>&nbsp;</th></tr>\n";
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                string placeholder = "Enter the weather station ID";

                page += "<tr>";
                page += "<form method=\"post\">";
                page += "<td><input style=\"width:150px\" required placeholder=\"" + placeholder + "\" name = \"station_id\" type=\"number\" value=\"" + s.station_id.ToString() + "\"></td>";
                page += "<td><input style=\"width:150px\" type=\"number\" readonly value=\"" + s.sky_id.ToString() + "\"></td>";
                page += "<td><input style=\"width:150px\" type=\"number\" readonly value=\"" + s.air_id.ToString() + "\"></td>";
                page += "<td><input style=\"width:150px\" name=\"elevation\" type=\"number\" step=\"any\" value=\"" + s.elevation.ToString() + "\"></td>";
                page += "<td><input style=\"width:50px\" ";
                page += (s.remote) ? "checked" : "unchecked";
                page += " type=\"checkbox\" name=\"remote\" value=\"1\"></td>";
                page += "<td><input style=\"width:50px\" ";
                page += (s.rapid) ? "checked" : "unchecked";
                page += " type=\"checkbox\" name=\"rapid\" value=\"2\"></td>";
                page += "<td><input name=\"stationcfg\" type=\"submit\" value=\"Update\"></td>";
                page += "<td><input name=\"stationcfg\" type=\"submit\" value=\"Delete\"></td>";
                page += "</form></tr>\n";
            }
            // Add input row
            page += "<form name=\"stations\" action=\"/config\" enctype=\"application/x-www-form-urlencoded\" method=\"post\">\n";
            page += "<tr>";
            page += "<td><input style=\"width:150px\" type=\"number\" step=\"any\" name=\"station_id\" value=\"\" required></td>\n";
            page += "<td><input style=\"width:150px\" type=\"number\" step=\"any\" name=\"sky_id\" value=\"\"></td>\n";
            page += "<td><input style=\"width:150px\" type=\"number\" step=\"any\" name=\"air_id\" value=\"\"></td>\n";
            page += "<td><input style=\"width:150px\" type=\"number\" step=\"any\" name=\"elevation\" value=\"\"></td>\n";
            page += "<td><input style=\"width:50px\" type=\"checkbox\"  name=\"remote\" value=\"1\"></td>\n";
            page += "<td><input style=\"width:50px\" type=\"checkbox\"  name=\"rapid\" value=\"1\"></td>\n";
            page += "<td><input name=\"stationcfg\" type=\"submit\" value=\"  Add  \"></td>\n";
            page += "</tr>";
            page += "</form>\n";

            page += "<tr>\n";
            page += "<td colspan=\"7\">";
            if (remote_configured) {
                page += "<form method=\"post\">\n";
                page += "<input type=\"submit\" name=\"websocket\" value=\" Start WebSocket Client \">";
                page += "&nbsp;";
                page += "<input type=\"submit\" name=\"websocket\" value=\" Stop WebSocket Client \">";
                page += "&nbsp;";
                page += "<input style=\"width: 120px; text-align: center; background-color: #e8e8e8;\" type=\"text\" name=\"wsstatus\" value=\"";
                page += (WeatherFlowNS.NS.wsi.Started) ? "Running" : "Stopped";
                page += "\" readonly>";
                page += "</form>";
            }
            page += "</td>\n";
            page += "</tr>\n";

            page += "</table>\n";
            page += "</div>\n";

            page += "<div style=\"padding-left: 4px; padding-right: 4px; padding-top: 20px; padding-bottom: 1px\">\n";
            page += "<hr>\n";
            page += "<div style=\"border: 1px solid; background-color: #D8D8D8; padding: 2px 2px 2px 2px\">";
            page += "Status: " + cfg_file_status;
            page += "</div>";
            page += "</div>";


            // Display a table of nodes that were found on the ISY
            page += "<div style=\"padding-left: 4px; padding-right: 4px; padding-top: 20px; padding-bottom: 1px\">\n";
            page += "<table width=\"450px\" border=\"1\">\n";
            page += "<tr><th>Node Address</th><th>Node Type</th></tr>\n";
            foreach (string n in WeatherFlowNS.NS.NodeList.Keys) {
                try {
                    page += "<tr><td style=\"padding: 1px 1px 1px 5px;\">" + n + "</td>";
                    page += "<td style=\"padding: 1px 1px 1px 5px;\">" + NodeDefs[WeatherFlowNS.NS.NodeList[n]] + "</td></tr>\n";
                } catch {
                    Console.WriteLine("Missing definition for node type " + WeatherFlowNS.NS.NodeList[n]);
                }
            }
            page += "</table>\n";
            page += "</div>\n";

            page += "</body> </html> \n";

            return page;
        }

    }
 
}
