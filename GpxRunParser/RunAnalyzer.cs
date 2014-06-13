using System;
using System.Linq;
using System.Xml.Linq;

namespace GpxRunParser
{
	public class RunAnalyzer
	{
		private double[] _heartRateZones;
		private TimeSpan[] _paceZones;

		public RunAnalyzer(double[] heartRateZones, TimeSpan[] paceZones)
		{
			_heartRateZones = heartRateZones;
			_paceZones = paceZones;
		}

		public RunStatistics Analyze(string fileName)
		{
			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxDoc = XDocument.Load(fileName);

			var runStats = new RunStatistics(_heartRateZones, _paceZones);
			var firstPoint = true;

			foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
				foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
					var points = from point in segment.Descendants(gpxNamespace + "trkpt")
						select new GpxTrackPoint(point);
					var iterator = points.GetEnumerator();
					if (iterator.MoveNext()) {
						var p0 = iterator.Current;
						runStats.UpdateMaxHeartRate(p0.HeartRate);
						if (firstPoint) {
							runStats.StartTime = p0.Time;
							firstPoint = false;
						}
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

			return runStats;
		}
	}
}

