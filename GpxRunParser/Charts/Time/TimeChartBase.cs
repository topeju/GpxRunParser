using System.Drawing;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace GpxRunParser.Charts.Time
{
	public abstract class TimeChartBase
	{
		protected readonly PlotModel Chart;
		protected readonly Axis XAxis;
		protected readonly string BaseFileName;
		protected readonly RunStatistics Stats;

		protected TimeChartBase(string baseBaseFileName, RunStatistics stats)
		{
			BaseFileName = baseBaseFileName;
			Stats = stats;

			Chart = new PlotModel();

			XAxis = new DateTimeAxis {
				Position = AxisPosition.Bottom,
				Minimum = DateTimeAxis.ToDouble(Stats.StartTime),
				Maximum = DateTimeAxis.ToDouble(Stats.EndTime),
				StringFormat = "HH:mm"
			};
			Chart.Axes.Add(XAxis);
			Chart.LegendPosition = LegendPosition.BottomRight;
		}

		public abstract void Draw();

		private void AddPauseSeries()
		{
			var pauseSeries = new AreaSeries {
				Title = "Paused",
				Color = OxyColors.DimGray,
				LineStyle = LineStyle.None,
				YAxisKey = "Secondary"
			};
			var secondYAxis = new LinearAxis {
				Position = AxisPosition.Right,
				Minimum = 0.0,
				Maximum = 1.0,
				Key = "Secondary",
				IsAxisVisible = false
			};
			foreach (var pause in Stats.Pauses) {
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseStart.Time), 0.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseStart.Time), 1.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseEnd.Time), 1.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseEnd.Time), 0.0));
			}
			Chart.Axes.Add(secondYAxis);
			Chart.Series.Add(pauseSeries);
		}

		public void SavePng()
		{
			AddPauseSeries(); // Needs to be added last so it shows up in the background
			PngExporter.Export(Chart, BaseFileName + ".png", 900, 500, Brushes.White);
		}
	}
}