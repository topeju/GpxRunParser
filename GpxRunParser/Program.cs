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
			var fileName = @"D:\Data\Dropbox\Apps\iSmoothRun\Export\2014-06-09T17_26_45+300_Running.gpx";
			var zoneStr = "120,140,173,182";
			var paceBinStr = "8:45,7:15,6:10";
			var opts = new OptionSet {
				{ "f|file=", "GPX file to parse", s => fileName = s },
				{ "z|zones=", "Heart rate zone limits, comma-separated", s => zoneStr = s },
				{ "p|paces=", "Pace bin limits, comma-separated mm:ss", s => paceBinStr = s }
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
			if (fileName == "") throw new OptionException("file", "The file parameter must be specified");
			var zones = (from zone in zoneStr.Split(',') select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			var paces =
				(from paceStr in paceBinStr.Split(',')
				 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1])).TotalMinutes).ToArray();

			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxDoc = XDocument.Load(fileName);

			var zoneBins = new TimeBin(zones);
			var paceBins = new TimeBin(paces);
			var totalTime = TimeSpan.Zero;
			var totalDistance = 0.0D;

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
							totalTime += deltaT;
							var pace = deltaT.TotalMinutes * 1000.0D / dist;
							zoneBins.Record(p0.HeartRate, deltaT);
							paceBins.Record(pace, deltaT);
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
				output.WriteLine();

				output.WriteLine("Time spent in each heart rate zone:");
				zoneBins.Output(output, totalTime);

				output.WriteLine();
				output.WriteLine("Time spent in each pace range:");
				paceBins.Output(output, totalTime);
			}

		}
	}

	public class TimeBin
	{
		private double[] Bins { get; set; }

		private TimeSpan[] Values { get; set; }

		public TimeBin(double[] boundaries)
		{
			Bins = new double[boundaries.Length];
			Values = new TimeSpan[boundaries.Length + 1];
			var i = 0;
			Values[0] = TimeSpan.Zero;
			foreach (var b in boundaries.OrderBy(bnd => bnd)) {
				Bins[i] = b;
				Values[i+1] = TimeSpan.Zero;
				i++;
			}
		}

		public void Record(double value, TimeSpan time)
		{
			var i = Bins.Length-1;
			while (i >= 0 && Bins[i] > value) i--;
			Values[i+1] += time;
		}

		public void Output(StreamWriter stream, TimeSpan totalTime)
		{
			stream.WriteLine("{0,-20}\t{1:g}\t{2:P0}", "< " + Bins[0], Values[0],
								  Values[0].TotalMilliseconds / totalTime.TotalMilliseconds);
			for (var i = 0; i < Bins.Length - 1; i++) {
				stream.WriteLine("{0,-20}\t{1:g}\t{2:P0}", Bins[i] + "..." + Bins[i + 1], Values[i + 1],
									  Values[i+1].TotalMilliseconds / totalTime.TotalMilliseconds);
			}
			stream.WriteLine("{0,-20}\t{1:g}\t{2:P0}", "> " + Bins[Bins.Length - 1], Values[Bins.Length],
								  Values[Bins.Length].TotalMilliseconds / totalTime.TotalMilliseconds);
		}
	}
}
