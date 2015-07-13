using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Time
{
	public class HeartRateTimeChart : TimeChartBase
	{
		public HeartRateTimeChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_hr", stats)
		{
		}

		private static readonly OxyColor[][] Palette = {
			new[] { OxyColors.White }, // no zones defined
			new[] { OxyColors.Green, OxyColors.Red }, // 1 zone boundary
			new[] { OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 2 zone boundaries
			new[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 3 zone boundaries
			new[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Orange, OxyColors.Red } // 4 zone boundaries
		};

		public override void Draw()
		{
			if (!Stats.HeartRateLog.Any()) {
				return;
			}
			var series = new LineSeries { Title = "Heart Rate", Color = OxyColors.Red, Smooth = false };
			var yAxis = new RangeColorAxis {
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0,
				ExtraGridlineColor = OxyColors.Magenta,
				ExtraGridlineStyle = LineStyle.Solid,
				ExtraGridlineThickness = 1.5,
			};
			var minHr = double.MaxValue;
			var maxHr = double.MinValue;
			foreach (var time in Stats.HeartRateLog.Keys) {
				var hr = Stats.HeartRateLog[time];
				series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), hr));
				if (hr < minHr) {
					minHr = hr;
				}
				if (hr > maxHr) {
					maxHr = hr;
				}
			}
			yAxis.Minimum = minHr;
			yAxis.Maximum = maxHr;
			yAxis.ExtraGridlines = Settings.HeartRateZones;
			var paletteIndex = Settings.HeartRateZones.Count();
			var lower = 0.0;
			var index = 0;
			foreach (var zoneLimit in Settings.HeartRateZones) {
				yAxis.AddRange(lower, zoneLimit, Palette[paletteIndex][index]);
				lower = zoneLimit;
				index++;
			}
			yAxis.AddRange(lower, double.MaxValue, Palette[paletteIndex][index]);
			Chart.Axes.Add(yAxis);
			Chart.Series.Add(series);
		}
	}
}