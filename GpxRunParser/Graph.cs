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

		public override void Draw()
		{
			if (!_stats.HeartRateLog.Any()) {
				return;
			}
			var series = new LineSeries();
			series.Title = "Heart Rate";
			series.Color = OxyColors.Red;
			var yAxis = new LinearAxis();
			yAxis.Position = AxisPosition.Left;
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
			var series = new LineSeries { Title = "Pace", Color = OxyColors.Blue };
			var yAxis = new TimeSpanAxis {
				Position = AxisPosition.Left,
				Key = "Primary",
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1.0,
				MinorGridlineStyle = LineStyle.Dot,
				MinorGridlineThickness = 1.0,
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
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}
}
