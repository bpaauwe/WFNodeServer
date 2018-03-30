﻿//
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
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Web.Script.Serialization;

namespace WFNodeServer {
    internal class StateObject {
        internal TcpClient workSocket = null;
        internal const int bufsize = 1024;
        internal byte[] buffer = new byte[bufsize];
        internal int offset = 0;
    }

    class wf_websocket {
        TcpClient client;
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        private static ManualResetEvent CloseDone = new ManualResetEvent(false);
        private static bool finished = false;
        private static Thread receive_thread;
        private Dictionary<string, bool> started = new Dictionary<string, bool>();
        private Dictionary<string, bool> started_rapid = new Dictionary<string, bool>();
        internal bool Started = false;
        private string Host;
        private int Port;
        private string Path;
        private string seckey = "di5kdaiynKWkf8cH";

        internal wf_websocket() {
        }

        internal wf_websocket(string host, int port, string path) {
            Host = host;
            Port = port;
            Path = path;
        }

        internal void Start() {
            byte[] buf = new byte[512];
            int len;
            bool header = false;

            while (Started) {
                int retries = 0;

                WFLogging.Warning("Attempt to start already active WebSocket connection " + retries.ToString());
                Thread.Sleep(1000);

                if (retries++ > 10) {
                    WFLogging.Error("Giving up after 10 attempts.");
                    return;
                }
            }

            client = new TcpClient(Host, Port);
            if (!client.Connected) {
                WFLogging.Error("Client not connected to " + Port);
            }

            WFLogging.Log("Starting communication with websocket server.");
            finished = false;
            Started = true;

            // Send header
            var seckeybytes = Encoding.UTF8.GetBytes(seckey);

            client.Client.Send(Encoding.ASCII.GetBytes("GET " + Path + " HTTP/1.1\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Host: " + Host + ":" + Port.ToString() + "\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Upgrade: websocket\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Connection: Upgrade\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Pragma: no-cache\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Origin: http://" + Host + "\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Cache-Control: no-cache\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Sec-WebSocket-Key: " + System.Convert.ToBase64String(seckeybytes) + "\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("Sec-WebSocket-Version: 13\r\n"));
            client.Client.Send(Encoding.ASCII.GetBytes("\r\n"));

            WFLogging.Info("    Waiting for handshake");
            Thread.Sleep(100);
            // Receive handshake
            while (!header) {
                if (client.Client.Available > 0) {
                    len = client.Client.Receive(buf, (client.Client.Available - 1), SocketFlags.None);
                    if (len > 0) {
                        string strbuf = Encoding.ASCII.GetString(buf, 0, len);
                        // Just look for the a websock type response, ignore the rest of the headers
                        if (strbuf.Contains("HTTP/1.1 101")) {
                            header = true;
                        }
                    }
                }
                Thread.Sleep(500);
            }

            receive_thread = new Thread(new ThreadStart(ReceiveLoop));
            receive_thread.IsBackground = true;
            receive_thread.Start();
        }

        internal void Send(string message) {
            SendMessage(client, message, 0x01);
        }

        internal void StartListen(string device_id) {
            string message = "{ \"type\":\"listen_start\", \"device_id\":";
            message += device_id;
            message += ", \"id\":\"random-id-23456\" }";

            WFLogging.Info("    Starting listen for device " + device_id);
            SendMessage(client, message, 0x01);
            started.Add(device_id, true);
        }

        internal void StopListen(string device_id) {
            string message = "{ \"type\":\"listen_stop\", \"device_id\":";
            message += device_id;
            message += ", \"id\":\"random-id-23456\" }";

            WFLogging.Info("    Stopping listen for device " + device_id);
            SendMessage(client, message, 0x01);
        }

        internal void StartListenRapid(string device_id) {
            string message = "{ \"type\":\"listen_rapid_start\", \"device_id\":";
            message += device_id;
            message += ", \"id\":\"random-id-23456\" }";

            WFLogging.Info("    Starting listen for device " + device_id);
            SendMessage(client, message, 0x01);
            started_rapid.Add(device_id, true);
        }

        internal void StopListenRapid(string device_id) {
            string message = "{ \"type\":\"listen_rapid_stop\", \"device_id\":";
            message += device_id;
            message += ", \"id\":\"random-id-23456\" }";

            WFLogging.Info("    Stopping listen for device " + device_id);
            SendMessage(client, message, 0x01);
        }

        internal void Stop() {
            string message = "close";

            WFLogging.Info("    Stop all listening.");
            foreach (string key in started.Keys) {
                StopListen(key);
            }
            started.Clear();

            foreach (string key in started_rapid.Keys) {
                StopListenRapid(key);
            }
            started_rapid.Clear();

            SendMessage(client, message, 0x08);
            finished = true;
        }

        private static void ProcessWSData(string json) {
            if (json.Contains("obs_air")) {
                WeatherFlowNS.NS.udp_client.WSObservations(json);
            } else if (json.Contains("obs_sky")) {
                WeatherFlowNS.NS.udp_client.WSObservations(json);
            } else if (json.Contains("rapid_wind")) {
                WeatherFlowNS.NS.udp_client.RapidWindEvt(json);
            } else if (json.Contains("evt_strike")) {
                WeatherFlowNS.NS.udp_client.LigtningStrikeEvt(json);
            } else if (json.Contains("evt_precip")) {
            } else if (json.Contains("ack")) {
            } else {
                WFLogging.Error("Unknown type of WebSocket packet");
                WFLogging.Error(json);
            }
        }

        private void ReceiveLoop() {
            StateObject state = new StateObject();

            WFLogging.Info("    Starting receive loop");
            state.workSocket = client;
            while (!finished) {
                // Start receive data from server
                while (client.Client.Available == 0)
                    Thread.Sleep(100);

                state.offset = 0;
                client.Client.BeginReceive(state.buffer, 0, StateObject.bufsize, 0, new AsyncCallback(ReceiveCallback), state);
                receiveDone.WaitOne();
            }

            // Wait for server to close connection
            WFLogging.Info("   Waiting for server close.");
            CloseDone.WaitOne();
            client.Close();
            Thread.Sleep(500);
            Started = false;
        }

        private static void ReceiveCallback(IAsyncResult ar) {
            StateObject state = (StateObject)ar.AsyncState;
            TcpClient client = state.workSocket;

            int bytes_read = client.Client.EndReceive(ar);

            if (bytes_read > 0) {
                int bi = 0;
                byte[] mask = new byte[4];
                if ((state.buffer[0] & 0x80) == 0x80) {
                    int payload_type = state.buffer[0] & 0x0f;
                    int payload_size = state.buffer[1] & 0x7f;
                    int payload_masking = state.buffer[1] & 0x80;
                    bi += 2;
                    //Console.WriteLine("type = " + payload_type.ToString() + " mask = " + payload_masking.ToString() + " len = " + payload_size.ToString());
                    if (payload_size == 126) {
                        payload_size = (state.buffer[bi++] << 8) + state.buffer[bi++];
                        //Console.WriteLine("extended size = " + payload_size.ToString());
                    } 
                    if (payload_masking == 0x80) {
                        mask[0] = state.buffer[bi++];
                        mask[1] = state.buffer[bi++];
                        mask[2] = state.buffer[bi++];
                        mask[3] = state.buffer[bi++];
                    }

                    if (bytes_read > payload_size) {
                        for (int i = 0; i < payload_size; i++) {
                            if (payload_masking == 0x80)
                                state.buffer[bi+i] = (byte)(state.buffer[bi+i] ^ mask[i % 4]);
                        }

                        if (payload_type == 0x01) {
                            // TODO: Text type payload so send it somewhere
                            //Console.WriteLine("Payload: " + Encoding.ASCII.GetString(state.buffer, bi, payload_size));
                            //Program.RaiseEvent(new WSEventArgs(Encoding.ASCII.GetString(state.buffer, bi, payload_size)));
                            ProcessWSData(Encoding.ASCII.GetString(state.buffer, bi, payload_size));
                        } else if (payload_type == 0x02) {
                            // Binary payload
                        } else if (payload_type == 0x00) {
                            WFLogging.Error("Got a continuation opcode, currently not supported.");
                        } else if (payload_type == 0x08) {
                            // Close frame
                            finished = true;
                            SendMessage(client, Encoding.ASCII.GetString(state.buffer, bi, payload_size), 0x08);
                        } else if (payload_type == 0x09) {
                            // Ping so we need to pong
                            SendMessage(client, Encoding.ASCII.GetString(state.buffer, bi, payload_size), 0x0A);
                        }

                        receiveDone.Set();
                        return;
                    } else {
                        // We need to read more data
                        WFLogging.Info("Need more data to complete frame");
                        client.Client.BeginReceive(state.buffer, 0, StateObject.bufsize, 0, new AsyncCallback(ReceiveCallback), state);
                        return;
                    }
                }
            }
            receiveDone.Set();
        }

        private static void SendMessage(TcpClient client, string message, int type) {
            byte[] payload = new byte[message.Length];
            byte[] frame = new byte[message.Length + 8];
            int framelen = 0;

            frame[0] = (byte)(0x80 | type); // Single frame and payload type
            if (message.Length > 126) {
                frame[1] = 0x80 | 126; // Masked
                frame[2] = (byte)((message.Length & 0xff00) >> 8);
                frame[3] = (byte)(message.Length & 0x00ff);
                frame[4] = 0x10; // Masking key
                frame[5] = 0x10; // Masking key
                frame[6] = 0x10; // Masking key
                frame[7] = 0x10; // Masking key
                framelen = 8;
            } else {
                frame[1] = (byte)(0x80 | message.Length);
                frame[2] = 0x10; // Masking key
                frame[3] = 0x10; // Masking key
                frame[4] = 0x10; // Masking key
                frame[5] = 0x10; // Masking key
                framelen = 6;
            }

            payload = Encoding.ASCII.GetBytes(message);
            for (int i = 0; i < message.Length; i++)
                frame[framelen + i] = (byte)(payload[i] ^ 0x10);

            // Send the frame
            client.Client.Send(frame, 0, message.Length + framelen, SocketFlags.None);

            if (type == 0x08) // Close connection message was sent
                CloseDone.Set();
        }
    }
}
