using System.Drawing;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;

namespace GpxRunParser.Charts.Distance
{
	public abstract class DistanceChartBase
	{
		protected PlotModel _chart;
		protected Axis _xAxis;
		protected string _baseFileName;
		protected RunStatistics _stats;

		public DistanceChartBase(string baseBaseFileName, RunStatistics stats)
		{
			_baseFileName = baseBaseFileName;
			_stats = stats;

			_chart = new PlotModel();

			_xAxis = new LinearAxis {
				Position = AxisPosition.Bottom,
				Minimum = 0,
				Maximum = _stats.TotalDistanceInKm,
				ExtraGridlineStyle = LineStyle.Solid,
				ExtraGridlineColor = OxyColors.DimGray
			};
			_xAxis.ExtraGridlines = _stats.Pauses.Select(p => _stats.DistanceLog[p.PauseEnd.Time]).ToArray();
			_chart.Axes.Add(_xAxis);
			_chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		public void SavePng()
		{
			PngExporter.Export(_chart, _baseFileName + "_dist.png", 900, 500, Brushes.White);
		}
	}
}