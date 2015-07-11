using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GpxRunParser
{
	public class RunAnalyzer
	{
		private readonly double[] _heartRateZones;
		private readonly TimeSpan[] _paceZones;

		public IDictionary<DateTime, RunStatistics> WeeklyStats { get; private set; }
		public IDictionary<DateTime, RunStatistics> MonthlyStats { get; private set; }

		public RunAnalyzer(double[] heartRateZones, TimeSpan[] paceZones)
		{
			_heartRateZones = heartRateZones;
			_paceZones = paceZones;
			WeeklyStats = new Dictionary<DateTime, RunStatistics>();
			MonthlyStats = new Dictionary<DateTime, RunStatistics>();
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
							if (MonthlyStats.ContainsKey(month)) {
								monthlyStats = MonthlyStats[month];
							} else {
								MonthlyStats[month] = monthlyStats = new RunStatistics(_heartRateZones, _paceZones);
								monthlyStats.StartTime = month;
							}
							var deltaDays = DayOfWeek.Monday - p0.Time.DayOfWeek;
							if (deltaDays > 0) {
								deltaDays -= 7;
							}
							var week = p0.Time.Date.AddDays(deltaDays);
							if (WeeklyStats.ContainsKey(week)) {
								weeklyStats = WeeklyStats[week];
							} else {
								WeeklyStats[week] = weeklyStats = new RunStatistics(_heartRateZones, _paceZones);
								weeklyStats.StartTime = week;
							}
							runStats.StartTime = p0.Time;
							runStats.Runs++;
							monthlyStats.Runs++;
							weeklyStats.Runs++;
							firstPoint = false;
						}
						runStats.SetStartPoint(p0);
						monthlyStats.SetStartPoint(p0);
						weeklyStats.SetStartPoint(p0);
						while (iterator.MoveNext()) {
							var pt = iterator.Current;
							runStats.RecordInterval(pt);
							monthlyStats.RecordInterval(pt);
							weeklyStats.RecordInterval(pt);
							p0 = pt;
						}
					}
				}
			}

			return runStats;
		}
	}
}
