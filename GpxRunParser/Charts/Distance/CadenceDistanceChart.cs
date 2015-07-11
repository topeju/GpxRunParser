using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Distance
{
	public class CadenceDistanceChart : DistanceChartBase
	{
		public CadenceDistanceChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_cad", stats)
		{
		}

		public override void Draw()
		{
			if (!_stats.CadenceLog.Any()) {
				return;
			}
			var series = new LineSeries();
			series.Title = "Cadence";
			series.Color = OxyColors.Green;
			series.Smooth = false;
			var yAxis = new LinearAxis();
			yAxis.Position = AxisPosition.Left;
			var minCadence = double.MaxValue;
			var maxCadence = double.MinValue;
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.CadenceLog.ContainsKey(time)) {
					continue;
				}
				var cadence = _stats.CadenceLog[time];
				series.Points.Add(new DataPoint(_stats.DistanceLog[time], cadence));
				if (cadence < minCadence) {
					minCadence = cadence;
				}
				if (cadence > maxCadence) {
					maxCadence = cadence;
				}
			}
			yAxis.Minimum = minCadence;
			yAxis.Maximum = maxCadence;
			yAxis.MajorGridlineStyle = LineStyle.Solid;
			yAxis.MajorGridlineThickness = 1.0;
			yAxis.MinorGridlineStyle = LineStyle.Dot;
			yAxis.MinorGridlineThickness = 1.0;
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}
}