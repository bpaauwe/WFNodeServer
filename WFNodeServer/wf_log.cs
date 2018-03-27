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
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

namespace WFNodeServer {

    public enum LOG_LEVELS {
        UPDATE = 0x00,
        ERROR,
        WARNING,
        INFO,
        DEBUG
    };

    internal static class WFLogging {
		public delegate void EventClient(string msg);
		public delegate void EventClientEx(LOG_LEVELS level, string msg);
        public delegate void EventClientUn(string timestamp, UInt32 level, string msg);
		public static ArrayList Events = new ArrayList();

		private static readonly object logger_lock = new object();
		private static Delegate clients = null;
		private static Delegate listeners = null;
        private static bool pause;
        private static int index;

        public static LOG_LEVELS Level = LOG_LEVELS.ERROR;
        public static bool Enabled { get; set; }
        public static bool TimeStamps { get; set; }

		private class logEntry {
			internal DateTime TimeStamp { get; set; }
			internal string Message { get; set; }
            internal string Function { get; set; }
		}

		public static bool Pause {
			set {
				pause = value;
				if (pause) {
					index = Events.Count;
				} else {
                    // Send out any log messages created while paused
					logEntry entry;
					for (int i = index; i < Events.Count; i++) {
						entry = (logEntry)Events[i];
						if (clients != null) {
							clients.DynamicInvoke(0, entry.Message);
						}
					}
				}
			}
            get { return pause; }
        }

		// Add a client
		//  A client will get both the message and a severity
		//  level. The client is responsible for calling the
		//  correct logging function based on the severity.
		public static void AddClient(EventClientEx client) {
			clients = MulticastDelegate.Combine(clients, client);
		}

		// Remove a client
        public static void RemoveClient(EventClientEx client) {
            clients = MulticastDelegate.Remove(clients, client);
        }

		public static void AddListener(EventClient client) {
			listeners = MulticastDelegate.Combine(listeners, client);
		}

		// Remove a listener
        public static void RemoveListener(EventClient client) {
			listeners = MulticastDelegate.Remove(listeners, client);
		}

        // General log message at INFO level
        public static void Log(string msg) {
			WriteEntry(LOG_LEVELS.UPDATE, msg, false);
        }
        // General log message at specified level
        public static void Log(LOG_LEVELS control, string msg) {
            WriteEntry(control, msg, false);
        }
        public static void Debug(string msg) {
            WriteEntry(LOG_LEVELS.DEBUG, msg, false);
        }
        public static void Info(string msg) {
            WriteEntry(LOG_LEVELS.INFO, msg, false);
        }
        public static void Error(string msg) {
            WriteEntry(LOG_LEVELS.ERROR, msg, false);
        }
        public static void Warning(string msg) {
            WriteEntry(LOG_LEVELS.WARNING, msg, false);
        }

		//  Clear all the events from the internal log
        public static void Clear() {
			lock (logger_lock) {
				Events.Clear();
			}
        }

        public new static string ToString() {
            int i;
            string s = "";
            logEntry e;
			if (Events.Count > 0) {
				for (i = 0; (i <= (Events.Count - 1)); i++) {
					e = (logEntry)Events[i];
					s = (s
								+ (e.TimeStamp.ToString() + ('\t' + (" "
								+ (e.Message + "\r\n")))));
				}
				return s;
			} else {
				return "";
			}
        }

        public static int EventLogCount {
            get {
                return Events.Count;
            }
        }

        public static string[] GetEvent(int index) {
            string[] events = new string[3];
            logEntry e;

            try {
                e = (logEntry)Events[index];
                events[0] = e.TimeStamp.ToString();
                events[1] = e.Message;
                events[2] = e.Function;
            } catch {
                // Invalid index?
            }

            return events;
        }

		//
        //  Save the interal log to a file
		//
        public static void Save(string fname) {
            FileStream fs;
            StreamWriter writer;
            logEntry e;
            int i;
            try {
				lock (logger_lock) {
					fs = new FileStream(fname, FileMode.Create);
					writer = new StreamWriter(fs);
					for (i = 0; (i
								<= (Events.Count - 1)); i++) {
						e = (logEntry)Events[i];
						writer.WriteLine((e.TimeStamp.ToString() + ('\t' + (" " + e.Message))));
					}
					writer.Close();
					fs.Close();
				}
            } catch (Exception ex){
            }
        }


        private static void WriteEntry(LOG_LEVELS level, string msg, bool millisec) {
            string outstr;

            lock (logger_lock) {
                if (level <= Level) {
                    outstr = "";
                    if (TimeStamps) {
                        if (millisec) {
                            outstr = DateTime.Now.ToShortDateString() + " " + DateTime.Now.TimeOfDay.ToString() + "\t";
                        } else {
                            outstr = DateTime.Now.ToString() + "\t ";
                        }
                    }
                    outstr += msg;

                    if (!pause) {
                        if (listeners != null) {
                            listeners.DynamicInvoke(outstr);
                        }
                        if (clients != null) {
                            clients.DynamicInvoke(level, outstr);
                        }
                    }

                    if (Enabled) {
                        logEntry entry = new logEntry();

                        entry.TimeStamp = DateTime.Now;
                        entry.Message = msg;
                        Events.Add(entry);

                        // Only 100000 events to be saved
                        if (Events.Count > 100000) {
                            Events.RemoveAt(0);
                            if (index > 0) {
                                index--;
                            }
                        }
                    }
                }
            }
        }

        private static void _log(LOG_LEVELS level, string function, string message) {
            string out_msg = "";

            if (TimeStamps) {
                out_msg = DateTime.Now.ToString() + "\t ";
            }

            out_msg += message;

            if (listeners != null) {
                listeners.DynamicInvoke(out_msg);
            }

            if (clients != null) {
                clients.DynamicInvoke(level, out_msg);
            }

            if (Enabled) {
                logEntry entry = new logEntry();

                entry.TimeStamp = DateTime.Now;
                entry.Message = message;
                entry.Function = function;
                Events.Add(entry);

                // Only 100000 events to be saved
                if (Events.Count > 100000) {
                    Events.RemoveAt(0);
                    if (index > 0) {
                        index--;
                    }
                }
            }
        }
    }
}
