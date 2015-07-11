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
						var pt = iterator.Current;
						if (firstPoint) {
							var month = new DateTime(pt.Time.Year, pt.Time.Month, 1);
							if (MonthlyStats.ContainsKey(month)) {
								monthlyStats = MonthlyStats[month];
							} else {
								MonthlyStats[month] = monthlyStats = new RunStatistics(_heartRateZones, _paceZones);
								monthlyStats.StartTime = month;
							}
							var deltaDays = DayOfWeek.Monday - pt.Time.DayOfWeek;
							if (deltaDays > 0) {
								deltaDays -= 7;
							}
							var week = pt.Time.Date.AddDays(deltaDays);
							if (WeeklyStats.ContainsKey(week)) {
								weeklyStats = WeeklyStats[week];
							} else {
								WeeklyStats[week] = weeklyStats = new RunStatistics(_heartRateZones, _paceZones);
								weeklyStats.StartTime = week;
							}
							runStats.StartTime = pt.Time;
							runStats.Runs++;
							monthlyStats.Runs++;
							weeklyStats.Runs++;
							firstPoint = false;
						}
						runStats.SetStartPoint(pt);
						monthlyStats.SetStartPoint(pt);
						weeklyStats.SetStartPoint(pt);
						while (iterator.MoveNext()) {
							pt = iterator.Current;
							runStats.RecordInterval(pt);
							monthlyStats.RecordInterval(pt);
							weeklyStats.RecordInterval(pt);
						}
					}
				}
			}

			return runStats;
		}
	}
}
