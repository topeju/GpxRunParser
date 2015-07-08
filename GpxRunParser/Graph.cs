using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System.IO;

namespace GpxRunParser
{
	public abstract class GraphBase
	{
		protected PlotModel _chart;
		protected Axis _xAxis;
		protected string _baseFileName;
		protected RunStatistics _stats;

		public GraphBase(string baseBaseFileName, RunStatistics stats)
		{
			_baseFileName = baseBaseFileName;
			_stats = stats;

			_chart = new PlotModel();

			_xAxis = new DateTimeAxis {
				Position = AxisPosition.Bottom,
				Minimum = DateTimeAxis.ToDouble(_stats.StartTime),
				Maximum = DateTimeAxis.ToDouble(_stats.EndTime),
				StringFormat = "HH:mm"
			};
			_chart.Axes.Add(_xAxis);
			_chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		private void AddPauseSeries()
		{
			var pauseSeries = new AreaSeries { Title = "Paused", Color = OxyColors.DimGray, LineStyle = LineStyle.None, YAxisKey = "Secondary" };
			var secondYAxis = new LinearAxis { Position = AxisPosition.Right, Minimum = 0.0, Maximum = 1.0, Key = "Secondary", IsAxisVisible = false };
			var startPointIndex = 0;
			var endPointIndex = 0;
			var paused = false;
			var numStartPoints = _stats.StartPoints.Count;
			var numEndPoints = _stats.EndPoints.Count;
			while (startPointIndex < numStartPoints && endPointIndex < numEndPoints) {
				if (_stats.StartPoints[startPointIndex] > _stats.EndPoints[endPointIndex]) {
					if (!paused) {
						pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_stats.EndPoints[endPointIndex]), 0.0));
						pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_stats.EndPoints[endPointIndex]), 1.0));
					}
					endPointIndex++;
					paused = true;
				}
				if (endPointIndex >= numEndPoints || _stats.StartPoints[startPointIndex] < _stats.EndPoints[endPointIndex]) {
					if (paused) {
						pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_stats.StartPoints[startPointIndex]), 1.0));
						pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(_stats.StartPoints[startPointIndex]), 0.0));
					}
					startPointIndex++;
					paused = false;
				}
			}
			_chart.Axes.Add(secondYAxis);
			_chart.Series.Add(pauseSeries);
		}

		public void SavePng()
		{
			AddPauseSeries(); // Needs to be added last so it shows up in the background
			PngExporter.Export(_chart, _baseFileName + ".png", 900, 500, Brushes.White);
		}

	}

	public class HeartRateChart : GraphBase
	{
		public HeartRateChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_hr", stats)
		{
		}

		private static readonly OxyColor[][] palette = new OxyColor[][] { 
			new OxyColor[] { OxyColors.White }, // no zones defined
			new OxyColor[] { OxyColors.Green, OxyColors.Red }, // 1 zone boundary
			new OxyColor[] { OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 2 zone boundaries
			new OxyColor[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 3 zone boundaries
			new OxyColor[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Orange, OxyColors.Red } // 4 zone boundaries
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
			double minHr = double.MaxValue;
			double maxHr = double.MinValue;
			foreach (var time in _stats.HeartRateLog.Keys) {
				var hr = _stats.HeartRateLog[time];
				series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), hr));
				if (hr < minHr)
					minHr = hr;
				if (hr > maxHr)
					maxHr = hr;
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

	public class CadenceChart : GraphBase
	{
		public CadenceChart(string baseFileName, RunStatistics stats)
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
			double minCadence = double.MaxValue;
			double maxCadence = double.MinValue;
			foreach (var time in _stats.HeartRateLog.Keys) {
				var cadence = _stats.CadenceLog[time];
				series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), cadence));
				if (cadence < minCadence)
					minCadence = cadence;
				if (cadence > maxCadence)
					maxCadence = cadence;
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

	public class PaceChart : GraphBase
	{
		public PaceChart(string baseFileName, RunStatistics stats)
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
			foreach (var time in _stats.PaceLog.Keys) {
				var pace = _stats.PaceLog[time];
				if (pace < slowestDisplayedPace) {
					series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), TimeSpanAxis.ToDouble(pace)));
					if (pace > slowestPace)
						slowestPace = pace;
					if (pace < fastestPace)
						fastestPace = pace;
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

	public class ElevationChart : GraphBase
	{
		public ElevationChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_elev", stats)
		{
		}

		public override void Draw()
		{
			if (!_stats.ElevationLog.Any()) {
				return;
			}
			var series = new LineSeries { Title = "Elevation", Color = OxyColors.Brown, Smooth = false };
			var yAxis = new LinearAxis {
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0
			};
			var minimumElevation = double.MaxValue;
			var maximumElevation = double.MinValue;
			foreach (var time in _stats.ElevationLog.Keys) {
				var elev = _stats.ElevationLog[time];
				series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), elev));
				if (elev < minimumElevation)
					minimumElevation = elev;
				if (elev > maximumElevation)
					maximumElevation = elev;
			}
			yAxis.Minimum = minimumElevation;
			yAxis.Maximum = maximumElevation;
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}

	public abstract class DistanceGraphBase
	{
		protected PlotModel _chart;
		protected Axis _xAxis;
		protected string _baseFileName;
		protected RunStatistics _stats;

		public DistanceGraphBase(string baseBaseFileName, RunStatistics stats)
		{
			_baseFileName = baseBaseFileName;
			_stats = stats;

			_chart = new PlotModel();

			_xAxis = new LinearAxis {
				Position = AxisPosition.Bottom,
				Minimum = 0,
				Maximum = _stats.TotalDistanceInKm
			};
			_chart.Axes.Add(_xAxis);
			_chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		public void SavePng()
		{
			PngExporter.Export(_chart, _baseFileName + "_dist.png", 900, 500, Brushes.White);
		}

	}

	public class HeartRateDistanceChart : DistanceGraphBase
	{
		public HeartRateDistanceChart(string baseFileName, RunStatistics stats)
			: base(baseFileName + "_hr", stats)
		{
		}

		private static readonly OxyColor[][] palette = new OxyColor[][] { 
			new OxyColor[] { OxyColors.White }, // no zones defined
			new OxyColor[] { OxyColors.Green, OxyColors.Red }, // 1 zone boundary
			new OxyColor[] { OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 2 zone boundaries
			new OxyColor[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Red }, // 3 zone boundaries
			new OxyColor[] { OxyColors.LightGray, OxyColors.Green, OxyColors.Yellow, OxyColors.Orange, OxyColors.Red } // 4 zone boundaries
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
			double minHr = double.MaxValue;
			double maxHr = double.MinValue;
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.HeartRateLog.ContainsKey(time))
					continue;
				var hr = _stats.HeartRateLog[time];
				series.Points.Add(new DataPoint(_stats.DistanceLog[time], hr));
				if (hr < minHr)
					minHr = hr;
				if (hr > maxHr)
					maxHr = hr;
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

	public class CadenceDistanceChart : DistanceGraphBase
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
			double minCadence = double.MaxValue;
			double maxCadence = double.MinValue;
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.CadenceLog.ContainsKey(time))
					continue;
				var cadence = _stats.CadenceLog[time];
				series.Points.Add(new DataPoint(_stats.DistanceLog[time], cadence));
				if (cadence < minCadence)
					minCadence = cadence;
				if (cadence > maxCadence)
					maxCadence = cadence;
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

	public class PaceDistanceChart : DistanceGraphBase
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
				if (!_stats.PaceLog.ContainsKey(time))
					continue;
				var pace = _stats.PaceLog[time];
				if (pace < slowestDisplayedPace) {
					series.Points.Add(new DataPoint(_stats.DistanceLog[time], TimeSpanAxis.ToDouble(pace)));
					if (pace > slowestPace)
						slowestPace = pace;
					if (pace < fastestPace)
						fastestPace = pace;
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

	public class ElevationDistanceChart : DistanceGraphBase
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
			var series = new LineSeries { Title = "Elevation", Color = OxyColors.Brown, Smooth = false };
			var yAxis = new LinearAxis {
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0
			};
			var minimumElevation = double.MaxValue;
			var maximumElevation = double.MinValue;
			foreach (var time in _stats.DistanceLog.Keys) {
				if (!_stats.ElevationLog.ContainsKey(time))
					continue;
				var elev = _stats.ElevationLog[time];
				series.Points.Add(new DataPoint(_stats.DistanceLog[time], elev));
				if (elev < minimumElevation)
					minimumElevation = elev;
				if (elev > maximumElevation)
					maximumElevation = elev;
			}
			yAxis.Minimum = minimumElevation;
			yAxis.Maximum = maximumElevation;
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}

}
