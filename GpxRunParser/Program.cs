using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NDesk.Options;

namespace GpxRunParser
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			var fileName = "";
			var zoneStr = "120,140,173,182";
			var paceBinStr = "8:45,7:15,6:10";
			var help = false;
			var opts = new OptionSet {
				{ "f|file=", "GPX file to parse", s => fileName = s },
				{ "z|zones=", "Heart rate zone limits, comma-separated", s => zoneStr = s },
				{ "p|paces=", "Pace bin limits, comma-separated mm:ss", s => paceBinStr = s },
				{ "h|?|help", "Show this help text", s => help = s != null }
			};
			try {
				opts.Parse(args);
			}
			catch (OptionException e) {
				Console.Error.Write("GpxRunParser: ");
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine("Try GpxRunParser --help for more information");
				return;
			}
			if (help) {
				Console.Out.WriteLine("Usage: GpxRunParser [OPTIONS]+");
				Console.Out.WriteLine("Parses the GPX file specified using the -f option to calculate statistics on the file.");
				opts.WriteOptionDescriptions(Console.Out);
				return;
			}
			if (fileName == "") {
				Console.Error.WriteLine("Input GPX file not specified");
				return;
			}
			var zones = (from zone in zoneStr.Split(',') select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			var paces =
				(from paceStr in paceBinStr.Split(',')
				 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1]))).ToArray();

			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxDoc = XDocument.Load(fileName);

			var zoneBins = new TimeBin<double>(zones);
			var paceBins = new TimeBin<TimeSpan>(paces);
			var totalTime = TimeSpan.Zero;
			var totalDistance = 0.0D;
			var heartTotal = 0.0D;
			var maxHeartRate = 0.0D;
			var totalSteps = 0.0D;

			foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
				foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
					var points = from point in segment.Descendants(gpxNamespace + "trkpt")
								 select new GpxTrackPoint(point);
					var iterator = points.GetEnumerator();
					if (iterator.MoveNext()) {
						var p0 = iterator.Current;
						if (p0.HeartRate > maxHeartRate) {
							maxHeartRate = p0.HeartRate;
						}
						while (iterator.MoveNext()) {
							var pt = iterator.Current;
							var dist = p0.DistanceTo(pt);
							totalDistance += dist;
							var deltaT = p0.TimeDifference(pt);
							zoneBins.Record(p0.HeartRate, deltaT);
							totalTime += deltaT;
							var pace = new TimeSpan((long)(1000.0D * deltaT.Ticks / dist));
							paceBins.Record(pace, deltaT);
							heartTotal += p0.HeartRate * deltaT.TotalMinutes;
							if (pt.HeartRate > maxHeartRate) {
								maxHeartRate = pt.HeartRate;
							}
							totalSteps += 2.0D * p0.Cadence * deltaT.TotalMinutes; // Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute?
							p0 = pt;
						}
					}
				}
			}

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);
			var outputFileName = extRegexp.Replace(fileName, ".txt");

			using (var output = File.CreateText(outputFileName)) {
				output.WriteLine("{0,-20}  {1:N2} km", "Total distance:", totalDistance/1000.0D);
				output.WriteLine("{0,-20}  {1:g}", "Total time:", totalTime);
				output.WriteLine("{0,-20}  {1:g}/km", "Average pace:", new TimeSpan((long)(1000.0D * totalTime.Ticks / totalDistance)));
				output.WriteLine("{0,-20}  {1:N2} km/h", "Average speed:", totalDistance / (1000.0D * totalTime.TotalHours));
				output.WriteLine("{0,-20}  {1:N0} beats/minute", "Average heart rate:", heartTotal / totalTime.TotalMinutes);
				output.WriteLine("{0,-20}  {1:N0} beats/minute", "Max heart rate:", maxHeartRate);
				output.WriteLine("{0,-20}  {1:N0}", "Total steps:", totalSteps);
				output.WriteLine("{0,-20}  {1:N0}", "Average cadence:", totalSteps / (2.0D * totalTime.TotalMinutes));
				output.WriteLine("{0,-20}  {1:N0} cm", "Avg stride length:", totalDistance * 100.0D / totalSteps);
				output.WriteLine();

				output.WriteLine("Time spent in each heart rate zone:");
				zoneBins.Output(output, totalTime);

				output.WriteLine();
				output.WriteLine("Time spent in each pace range:");
				paceBins.Output(output, totalTime);
			}

		}
	}
}
