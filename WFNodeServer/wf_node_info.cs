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

namespace WFNodeServer {
    internal static class WFNodeInfo {
        internal static void NodeDocPage(HttpListenerContext context) {
            byte[] page;

            page = ASCIIEncoding.ASCII.GetBytes(MakeDocs());

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = page.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(page, 0, page.Length);
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
            context.Response.Close();
        }

        private static string TableRow(string name, string address, string gv, string value, string note) {
            string table_row;

            table_row = "<tr><td>GV" + gv + "</td>";
            table_row += "<td>" + name + "</td>";
            table_row += "<td>${sys.node." + address + ".GV" + gv + "}</td>";
            table_row += "<td>" + value + "</td>";
            table_row += "<td>" + note + "</td>";
            table_row += "</tr>";

            return table_row;
        }

        private static string AirDoc(string address) {
            string table;
            AirEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Air</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (AirEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Last Update", address, "0", "", "Time in seconds since last update");
                table += TableRow("Temperature", address, "1", args.Temperature, "");
                table += TableRow("Humidity", address, "2", args.Humidity, "");
                table += TableRow("Barometric Pressure", address, "3", args.SeaLevel, "");
                table += TableRow("Lightning Strikes", address, "4", args.Strikes, "");
                table += TableRow("Ligthning Distance", address, "5", args.Distance, "");
                table += TableRow("DewPoint", address, "6", args.Dewpoint, "");
                table += TableRow("Apparent Temperature", address, "7", args.ApparentTemp, "");
                table += TableRow("Pressure Trend", address, "8", args.Trend, "0 = falling, 1 = steady, 2 = rising");
                table += TableRow("Battery", address, "9", args.Battery, "");
            } else {
                table += TableRow("Last Update", address, "0", "", "Time in seconds since last update");
                table += TableRow("Temperature", address, "1", "###", "");
                table += TableRow("Humidity", address, "2", "###", "");
                table += TableRow("Barometric Pressure", address, "3", "###", "");
                table += TableRow("Lightning Strikes", address, "4", "###", "");
                table += TableRow("Ligthning Distance", address, "5", "###", "");
                table += TableRow("DewPoint", address, "6", "###", "");
                table += TableRow("Apparent Temperature", address, "7", "###", "");
                table += TableRow("Pressure Trend", address, "8", "###", "0 = falling, 1 = steady, 2 = rising");
                table += TableRow("Battery", address, "9", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string AirDeviceDoc(string address) {
            string table;
            DeviceEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Air Device Info</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (DeviceEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Battery", address, "0", args.Voltage, "");
                table += TableRow("Uptime", address, "1", args.UpTime, "");
                table += TableRow("Signal Strength", address, "2", args.RSSI, "");
                table += TableRow("Lightning Sensor", address, "3", args.SensorStatus(0), "");
                table += TableRow("Lightning Noise", address, "4", args.SensorStatus(1), "Triggered if noise detected by sensor");
                table += TableRow("Ligthning Disturber", address, "5", args.SensorStatus(2), "Triggered if ???");
                table += TableRow("Pressure Sensor", address, "6", args.SensorStatus(3), "");
                table += TableRow("Temperature Sensor", address, "7", args.SensorStatus(4), "");
                table += TableRow("Humidity Sensor", address, "8", args.SensorStatus(5), "");
                table += TableRow("Firmware Revision", address, "9", args.Firmware, "");
            } else {
                table += TableRow("Battery", address, "0", "###", "");
                table += TableRow("Uptime", address, "1", "###", "");
                table += TableRow("Signal Strength", address, "2", "###", "");
                table += TableRow("Lightning Sensor", address, "3", "###", "");
                table += TableRow("Lightning Noise", address, "4", "###", "Triggered if noise detected by sensor");
                table += TableRow("Ligthning Disturber", address, "5", "###", "Triggered if ???");
                table += TableRow("Pressure Sensor", address, "6", "###", "");
                table += TableRow("Temperature Sensor", address, "7", "###", "");
                table += TableRow("Humidity Sensor", address, "8", "###", "");
                table += TableRow("Firmware Revision", address, "9", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string SkyDoc(string address) {
            string table;
            SkyEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Sky</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (SkyEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Last Update", address, "0", "", "Time in seconds since last update");
                table += TableRow("Illumination", address, "1", args.Illumination, "");
                table += TableRow("UV Index", address, "2", args.UV, "");
                table += TableRow("Solar Radiation", address, "3", args.SolarRadiation, "");
                table += TableRow("Wind Speed", address, "4", args.WindSpeed, "");
                table += TableRow("Gust Speed", address, "5", args.GustSpeed, "");
                table += TableRow("Wind Lull", address, "6", args.WindLull, "");
                table += TableRow("Wind Direction", address, "7", args.WindDirection, "");
                table += TableRow("Rain Rate", address, "8", args.RainRate, "");
                table += TableRow("Daily Rainfall", address, "9", args.Rain, "");
                table += TableRow("Battery", address, "10", args.Battery, "");
                table += TableRow("Rain Type", address, "11", args.PrecipitationType, "");
            } else {
                table += TableRow("Last Update", address, "0", "", "Time in seconds since last update");
                table += TableRow("Illumination", address, "1", "###", "");
                table += TableRow("UV Index", address, "2", "###", "");
                table += TableRow("Solar Radiation", address, "3", "###", "");
                table += TableRow("Wind Speed", address, "4", "###", "");
                table += TableRow("Gust Speed", address, "5", "###", "");
                table += TableRow("Wind Lull", address, "6", "###", "");
                table += TableRow("Wind Direction", address, "7", "###", "");
                table += TableRow("Rain Rate", address, "8", "###", "");
                table += TableRow("Daily Rainfall", address, "9", "###", "");
                table += TableRow("Battery", address, "10", "###", "");
                table += TableRow("Rain Type", address, "11", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string SkyDeviceDoc(string address) {
            string table;
            DeviceEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Sky Device Info</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (DeviceEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Battery", address, "0", args.Voltage, "");
                table += TableRow("Uptime", address, "1", args.UpTime, "");
                table += TableRow("Signal Strength", address, "2", args.RSSI, "");
                table += TableRow("Wind Sensor", address, "3", args.SensorStatus(0), "");
                table += TableRow("Precipitation Sensor", address, "4", args.SensorStatus(1), "");
                table += TableRow("Light/UV Sensor", address, "5", args.SensorStatus(2), "");
                table += TableRow("Firmware Revision", address, "6", args.Firmware, "");
            } else {
                table += TableRow("Battery", address, "0", "###", "");
                table += TableRow("Uptime", address, "1", "###", "");
                table += TableRow("Signal Strength", address, "2", "###", "");
                table += TableRow("Wind Sensor", address, "3", "###", "");
                table += TableRow("Precipitation Sensor", address, "4", "###", "");
                table += TableRow("Light/UV Sensor", address, "5", "###", "");
                table += TableRow("Firmware Revision", address, "6", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string RapidDoc(string address) {
            string table;
            RapidEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Rapid Wind</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (RapidEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Rapid Wind Direction", address, "0", args.Direction.ToString(), "");
                table += TableRow("Rapid Wind Speed", address, "1", args.Speed, "");
            } else {
                table += TableRow("Rapid Wind Direction", address, "0", "###", "");
                table += TableRow("Rapid Wind Speed", address, "1", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string LightningDoc(string address) {
            string table;
            LightningEventArgs args = null;

            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Lightning</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (LightningEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Time", address, "0", args.TimeStamp, "Epoch time of strike");
                table += TableRow("Distance", address, "1", args.Distance, "");
                table += TableRow("Energy", address, "2", args.Energy, "Strike energy, unknown units");
            } else {
                table += TableRow("Time", address, "0", "###", "Epoch time of strike");
                table += TableRow("Distance", address, "1", "###", "");
                table += TableRow("Energy", address, "2", "###", "Strike energy, unknown units");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string HubDoc(string address) {
            string table;
            HubEventArgs args = null;


            table = "<tr><th colspan=\"5\">WeatherFlow Smart Weather Station Sky Hub Info</th></tr>";
            table += "<tr><th>Value</th><th>Name</th><th>Custom Notification</th><th>Value</th><th>Notes</th></tr>";
            if (WeatherFlowNS.NS.NodeData.ContainsKey(address)) {
                args = (HubEventArgs)WeatherFlowNS.NS.NodeData[address];
                table += TableRow("Heartbeat", address, "0", "", "Toggles between -1 and 1");
                table += TableRow("Firmware Revision", address, "1", args.Firmware, "");
                table += TableRow("Uptime", address, "2", args.Uptime.ToString(), "");
                table += TableRow("Signal Strength", address, "3", args.RSSI, "");
                table += TableRow("Sequence", address, "4", args.Sequence, "");
            } else {
                table += TableRow("Heartbeat", address, "0", "", "Toggles between -1 and 1");
                table += TableRow("Firmware Revision", address, "1", "###", "");
                table += TableRow("Uptime", address, "2", "###", "");
                table += TableRow("Signal Strength", address, "3", "###", "");
                table += TableRow("Sequence", address, "4", "###", "");
            }
            table += "<tr><th colspan=\"5\">&nbsp;</th></tr>";

            return table;
        }

        private static string MakeDocs() {
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
            page += "<div align=\"center\" style=\"width: 920px; margin: 0 auto;\">\n";
            page += WFNServer.MakeMenu();

            foreach (string addr in WeatherFlowNS.NS.NodeList.Keys) {
                page += "<table border=\"1\" style=\"width: 900px; border-collapse:collapse; box-shadow: 4px 4px 4px #999; \">";
                if (WeatherFlowNS.NS.NodeList[addr] == "WF_AirUS")
                    page += AirDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_Air")
                    page += AirDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_AirUK")
                    page += AirDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_AirD")
                    page += AirDeviceDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_SkyUS")
                    page += SkyDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_SkyUK")
                    page += SkyDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_Sky")
                    page += SkyDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_SkyD")
                    page += SkyDeviceDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_Hub")
                    page += HubDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_Lightning")
                    page += LightningDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_LightningUS")
                    page += LightningDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_LightningUK")
                    page += LightningDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_RapidWind")
                    page += RapidDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_RapidWindUS")
                    page += RapidDoc(addr);
                else if (WeatherFlowNS.NS.NodeList[addr] == "WF_RapidWindUK")
                    page += RapidDoc(addr);
                else
                    WFLogging.Log("Unknown node type " + WeatherFlowNS.NS.NodeList[addr]);
                page += "</table>";
                page += "<p>";
            }

            page += "</div>";
            page += "</body>\n";
            page += "</html>\n";

            return page;
        }
    }
}
