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

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);

			var runs = new List<RunInfo>();

			foreach (var dirName in args) {
				var gpxFiles = Directory.EnumerateFiles(dirName, Settings.FilePattern);

				foreach (var fileName in gpxFiles) {
					var runStats = analyzer.Analyze(fileName);

					var baseFileName = extRegexp.Replace(fileName, "");
					var outputFileName = baseFileName + ".html";
					var viewBag = new DynamicViewBag();
					viewBag.AddValue("FileName", baseFileName);
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

					runs.Add(new RunInfo(outputFileName, runStats));

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

			if (!monthlyCompiled) {
				using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.MonthlyStatistics.cshtml")) {
					using (var reader = new StreamReader(stream)) {
						pageTemplate = reader.ReadToEnd();
					}
				}
			}

			foreach (var month in analyzer.MonthlyStats.Keys) {
				var outputFileName = String.Format("Monthly-{0:yyyy-MM}.html", month);
				string page;
				if (!monthlyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "MonthlyStatistics", typeof(RunStatistics), analyzer.MonthlyStats[month]);
					monthlyCompiled = true;
				} else {
					page = Engine.Razor.Run("MonthlyStatistics", typeof(RunStatistics), analyzer.MonthlyStats[month]);
				}
				using (var output = File.CreateText(outputFileName)) {
					output.Write(page);
				}
			}

			if (!weeklyCompiled) {
				using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.WeeklyStatistics.cshtml")) {
					using (var reader = new StreamReader(stream)) {
						pageTemplate = reader.ReadToEnd();
					}
				}
			}

			foreach (var week in analyzer.WeeklyStats.Keys) {
				var calendar = CultureInfo.CurrentUICulture.Calendar;
				var outputFileName = String.Format("Weekly-{0:yyyy}-{1:D2}.html",
												   week,
												   calendar.GetWeekOfYear(week, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
				string page;
				if (!weeklyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "WeeklyStatistics", typeof(RunStatistics), analyzer.WeeklyStats[week]);
					weeklyCompiled = true;
				} else {
					page = Engine.Razor.Run("WeeklyStatistics", typeof(RunStatistics), analyzer.WeeklyStats[week]);
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

				var indexViewBag = new DynamicViewBag();
				var startDate = runs.Min(r => r.StartTime);
				startDate = new DateTime(startDate.Year, startDate.Month, 1);
				var endDate = runs.Max(r => r.StartTime);
				endDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1).AddDays(-1);
				indexViewBag.AddValue("StartDate", startDate);
				indexViewBag.AddValue("EndDate", endDate);
				var indexPage = Engine.Razor.RunCompile(pageTemplate, "Index", typeof(IList<RunInfo>), runs, indexViewBag);
				using (var output = File.CreateText("Index.html")) {
					output.Write(indexPage);
				}
			}
		}
	}
}
