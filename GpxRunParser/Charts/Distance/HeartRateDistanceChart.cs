using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Distance
{
	public class HeartRateDistanceChart : DistanceChartBase
	{
		public HeartRateDistanceChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_hr", stats)
		{
		}

		private static readonly OxyColor[][] palette = {
			new[] { OxyColors.White }, // no zones defined
			new[] { OxyColors.Green, OxyColors.Red }, // 1 zone boundary
			new[] { OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 2 zone boundaries
			new[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 3 zone boundaries
			new[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Orange, OxyColors.Red } // 4 zone boundaries
		};

		public override void Draw()
		{
			if (!_stats.HeartRateLog.Any()) {
				return;
			}
			var series = new LineSeries();
			series.Title = "Heart Rate";
			series.Color = OxyColors.Red;
			series.Smooth = false;
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
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.HeartRateLog.ContainsKey(time)) {
					continue;
				}
				var hr = _stats.HeartRateLog[time];
				series.Points.Add(new DataPoint(_stats.DistanceLog[time], hr));
				if (hr < minHr) {
					minHr = hr;
				}
				if (hr > maxHr) {
					maxHr = hr;
				}
			}
			yAxis.Minimum = minHr;
			yAxis.Maximum = maxHr;
			yAxis.ExtraGridlines = _stats.ZoneBins.Bins.ToArray();
			var paletteIndex = _stats.ZoneBins.Bins.Count();
			var lower = 0.0;
			var index = 0;
			foreach (var zoneLimit in _stats.ZoneBins.Bins) {
				yAxis.AddRange(lower, zoneLimit, palette[paletteIndex][index]);
				lower = zoneLimit;
				index++;
			}
			yAxis.AddRange(lower, double.MaxValue, palette[paletteIndex][index]);
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}
}