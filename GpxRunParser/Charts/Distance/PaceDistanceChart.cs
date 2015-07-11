using System;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Distance
{
	public class PaceDistanceChart : DistanceChartBase
	{
		public PaceDistanceChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_pace", stats)
		{
		}

		private readonly TimeSpan slowestDisplayedPace = new TimeSpan(0, 12, 0);

		public override void Draw()
		{
			if (!_stats.PaceLog.Any()) {
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
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.PaceLog.ContainsKey(time)) {
					continue;
				}
				var pace = _stats.PaceLog[time];
				if (pace < slowestDisplayedPace) {
					series.Points.Add(new DataPoint(_stats.DistanceLog[time], TimeSpanAxis.ToDouble(pace)));
					if (pace > slowestPace) {
						slowestPace = pace;
					}
					if (pace < fastestPace) {
						fastestPace = pace;
					}
				}
			}
			if (slowestPace > slowestDisplayedPace) {
				slowestPace = slowestDisplayedPace;
			}
			yAxis.Minimum = TimeSpanAxis.ToDouble(fastestPace);
			yAxis.Maximum = TimeSpanAxis.ToDouble(slowestPace);
			yAxis.ExtraGridlines = _stats.PaceBins.Bins.Select(b => TimeSpanAxis.ToDouble(b)).ToArray();
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}
}