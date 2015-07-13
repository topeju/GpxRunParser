using System;
using Newtonsoft.Json;

namespace GpxRunParser
{
	public class AggregateStatistics
	{
		#region Actual properties
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
		}
	}
}
