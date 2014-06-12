using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NDesk.Options;
using RazorEngine;

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
			} catch (OptionException e) {
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
			var zones = (from zone in zoneStr.Split(',')
			             select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			var paces =
				(from paceStr in paceBinStr.Split(',')
				 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1]))).ToArray();

			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxDoc = XDocument.Load(fileName);

			var runStats = new RunStats(zones, paces);

			foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
				foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
					var points = from point in segment.Descendants(gpxNamespace + "trkpt")
					             select new GpxTrackPoint(point);
					var iterator = points.GetEnumerator();
					if (iterator.MoveNext()) {
						var p0 = iterator.Current;
						runStats.UpdateMaxHeartRate(p0.HeartRate);
						while (iterator.MoveNext()) {
							var pt = iterator.Current;
							var dist = p0.DistanceTo(pt);
							var deltaT = p0.TimeDifference(pt);
							runStats.RecordInterval(deltaT, dist, pt.HeartRate, pt.Cadence);
							p0 = pt;
						}
					}
				}
			}

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);
			var outputFileName = extRegexp.Replace(fileName, ".html");

			var assembly = Assembly.GetExecutingAssembly();

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.index.cshtml")) {
				using (var reader = new StreamReader(stream)) {
					var page = Razor.Parse(reader.ReadToEnd(), runStats);
				}
			}

			using (var output = File.CreateText(outputFileName)) {
				output.WriteLine("{0,-20}  {1:N2} km", "Total distance:", totalDistance / 1000.0D);
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

	public class RunStats
	{
		public double TotalDistance { get; private set; }

		public double TotalDistanceInKm { get { return TotalDistance / 1000.0D; } }

		public TimeSpan TotalTime { get; private set; }

		public TimeSpan AveragePace { get { return new TimeSpan((long)(1000.0D * TotalTime.Ticks / TotalDistanceInKm)); } }

		public double AverageSpeed { get { return TotalDistanceInKm / TotalTime.TotalHours; } }

		public double TotalHeartbeats { get; private set; }

		public double AverageHeartRate { get { return TotalHeartbeats / TotalTime.TotalMinutes; } }

		public double MaxHeartRate { get; private set; }

		public double TotalSteps { get; private set; }

		public double AverageCadence { get { return TotalSteps / (2.0D * TotalTime.TotalMinutes); } }

		public double AverageStrideLength { get { return TotalDistanceInKm * 100.0D / TotalSteps; } }

		private TimeBin<double> _zoneBins;
		private TimeBin<TimeSpan> _paceBins;

		public RunStats(double[] zones, TimeSpan[] paces)
		{
			_zoneBins = new TimeBin<double>(zones);
			_paceBins = new TimeBin<TimeSpan>(paces);
			TotalDistance = 0.0D;
			TotalTime = TimeSpan.Zero;
			TotalHeartbeats = 0.0D;
			MaxHeartRate = 0.0D;
			TotalSteps = 0.0D;
		}

		public void RecordInterval(TimeSpan duration, double distance, double heartRate, double cadence)
		{
			TotalTime += duration;
			TotalDistance += distance;
			_zoneBins.Record(heartRate, duration);
			var pace = new TimeSpan((long)(1000.0D * duration.Ticks / distance));
			_paceBins.Record(pace, duration);
			TotalHeartbeats += heartRate * duration.TotalMinutes;
			UpdateMaxHeartRate(heartRate);
			// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute?
			TotalSteps += 2.0D * cadence * duration.TotalMinutes;
		}

		public void UpdateMaxHeartRate(double heartRate)
		{
			if (heartRate > MaxHeartRate)
				MaxHeartRate = heartRate;
		}
		
	}
}