using System;
using System.IO;

namespace BrocktonBay {

	public enum OS {
		Windows,
		Linux,
		Mac
	}

	public static class PlatformDetection {

		public readonly static OS os;

		static PlatformDetection () {
			switch (Environment.OSVersion.Platform) {
				case PlatformID.Unix:
					// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
					// Instead of platform check, we'll do a feature checks (Mac specific root folders)
					if (Directory.Exists("/Applications")
						& Directory.Exists("/System")
						& Directory.Exists("/Users")
						& Directory.Exists("/Volumes"))
						os = OS.Mac;
					else
						os = OS.Linux;
					break;
				case PlatformID.MacOSX:
					os = OS.Mac;
					break;
				default:
					os = OS.Windows;
					break;
			}
		}

	}
}
