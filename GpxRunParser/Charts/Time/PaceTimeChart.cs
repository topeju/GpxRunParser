using System;
using System.Configuration;
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
			var slowestPace = ConfigurationManager.AppSettings["SlowestDisplayedPace"].Split(':');
			var slowH = int.Parse(slowestPace[0]);
			var slowM = int.Parse(slowestPace[1]);
			var slowS = double.Parse(slowestPace[2]);
			_slowestDisplayedPace = new TimeSpan(0, slowH, slowM, (int)slowS, (int)((slowS - (int)slowS) * 1000.0D));
		}

		private readonly TimeSpan _slowestDisplayedPace;

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
				if (pace < _slowestDisplayedPace) {
					series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), TimeSpanAxis.ToDouble(pace)));
					if (pace > slowestPace) {
						slowestPace = pace;
					}
					if (pace < fastestPace) {
						fastestPace = pace;
					}
				}
			}
			if (slowestPace > _slowestDisplayedPace) {
				slowestPace = _slowestDisplayedPace;
			}
			yAxis.Minimum = TimeSpanAxis.ToDouble(fastestPace);
			yAxis.Maximum = TimeSpanAxis.ToDouble(slowestPace);
			yAxis.ExtraGridlines = Stats.PaceBins.Bins.Select(TimeSpanAxis.ToDouble).ToArray();
			Chart.Axes.Add(yAxis);
			Chart.Series.Add(series);
		}
	}
}