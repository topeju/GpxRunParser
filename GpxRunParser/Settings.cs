using System;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace GpxRunParser
{
	public static class Settings
	{
		public static readonly double[] HeartRateZones;
		public static readonly TimeSpan[] PaceBins;
		public static readonly TimeSpan SlowestDisplayedPace;
		public static readonly int AveragingPeriod = int.Parse(ConfigurationManager.AppSettings["AveragingPeriod"]);
		public static readonly double MaximumDisplayedSlope = double.Parse(ConfigurationManager.AppSettings["MaximumDisplayedSlope"]);
		public static readonly string FilePattern = ConfigurationManager.AppSettings["FilePattern"];
		public static readonly string RunStatsCacheFile = ConfigurationManager.AppSettings["RunStatsCacheFile"];
		public static readonly TimeSpan AggregateSamplingPeriod;

		static Settings()
		{
			HeartRateZones = (from zone in ConfigurationManager.AppSettings["HeartRateZones"].Split(',')
							  select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			PaceBins = (from paceStr in ConfigurationManager.AppSettings["PaceBins"].Split(',')
						select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1])))
				.OrderBy(ts => ts).ToArray();
			var slowestPace = ConfigurationManager.AppSettings["SlowestDisplayedPace"].Split(':');
			var slowH = int.Parse(slowestPace[0]);
			var slowM = int.Parse(slowestPace[1]);
			var slowS = double.Parse(slowestPace[2]);
			SlowestDisplayedPace = new TimeSpan(0, slowH, slowM, (int) slowS, (int) ((slowS - (int) slowS) * 1000.0D));
			var samplingPeriod = ConfigurationManager.AppSettings["AggregateSamplingPeriod"].Split(':');
			var samplH = int.Parse(samplingPeriod[0]);
			var samplM = int.Parse(samplingPeriod[1]);
			var samplS = double.Parse(samplingPeriod[2]);
			AggregateSamplingPeriod = new TimeSpan(0, samplH, samplM, (int) samplS, (int) ((samplS - (int) samplS) * 1000.0D));
		}
	}
}
