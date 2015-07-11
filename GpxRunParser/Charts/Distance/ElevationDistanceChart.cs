using System;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Distance
{
	public class ElevationDistanceChart : DistanceChartBase
	{
		public ElevationDistanceChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_elev", stats)
		{
		}

		public override void Draw()
		{
			if (!_stats.ElevationLog.Any()) {
				return;
			}
			var elevationSeries = new LineSeries { Title = "Elevation", Color = OxyColors.Brown, YAxisKey = "Elevation", Smooth = false };
			var slopeSeries = new LineSeries { Title = "Slope", Color = OxyColors.Blue, YAxisKey = "Slope", Smooth = false };
			var elevationAxis = new LinearAxis {
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0,
				Key = "Elevation"
			};
			var slopeAxis = new LinearAxis {
				Position = AxisPosition.Right,
				Key = "Slope"
			};
			var minimumElevation = double.MaxValue;
			var maximumElevation = double.MinValue;
			var minimumSlope = double.MaxValue;
			var maximumSlope = double.MinValue;
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.ElevationLog.ContainsKey(time)) {
					continue;
				}
				var elev = _stats.ElevationLog[time];
				elevationSeries.Points.Add(new DataPoint(_stats.DistanceLog[time], elev));
				if (elev < minimumElevation) {
					minimumElevation = elev;
				}
				if (elev > maximumElevation) {
					maximumElevation = elev;
				}
				var slope = _stats.SlopeLog[time];
				slopeSeries.Points.Add(new DataPoint(_stats.DistanceLog[time], slope));
				if (slope < minimumSlope) {
					minimumSlope = slope;
				}
				if (slope > maximumSlope) {
					maximumSlope = slope;
				}
			}
			elevationAxis.Minimum = minimumElevation;
			elevationAxis.Maximum = maximumElevation;
			slopeAxis.Minimum = Math.Max(minimumSlope, -60.0D);
			slopeAxis.Maximum = Math.Min(maximumSlope, 60.0D);
			_chart.Axes.Add(slopeAxis);
			_chart.Series.Add(slopeSeries);
			_chart.Axes.Add(elevationAxis);
			_chart.Series.Add(elevationSeries);
		}
	}
}
