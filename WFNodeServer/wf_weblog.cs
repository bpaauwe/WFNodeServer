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
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Web;
using System.Net.NetworkInformation;

namespace WFNodeServer {
    internal static class WFWebLog {

        private static string element(string resp, string key) {
            foreach (string item in resp.Split('&')) {
                string[] pair = item.Split('=');
                if (pair[0] == key)
                    return HttpUtility.UrlDecode(pair[1]);
            }
            return "";
        }

        internal static void LogPage(HttpListenerContext context) {
            byte[] page;

            // Process changes
            if (context.Request.ContentLength64 > 0) {
                int len = (int)context.Request.ContentLength64;
                byte[] post = new byte[1024];

                context.Request.InputStream.Read(post, 0, len);
                string resp = Encoding.Default.GetString(post);
                resp = resp.Substring(0, len);

                foreach (string item in resp.Split('&')) {
                    string[] pair = item.Split('=');
                    switch (pair[0]) {
                        case "sLogLevel":
                            int l = 0;
                            int.TryParse(pair[1], out l);
                            if (l != WF_Config.LogLevel) {
                                WF_Config.LogLevel = l;
                                WFLogging.Level = (LOG_LEVELS)l;
                                WeatherFlowNS.SaveConfiguration();
                            }
                            break;
                        case "Save":
                            // Look up value of filename and save log to that file
                            string fname = element(resp, "filename");
                            try {
                                using (StreamWriter sw = new StreamWriter(fname)) {
                                    sw.Write(WFLogging.ToString());
                                }
                            } catch (Exception ex) {
                                WFLogging.Error(ex.Message);
                            }
                            break;
                        case "Clear":
                            WFLogging.Clear();
                            break;
                    }
                }
            }

            page = ASCIIEncoding.ASCII.GetBytes(MakeLog());

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = page.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(page, 0, page.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            context.Response.Close();
        }

        internal static void LogText(HttpListenerContext context) {
            byte[] page;

            page = ASCIIEncoding.ASCII.GetBytes(WFLogging.ToString());

            context.Response.ContentType = "text/text";
            context.Response.ContentLength64 = page.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(page, 0, page.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            context.Response.Close();
        }

        private static string MakeLog() {
            string page;

            page = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html\">\n";
            page += "<meta http-equiv=\"cache-control\" content=\"no-cache\">\n";
            page += "<meta http-equiv=\"expires\" content=\"0\">\n";
            page += "<meta http-equiv=\"pragma\" content=\"no-cache\">\n";
            page += "<meta http-equiv=\"Content-Language\" content=\"en\">\n";
            page += "<meta charset=\"UTF-8\">\n";
            page += "<style>\n";
            page += "	body { font-family: Sans-Serif; }\n";
            page += "</style>\n";
            page += "<script>\n";
            page += "  var paused = false;\n";
            page += "  var pause_log = \"\";\n";
            page += "  var logSocket = new WebSocket(\"ws://" + ISYDetect.GetMyIPAddress() + ":8289\", \"Log\");";
            page += "  logSocket.onopen = function(event) {\n";
            page += "      //alert(\"Opened websocket connection\");\n";
            page += "  }\n";
            page += "  logSocket.onmessage = function(event) {\n";
            page += "      if (!paused) {\n";
            page += "          var elem = document.getElementById(\"log\");\n";
            page += "          elem.value += (event.data + \"\\n\");\n";
            page += "          var total = elem.value.split(\"\\n\");\n";
            page += "          if (total.length > 8000)\n";
            page += "              total = total.slice(-8000);\n";
            page += "          elem.value = total.join(\"\\n\");\n";
            page += "          elem.scrollTop = elem.scrollHeight;\n";
            page += "      } else {\n";
            page += "          pause_log += (event.data + \"\\r\\n\");\n";
            page += "      }\n";
            page += "  }\n";
            page += "  function PauseLog() {\n";
            page += "    if (!paused) {\n";
            page += "       document.getElementById(\"Pause\").value = \"Paused\";\n";
            page += "       paused = true;\n";
            page += "    } else {\n";
            page += "       document.getElementById(\"Pause\").value = \"Pause\";\n";
            page += "       var elem = document.getElementById(\"log\");\n";
            page += "       elem.innerHTML += pause_log;\n";
            page += "       elem.scrollTop = elem.scrollHeight;\n";
            page += "       paused = false;\n";
            page += "       pause_log = \"\";\n";
            page += "    }\n";
            page += "  }\n";
            page += "</script>\n";
            page += "<title>WeatherFlow Nodeserver Web Interface - Log</title>\n";
            page += "</head><body>\n";
            page += "<div align=\"center\" style=\"width: 900px; margin: 0 auto;\">\n";
            page += WFNServer.MakeMenu();
            page += "<textarea id=\"log\" rows=\"45\" cols=\"120\">";
            page += "</textarea>\n";
            page += "<hr>\n";
            page += "<form name=\"log\" action=\"/log\" enctype=\"application/x-www-form-urlencoded\" method=\"post\">\n";
            page += "<input type=\"Button\" id=\"Pause\" name=\"Pause\" value=\"Pause\" onclick=\"PauseLog();\"> &nbsp;";
            page += "<input type=\"submit\" id=\"Clear\" name=\"Clear\" value=\"Clear\"> &nbsp;";
            page += "<input type=\"submit\" name=\"Save\" value=\"Save to\"> &nbsp;";
            page += "<input id=\"the-file-input\" type=\"text\" name=\"filename\"> &nbsp;";
            page += "Log Level: ";
            page += "<select name=\"sLogLevel\" onchange=\"this.form.submit()\">";
            page += "<option value=\"0\" " + ((WF_Config.LogLevel == 0) ? "selected" : "") + ">Updates</option>";
            page += "<option value=\"1\" " + ((WF_Config.LogLevel == 1) ? "selected" : "") + ">Errors</option>";
            page += "<option value=\"2\" " + ((WF_Config.LogLevel == 2) ? "selected" : "") + ">Warnings</option>";
            page += "<option value=\"3\" " + ((WF_Config.LogLevel == 3) ? "selected" : "") + ">Info</option>";
            page += "<option value=\"4\" " + ((WF_Config.LogLevel == 4) ? "selected" : "") + ">Debug</option>";
            page += "</select>\n";
            page += "</form>\n";
            page += "</div>\n";
            page += "</body></html>\n";

            return page;
        }

    }
}
