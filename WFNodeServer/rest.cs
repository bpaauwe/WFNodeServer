﻿using System;
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
            Console.WriteLine("Authorize: encoding = \"" + s + "\" to \"" + auth + "\"");

            return ("Basic " + auth);
        }

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

            if (AuthHeader == "")
                AuthHeader = Authorize();

            //Console.WriteLine(rest_url);
            request = (HttpWebRequest)HttpWebRequest.Create(rest_url);
            request.Headers.Add("Authorization", AuthHeader);
            request.Proxy = null;

            // Read data from the stream
            try {
                response = (HttpWebResponse)request.GetResponse();
                if (response.ContentLength > 0) {
                    code = (int)response.StatusCode;
                    if (code != 200) {
                        if (code == 404) {
                            // This isn't really an error, it just means
                            // there's no entries at this URL
                            Console.WriteLine("REST request " +
                                    url + " failed " +
                                    response.StatusDescription);
                        } else if (code == 405) {
                            // This isn't really an error. It means that
                            // the URL doesn't exist (mostly seen when
                            // trying to query information about a module that
                            // is not installed.
                            Console.WriteLine("REST request " +
                                    url + " failed " +
                                    response.StatusDescription);
                        } else {
                            Console.WriteLine("ISY REST request to URL, " +
                                    url + ", failed with " +
                                    response.StatusDescription);
                        }
                    } else {
                        byte[] buf = new byte[4096];
                        int len;
                        string chunk;

                        reader = response.GetResponseStream();
                        do {
                            len = reader.Read(buf, 0, buf.Length);
                            if (len > 0) {
                                chunk = new String(Encoding.ASCII.GetString(buf).ToCharArray(), 0, len);
                                xml += chunk;
                            }
                        } while (len > 0);
                        reader.Close();
                        //Console.WriteLine("REST got: " + xml);
                    }
                }
                response.Close();
            } catch (WebException ex) {
                // Try to get more information about the error
                if (ex.Response != null) {
                    HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                    code = (int)errorResponse.StatusCode;
                    if (code == 200) {
                        xml = ex.Message;
                    } else if (code == 404) {
                        // Not found errors are not really errors.
                        Console.WriteLine("REST request " +
                                url + " failed {" + code.ToString() + "} " +
                                errorResponse.StatusDescription);
                        xml = ex.Message;
                    } else {
                        Console.WriteLine("ISY REST request to URL, " +
                                url + ", failed with {" + code.ToString() + "} " +
                                errorResponse.StatusDescription);
                    }
                } else {
                    xml = "ISY REST request " + url + " failed with " + ex.Message;
                    Console.WriteLine(xml);
                    //throw new RestException();
                }
            }

            return xml;
        }
    }
}
