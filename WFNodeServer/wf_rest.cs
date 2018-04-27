
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
using System.Web;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;


namespace WFNodeServer {
    internal class RestStats {
        internal DateTime Start { get; set; }
        internal double RequestTime { get; set; }
        private int count;
        private DateTime last;

        internal RestStats() {
            Start = DateTime.Now;
            count = 0;
            last = Start;
        }

        internal int Count {
            get { return count; }
            set {
                TimeSpan t = DateTime.Now.Subtract(last);
                string l = String.Format("{0,12:0.0000ms}", t.TotalMilliseconds);
                WFLogging.Info("Time since last request = " + l + "  took " + RequestTime.ToString() + "ms");
                count = value;
                last = DateTime.Now;
            }
        }

        internal string Rate {
            get {
                TimeSpan t = DateTime.Now.Subtract(Start);
                if (t.TotalMinutes == 0)
                    return count.ToString();

                return (count / t.TotalMinutes).ToString("0.##");
            }
        }
    }

    internal class rest {
        internal string Password { get; set; }
        internal string Username { get; set; }
        internal string Base;
        internal bool AuthRequired = true;
        internal RestStats stats = new RestStats();
        private object RateLimit = new object();

        internal rest() {
        }

        internal rest(string base_url) {
            Base = base_url;
            System.Net.ServicePointManager.Expect100Continue = false;
        }

        // 
        //  Build the authorization header needed to send requests to
        //  the ISY. 
        //
        internal string Authorize() {
            System.Text.ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] u;
            string s;
            string auth;

            auth = Convert.ToBase64String(encoding.GetBytes(
                Username + ":" + Password));

            // Decode the string for debugging purposes.
            u = Convert.FromBase64String(auth);
            s = encoding.GetString(u);
            //Console.WriteLine("Authorize: encoding = \"" + s + "\" to \"" + auth + "\"");

            return ("Basic " + auth);
        }

        [Serializable]
        internal class RestException : Exception {
            internal RestException() : base() { }
            internal RestException(string message) : base(message) { }
        }

        //
        // Given a REST partial URL, make the connection and return
        // the XML response
        //
        internal string REST(string url) {
            HttpWebRequest request;
            HttpWebResponse response;
            string xml = "";
            string rest_url;
            int code;
            DateTime start = DateTime.Now;

            rest_url = Base + url;

            if (rest_url == "") {
                WFLogging.Error("ISY REST called with missing URL.");
                WFLogging.Error("  Does this mean there's no connection to an ISY?");
                return "";
            }

            lock (RateLimit) {
                WFLogging.Debug(rest_url);
                request = (HttpWebRequest)HttpWebRequest.Create(rest_url);
                request.UserAgent = "WFNodeServer";
                if (AuthRequired)
                    request.Headers.Add("Authorization", Authorize());
                request.Proxy = null;
                request.ServicePoint.ConnectionLimit = 10;
                request.Timeout = 2000;
                request.KeepAlive = true;
                //request.Pipelined = true;

                // Read data from the stream
                try {
                    response = (HttpWebResponse)request.GetResponse();
                    if (response.ContentLength > 0) {
                        code = (int)response.StatusCode;
                        if (code != 200) {
                            if (code == 404) {  // No entries match this request
                                WFLogging.Error("REST request " + url + " failed " + response.StatusDescription);
                            } else if (code == 405) { // URL doesn't exist (bad request)
                                WFLogging.Error("REST request " + url + " failed " + response.StatusDescription);
                            } else {
                                WFLogging.Error("ISY REST request to URL, " +
                                        url + ", failed with " +
                                        response.StatusDescription);
                            }
                        } else {  // code == 200 OK, read response
                            xml = ChunkedRead(response);
                        }
                    } else if (response.ContentLength == -1) {// End of content length > 0
                        // Response is chunked?
                        xml = ChunkedRead(response);
                    }
                    response.Close();
                } catch (WebException ex) {
                    xml = "";
                    //Console.WriteLine(xml);
                    //throw new RestException();
                    WFLogging.Error("REST request " + url + " failed:");
                    WFLogging.Error("    " + ex.Message);
                }

                stats.RequestTime = DateTime.Now.Subtract(start).TotalMilliseconds;
                stats.Count++;
                //Thread.Sleep(50);
                return xml;
            }
        }

        internal void REST_POST(string url, string content, int len) {
            HttpWebRequest request;
            HttpWebResponse response;
            string rest_url;
            int code;

            rest_url = Base + url;

            WFLogging.Debug(rest_url);
            request = (HttpWebRequest)HttpWebRequest.Create(rest_url);
            request.UserAgent = "WFNodeServer";
            if (AuthRequired)
                request.Headers.Add("Authorization", Authorize());
            request.Proxy = null;
            request.KeepAlive = false;
            request.Method = "POST";
            request.ContentLength = len;
            request.ContentType = "application/xml";

            Stream datastream = request.GetRequestStream();
            datastream.Write(Encoding.ASCII.GetBytes(content), 0, len);
            datastream.Close();

            try {
                response = (HttpWebResponse)request.GetResponse();
                code = (int)response.StatusCode;
                response.Close();
            } catch (Exception ex) {
                WFLogging.Error(ex.Message);
            }
        }

        private string ChunkedRead(HttpWebResponse response) {
            Stream reader;
            byte[] buf = new byte[4096];
            int len;
            string chunk;
            string resp = "";

            reader = response.GetResponseStream();
            try {
                do {
                    len = reader.Read(buf, 0, buf.Length);
                    //Console.WriteLine("  -> Read " + len.ToString() + " bytes from stream.");
                    if (len > 0) {
                        chunk = new String(Encoding.ASCII.GetString(buf).ToCharArray(), 0, len);
                        resp += chunk;
                    }
                } while (len > 0);
            } catch {
                WFLogging.Debug("Ignoring reader exception?");
            }

            reader.Close();

            return resp;
        }

        // This is handling the SetParent service only at this point but could
        // be expanded to handle other ISY WSDL reuests if needed.
        internal void SendWSDLReqeust(string service, string parent, string node) {
            string url = "http://" + WF_Config.ISY + "/services";
            HttpWebRequest request;
            HttpWebResponse response;
            string reqString = "";

            reqString = "<? version=\'1.0\' encoding=\'utf-8\'?>";
            reqString += "<s:Envelope>";
            reqString += "<s:Body>";
            reqString += "<u:" + service + " xmlns:u=\'urn:udi-com:service:X_Insteon_Lighting_Service:1\'>";

            reqString += "<node>" + node + "</node>";
            reqString += "<nodeType>1</nodeType>";
            reqString += "<parent>" + parent + "</parent>";
            reqString += "<parentType>1</parentType>";

            reqString += "</u:" + service + ">";
            reqString += "</s:Body>";
            reqString += "</s:Envelope>";
            reqString += "\r\n";

            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentType = "text/xml; charset-utf-8";
            request.Headers.Add("Authorization", Authorize());
            request.Headers.Add("SOAPAction", "urn:udi-com:device:X_Insteon_Lighting_Service:1#" + service);

            Stream data = request.GetRequestStream();
            data.Write(Encoding.ASCII.GetBytes(reqString), 0, reqString.Length);
            data.Flush();
            data.Close();

            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
                WFLogging.Log("Grouped " + node + " as a child of " + parent);
            else
                WFLogging.Error("Group of " + node + " under " + parent + "failed: " + response.StatusDescription);

            response.Close();

            return;
        }

    }
}
