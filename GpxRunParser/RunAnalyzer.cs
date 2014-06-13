using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GpxRunParser
{
	public class RunAnalyzer
	{
		private double[] _heartRateZones;
		private TimeSpan[] _paceZones;

		private Dictionary<DateTime,RunStatistics> _weeklyStats;
		private Dictionary<DateTime,RunStatistics> _monthlyStats;

		public RunAnalyzer(double[] heartRateZones, TimeSpan[] paceZones)
		{
			_heartRateZones = heartRateZones;
			_paceZones = paceZones;
			_weeklyStats = new Dictionary<DateTime, RunStatistics>();
			_monthlyStats = new Dictionary<DateTime, RunStatistics>();
		}

		public RunStatistics Analyze(string fileName)
		{
			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxDoc = XDocument.Load(fileName);

			var runStats = new RunStatistics(_heartRateZones, _paceZones);
			RunStatistics monthlyStats = null;
			RunStatistics weeklyStats = null;
			var firstPoint = true;

			foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
				foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
					var points = from point in segment.Descendants(gpxNamespace + "trkpt")
						select new GpxTrackPoint(point);
					var iterator = points.GetEnumerator();
					if (iterator.MoveNext()) {
						var p0 = iterator.Current;
						if (firstPoint) {
							var month = new DateTime(p0.Time.Year, p0.Time.Month, 1);
							if (_monthlyStats.ContainsKey(month)) {
								monthlyStats = _monthlyStats[month];
							} else {
								_monthlyStats[month] = monthlyStats = new RunStatistics(_heartRateZones, _paceZones);
								monthlyStats.StartTime = month;
							}
							int deltaDays = DayOfWeek.Monday - p0.Time.DayOfWeek;
							if (deltaDays > 0) {
								deltaDays -= 7;
							}
							var week = p0.Time.Date.AddDays(deltaDays);
							if (_weeklyStats.ContainsKey(week)) {
								weeklyStats = _weeklyStats[week];
							} else {
								_weeklyStats[week] = weeklyStats = new RunStatistics(_heartRateZones, _paceZones);
								weeklyStats.StartTime = week;
							}
							runStats.StartTime = p0.Time;
							firstPoint = false;
						}
						runStats.UpdateMaxHeartRate(p0.HeartRate);
						monthlyStats.UpdateMaxHeartRate(p0.HeartRate);
						weeklyStats.UpdateMaxHeartRate(p0.HeartRate);
						while (iterator.MoveNext()) {
							var pt = iterator.Current;
							var dist = p0.DistanceTo(pt);
							var deltaT = p0.TimeDifference(pt);
							runStats.RecordInterval(deltaT, dist, pt.HeartRate, pt.Cadence);
							monthlyStats.RecordInterval(deltaT, dist, pt.HeartRate, pt.Cadence);
							weeklyStats.RecordInterval(deltaT, dist, pt.HeartRate, pt.Cadence);
							p0 = pt;
						}
					}
				}
			}

			return runStats;
		}
	}
}

