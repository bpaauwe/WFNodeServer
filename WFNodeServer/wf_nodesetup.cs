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
using System.IO;
using System.Reflection;

namespace WFNodeServer {
    class wf_nodesetup {
        internal static void UploadFile(rest Rest, int profile, string resname, string type) {
            Assembly assembly;
            string resourceName;
            string contents = "";

            // Load resource into string
            assembly = Assembly.GetExecutingAssembly();
            resourceName = "WFNodeServer.nodesetup." + resname;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    contents = reader.ReadToEnd();
                }
             }

            if (contents.Length > 0) {
                // Send to ISY
                // This is just a http post to /rest/ns/profile/<profile>/upload/<type>/<filename>
                Rest.AuthRequired = true;
                try {
                    Rest.REST_POST("ns/profile/" + profile.ToString() + "/upload/" + type + "/" + resname, contents, contents.Length);
                } catch (Exception ex) {
                    WFLogging.Error(resname + " upload failed: " + ex.Message);
                }
            }
        }
    }
}
