
//
// WFNodeServer - ISY Node Server for WeatherFlow weather station data
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
using System.Net.NetworkInformation;
using System.Threading;

namespace WFNodeServer {
    static class ISYDetect {
        internal static string ISYAddress = "";

        // If I'm understanding this right.  To find an ISY on the network do:
        // broadcast a packet to the broadcast address on port 20034.
        // The ISY should respond with a packet that includes the IP address
        // and optionally, the port.
        //
        // The packet we need to send look like:
        // 42 55 52 4e 52 00 00 00 7c e8 a5 a9 36 7a xx xx 00 00 00 00 00 00 00 00
        //                      ^^  this data seems random
        private class UdpState {
            internal UdpClient client { get; set; }
            internal IPEndPoint ep { get; set; }
        }

        internal static void callback(IAsyncResult result) {
            UdpClient u = (UdpClient)((UdpState)(result.AsyncState)).client;
            IPEndPoint e = (IPEndPoint)((UdpState)(result.AsyncState)).ep;

            Byte[] receiveBytes = u.EndReceive(result, ref e);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);
            //Console.WriteLine("Received: {0}", receiveString);

            // Parse IP address / port from recieved text
            int i1 = receiveString.IndexOf("//");
            int i2 = receiveString.IndexOf("/desc");
            if (i1 > 0) {
                i1 += 2;
                ISYAddress = receiveString.Substring(i1, (i2 - i1));
            }
            Console.WriteLine("Found ISY at " + ISYAddress);
        }

        private static string GetBroadcast() {
            string bcast_addr = "";

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
                // Look for type == "Ethernet" && status == "Up"
                if (ni.NetworkInterfaceType.ToString() != "Ethernet")
                    continue;
                if (ni.OperationalStatus.ToString() != "Up")
                    continue;

                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses) {
                    if (ip.IPv4Mask.ToString() == "0.0.0.0")
                        continue;
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var addrInt = BitConverter.ToInt32(ip.Address.GetAddressBytes(), 0);
                    var maskInt = BitConverter.ToInt32(ip.IPv4Mask.GetAddressBytes(), 0);
                    var bcastInt = addrInt | ~maskInt;
                    IPAddress broadcast = new IPAddress(BitConverter.GetBytes(bcastInt));
                    //Console.WriteLine("Broadcast Address: " + broadcast.ToString());
                    bcast_addr = broadcast.ToString();
                }
            }

            return bcast_addr;
        }

        internal static string FindISY() {
            UdpClient listen_udp;
            UdpState state;
            IPEndPoint group_ep;

            byte[] handshake = new byte[24]
                        { 0x42, 0x55, 0x52, 0x4e, 0x52, 0x00, 0x00, 0x00,
                          0x7c, 0xe8, 0xa5, 0xa9, 0x36, 0x7a, 0x12, 0x34,
                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            using (listen_udp = new UdpClient()) {
                listen_udp.ExclusiveAddressUse = false;
                listen_udp.EnableBroadcast = true;
                listen_udp.Client.SetSocketOption(SocketOptionLevel.Socket,
                        SocketOptionName.ReuseAddress, true);


                group_ep = new IPEndPoint(IPAddress.Any, 20034);

                state = new UdpState();
                state.client = listen_udp;
                state.ep = group_ep;

                try {
                    listen_udp.Send(handshake, 24, GetBroadcast(), 20034);
                    Thread.Sleep(50);

                    // Wait for 1/2 second for the ISY to respond
                    listen_udp.BeginReceive(new AsyncCallback(callback), state);
                    Thread.Sleep(500);

                    //listen_udp.Close();
                } catch {
                    //listen_udp.Close();
                }
            }

            return ISYAddress;
        }




		//  Look for UPNP broadcast messages from an ISY. If it finds
		//  one, then use it.
		//
		//  FIXME: How do we know this is the right ISY?  What if there
		//  is more than one on the network?
		internal static string IsyAutoDetect() {
			UdpClient listen_udp;
			IPAddress group_ip;
			IPEndPoint group_ep;
			string ip = "";
			byte[] recv_data;
			int i;
			string buf = "";
			int tries = 100;

            using (listen_udp = new UdpClient()) {
                listen_udp.ExclusiveAddressUse = false;
                listen_udp.Client.SetSocketOption(SocketOptionLevel.Socket,
                        SocketOptionName.ReuseAddress, true);
                group_ip = IPAddress.Parse("239.255.255.250");
                //group_ep = new IPEndPoint(IPAddress.Any, 1900);
                group_ep = new IPEndPoint(IPAddress.Any, 20034);

                try {
                    listen_udp.Client.Bind(group_ep);
                } catch (Exception e) {
                    Console.WriteLine("Failed to bind to broadcast address");
                    Console.WriteLine(e.Message);
                    return "";
                }

                listen_udp.EnableBroadcast = true;

                try {
                    listen_udp.JoinMulticastGroup(group_ip);
                } catch (Exception e) {
                    Console.WriteLine("Failed to join Multicast group: " + e.Message);
                    //listen_udp.Close();
                    return "";
                }

                // Set the timeout at 90 seconds.  If we haven't received anything in
                // that time, we probably won't.
                listen_udp.Client.ReceiveTimeout = 90000;

                while ((ip == "")) {
                    try {
                        recv_data = listen_udp.Receive(ref group_ep);
                    } catch {
                        Console.WriteLine("Timed out trying to discover ISY.");
                        return "";
                    }
                    if (recv_data.Length != 0) {
                        // Found somelthing
                        buf = Encoding.ASCII.GetString(recv_data);

                        // Now see if this is really an ISY
                        if (buf.Contains("X_Insteon") == false) {
                            if (--tries == 0) {
                                Console.WriteLine("Failed to detect ISY on the network.");
                                return "";
                            }
                        } else {
                            // This really is an ISY.  Pull the location field
                            // from the string.
                            i = buf.IndexOf("LOCATION:");
                            if ((i > 0)) {
                                ip = buf.Substring((i + 9)).Split('\r')[0];
                            }
                        }
                    }
                }
                listen_udp.DropMulticastGroup(group_ip);
                //listen_udp.Close();
            }

			Console.WriteLine(("Found an ISY: " + ip));
			return ip;
		}
    }
}
