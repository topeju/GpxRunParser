using System;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace GpxRunParser
{
	public static class Settings
	{
		private static double[] _heartRateZones;
		private static TimeSpan[] _paceBins;
		private static TimeSpan? _slowestDisplayedPace;

		public static double[] HeartRateZones
		{
			get
			{
				if (_heartRateZones == null) {
					_heartRateZones = (from zone in ConfigurationManager.AppSettings["HeartRateZones"].Split(',')
									   select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
				}
				return _heartRateZones;
			}
		}

		public static TimeSpan[] PaceBins
		{
			get
			{
				if (_paceBins == null) {
					_paceBins = (from paceStr in ConfigurationManager.AppSettings["PaceBins"].Split(',')
								 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1]))).ToArray();
				}
				return _paceBins;
			}
		}

		public static TimeSpan SlowestDisplayedPace
		{
			get
			{
				if (_slowestDisplayedPace == null) {
					var slowestPace = ConfigurationManager.AppSettings["SlowestDisplayedPace"].Split(':');
					var slowH = int.Parse(slowestPace[0]);
					var slowM = int.Parse(slowestPace[1]);
					var slowS = double.Parse(slowestPace[2]);
					_slowestDisplayedPace = new TimeSpan(0, slowH, slowM, (int) slowS, (int) ((slowS - (int) slowS) * 1000.0D));
				}
				return _slowestDisplayedPace.Value;
			}
		}

		public static readonly int AveragingPeriod = int.Parse(ConfigurationManager.AppSettings["AveragingPeriod"]);
		public static readonly double MaximumDisplayedSlope = double.Parse(ConfigurationManager.AppSettings["MaximumDisplayedSlope"]);
		public static readonly string FilePattern = ConfigurationManager.AppSettings["FilePattern"];
	}
}
