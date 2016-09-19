using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GpxRunParser
{
	public class RunStatistics
	{
		#region Actual properties
		// ReSharper disable MemberCanBePrivate.Global
		public double TotalDistance { get; set; }
		public TimeSpan TotalTime { get; set; }
		public double TotalHeartbeats { get; set; }
		public double MaxHeartRate { get; set; }
		public double TotalSteps { get; set; }
		public double TotalClimb { get; set; }

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public TimeHistogram<double> HeartRateHistogram { get; set; }
		public TimeHistogram<TimeSpan> PaceHistogram { get; set; }

		public IList<PauseInfo> Pauses { get; set; }
		public IDictionary<DateTime,GpxTrackPoint> Route { get; set; }
		// ReSharper restore MemberCanBePrivate.Global
		#endregion

		#region Properties not persisted in the cache (i.e. not needed for aggregate statistics)
		[JsonIgnore]
		public TimeSpan FastestPace { get; private set; }
		[JsonIgnore]
		public TimeSpan SlowestPace { get; private set; }

        [JsonIgnore]
        public double MaxSpeed { get; private set; }

        // ReSharper disable MemberCanBePrivate.Global
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

		#region Auxiliary classes and structures
		public struct PauseInfo
		{
			public GpxTrackPoint PauseStart { get; set; }
			public GpxTrackPoint PauseEnd { get; set; }
		}

		private struct PointIntervalData
		{
			public double Distance;
			public double Climb;
			public TimeSpan Interval;
		}
		#endregion

		#region Interim data used during analysis
		private GpxTrackPoint _lastPoint;
		private readonly PointIntervalData[] _lastIntervals;
		private int _latestPointOffset = -1;
		private int _earliestPointOffset = -1;
		private int _bufferCount;
		private TimeSpan _lastPace = new TimeSpan();
		#endregion

		public RunStatistics()
		{
			HeartRateHistogram = new TimeHistogram<double>();
			PaceHistogram = new TimeHistogram<TimeSpan>();
			TotalDistance = 0.0D;
			TotalTime = TimeSpan.Zero;
			TotalHeartbeats = 0.0D;
			MaxHeartRate = 0.0D;
			TotalSteps = 0.0D;
			TotalClimb = 0.0D;
			Pauses = new List<PauseInfo>();
			_lastPoint = null;
			Route = new SortedDictionary<DateTime,GpxTrackPoint>();
			MinLatitude = double.MaxValue;
			MaxLatitude = double.MinValue;
			MinLongitude = double.MaxValue;
			MaxLongitude = double.MinValue;
			_lastIntervals = new PointIntervalData[Settings.AveragingPeriod];
			FastestPace = Settings.SlowestDisplayedPace;
			SlowestPace = new TimeSpan();
            MaxSpeed = double.MinValue;
		}

		public void RefreshCalculatedProperties()
		{
			MinLatitude = double.MaxValue;
			MaxLatitude = double.MinValue;
			MinLongitude = double.MaxValue;
			MaxLongitude = double.MinValue;
			foreach (var point in Route.Values) {
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

		public void SetStartPoint(GpxTrackPoint point)
		{
			_latestPointOffset = -1;
			_earliestPointOffset = -1;
			_bufferCount = 0;
			if (_lastPoint != null) {
				Pauses.Add(new PauseInfo {
					PauseStart = _lastPoint,
					PauseEnd = point
				});
			}
			point.Slope = 0.0D;
			point.Pace = _lastPace;
            point.Speed = 3600.0 / _lastPace.TotalSeconds;
			_lastPoint = point;
			RecordRoutePoint(point);
		}

		public void RecordInterval(GpxTrackPoint point)
		{
			// Sequence (AveragingPeriod=4):
			// ___, ___, ___, ___ (latest = -1, earliest = -1)
			// Δ01, ___, ___, ___ (latest = 0, earliest = 0)
			// Δ01, Δ12, ___, ___ (latest = 1, earliest = 0)
			// Δ01, Δ12, Δ23, ___ (latest = 2, earliest = 0)
			// Δ01, Δ12, Δ23, Δ34 (latest = 3, earliest = 0)
			// Δ45, Δ12, Δ23, Δ34 (latest = 0, earliest = 1)
			// Δ45, Δ56, Δ23, Δ34 (latest = 1, earliest = 2)
			// Δ45, Δ56, Δ67, Δ34 (latest = 2, earliest = 3)
			// Δ45, Δ56, Δ67, Δ78 (latest = 3, earliest = 0)
			// Δ89, Δ56, Δ67, Δ78 (latest = 0, earliest = 1)
			_latestPointOffset++;
			if (_bufferCount < Settings.AveragingPeriod) {
				_bufferCount++;
			}
			if (_latestPointOffset >= Settings.AveragingPeriod) {
				_latestPointOffset = 0;
			}
			if (_earliestPointOffset < 0) {
				_earliestPointOffset = 0;
			} else if (_latestPointOffset == _earliestPointOffset) {
				_earliestPointOffset++;
				if (_earliestPointOffset >= Settings.AveragingPeriod) {
					_earliestPointOffset = 0;
				}
			}

			_lastIntervals[_latestPointOffset].Distance = _lastPoint.DistanceTo(point);
			_lastIntervals[_latestPointOffset].Climb = point.Elevation - _lastPoint.Elevation;
			_lastIntervals[_latestPointOffset].Interval = _lastPoint.TimeDifference(point);

			var dist = _lastPoint.DistanceTo(point);
			var deltaT = _lastPoint.TimeDifference(point);

			var averagedDistance = 0.0;
			var averagedClimb = 0.0;
			var averageTime = new TimeSpan(0);
			for (var i = 0; i < _bufferCount; i++) {
				averagedDistance += _lastIntervals[i].Distance;
				averagedClimb += _lastIntervals[i].Climb;
				averageTime += _lastIntervals[i].Interval;
			}

			HeartRateHistogram.Record(point.HeartRate, deltaT);
			TotalHeartbeats += point.HeartRate * deltaT.TotalMinutes;

			var averagedPace = new TimeSpan((long) (1000.0D * averageTime.Ticks / averagedDistance));
			// ReSharper disable once PossibleLossOfFraction
			PaceHistogram.Record(TimeSpan.FromSeconds(averagedPace.Ticks / TimeSpan.TicksPerSecond), deltaT);
			if (averagedDistance > 0.0 /* && averagedPace < Settings.SlowestDisplayedPace*/) {
				point.Pace = averagedPace;
				if (averagedPace < FastestPace) {
					FastestPace = averagedPace;
				}
				if (averagedPace > SlowestPace) {
					SlowestPace = (averagedPace > Settings.SlowestDisplayedPace ? Settings.SlowestDisplayedPace : averagedPace);
				}
			} else {
				point.Pace = Settings.SlowestDisplayedPace;
			}
            point.Speed = 3600.0 / averagedPace.TotalSeconds;
            if (point.Speed > MaxSpeed) MaxSpeed = point.Speed;

			// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute
			TotalSteps += 2.0D * point.Cadence * deltaT.TotalMinutes;

			point.Slope = 100.0D * Math.Tan(averagedClimb / averagedDistance);

			TotalDistance += dist;
			TotalTime += deltaT;

			var intervalClimb = point.Elevation - _lastPoint.Elevation;
			if (intervalClimb > 0.0D) {
				TotalClimb += intervalClimb;
			}

			EndTime = point.Time > EndTime ? point.Time : EndTime;

			RecordRoutePoint(point);

			_lastPoint = point;
			_lastPace = averagedDistance > 0.0 ? averagedPace : Settings.SlowestDisplayedPace;
		}

		public void EndSegment()
		{
		}

		private void RecordRoutePoint(GpxTrackPoint point)
		{
			point.CumulativeClimb = TotalClimb;
			point.CumulativeDistance = TotalDistanceInKm;
			Route[point.Time] = point;
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
			if (point.HeartRate > MaxHeartRate) {
				MaxHeartRate = point.HeartRate;
			}
		}

	}
}
