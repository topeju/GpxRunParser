using System;
using System.Collections.Generic;
using System.Configuration;

namespace GpxRunParser
{
	public class RunStatistics
	{
		public double TotalDistance { get; private set; }

		public double TotalDistanceInKm
		{
			get { return TotalDistance / 1000.0D; }
		}

		public TimeSpan TotalTime { get; private set; }

		public TimeSpan AveragePace
		{
			get { return new TimeSpan((long) (1000.0D * TotalTime.Ticks / TotalDistance)); }
		}

		public double AverageSpeed
		{
			get { return TotalDistanceInKm / TotalTime.TotalHours; }
		}

		public double TotalHeartbeats { get; private set; }

		public double AverageHeartRate
		{
			get { return TotalHeartbeats / TotalTime.TotalMinutes; }
		}

		public double MaxHeartRate { get; private set; }

		public double TotalSteps { get; private set; }

		public double AverageCadence
		{
			get { return TotalSteps / (2.0D * TotalTime.TotalMinutes); }
		}

		public double AverageStrideLength
		{
			get { return TotalDistance * 100.0D / TotalSteps; }
		}

		public double TotalClimb { get; private set; }

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; private set; }

		public TimeBin<double> ZoneBins { get; private set; }
		public TimeBin<TimeSpan> PaceBins { get; private set; }

		public int Runs { get; set; }

		public IDictionary<DateTime, double> HeartRateLog { get; private set; }
		public IDictionary<DateTime, TimeSpan> PaceLog { get; private set; }
		public IDictionary<DateTime, double> CadenceLog { get; private set; }
		public IDictionary<DateTime, double> ElevationLog { get; private set; }
		public IDictionary<DateTime, double> SlopeLog { get; private set; }
		public IDictionary<DateTime, double> DistanceLog { get; private set; }

		public class PauseInfo
		{
			public GpxTrackPoint PauseStart { get; set; }
			public GpxTrackPoint PauseEnd { get; set; }
		}

		public IList<PauseInfo> Pauses { get; private set; }

		public IList<GpxTrackPoint> Route { get; private set; }
		public double MinLatitude { get; private set; }
		public double MaxLatitude { get; private set; }
		public double MinLongitude { get; private set; }
		public double MaxLongitude { get; private set; }

		private struct PointIntervalData
		{
			public double Distance;
			public double Climb;
			public TimeSpan Interval;
		}

		private GpxTrackPoint _lastPoint;
		private readonly TimeSpan _slowestDisplayedPace;
		private readonly PointIntervalData[] _lastIntervals;
		private readonly int _averagingPeriod;
		private int _latestPointOffset = -1;
		private int _earliestPointOffset = -1;
		private int _bufferCount;

		public RunStatistics(double[] zones, TimeSpan[] paces)
		{
			ZoneBins = new TimeBin<double>(zones);
			PaceBins = new TimeBin<TimeSpan>(paces);
			TotalDistance = 0.0D;
			TotalTime = TimeSpan.Zero;
			TotalHeartbeats = 0.0D;
			MaxHeartRate = 0.0D;
			TotalSteps = 0.0D;
			TotalClimb = 0.0D;
			Runs = 0;
			HeartRateLog = new SortedDictionary<DateTime, double>();
			PaceLog = new SortedDictionary<DateTime, TimeSpan>();
			CadenceLog = new SortedDictionary<DateTime, double>();
			ElevationLog = new SortedDictionary<DateTime, double>();
			SlopeLog = new SortedDictionary<DateTime, double>();
			DistanceLog = new SortedDictionary<DateTime, double>();
			Pauses = new List<PauseInfo>();
			_lastPoint = null;
			Route = new List<GpxTrackPoint>();
			MinLatitude = double.MaxValue;
			MaxLatitude = double.MinValue;
			MinLongitude = double.MaxValue;
			MaxLongitude = double.MinValue;
			_averagingPeriod = int.Parse(ConfigurationManager.AppSettings["AveragingPeriod"]);
			_lastIntervals = new PointIntervalData[_averagingPeriod];
			var slowestPace = ConfigurationManager.AppSettings["SlowestDisplayedPace"].Split(':');
			var slowH = int.Parse(slowestPace[0]);
			var slowM = int.Parse(slowestPace[1]);
			var slowS = double.Parse(slowestPace[2]);
			_slowestDisplayedPace = new TimeSpan(0, slowH, slowM, (int)slowS, (int)((slowS - (int)slowS) * 1000.0D));
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
			UpdateMaxHeartRate(point.HeartRate);
			DistanceLog[point.Time] = TotalDistanceInKm;
			SlopeLog[point.Time] = 0.0D;
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
			if (_bufferCount < _averagingPeriod) {
				_bufferCount++;
			}
			if (_latestPointOffset >= _averagingPeriod) {
				_latestPointOffset = 0;
			}
			if (_earliestPointOffset < 0) {
				_earliestPointOffset = 0;
			} else if (_latestPointOffset == _earliestPointOffset) {
				_earliestPointOffset++;
				if (_earliestPointOffset >= _averagingPeriod) {
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

			HeartRateLog[point.Time] = point.HeartRate;
			ZoneBins.Record(point.HeartRate, deltaT);
			TotalHeartbeats += point.HeartRate * deltaT.TotalMinutes;
			UpdateMaxHeartRate(point.HeartRate);

			var averagedPace = new TimeSpan((long) (1000.0D * averageTime.Ticks / averagedDistance));
			PaceBins.Record(averagedPace, deltaT);
			if (averagedDistance > 0.0 /* && averagedPace < SlowestDisplayedPace*/) {
				PaceLog[point.Time] = averagedPace;
			} else {
				PaceLog[point.Time] = _slowestDisplayedPace; //new TimeSpan(0);
			}

			CadenceLog[point.Time] = point.Cadence;
			// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute
			TotalSteps += 2.0D * point.Cadence * deltaT.TotalMinutes;

			ElevationLog[point.Time] = point.Elevation;
			SlopeLog[point.Time] = 100.0D * Math.Tan(averagedClimb / averagedDistance);

			TotalDistance += dist;
			TotalTime += deltaT;

			var intervalClimb = point.Elevation - _lastPoint.Elevation;
			if (intervalClimb > 0.0D) {
				TotalClimb += intervalClimb;
			}

			DistanceLog[point.Time] = TotalDistanceInKm;

			EndTime = point.Time > EndTime ? point.Time : EndTime;

			RecordRoutePoint(point);

			_lastPoint = point;
		}

		private void RecordRoutePoint(GpxTrackPoint point)
		{
			Route.Add(point);
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

		private void UpdateMaxHeartRate(double heartRate)
		{
			if (heartRate > MaxHeartRate) {
				MaxHeartRate = heartRate;
			}
		}
	}
}
