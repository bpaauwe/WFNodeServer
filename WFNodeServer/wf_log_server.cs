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
using System.Threading;

namespace WFNodeServer {
    // Does it make any sense to create a simple websocket server
    // that just sends log information to any client that connects?
    class WF_WebsocketLog {
        string Address;
        int Port;
        Thread ws_server_thread;
        private static ManualResetEvent CloseDone = new ManualResetEvent(false);
        private TcpClient client;
        private List<TcpClient> clients = new List<TcpClient>();

        internal WF_WebsocketLog(int port) {
            Address = "0.0.0.0";
            Port = port;

            ws_server_thread = new Thread(new ThreadStart(Server));
            ws_server_thread.IsBackground = true;
            ws_server_thread.Start();
        }

        internal void Server() {
            Thread handleClient;
            TcpListener server = new TcpListener(IPAddress.Parse(Address), Port);

            server.Start();
            WFLogging.Log("Logging server has started on port " + Port.ToString());

            // Add a new log client to get new events and send over
            // the websocket.
            WFLogging.AddListener(WSLog);

            while (true) {
                WFLogging.Info("Waiting for log client to connect");
                client = server.AcceptTcpClient();
                handleClient = new Thread(() => ClientHandler(client));
                handleClient.Start();
            }
        }

        // handle a client connection
        private void ClientHandler(TcpClient client) {
            Byte[] bytes;
            NetworkStream stream = client.GetStream();

            WFLogging.Info("Log client connected.");
            // Wait for data to be available
            while (!stream.DataAvailable) ;

            bytes = new Byte[client.Available];
            stream.Read(bytes, 0, bytes.Length);

            //translate bytes of request to string
            String data = Encoding.UTF8.GetString(bytes);

            byte[] response = null;
            const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

            WFLogging.Debug("GOT:" + bytes.Length.ToString() + ": " + data);

            if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data)) {
            }

