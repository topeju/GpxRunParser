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

			_xAxis = new DateTimeAxis();
			_xAxis.Position = AxisPosition.Bottom;
			_xAxis.Minimum = DateTimeAxis.ToDouble(_stats.StartTime);
			_xAxis.Maximum = DateTimeAxis.ToDouble(_stats.StartTime + _stats.TotalTime);
			_xAxis.StringFormat = "HH:mm";
			_chart.Axes.Add(_xAxis);
			_chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		public void SavePng()
		{
			PngExporter.Export(_chart, _baseFileName + ".png", 600, 300, Brushes.White);
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
			_chart.Axes.Add(yAxis);
			_chart.Series.Add(series);
		}
	}
}
