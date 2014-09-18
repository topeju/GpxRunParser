using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace GpxRunParser
{
	public abstract class GraphBase
	{
		protected Chart _chart;
		protected ChartArea _chartArea;
		protected string _baseFileName;
		protected RunStatistics _stats;

		public GraphBase(string baseBaseFileName, RunStatistics stats)
		{
			_baseFileName = baseBaseFileName;
			_stats = stats;

			_chart = new Chart();

			_chart.Size = new Size(600, 300);

			_chartArea = new ChartArea();
			_chartArea.AxisX.LabelStyle.Format = "HH:mm";
			_chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
			_chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
			_chartArea.AxisX.LabelStyle.Font = new Font(FontFamily.GenericSansSerif, 8);
			_chartArea.AxisY.LabelStyle.Font = new Font(FontFamily.GenericSansSerif, 8);
			_chart.ChartAreas.Add(_chartArea);
		}

		public abstract void Draw();

		public void SavePng()
		{
			_chart.Invalidate();
			_chart.SaveImage(_baseFileName + ".png", ChartImageFormat.Png);
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
			var series = new Series();
			series.Name = "Heart Rate";
			series.ChartType = SeriesChartType.Line;
			series.XAxisType = AxisType.Primary;
			series.XValueType = ChartValueType.DateTime;
			double minHr = double.MaxValue;
			double maxHr = double.MinValue;
			foreach (var time in _stats.HeartRateLog.Keys) {
				var hr = _stats.HeartRateLog[time];
				series.Points.AddXY(time, hr);
				if (hr < minHr)
					minHr = hr;
				if (hr > maxHr)
					maxHr = hr;
			}
			_chartArea.AxisY.Crossing = Double.MinValue;
			_chartArea.AxisY.Minimum = minHr;
			_chartArea.AxisY.Maximum = maxHr;
			_chart.Series.Add(series);
		}
	}
}
