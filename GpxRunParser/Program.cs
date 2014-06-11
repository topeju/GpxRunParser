using System;
using System.Collections.Generic;
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
			var steps = 0.0D;

			foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
				foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
					var points = from point in segment.Descendants(gpxNamespace + "trkpt")
								 select new GpxTrackPoint(point);
					var iterator = points.GetEnumerator();
					if (iterator.MoveNext()) {
						var p0 = iterator.Current;
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
							if (p0.HeartRate > maxHeartRate) {
								maxHeartRate = p0.HeartRate;
							}
							steps += p0.Cadence * deltaT.TotalMinutes;
							p0 = pt;
						}
					}
				}
			}

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);
			var outputFileName = extRegexp.Replace(fileName, ".txt");

			using (var output = File.CreateText(outputFileName)) {
				output.WriteLine("Total distance:\t{0:N2} km", totalDistance/1000.0D);
				output.WriteLine("Total time:\t{0:g}", totalTime);
				output.WriteLine("Average pace:\t{0:g}/km", new TimeSpan((long)(1000.0D * totalTime.Ticks / totalDistance)));
				output.WriteLine("Average speed:\t{0:N2} km/h", totalDistance / (1000.0D * totalTime.TotalHours));
				output.WriteLine("Avg heart rate:\t{0:N0}", heartTotal / totalTime.TotalMinutes);
				output.WriteLine("Max heart rate:\t{0:N0}", maxHeartRate);
				output.WriteLine("Total steps:\t{0:N0}", steps);
				output.WriteLine("Avg cadence:\t{0:N0}", steps / totalTime.TotalMinutes);
				output.WriteLine();

				output.WriteLine("Time spent in each heart rate zone:");
				zoneBins.Output(output, totalTime);

				output.WriteLine();
				output.WriteLine("Time spent in each pace range:");
				paceBins.Output(output, totalTime);
			}

		}
	}

	public class TimeBin<T> where T : IComparable
	{
		private T[] Bins { get; set; }

		private TimeSpan[] Values { get; set; }

		public TimeBin(T[] boundaries)
		{
			Bins = new T[boundaries.Length];
			Values = new TimeSpan[boundaries.Length + 1];
			var i = 0;
			Values[0] = TimeSpan.Zero;
			foreach (var b in boundaries.OrderBy(bnd => bnd)) {
				Bins[i] = b;
				Values[i+1] = TimeSpan.Zero;
				i++;
			}
		}

		public void Record(T value, TimeSpan time)
		{
			var i = Bins.Length-1;
			while (i >= 0 && Bins[i].CompareTo(value) > 0) i--;
			Values[i+1] += time;
		}

		public void Output(StreamWriter stream, TimeSpan totalTime)
		{
			stream.WriteLine("< {0,-18}\t{1:g}\t{2:P0}", Bins[0], Values[0],
								  Values[0].TotalMilliseconds / totalTime.TotalMilliseconds);
			for (var i = 0; i < Bins.Length - 1; i++) {
				stream.WriteLine("{0,-20}\t{1:g}\t{2:P0}", Bins[i] + "..." + Bins[i + 1], Values[i + 1],
									  Values[i+1].TotalMilliseconds / totalTime.TotalMilliseconds);
			}
			stream.WriteLine("> {0,-18}\t{1:g}\t{2:P0}", Bins[Bins.Length - 1], Values[Bins.Length],
								  Values[Bins.Length].TotalMilliseconds / totalTime.TotalMilliseconds);
		}
	}
}
