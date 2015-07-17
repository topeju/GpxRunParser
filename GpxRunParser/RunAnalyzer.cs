using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GpxRunParser
{
	public class RunAnalyzer
	{
		public IDictionary<DateTime, AggregateStatistics> WeeklyStats { get; private set; }

		public IDictionary<DateTime, AggregateStatistics> MonthlyStats { get; private set; }

		public RunAnalyzer()
		{
			WeeklyStats = new Dictionary<DateTime, AggregateStatistics>();
			MonthlyStats = new Dictionary<DateTime, AggregateStatistics>();
		}

		public bool Analyze(string fileName, out RunStatistics runStats)
		{
			runStats = AnalysisCache.Fetch(fileName);
			var analysisRun = false;
			if (runStats == null) {
				analysisRun = true;
				var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
				var gpxDoc = XDocument.Load(fileName);

				runStats = new RunStatistics();
				var firstPoint = true;

				foreach (var track in gpxDoc.Descendants(gpxNamespace + "trk")) {
					foreach (var segment in track.Descendants(gpxNamespace + "trkseg")) {
						var points = from point in segment.Descendants(gpxNamespace + "trkpt")
						            select new GpxTrackPoint(point);
						var iterator = points.GetEnumerator();
						if (iterator.MoveNext()) {
							var pt = iterator.Current;
							if (firstPoint) {
								runStats.StartTime = pt.Time;
								firstPoint = false;
							}
							runStats.SetStartPoint(pt);
							while (iterator.MoveNext()) {
								pt = iterator.Current;
								runStats.RecordInterval(pt);
							}
							runStats.EndSegment();
						}
					}
				}

				AnalysisCache.Store(fileName, runStats);
			}

			AggregateStatistics monthlyStats;
			AggregateStatistics weeklyStats;

			var month = new DateTime(runStats.StartTime.Year, runStats.StartTime.Month, 1);
			if (MonthlyStats.ContainsKey(month)) {
				monthlyStats = MonthlyStats[month];
			} else {
				MonthlyStats[month] = monthlyStats = new AggregateStatistics();
				monthlyStats.StartTime = month;
			}
			monthlyStats.AddRun(runStats);
			var deltaDays = DayOfWeek.Monday - runStats.StartTime.DayOfWeek;
			if (deltaDays > 0) {
				deltaDays -= 7;
			}
			var week = runStats.StartTime.Date.AddDays(deltaDays);
			if (WeeklyStats.ContainsKey(week)) {
				weeklyStats = WeeklyStats[week];
			} else {
				WeeklyStats[week] = weeklyStats = new AggregateStatistics();
				weeklyStats.StartTime = week;
			}
			weeklyStats.AddRun(runStats);

			return analysisRun;
		}
	}
}
