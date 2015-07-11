using System.Drawing;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace GpxRunParser.Charts.Time
{
	public abstract class TimeChartBase
	{
		protected PlotModel _chart;
		protected Axis _xAxis;
		protected string _baseFileName;
		protected RunStatistics _stats;

		public TimeChartBase(string baseBaseFileName, RunStatistics stats)
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
			foreach (var pause in _stats.Pauses) {
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseStart.Time), 0.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseStart.Time), 1.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseEnd.Time), 1.0));
				pauseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(pause.PauseEnd.Time), 0.0));
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
}