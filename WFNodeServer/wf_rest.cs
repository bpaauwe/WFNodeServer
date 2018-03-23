
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
    internal class rest {
        internal string Password { get; set; }
        internal string Username { get; set; }
        internal string Base;
        internal string AuthHeader = "";
        internal bool AuthRequired = true;

        internal rest() {
        }

        internal rest(string base_url) {
            Base = base_url;
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
            Stream reader;
            string xml = "";
            string rest_url;
            int code;

            rest_url = Base + url;

            if (rest_url == "") {
                Console.WriteLine("ISY REST called with missing URL.");
                Console.WriteLine("  Does this mean there's no connection to an ISY?");
                return "";
            }

            if (AuthHeader == "" && AuthRequired)
                AuthHeader = Authorize();

            //Console.WriteLine(rest_url);
            request = (HttpWebRequest)HttpWebRequest.Create(rest_url);
            request.UserAgent = "WFNodeServer";
            if (AuthRequired)
                request.Headers.Add("Authorization", AuthHeader);
            request.Proxy = null;
            request.KeepAlive = false;

            // Read data from the stream
            try {
                response = (HttpWebResponse)request.GetResponse();
                    if (response.ContentLength > 0) {
                        code = (int)response.StatusCode;
                        if (code != 200) {
                            if (code == 404) {  // No entries match this request
                                Console.WriteLine("REST request " + url + " failed " + response.StatusDescription);
                            } else if (code == 405) { // URL doesn't exist (bad request)
                                Console.WriteLine("REST request " + url + " failed " + response.StatusDescription);
                            } else {
                                Console.WriteLine("ISY REST request to URL, " +
                                        url + ", failed with " +
                                        response.StatusDescription);
                            }
                        } else {  // code == 200 OK, read response
                            byte[] buf = new byte[4096];
                            int len;
                            string chunk;

                            reader = response.GetResponseStream();
                            try {
                                do {
                                    len = reader.Read(buf, 0, buf.Length);
                                    //Console.WriteLine("  -> Read " + len.ToString() + " bytes from stream.");
                                    if (len > 0) {
                                        chunk = new String(Encoding.ASCII.GetString(buf).ToCharArray(), 0, len);
                                        xml += chunk;
                                    }
                                } while (len > 0);
                            } catch {
                                Console.WriteLine("Ignoring reader exception?");
                            }

                            reader.Close();
                            //Console.WriteLine("REST got: " + xml);
                        }
                    } // End of content length > 0
                response.Close();
            } catch (WebException ex) {
                xml = "";
                //Console.WriteLine(xml);
                //throw new RestException();
                Console.WriteLine(ex.Message);
            }

            request.Abort();
            return xml;
        }

        internal void REST_POST(string url, string content, int len) {
            HttpWebRequest request;
            HttpWebResponse response;
            string rest_url;
            int code;

            rest_url = Base + url;
            if (AuthHeader == "" && AuthRequired)
                AuthHeader = Authorize();

            Console.WriteLine(rest_url);
            request = (HttpWebRequest)HttpWebRequest.Create(rest_url);
            request.UserAgent = "WFNodeServer";
            if (AuthRequired)
                request.Headers.Add("Authorization", AuthHeader);
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
                Console.WriteLine(ex.Message);
            }
        }
    }
}
