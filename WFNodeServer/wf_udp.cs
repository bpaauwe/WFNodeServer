using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WFNodeServer {
    partial class WeatherFlow_UDP {

        int port = 50222;

        internal WeatherFlow_UDP() {
        }

        internal WeatherFlow_UDP(int p) {
            port = p;
        }

        internal void WeatherFlowThread() {
            Socket s;
            IPEndPoint groupEP;
            EndPoint remoteEP;
            string json;
            int len;

            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            groupEP = new IPEndPoint(IPAddress.Any, port);
            s.Bind(groupEP);
            remoteEP = (EndPoint)groupEP;

            while (true) {
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
                        } else if (json.Contains("evt_strike")) {
                            //LigtningStrikeEvt(json);
                        } else if (json.Contains("evt_precip")) {
                            //PrecipitationEvt(json);
                        } else if (json.Contains("device_status")) {
                            DeviceStatus(json);
                        } else if (json.Contains("hub_status")) {
                            HubStatus(json);
                        } else {
                            // Unknown type of packet
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine("UDP Listener failed: " + ex.Message);
                }
            }
            s.Close();
        }
    }
}