            string protocol = new System.Text.RegularExpressions.Regex("Sec-WebSocket-Protocol: (.*)").Match(data).Groups[1].Value.Trim();
            Console.WriteLine("Protocol: " + protocol);
            int l = bytes.Length;
            //Console.WriteLine("Checking for end of header");
            if (bytes[l-1] == '\n' && bytes[l-2] == '\r' && bytes[l-3] == '\n') {
                response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                                + "Upgrade: websocket" + eol
                                + "Connection: Upgrade" + eol
                                + "Sec-WebSocket-Protocol: " + protocol + eol
                                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                    System.Security.Cryptography.SHA1.Create().ComputeHash(
                                            Encoding.UTF8.GetBytes(
                                            new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                            )
                                        )
                                    ) + eol
                                + eol);
                //Console.WriteLine("sending response, " + response.Length.ToString());
                stream.Write(response, 0, response.Length);
                //stream.WriteByte(0x00);
                stream.Flush();
                WFLogging.Debug(Encoding.ASCII.GetString(response));
            } else {
                // What do we do here if we don't get a proper header (or complete header)?
                WFLogging.Info("Didn't get a proper header, closing connection");
                client.Close();
                return;
            }

            // Start sending the log.  This needs to be formatted properly as
            // an actual frame of data.
            int start = (WFLogging.EventLogCount > 500) ? (WFLogging.EventLogCount - 500) : 0;
            for (int i = start; i < WFLogging.EventLogCount; i++) {
                string[] e = WFLogging.GetEvent(i);
                try {
                    SendMessage(client, e[0] + "\t" + e[1], 0x01);
                } catch {
                    // Sending to client failed for some reason. So abort.
                    client.Close();
                    return;
                }
            }

            // Push the client to the client list
            Console.WriteLine("Adding client " + client.Client.Handle.ToString() + " to client list");
            clients.Add(client);

            // Handle data comming in over the connection. Mainly we want to
            // check for a close connection frame. If we get a close frame
            // then close the connection.
            while (stream.DataAvailable) {
                bytes = new Byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);

                // Data from client that needs to be decoded?
                WFLogging.Debug("Got " + bytes.Length.ToString() + " bytes from client to decode");
                if ((bytes[0] & 0x80) == 0x80) {
                    int payload_type = bytes[0] & 0x0f;
                    int payload_size = bytes[1] & 0x7f;
                    int payload_masking = bytes[1] & 0x80;
                    WFLogging.Debug("type = " + payload_type.ToString() + " mask = " + payload_masking.ToString() + " len = " + payload_size.ToString());
                    if (payload_size < 126) {
                        if (payload_masking == 0x80) {
                            byte[] mask = new byte[4];
                            mask[0] = bytes[2];
                            mask[1] = bytes[3];
                            mask[2] = bytes[4];
                            mask[3] = bytes[5];
                            for (int i = 0; i < payload_size; i++)
                                bytes[6 + i] = (byte)(bytes[6 + i] ^ mask[i % 4]);
                            WFLogging.Debug("Payload: " + Encoding.ASCII.GetString(bytes, 6, payload_size));
                        } else {
                            //for (int i = 0; i < payload_size; i++)
                            //   state.buffer[2+i] = (byte)(state.buffer[2+i] ^ 0x10);
                            WFLogging.Debug("Payload: " + Encoding.ASCII.GetString(bytes, 2, payload_size));
                        }
                    } else {
                        WFLogging.Debug("Extended size: " + payload_size.ToString());
                    }

                    switch (payload_type) {
                        case 0x01:  // text payload
                            Console.WriteLine("Got a text payload");
                            break;
                        case 0x02:  // binary payload
                            Console.WriteLine("Got a binary payload");
                            break;
                        case 0x0A:  // Pong
                            break;
                        case 0x09:  // Ping
                            // Send a pong message back
                            Console.WriteLine("Received ping frame, should send a pong");
                            break;
                        case 0x08:  // close connection
                            Console.WriteLine("Received close frame, closing connection");
                            clients.Remove(client);
                            client.Close();
                            return;
                    }
                } else {
                    WFLogging.Debug("Non Frame: " + Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                }
            }
        }

        //
        // Don't use WFLogging inside this function!
        private void WSLog(string log_message) {
            List<TcpClient> rm = new List<TcpClient>();

            int lines = WFLogging.EventLogCount;
            if (lines % 100 == 0)
                Console.WriteLine("*** Event Log is at " + lines.ToString() + " lines.");

            foreach (TcpClient c in clients) {
                try {
                    //Console.WriteLine("Sending to client " + c.Client.Handle.ToString());
                    SendMessage(c, log_message, 0x01);
                } catch {
                    rm.Add(c);
                }
            }

            //Console.WriteLine("Remove List has " + rm.Count.ToString() + " clients");
            if (rm.Count > 0) {
                foreach (TcpClient c in rm) {
                    //Console.WriteLine("Removing client " + c.Client.Handle.ToString() + " from the list");
                    c.Close();
                    clients.Remove(c);
                }
                rm.Clear();
            }
        }

        private static void SendMessage(TcpClient client, string message, int type) {
            byte[] payload = new byte[message.Length];
            byte[] frame = new byte[message.Length + 8];
            int framelen = 0;

            frame[0] = (byte)(0x80 | type); // Single frame and payload type
            if (message.Length > 126) {
                frame[1] = 0x00 | 126; // Not Masked
                frame[2] = (byte)((message.Length & 0xff00) >> 8);
                frame[3] = (byte)(message.Length & 0x00ff);
                framelen = 4;
            } else {
                frame[1] = (byte)(0x00 | message.Length);
                framelen = 2;
            }

            payload = Encoding.ASCII.GetBytes(message);
            for (int i = 0; i < message.Length; i++)
                frame[framelen + i] = (byte)(payload[i] ^ 0x00);

            // Send the frame
            client.Client.Send(frame, 0, message.Length + framelen, SocketFlags.None);

            if (type == 0x08) // Close connection message was sent
                CloseDone.Set();
        }
    }
}
