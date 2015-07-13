using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GpxRunParser.Charts.Distance;
using GpxRunParser.Charts.Time;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace GpxRunParser
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length == 0) {
				Console.Error.WriteLine("Input directory/directories not specified");
				return;
			}

			var analyzer = new RunAnalyzer(Settings.HeartRateZones, Settings.PaceBins);

			var assembly = Assembly.GetExecutingAssembly();
			var pageTemplate = "";
			bool individualRunCompiled = false;
			bool monthlyCompiled = false;
			bool weeklyCompiled = false;

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.IndividualRun.cshtml")) {
				using (var reader = new StreamReader(stream)) {
					pageTemplate = reader.ReadToEnd();
				}
			}

			// Workaround for https://github.com/Antaris/RazorEngine/issues/244 - note that this is not the recommended method!
			var config = new TemplateServiceConfiguration {
				DisableTempFileLocking = true,
				CachingProvider = new DefaultCachingProvider(t => { }),
				//Debug = true
			};
			Engine.Razor = RazorEngineService.Create(config);

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);

			var runs = new List<RunInfo>();

			foreach (var dirName in args) {
				var gpxFiles = Directory.EnumerateFiles(dirName, Settings.FilePattern);

				foreach (var fileName in gpxFiles) {
					RunStatistics runStats;
					var newResults = analyzer.Analyze(fileName, out runStats);
					var baseFileName = extRegexp.Replace(fileName, "");
					var outputFileName = baseFileName + ".html";
					runs.Add(new RunInfo(outputFileName, runStats));

					if (newResults) {
						var viewBag = new DynamicViewBag();
						viewBag.AddValue("FileName", baseFileName);
						viewBag.AddValue("HeartRateZones", Settings.HeartRateZones);
						viewBag.AddValue("PaceBins", Settings.PaceBins);
						viewBag.AddValue("SlowestDisplayedPace", Settings.SlowestDisplayedPace);
						string page;
						if (!individualRunCompiled) {
							page = Engine.Razor.RunCompile(pageTemplate, "IndividualRun", typeof(RunStatistics), runStats, viewBag);
							individualRunCompiled = true;
						} else {
							page = Engine.Razor.Run("IndividualRun", typeof(RunStatistics), runStats, viewBag);
						}
						using (var output = File.CreateText(outputFileName)) {
							output.Write(page);
						}

						var hrChart = new HeartRateTimeChart(baseFileName, runStats);
						hrChart.Draw();
						hrChart.SavePng();

						var paceChart = new PaceTimeChart(baseFileName, runStats);
						paceChart.Draw();
						paceChart.SavePng();

						var cadenceChart = new CadenceTimeChart(baseFileName, runStats);
						cadenceChart.Draw();
						cadenceChart.SavePng();

						var elevationChart = new ElevationTimeChart(baseFileName, runStats);
						elevationChart.Draw();
						elevationChart.SavePng();

						var hrDistChart = new HeartRateDistanceChart(baseFileName, runStats);
						hrDistChart.Draw();
						hrDistChart.SavePng();

						var paceDistChart = new PaceDistanceChart(baseFileName, runStats);
						paceDistChart.Draw();
						paceDistChart.SavePng();

						var cadenceDistChart = new CadenceDistanceChart(baseFileName, runStats);
						cadenceDistChart.Draw();
						cadenceDistChart.SavePng();

						var elevationDistChart = new ElevationDistanceChart(baseFileName, runStats);
						elevationDistChart.Draw();
						elevationDistChart.SavePng();
					}
				}
			}

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.MonthlyStatistics.cshtml")) {
				using (var reader = new StreamReader(stream)) {
					pageTemplate = reader.ReadToEnd();
				}
			}

			foreach (var month in analyzer.MonthlyStats.Keys) {
				var outputFileName = String.Format("Monthly-{0:yyyy-MM}.html", month);
				var viewBag = new DynamicViewBag();
				viewBag.AddValue("HeartRateZones", Settings.HeartRateZones);
				viewBag.AddValue("PaceBins", Settings.PaceBins);
				viewBag.AddValue("SlowestDisplayedPace", Settings.SlowestDisplayedPace);
				string page;
				if (!monthlyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "MonthlyStatistics", typeof(AggregateStatistics), analyzer.MonthlyStats[month], viewBag);
					monthlyCompiled = true;
				} else {
					page = Engine.Razor.Run("MonthlyStatistics", typeof(AggregateStatistics), analyzer.MonthlyStats[month], viewBag);
				}
				using (var output = File.CreateText(outputFileName)) {
					output.Write(page);
				}
			}

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.WeeklyStatistics.cshtml")) {
				using (var reader = new StreamReader(stream)) {
					pageTemplate = reader.ReadToEnd();
				}
			}

			foreach (var week in analyzer.WeeklyStats.Keys) {
				var calendar = CultureInfo.CurrentUICulture.Calendar;
				var outputFileName = String.Format("Weekly-{0:yyyy}-{1:D2}.html",
												   week,
												   calendar.GetWeekOfYear(week, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
				var viewBag = new DynamicViewBag();
				viewBag.AddValue("HeartRateZones", Settings.HeartRateZones);
				viewBag.AddValue("PaceBins", Settings.PaceBins);
				viewBag.AddValue("SlowestDisplayedPace", Settings.SlowestDisplayedPace);
				string page;
				if (!weeklyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "WeeklyStatistics", typeof(AggregateStatistics), analyzer.WeeklyStats[week], viewBag);
					weeklyCompiled = true;
				} else {
					page = Engine.Razor.Run("WeeklyStatistics", typeof(AggregateStatistics), analyzer.WeeklyStats[week], viewBag);
				}
				using (var output = File.CreateText(outputFileName)) {
					output.Write(page);
				}
			}

			if (runs.Count > 0) {
				using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.Index.cshtml")) {
					using (var reader = new StreamReader(stream)) {
						pageTemplate = reader.ReadToEnd();
					}
				}

				var startDate = runs.Min(r => r.StartTime);
				startDate = new DateTime(startDate.Year, startDate.Month, 1);
				var endDate = runs.Max(r => r.StartTime);
				endDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1).AddDays(-1);
				var viewBag = new DynamicViewBag();
				viewBag.AddValue("StartDate", startDate);
				viewBag.AddValue("EndDate", endDate);
				var indexPage = Engine.Razor.RunCompile(pageTemplate, "Index", typeof(IList<RunInfo>), runs, viewBag);
				using (var output = File.CreateText("Index.html")) {
					output.Write(indexPage);
				}
			}

			AnalysisCache.SaveCache();
		}
	}
}
