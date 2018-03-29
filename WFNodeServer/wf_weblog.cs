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

namespace WFNodeServer {
    internal static class WFWebLog {

        internal static void LogPage(HttpListenerContext context) {
            byte[] page;

            page = ASCIIEncoding.ASCII.GetBytes(MakeLog());

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = page.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(page, 0, page.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
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
        }

        private static string MakeLog() {
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
            page += "<script>\n";
            page += "  function loadLog(callback) {\n";
            page += "    var xhttp;\n";
            page += "    xhttp = new XMLHttpRequest();\n";
            page += "    xhttp.onreadystatechange = function() {\n";
            page += "      if (this.readyState == 4 && this.status == 200) {\n";
            page += "         callback(this);\n";
            page += "      }\n";
            page += "    };\n";
            page += "    xhttp.open(\"GET\", \"/wflog\", true);\n";
            page += "    xhttp.send();\n";
            page += "  }\n";
            page += "  function UpdateLog(xhttp) {\n";
            page += "    var elem = document.getElementById(\"log\");\n";
            page += "    elem.innerHTML = xhttp.responseText;\n";
            page += "    elem.scrollTop = elem.scrollHeiht;\n";
            page += "  }\n";
            page += "  function Start() {\n";
            page += "    setInterval(() => loadLog(UpdateLog), 2000);\n";
            page += "  }\n";
            page += "</script>\n";
            page += "<title>WeatherFlow Nodeserver Web Interface</title>\n";
            page += "</head><body onload=\"Start();\">\n";
            page += "<textarea id=\"log\" rows=\"45\" cols=\"120\">";
            page += "</textarea>\n";
            page += "</body></html>\n";

            return page;
        }
    }
}
