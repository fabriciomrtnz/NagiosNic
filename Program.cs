using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Nagios
{
	enum SerivceOutputCode {
		OK = 0,
		WARNING = 1,
		CRITICAL = 2,
		UNKNOWN = 3
	}

	struct Commands 
	{
		public string Interface;

		public long? BytesInWarning;
		public long? BytesInCritical;
		public long? BytesOutWarning;
		public long? BytesOutCritical;

		public int? ErrorsInWarning;
		public int? ErrorsInCritical;
		public int? ErrorsOutWarning;
		public int? ErrorsOutCritical;

		public int? DiscardedPacketsInWarning;
		public int? DiscardedPacketsInCritical;
		public int? DiscardedPacketsOutWarning;
		public int? DiscardedPacketsOutCritical;

		public bool ShowStatsAllNics;
		public string StatNic;
		public bool ShowHelp;
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			Commands inCmd;
			NetworkInterface nic = null;

			try 
			{
				inCmd = GetCommands(args);
				if (inCmd.ShowHelp)
				{
					ShowCommands();
					Environment.Exit((int)SerivceOutputCode.UNKNOWN);
				}
				else if(inCmd.ShowStatsAllNics)
				{
					ShowAllStatus();
					Environment.Exit((int)SerivceOutputCode.OK);
				}
				else if(!string.IsNullOrEmpty(inCmd.StatNic))
				{
					NetworkInterface.GetAllNetworkInterfaces().ToList().ForEach(x => 
					{
						if (x.Name.ToLower() == inCmd.StatNic.ToLower()) nic = x;
					});
					ShowNicStatus(nic);
					Environment.Exit((int)SerivceOutputCode.OK);
				}
				else if (!string.IsNullOrEmpty(inCmd.Interface))
				{
					NetworkInterface.GetAllNetworkInterfaces().ToList().ForEach(x => 
					{
						if (x.Name.ToLower() == inCmd.Interface.ToLower()) nic = x;
					});

					if (nic == null)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Not Found", SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}

					//1. Check nic up/down
					if (nic.OperationalStatus != OperationalStatus.Up)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} is Down {2}", SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}

					//2. Check Errors levels
					if (inCmd.ErrorsInCritical.HasValue && inCmd.ErrorsInCritical <= nic.GetIPv4Statistics().IncomingPacketsWithErrors)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Recieved {2} Incoming Packets With Errors {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().IncomingPacketsWithErrors,
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}
					if (inCmd.ErrorsOutCritical.HasValue  && inCmd.ErrorsOutCritical <= nic.GetIPv4Statistics().OutgoingPacketsWithErrors)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Sent {2} Outgoing Packets With Errors {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().OutgoingPacketsWithErrors,
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}
					if (inCmd.DiscardedPacketsInCritical.HasValue  && inCmd.DiscardedPacketsInCritical <= nic.GetIPv4Statistics().IncomingPacketsDiscarded)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Discarded {2} Incoming Packets Discarded {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().IncomingPacketsDiscarded, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}
					if (inCmd.DiscardedPacketsOutCritical.HasValue  && inCmd.DiscardedPacketsOutCritical <= nic.GetIPv4Statistics().OutgoingPacketsDiscarded)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Discarded {2} Outgoing Packets Discarded {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().OutgoingPacketsDiscarded, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}
					if (inCmd.BytesInCritical.HasValue  && inCmd.BytesInCritical <= nic.GetIPv4Statistics().BytesReceived)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Recieved {2} Bytes {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().BytesReceived, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}
					if (inCmd.BytesOutCritical.HasValue  && inCmd.BytesOutCritical <= nic.GetIPv4Statistics().BytesSent)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Sent {2} Bytes {3}", 
							SerivceOutputCode.CRITICAL.ToString(), inCmd.Interface, nic.GetIPv4Statistics().BytesReceived, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.CRITICAL);
					}

					//3. Check Warning Levels
					if (inCmd.ErrorsInCritical.HasValue  && inCmd.ErrorsInCritical <= nic.GetIPv4Statistics().IncomingPacketsWithErrors)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Recieved {2} Incoming Packets With Errors {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().IncomingPacketsWithErrors, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
					if (inCmd.ErrorsOutCritical.HasValue  && inCmd.ErrorsOutCritical <= nic.GetIPv4Statistics().OutgoingPacketsWithErrors)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Sent {2} Outgoing Packets With Errors {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().OutgoingPacketsWithErrors, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
					if (inCmd.DiscardedPacketsInCritical.HasValue  && inCmd.DiscardedPacketsInCritical <= nic.GetIPv4Statistics().IncomingPacketsDiscarded)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Discarded {2} Incoming Packets Discarded {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().IncomingPacketsDiscarded, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
					if (inCmd.DiscardedPacketsOutCritical.HasValue  && inCmd.DiscardedPacketsOutCritical <= nic.GetIPv4Statistics().OutgoingPacketsDiscarded)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Discarded {2} Outgoing Packets Discarded {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().OutgoingPacketsDiscarded, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
					if (inCmd.BytesInWarning.HasValue  && inCmd.BytesInWarning <= nic.GetIPv4Statistics().BytesReceived)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Recieved {2} Bytes {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().BytesReceived, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
					if (inCmd.BytesOutCritical.HasValue  && inCmd.BytesOutCritical <= nic.GetIPv4Statistics().BytesSent)
					{
						Console.WriteLine(String.Format("{0} - Nic {1} Sent {2} Bytes {3}", 
							SerivceOutputCode.WARNING.ToString(), inCmd.Interface, nic.GetIPv4Statistics().BytesReceived, 
							GetStats(nic.GetIPv4Statistics())));
						Environment.Exit((int)SerivceOutputCode.WARNING);
					}
				}

				Console.WriteLine("OK " + GetStats(nic.GetIPv4Statistics()));
				Environment.Exit((int)SerivceOutputCode.OK);

			} catch (Exception ex) {
				ShowCommands ();
				Environment.Exit((int)SerivceOutputCode.UNKNOWN);
			}

		}

		private static void ShowCommands()
		{
			Console.WriteLine(".::%%% NIC Stat Nagios Plugin %%%::.");
			Console.WriteLine("-i - select interface. Ex -i ppp0");

			Console.WriteLine("-wbi - warning bytes in. Ex -wbi 10000");
			Console.WriteLine("-wbo - warning bytes out. Ex -wbo 10000");
			Console.WriteLine("-cbi - critical bytes in. Ex -cbi 10000");
			Console.WriteLine("-cbo - critical bytes out. Ex -cbi 10000");

			Console.WriteLine("-wei - warning errors in. Ex -wei 100");
			Console.WriteLine("-weo - warning errors out. Ex -weo 100");
			Console.WriteLine("-cei - critical errors in. Ex -cei 100");
			Console.WriteLine("-ceo - critical errors out. Ex -ceo 100");

			Console.WriteLine("-wdi - warning discarded paquets in. Ex -wdi 100");
			Console.WriteLine("-wdo - warning discarded paquets out. Ex -wdo 100");
			Console.WriteLine("-cdi - critical discarded paquets in. Ex -cdi 100");
			Console.WriteLine("-cdo - critical discarded paquets out. Ex -cdo 100");

			Console.WriteLine("-stat all - show information on all interfaces");
			Console.WriteLine("-stat interface - show interface information");
			Console.WriteLine("-h - help");
		}

		private static void ShowAllStatus()
		{
			NetworkInterface.GetAllNetworkInterfaces ().ToList ().ForEach (x => 
				{
					IPv4InterfaceStatistics stats = x.GetIPv4Statistics();
					Console.WriteLine(String.Format("nic {0} || bytes IN {1} || bytes OUT {2} || errors IN {3} || errores OUT {4} || discarded IN {5} || discarded OUT {6}",
						x.Name, stats.BytesReceived, stats.BytesSent, stats.IncomingPacketsWithErrors, stats.OutgoingPacketsWithErrors,
						stats.IncomingPacketsDiscarded, stats.OutgoingPacketsDiscarded));
				});
		}

		private static void ShowNicStatus(NetworkInterface nic)
		{
			if (nic != null) {
				IPv4InterfaceStatistics stats = nic.GetIPv4Statistics ();
				Console.WriteLine (String.Format ("nic {0} || bytes IN {1} || bytes OUT {2} || errors IN {3} || errores OUT {4} || discarded IN {5} || discarded OUT {6}",
					nic.Name, stats.BytesReceived, stats.BytesSent, stats.IncomingPacketsWithErrors, stats.OutgoingPacketsWithErrors,
					stats.IncomingPacketsDiscarded, stats.OutgoingPacketsDiscarded));
			}
		}

		private static string GetStats(IPv4InterfaceStatistics stats)
		{
			return String.Format ("|BytesIn={0}B BytesOut={1}B ErrorsIn={2} ErrorsOut={3} PacketDropedIn={4} PacketDropedOut={5}", 
				stats.BytesReceived, stats.BytesSent, stats.IncomingPacketsWithErrors, stats.OutgoingPacketsWithErrors, 
				stats.IncomingPacketsDiscarded, stats.OutgoingPacketsDiscarded);
		}

		private static Commands GetCommands(string[] args)
		{
			Commands cmd = new Commands();

			for (int i = 0; i < args.Length; i++) {
				switch (args [i].ToLower ()) {
				case "-i":
					cmd.Interface = args [i + 1].ToLower ();
					break;
				case "-wbi":
					cmd.BytesInWarning = long.Parse (args [i + 1]);
					break;
				case "-wbo":
					cmd.BytesOutWarning = long.Parse (args [i + 1]);
					break;
				case "-cbi":
					cmd.BytesInCritical = long.Parse (args [i + 1]);
					break;
				case "-cbo":
					cmd.BytesOutCritical = long.Parse (args [i + 1]);
					break;
				case "-wei":
					cmd.ErrorsInWarning = int.Parse (args [i + 1]);
					break;
				case "-weo":
					cmd.ErrorsOutWarning = int.Parse (args [i + 1]);
					break;
				case "-cei":
					cmd.ErrorsInCritical = int.Parse (args [i + 1]);
					break;
				case "-ceo":
					cmd.ErrorsOutCritical = int.Parse (args [i + 1]);
					break;
				case "-wdi":
					cmd.DiscardedPacketsInWarning = int.Parse (args [i + 1]);
					break;
				case "-wdo":
					cmd.DiscardedPacketsOutWarning = int.Parse (args [i + 1]);
					break;
				case "-cdi":
					cmd.DiscardedPacketsInCritical = int.Parse (args [i + 1]);
					break;
				case "-cdo":
					cmd.DiscardedPacketsOutCritical = int.Parse (args [i + 1]);
					break;
				case "-stat":
					if (args [i + 1].ToLower () == "all")
						cmd.ShowStatsAllNics = true;
					else
						cmd.StatNic = args [i + 1].ToLower (); 
					break;
				case "-h":
					cmd.ShowHelp = true;
					break;
				}
			}

			return cmd;
		}
	}
}

