using System;

namespace GpxRunParser
{
	public class RunInfo
	{
		public string FileName { get; set; }
		public DateTime StartTime { get; private set; }
		public TimeSpan Duration { get; set; }
		public double DistanceInKm { get; set; }

		public RunInfo(string fileName, RunStatistics stats)
		{
			FileName = fileName;
			StartTime = stats.StartTime;
			Duration = stats.TotalTime;
			DistanceInKm = stats.TotalDistanceInKm;
		}
	}
}
