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

		public IDictionary<DateTime,RunStatistics> WeeklyStats { get; private set; }
		public IDictionary<DateTime,RunStatistics> MonthlyStats { get; private set; }

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
						var trackFilter = new KalmanFilter(1.0);
						trackFilter.Process(p0, 1.0);
						if (firstPoint) {
							var month = new DateTime(trackFilter.Point.Time.Year, trackFilter.Point.Time.Month, 1);
							if (MonthlyStats.ContainsKey(month)) {
								monthlyStats = MonthlyStats[month];
							} else {
								MonthlyStats[month] = monthlyStats = new RunStatistics(_heartRateZones, _paceZones);
								monthlyStats.StartTime = month;
							}
							int deltaDays = DayOfWeek.Monday - trackFilter.Point.Time.DayOfWeek;
							if (deltaDays > 0) {
								deltaDays -= 7;
							}
							var week = trackFilter.Point.Time.Date.AddDays(deltaDays);
							if (WeeklyStats.ContainsKey(week)) {
								weeklyStats = WeeklyStats[week];
							} else {
								WeeklyStats[week] = weeklyStats = new RunStatistics(_heartRateZones, _paceZones);
								weeklyStats.StartTime = week;
							}
							runStats.StartTime = trackFilter.Point.Time;
							runStats.Runs++;
							monthlyStats.Runs++;
							weeklyStats.Runs++;
							firstPoint = false;
						}
						runStats.UpdateMaxHeartRate(trackFilter.Point.HeartRate);
						monthlyStats.UpdateMaxHeartRate(trackFilter.Point.HeartRate);
						weeklyStats.UpdateMaxHeartRate(trackFilter.Point.HeartRate);
						while (iterator.MoveNext()) {
							var previousPoint = new GpxTrackPoint(trackFilter.Point);
							trackFilter.Process(iterator.Current, 1.0);
							var dist = previousPoint.DistanceTo(trackFilter.Point);
							var deltaT = previousPoint.TimeDifference(trackFilter.Point);
							runStats.RecordInterval(deltaT, dist, trackFilter.Point.HeartRate, trackFilter.Point.Cadence);
							monthlyStats.RecordInterval(deltaT, dist, trackFilter.Point.HeartRate, trackFilter.Point.Cadence);
							weeklyStats.RecordInterval(deltaT, dist, trackFilter.Point.HeartRate, trackFilter.Point.Cadence);
						}
					}
				}
			}

			return runStats;
		}
	}
}

