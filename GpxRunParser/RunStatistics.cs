using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GpxRunParser;

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

	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }

	public TimeBin<double> ZoneBins { get; private set; }
	public TimeBin<TimeSpan> PaceBins { get; private set; }

	public int Runs { get; set; }

	public IDictionary<DateTime, double> HeartRateLog { get; set; }
	public IDictionary<DateTime, TimeSpan> PaceLog { get; set; }
	public IDictionary<DateTime, double> CadenceLog { get; set; }
	public IDictionary<DateTime, double> ElevationLog { get; set; }
	public IDictionary<DateTime, double> DistanceLog { get; set; }

	public IList<DateTime> EndPoints { get; set; }
	public IList<DateTime> StartPoints { get; set; }

	public IList<GpxTrackPoint> Route { get; set; }
	public double MinLatitude { get; set; }
	public double MaxLatitude { get; set; }
	public double MinLongitude { get; set; }
	public double MaxLongitude { get; set; }

	private struct PointIntervalData
	{
		public double distance;
		public TimeSpan interval;
	}

	private GpxTrackPoint _lastPoint;
	private readonly PointIntervalData[] _lastIntervals = new PointIntervalData[AveragingPeriod];
	private int _latestPointOffset = -1;
	private int _earliestPointOffset = -1;
	private int _bufferCount = 0;

	public RunStatistics(double[] zones, TimeSpan[] paces)
	{
		ZoneBins = new TimeBin<double>(zones);
		PaceBins = new TimeBin<TimeSpan>(paces);
		TotalDistance = 0.0D;
		TotalTime = TimeSpan.Zero;
		TotalHeartbeats = 0.0D;
		MaxHeartRate = 0.0D;
		TotalSteps = 0.0D;
		Runs = 0;
		HeartRateLog = new SortedDictionary<DateTime, double>();
		PaceLog = new SortedDictionary<DateTime, TimeSpan>();
		CadenceLog = new SortedDictionary<DateTime, double>();
		ElevationLog = new SortedDictionary<DateTime, double>();
		DistanceLog = new SortedDictionary<DateTime, double>();
		StartPoints = new List<DateTime>();
		EndPoints = new List<DateTime>();
		_lastPoint = null;
		Route = new List<GpxTrackPoint>();
		MinLatitude = double.MaxValue;
		MaxLatitude = double.MinValue;
		MinLongitude = double.MaxValue;
		MaxLongitude = double.MinValue;
	}

	private const int AveragingPeriod = 15; // Seconds. Also, number of point distance values stored (-1).
	private static readonly TimeSpan SlowestDisplayedPace = new TimeSpan(0, 20, 0);

	public void SetStartPoint(GpxTrackPoint point)
	{
		_latestPointOffset = -1;
		_earliestPointOffset = -1;
		_bufferCount = 0;
		if (_lastPoint != null) {
			EndPoints.Add(_lastPoint.Time);
		}
		StartPoints.Add(point.Time);
		UpdateMaxHeartRate(point.HeartRate);
		ElevationLog[point.Time] = point.Elevation;
		DistanceLog[point.Time] = TotalDistanceInKm;
		_lastPoint = point;
		RecordRoutePoint(point);
	}

	public void RecordInterval(GpxTrackPoint point)
	{
		// Sequence (AveragingPeriod=4):
		// ___, ___, ___, ___ (latest = -1, earliest = -1)
		// P01, ___, ___, ___ (latest = 0, earliest = 0)
		// P01, P12, ___, ___ (latest = 1, earliest = 0)
		// P01, P12, P23, ___ (latest = 2, earliest = 0)
		// P01, P12, P23, P34 (latest = 3, earliest = 0)
		// P45, P12, P23, P34 (latest = 0, earliest = 1)
		// P45, P56, P23, P34 (latest = 1, earliest = 2)
		// P45, P56, P67, P34 (latest = 2, earliest = 3)
		// P45, P56, P67, P78 (latest = 3, earliest = 0)
		// P89, P56, P67, P78 (latest = 0, earliest = 1)
		_latestPointOffset++;
		if (_bufferCount < AveragingPeriod) _bufferCount++;
		if (_latestPointOffset >= AveragingPeriod) {
			_latestPointOffset = 0;
		}
		if (_earliestPointOffset < 0) {
			_earliestPointOffset = 0;
		} else if (_latestPointOffset == _earliestPointOffset) {
			_earliestPointOffset++;
			if (_earliestPointOffset >= AveragingPeriod) {
				_earliestPointOffset = 0;
			}
		}

		_lastIntervals[_latestPointOffset].distance = _lastPoint.DistanceTo(point);
		_lastIntervals[_latestPointOffset].interval = _lastPoint.TimeDifference(point);

		var dist = _lastPoint.DistanceTo(point);
		var deltaT = _lastPoint.TimeDifference(point);

		double averagedDistance = 0.0;
		var averageTime = new TimeSpan(0);
		for (var i = 0; i < _bufferCount; i++) {
			averagedDistance += _lastIntervals[i].distance;
			averageTime += _lastIntervals[i].interval;
		}

		HeartRateLog[point.Time] = point.HeartRate;
		ZoneBins.Record(point.HeartRate, deltaT);
		TotalHeartbeats += point.HeartRate * deltaT.TotalMinutes;
		UpdateMaxHeartRate(point.HeartRate);

		var averagedPace = new TimeSpan((long) (1000.0D * averageTime.Ticks / averagedDistance));
		PaceBins.Record(averagedPace, deltaT);
		if (averagedDistance > 0.0/* && averagedPace < SlowestDisplayedPace*/) {
			PaceLog[point.Time] = averagedPace;
		} else {
			PaceLog[point.Time] = SlowestDisplayedPace; //new TimeSpan(0);
		}

		CadenceLog[point.Time] = point.Cadence;
		// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute
		TotalSteps += 2.0D * point.Cadence * deltaT.TotalMinutes;

		ElevationLog[point.Time] = point.Elevation;

		TotalDistance += dist;
		TotalTime += deltaT;

		DistanceLog[point.Time] = TotalDistanceInKm;

		EndTime = point.Time > EndTime ? point.Time : EndTime;

		RecordRoutePoint(point);

		_lastPoint = point;
	}

	private void RecordRoutePoint(GpxTrackPoint point)
	{
		Route.Add(point);
		if (point.Latitude < MinLatitude)
			MinLatitude = point.Latitude;
		if (point.Latitude > MaxLatitude)
			MaxLatitude = point.Latitude;
		if (point.Longitude < MinLongitude)
			MinLongitude = point.Longitude;
		if (point.Longitude > MaxLongitude)
			MaxLongitude = point.Longitude;
	}

	public void UpdateMaxHeartRate(double heartRate)
	{
		if (heartRate > MaxHeartRate)
			MaxHeartRate = heartRate;
	}
}