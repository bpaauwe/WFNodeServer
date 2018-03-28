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
using System.Web;
using System.Web.Script.Serialization;

namespace WFNodeServer {
    class wf_station {
        private rest WFRest;
        private string api_key;
        internal double Latitude { get; set; }
        internal double Longitude { get; set; }
        internal double Elevation { get; set; }
        internal int Air { get; set; }
        internal int Sky { get; set; }
        internal string AirSN { get; set; }
        internal string SkySN { get; set; }

        public class StationMeta {
            public bool share_with_wf { get; set; }
            public bool share_with_wu { get; set; }
            public double elevation { get; set; }
        }

        public class DeviceMeta {
            public double agl { get; set; }
            public string name { get; set; }
            public string environment { get; set; }
            public string wifi_network_name { get; set; }
        }

        public class Device {
            public int device_id { get; set; }
            public string serial_number { get; set; }
            public DeviceMeta device_meta { get; set; }
            public string device_type { get; set; }
            public string hardware_revision { get; set; }
            public string firmware_revision { get; set; }
            public string notes { get; set; }
        }

        public class StationItem {
            public int location_item_id { get; set; }
            public int location_id { get; set; }
            public int device_id { get; set; }
            public string item { get; set; }
            public int sort { get; set; }
            public int station_id { get; set; }
            public int station_item_id { get; set; }
        }

        public class Station {
            public int location_id { get; set; }
            public string name { get; set; }
            public string public_name { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public string timezone { get; set; }
            public StationMeta station_meta { get; set; }
            public int last_modified_epoch { get; set; }
            public List<Device> devices { get; set; }
            public List<StationItem> station_items { get; set; }
            public bool is_local_mode { get; set; }
            public int station_id { get; set; }
        }

        public class Status {
            public int status_code { get; set; }
            public string status_message { get; set; }
        }

        public class RootObject {
            public List<Station> stations { get; set; }
            public Status status { get; set; }
        }

        internal wf_station(string key) {
            api_key = key;
            WFRest = new rest();
            WFRest.Base = "http://swd.weatherflow.com";
            WFRest.AuthRequired = false;
            Latitude = 0;
            Longitude = 0;
            Elevation = 0;
            Air = 0;
            Sky = 0;
            AirSN = "";
            SkySN = "";
        }


        internal bool GetStationMeta(string station_id) {
            string resp;
            RootObject root;
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            resp = WFRest.REST("/swd/rest/stations/" + station_id + "?api_key=6c8c96f9-e561-43dd-b173-5198d8797e0a");

            if (resp == "")
                return false;
            if (resp.Contains("ERROR"))
                return false;

            if (resp != "") {
                try {
                    root = serializer.Deserialize<RootObject>(resp);

                    if (root.stations.Count == 0)
                        return false;

                    Latitude = root.stations[0].latitude;
                    Longitude = root.stations[0].longitude;

                    //Console.WriteLine("latitude:  " + root.stations[0].latitude.ToString());
                    //Console.WriteLine("longitude: " + root.stations[0].longitude.ToString());
                    //Console.WriteLine("elevation: " + root.stations[0].station_meta.elevation.ToString());
                    foreach (Device device in root.stations[0].devices) {
                        // Types that we're interested in are:
                        //   SK - sky
                        //   AR - air
                        //Console.WriteLine("Sensor: " + device.device_id.ToString() + " is " + device.device_type + " -agl = " + device.device_meta.agl.ToString());
                        if (device.device_type == "AR") {
                            Elevation = root.stations[0].station_meta.elevation + device.device_meta.agl;
                            Air = device.device_id;
                            AirSN = device.serial_number;
                        } else if (device.device_type == "SK") {
                            Sky = device.device_id;
                            SkySN = device.serial_number;
                        }
                    }

                } catch (Exception ex) {
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
            return true;
        }

        internal static StationInfo FindStationAir(string serial) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.air_sn == serial)
                    return s;
            }

            return new StationInfo();
        }
        internal static StationInfo FindStationSky(string serial) {
            foreach (StationInfo s in WF_Config.WFStationInfo) {
                if (s.sky_sn == serial)
                    return s;
            }

            return new StationInfo();
        }
    }
}
