
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
    partial class WeatherFlow_UDP {
        internal bool Active { get; set; }
        internal int Port { get; set; }
        private Thread udp_thread;
        private ManualResetEvent threadDone = new ManualResetEvent(false);

        internal WeatherFlow_UDP() {
            Port = 50222;
        }

        internal WeatherFlow_UDP(int p) {
            Port = p;
        }

        internal void Stop() {
            Active = false;

            // Wait for thread to stop
            threadDone.WaitOne();
        }

        internal void Start() {
            if (!Active) {
                WFLogging.Log("Starting WeatherFlow data collection thread.");
                udp_thread = new Thread(new ThreadStart(WeatherFlowThread));
                udp_thread.IsBackground = true;
                udp_thread.Start();
            }
        }

        internal void WeatherFlowThread() {
            Socket s;
            IPEndPoint groupEP;
            EndPoint remoteEP;
            string json;
            int len;

            using (s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                groupEP = new IPEndPoint(IPAddress.Any, Port);
                s.Bind(groupEP);
                remoteEP = (EndPoint)groupEP;
                Active = true;

                while (Active) {
                    if (s.Available == 0) {
                        Thread.Sleep(200);
                        continue;
                    }

                    try {
                        // Listen for UDP packets
                        //byte[] bytes = listener.Receive(ref groupEP);
                        byte[] bytes = new Byte[1500];
                        len = s.ReceiveFrom(bytes, ref remoteEP);
                        if (len > 0) {
                            json = Encoding.ASCII.GetString(bytes, 0, len);

                            if (json.Contains("obs_air")) {
                                //Console.WriteLine(json);
                                AirObservations(json);
                            } else if (json.Contains("obs_sky")) {
                                SkyObservations(json);
                            } else if (json.Contains("rapid_wind")) {
                                RapidWindEvt(json);
                            } else if (json.Contains("evt_strike")) {
                                LigtningStrikeEvt(json);
                            } else if (json.Contains("evt_precip")) {
                                PrecipitationEvt(json);
                            } else if (json.Contains("device_status")) {
                                DeviceStatus(json);
                            } else if (json.Contains("hub_status")) {
                                HubStatus(json);
                            } else {
                                // Unknown type of packet
                            }
                        }
                    } catch (Exception ex) {
                        WFLogging.Error("UDP Listener failed: " + ex.Message);
                    }
                }
                threadDone.Set();
            }
        }
    }
}
