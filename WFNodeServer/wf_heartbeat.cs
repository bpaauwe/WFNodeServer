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
using System.Timers;

namespace WFNodeServer {
    class Heartbeat {
        private Timer UpdateTimer = new System.Timers.Timer();
        private ElapsedEventHandler handler;
        private Dictionary<string, bool> HeartBeat = new Dictionary<string, bool>();
        private Dictionary<string, int> SecondsSinceUpdate = new Dictionary<string, int>();
        private object _locker = new object();
        private int interval = 0;

        internal void Start() {
            // Start a timer to track time since Last Update 
            if (!UpdateTimer.Enabled) {
                UpdateTimer.AutoReset = true;
                handler = new ElapsedEventHandler(UpdateTimer_Elapsed);
                UpdateTimer.Elapsed += handler;
                UpdateTimer.Interval = 30000;
                UpdateTimer.Start();
            }
        }

        internal void Stop() {
            if (UpdateTimer.Enabled) {
                UpdateTimer.Stop();
                UpdateTimer.Enabled = false;
                UpdateTimer.Elapsed -= handler;
            }
        }

        internal void Clear() {
            HeartBeat.Clear();
            SecondsSinceUpdate.Clear();
        }

        internal void Updated(string address) {
            lock (_locker) {
                if (!SecondsSinceUpdate.ContainsKey(address))
                    SecondsSinceUpdate.Add(address, 0);

                SecondsSinceUpdate[address] = 0;
            }
        }

        private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            string report;
            string prefix = "ns/" + WF_Config.Profile.ToString() + "/nodes/";

            if (++interval == 10) {
                WFLogging.Info("ISY Request Rate: " + WeatherFlowNS.NS.Rest.stats.Rate.ToString() + " requests/minute");
                interval = 0;
            }

            foreach (string address in WeatherFlowNS.NS.NodeList.Keys) {
                if (WeatherFlowNS.NS.NodeList[address] == "WF_Hub") {
                    if (!HeartBeat.ContainsKey(address))
                        HeartBeat.Add(address, true);

                    if (HeartBeat[address]) {
                        report = prefix + address + "/report/status/GV0/1/0";
                    } else {
                        report = prefix + address + "/report/status/GV0/-1/0";
                    }
                    HeartBeat[address] = !HeartBeat[address];
                    WeatherFlowNS.NS.Rest.REST(report);

                    // CHECKME: Should we have a last update value for the hub?
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_AirD") {
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_SkyD") {
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_Lightning") {
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_RapidWind") {
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_LightningSI") {
                } else if (WeatherFlowNS.NS.NodeList[address] == "WF_RapidWindSI") {
                } else {
                    // this should only sky & air nodes
                    lock (_locker) {
                        if (!SecondsSinceUpdate.ContainsKey(address))
                            SecondsSinceUpdate.Add(address, 0);

                        report = prefix + address + "/report/status/GV0/" + SecondsSinceUpdate[address].ToString() + "/58";
                        WeatherFlowNS.NS.Rest.REST(report);
                        SecondsSinceUpdate[address] += 30;
                    }
                }
            }
        }
    }
}
