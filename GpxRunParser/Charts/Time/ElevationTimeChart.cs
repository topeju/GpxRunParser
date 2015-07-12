using System;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace GpxRunParser.Charts.Time
{
	public class ElevationTimeChart : TimeChartBase
	{
		public ElevationTimeChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_elev", stats)
		{
		}

		public override void Draw()
		{
			if (!Stats.ElevationLog.Any()) {
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
			foreach (var time in Stats.ElevationLog.Keys) {
				var elev = Stats.ElevationLog[time];
				elevationSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), elev));
				if (elev < minimumElevation) {
					minimumElevation = elev;
				}
				if (elev > maximumElevation) {
					maximumElevation = elev;
				}
				var slope = Stats.SlopeLog[time];
				slopeSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), slope));
				if (slope < minimumSlope) {
					minimumSlope = slope;
				}
				if (slope > maximumSlope) {
					maximumSlope = slope;
				}
			}
			elevationAxis.Minimum = minimumElevation;
			elevationAxis.Maximum = maximumElevation;
			slopeAxis.Minimum = Math.Max(minimumSlope, -Settings.MaximumDisplayedSlope);
			slopeAxis.Maximum = Math.Min(maximumSlope, Settings.MaximumDisplayedSlope);
			Chart.Axes.Add(slopeAxis);
			Chart.Series.Add(slopeSeries);
			Chart.Axes.Add(elevationAxis);
			Chart.Series.Add(elevationSeries);
		}
	}
}
