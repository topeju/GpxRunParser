using System.Drawing;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;

namespace GpxRunParser.Charts.Distance
{
	public abstract class DistanceChartBase
	{
		protected readonly PlotModel Chart;
		protected readonly Axis XAxis;
		protected readonly string BaseFileName;
		protected readonly RunStatistics Stats;

		public DistanceChartBase(string baseBaseFileName, RunStatistics stats)
		{
			BaseFileName = baseBaseFileName;
			Stats = stats;

			Chart = new PlotModel();

			XAxis = new LinearAxis {
				Position = AxisPosition.Bottom,
				Minimum = 0,
				Maximum = Stats.TotalDistanceInKm,
				ExtraGridlineStyle = LineStyle.Solid,
				ExtraGridlineColor = OxyColors.DimGray,
				ExtraGridlines = Stats.Pauses.Select(p => Stats.DistanceLog[p.PauseEnd.Time]).ToArray()
			};
			Chart.Axes.Add(XAxis);
			Chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		public void SavePng()
		{
			PngExporter.Export(Chart, BaseFileName + "_dist.png", 900, 500, Brushes.White);
		}
	}
}