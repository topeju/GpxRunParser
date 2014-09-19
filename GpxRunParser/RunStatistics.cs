using System;
using System.Collections;
using System.Collections.Generic;
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
	}

	private readonly TimeSpan slowestDisplayedPace = new TimeSpan(0, 20, 0);

	public void RecordInterval(TimeSpan duration, double distance, double heartRate, double cadence)
	{
		HeartRateLog[StartTime + TotalTime] = heartRate;
		ZoneBins.Record(heartRate, duration);
		var pace = new TimeSpan((long) (1000.0D * duration.Ticks / distance));
		PaceBins.Record(pace, duration);
		if (distance > 0.0 && pace < slowestDisplayedPace) {
			PaceLog[StartTime + TotalTime] = pace;
		} else {
			PaceLog[StartTime + TotalTime] = new TimeSpan(0);
		}
		TotalHeartbeats += heartRate * duration.TotalMinutes;
		UpdateMaxHeartRate(heartRate);
		// Cadence is number of full cycles per minute by the pair of feet, thus there are two steps per cadence per minute?
		TotalSteps += 2.0D * cadence * duration.TotalMinutes;
		TotalDistance += distance;
		TotalTime += duration;
	}

	public void UpdateMaxHeartRate(double heartRate)
	{
		if (heartRate > MaxHeartRate)
			MaxHeartRate = heartRate;
	}
}