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

namespace WFNodeServer {
    internal partial class NodeServer {
        // API Key used to access station data via Weather Flow's websocket
        // interface. Change this key to a WeatherFlow provided key before
        // deploying.
        private static string api_key = "20c70eae-e62f-4d3b-b3a4-8586e90f3ac8";
    }
}
