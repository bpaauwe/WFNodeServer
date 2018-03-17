
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
        private int profile;

        internal int Port {
            get {
                return port;
            }
            private set { }
        }

        internal WFNServer(string path, int port, int profile) {
            this.profile = profile;
            this.Initialize(path, port);
        }

        // Auto assign port. Can probably remove this
        internal WFNServer(string path, int profile) {
            this.profile = profile;
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
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port.ToString() + "/");
            listener.Start();
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

            str = "ns/" + profile.ToString() + "/nodes/n" + profile.ToString("000") + "_weather_flow/add/WeatherFlow/?name=WeatherFlow";
            WeatherFlowNS.NS.Rest.REST(str);
        }

        private void NodeStatus() {
            WeatherFlowNS.NS.RaiseAirEvent(this, new WFNodeServer.AirEventArgs(WeatherFlowNS.NS.udp_client.AirObj));
            WeatherFlowNS.NS.RaiseSkyEvent(this, new WFNodeServer.SkyEventArgs(WeatherFlowNS.NS.udp_client.SkyObj));
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

                context.Request.InputStream.Read(post, 0, len);
                string resp = Encoding.Default.GetString(post);
                resp = resp.Substring(0, len);

                list = resp.Split('&');
                foreach (string item in list) {

                    pair = item.Split('=');
                    switch (pair[0]) {
                        case "sAddress":
                            WF_Config.ISY = pair[1];
                            break;
                        case "sUsername":
                            WF_Config.Username = pair[1];
                            break;
                        case "sPassword":
                            WF_Config.Password = pair[1];
                            break;
                        case "sProfile":
                            int p = 0; ;
                            int.TryParse(pair[1], out p);
                            WF_Config.Profile = p;
                            break;
                        case "sId":
                            int i = 0; ;
                            int.TryParse(pair[1], out i);
                            WF_Config.StationID = i;
                            break;
                        case "webPort":
                            int w = 0; ;
                            int.TryParse(pair[1], out w);
                            WF_Config.Port = w;
                            break;
                        case "sElevation":
                            double e = 0;
                            double.TryParse(pair[1], out e);
                            WF_Config.Elevation = e;
                            break;
                        case "sSI":
                            WF_Config.SI = (pair[1] == "1");
                            break;
                        case "sHub":
                            WF_Config.Hub = (pair[1] == "1");
                            break;
                        case "restart":
                            WeatherFlowNS.NS.SetupRest();
                            WeatherFlowNS.NS.ConfigureNodes();
                            WeatherFlowNS.NS.StartUDPMonitor();
                            WeatherFlowNS.NS.StartHeartbeat();
                            break;
                        default:
                            break;
                    }
                }

                WeatherFlowNS.SaveConfiguration();
            }

            cfg_page = MakeConfigPage();
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
            item += "<form method=\"post\" action=\"/config\">\n";
            item += "<input type=\"hidden\" name=\"" + varname + "\" value=\"" + "0" + "\">\n";
            item += "<td><b>" + title + "</b></td>\n";
            item += "<td width=\"5%\"><input " + check + " type=\"checkbox\" name=\"" + varname + "\" value=\"" + "1" + "\" onClick=\"this.form.submit();\"></td>\n";
            item += "</form></tr>\n";

            return item;
        }

        private string ConfigItem(string title, string varname, string varvalue, int type) {
            string item;

            item = "<tr>\n";
            item += "<td width=\"50%\" class=\"fieldTitle\">" + title + "</td>\n";
            item += "<form method=\"post\">\n";
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
            item += "</form>\n";
            item += "<td></td>\n";
            item += "</tr>\n";

            return item;
        }

        private string MakeConfigPage() {
            string page;

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
            page += ConfigItem("Station ID", "sId", WF_Config.StationID.ToString(), 0);
            page += ConfigItem("Profile Number", "sProfile", WF_Config.Profile.ToString(), 0);
            page += ConfigItem("Station Elevation (meters)", "sElevation", WF_Config.Elevation.ToString(), 0);
            page += ConfigBoolItem("Use SI Units", "sSI", WF_Config.SI);
            page += ConfigBoolItem("Include Hub data", "sHub", WF_Config.Hub);

            page += "<tr>\n";
            page += "<td width=\"50%\" class=\"fieldTitle\">Start Node Server with these settings</td>\n";
            page += "<form method=\"post\">\n";
            page += "<input type=\"hidden\" name=\"restart\" value=\"" + "1" + "\">\n";
            page += "<td width=\"40%\"><input style=\"width:250px\" type=\"submit\" value=\" Start Node Server \"></td>";
            page += "<td width=\"10%\"></td>\n";
            page += "</form>\n";
            page += "<td></td>\n";
            page += "</tr>\n";

            page += "</table> </div> </table> </form>\n";

            page += "<div style=\"padding-left: 4px; padding-right: 4px; padding-top: 20px; padding-bottom: 1px\">\n";
            page += "<table width=\"400px\" border=\"1\">\n";
            page += "<tr><th>Node Address</th><th>Node Type</th></tr>\n";
            foreach (string n in WeatherFlowNS.NS.NodeList.Keys) {
                page += "<tr><td>" + n + "</td><td>" + WeatherFlowNS.NS.NodeList[n] + "</td></tr>\n";
            }
            page += "</table>\n";
            page += "</div>\n";

            page += "</body> </html> \n";

            return page;
        }

    }
 
}
