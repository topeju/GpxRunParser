using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace GpxRunParser
{
	public class AggregateStatistics
	{
		#region Actual properties
		// ReSharper disable MemberCanBePrivate.Global
		public double TotalDistance { get; private set; }

		public TimeSpan TotalTime { get; private set; }

		public double TotalHeartbeats { get; private set; }

		public double MaxHeartRate { get; private set; }

		public double TotalSteps { get; private set; }

		public double TotalClimb { get; private set; }

		public DateTime StartTime { get; set; }

		public TimeHistogram<double> HeartRateHistogram { get; private set; }
		public TimeHistogram<TimeSpan> PaceHistogram { get; private set; }

		public int Runs { get; set; }

		[JsonIgnore]
		public IDictionary<DateTime,IList<GpxTrackPoint>> Routes { get; private set; }
		[JsonIgnore]
		public double MinLatitude { get; private set; }
		[JsonIgnore]
		public double MaxLatitude { get; private set; }
		[JsonIgnore]
		public double MinLongitude { get; private set; }
		[JsonIgnore]
		public double MaxLongitude { get; private set; }
		// ReSharper restore MemberCanBePrivate.Global
		#endregion

		#region Properties calculated from the above
		[JsonIgnore]
		public double TotalDistanceInKm
		{
			get { return TotalDistance / 1000.0D; }
		}

		[JsonIgnore]
		public TimeSpan AveragePace
		{
			get { return new TimeSpan((long) (1000.0D * TotalTime.Ticks / TotalDistance)); }
		}

		[JsonIgnore]
		public double AverageSpeed
		{
			get { return TotalDistanceInKm / TotalTime.TotalHours; }
		}

		[JsonIgnore]
		public double AverageHeartRate
		{
			get { return TotalHeartbeats / TotalTime.TotalMinutes; }
		}

		[JsonIgnore]
		public double AverageCadence
		{
			get { return TotalSteps / (2.0D * TotalTime.TotalMinutes); }
		}

		[JsonIgnore]
		public double AverageStrideLength
		{
			get { return TotalDistance * 100.0D / TotalSteps; }
		}
		#endregion

		public AggregateStatistics()
		{
			HeartRateHistogram = new TimeHistogram<double>();
			PaceHistogram = new TimeHistogram<TimeSpan>();
			Routes = new SortedDictionary<DateTime, IList<GpxTrackPoint>>();
			MinLatitude = double.MaxValue;
			MaxLatitude = double.MinValue;
			MinLongitude = double.MaxValue;
			MaxLongitude = double.MinValue;
		}

		public void AddRun(RunStatistics run)
		{
			HeartRateHistogram.Record(run.HeartRateHistogram);
			PaceHistogram.Record(run.PaceHistogram);
			TotalDistance += run.TotalDistance;
			TotalTime += run.TotalTime;
			TotalHeartbeats += run.TotalHeartbeats;
			MaxHeartRate = Math.Max(MaxHeartRate, run.MaxHeartRate);
			TotalSteps += run.TotalSteps;
			TotalClimb += run.TotalClimb;
			Runs++;
			DateTime lastPointTime = DateTime.MinValue;
			var sampledRoute = new SortedDictionary<DateTime,GpxTrackPoint>();
			foreach (var time in run.Route.Keys) {
				if (time >= lastPointTime + Settings.AggregateSamplingPeriod) {
					sampledRoute[time] = run.Route[time];
					lastPointTime = time;
				}
			}
			var lastPoint = run.Route.Last();
			if (!sampledRoute.ContainsKey(lastPoint.Key)) {
				sampledRoute[lastPoint.Key] = lastPoint.Value;
			}
			foreach (var point in run.Pauses) {
				if (!sampledRoute.ContainsKey(point.PauseStart.Time)) { 
					sampledRoute[point.PauseStart.Time] = point.PauseStart;
				}
				if (!sampledRoute.ContainsKey(point.PauseEnd.Time)) {
					sampledRoute[point.PauseEnd.Time] = point.PauseEnd;
				}
			}
			Routes[run.StartTime] = sampledRoute.Values.ToList();
			foreach (var point in sampledRoute.Values) {
				if (point.Latitude < MinLatitude) {
					MinLatitude = point.Latitude;
				}
				if (point.Latitude > MaxLatitude) {
					MaxLatitude = point.Latitude;
				}
				if (point.Longitude < MinLongitude) {
					MinLongitude = point.Longitude;
				}
				if (point.Longitude > MaxLongitude) {
					MaxLongitude = point.Longitude;
				}
			}
		}
	}
}
