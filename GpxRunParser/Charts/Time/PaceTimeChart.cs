using System;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Time
{
	public class PaceTimeChart : TimeChartBase
	{
		public PaceTimeChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_pace", stats)
		{
		}

		public override void Draw()
		{
			if (!Stats.PaceLog.Any()) {
				return;
			}
			var series = new LineSeries { Title = "Pace", Color = OxyColors.Blue, Smooth = false };
			var yAxis = new TimeSpanAxis {
				Position = AxisPosition.Left,
				Key = "Primary",
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0,
				ExtraGridlineColor = OxyColors.Magenta,
				ExtraGridlineStyle = LineStyle.Solid,
				ExtraGridlineThickness = 1.5,
				MajorStep = TimeSpanAxis.ToDouble(TimeSpan.FromMinutes(1)),
				MinorStep = TimeSpanAxis.ToDouble(TimeSpan.FromSeconds(30)),
				StartPosition = 1,
				EndPosition = 0
			};
			var slowestPace = TimeSpan.MinValue;
			var fastestPace = TimeSpan.MaxValue;
			foreach (var time in Stats.PaceLog.Keys) {
				var pace = Stats.PaceLog[time];
				if (pace < Settings.SlowestDisplayedPace) {
					series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), TimeSpanAxis.ToDouble(pace)));
					if (pace > slowestPace) {
						slowestPace = pace;
					}
					if (pace < fastestPace) {
						fastestPace = pace;
					}
				}
			}
			if (slowestPace > Settings.SlowestDisplayedPace) {
				slowestPace = Settings.SlowestDisplayedPace;
			}
			yAxis.Minimum = TimeSpanAxis.ToDouble(fastestPace);
			yAxis.Maximum = TimeSpanAxis.ToDouble(slowestPace);
			yAxis.ExtraGridlines = Stats.PaceBins.Bins.Select(TimeSpanAxis.ToDouble).ToArray();
			Chart.Axes.Add(yAxis);
			Chart.Series.Add(series);
		}
	}
}
