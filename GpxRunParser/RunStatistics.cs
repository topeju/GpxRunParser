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

	public TimeBin<double> ZoneBins { get; private set; }
	public TimeBin<TimeSpan> PaceBins { get; private set; }

	public int Runs { get; set; }

	public IDictionary<DateTime, double> HeartRateLog { get; set; }
	public IDictionary<DateTime, TimeSpan> PaceLog { get; set; }

	public IList<DateTime> StartPoints { get; set; }

	private SortedDictionary<DateTime, GpxTrackPoint> LastPoints { get; set; }

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
		LastPoints = new SortedDictionary<DateTime, GpxTrackPoint>();
		StartPoints = new List<DateTime>();
	}

	private static readonly TimeSpan AveragingPeriod = new TimeSpan(0, 0, 15);
	private static readonly TimeSpan SlowestDisplayedPace = new TimeSpan(0, 20, 0);

	public void SetStartPoint(GpxTrackPoint point)
	{
		LastPoints = new SortedDictionary<DateTime, GpxTrackPoint>();
		LastPoints[point.Time] = point;
		StartPoints.Add(point.Time);
		UpdateMaxHeartRate(point.HeartRate);
	}

	public void RecordInterval(GpxTrackPoint point)
	{
		// FIXME: This is a silly way to do moving averages. Instead, store the distance and time values in a queue and add and drop them as needed. Summing them up then becomes just summing up the stored numbers, rather than calculating the distances all over again.
		LastPoints[point.Time] = point;
		foreach (var key in LastPoints.Keys.Where(k => k < point.Time - AveragingPeriod).ToList()) {
			// Make sure at least two points remain so we can calculate the time and distance traveled:
			if (LastPoints.Keys.Count <= 2) {
				break;
			}
			LastPoints.Remove(key);
			// TODO: What about time travel? (i.e. point.Time < highest Time in LastPoints) - may be rather theoretical.
		}

		var firstPoint = LastPoints.First().Value;
		var lastPoint = point;
		var lastLessOnePoint = LastPoints.Last(pt => pt.Key < point.Time).Value;

		var dist = lastLessOnePoint.DistanceTo(lastPoint);
		var deltaT = lastLessOnePoint.TimeDifference(lastPoint);

		double averagedDistance = 0.0;
		var averageTime = new TimeSpan(0);
		var times = LastPoints.Keys.ToArray();
		var prevPoint = firstPoint;
		for (var i = 1; i < LastPoints.Count; i++) {
			var t = times[i];
			averagedDistance += prevPoint.DistanceTo(LastPoints[t]);
			averageTime += prevPoint.TimeDifference(LastPoints[t]);
			prevPoint = LastPoints[t];
		}

		HeartRateLog[StartTime + TotalTime] = lastPoint.HeartRate;
		ZoneBins.Record(lastPoint.HeartRate, deltaT);
		var averagedPace = new TimeSpan((long) (1000.0D * averageTime.Ticks / averagedDistance));
		PaceBins.Record(averagedPace, deltaT);
		if (averagedDistance > 0.0 && averagedPace < SlowestDisplayedPace) {
			PaceLog[StartTime + TotalTime] = averagedPace;
		} else {
			PaceLog[StartTime + TotalTime] = new TimeSpan(0);
		}
		TotalHeartbeats += lastPoint.HeartRate * deltaT.TotalMinutes;
		UpdateMaxHeartRate(lastPoint.HeartRate);
		// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute?
		TotalSteps += 2.0D * lastPoint.Cadence * deltaT.TotalMinutes;
		TotalDistance += dist;
		TotalTime += deltaT;
	}

	public void UpdateMaxHeartRate(double heartRate)
	{
		if (heartRate > MaxHeartRate)
			MaxHeartRate = heartRate;
	}
}