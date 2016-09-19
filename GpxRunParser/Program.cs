using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RazorEngine;
using RazorEngine.Compilation.ImpromptuInterface;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using WebMarkupMin.Core.Minifiers;
using NDesk.Options;

namespace GpxRunParser
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			bool ignoreCache = false;
			var options = new OptionSet {
				{ "i|ignore", v => ignoreCache = true }
			};
			var folders = options.Parse(args);
			
			if (folders.Count == 0) {
				folders = new List<string> { "." };
			}

			AnalysisCache cache;
			if (ignoreCache) {
				cache = new AnalysisCache();
			} else {
				cache = new AnalysisCache(Settings.RunStatsCacheFile);
			}

			var analyzer = new RunAnalyzer(cache);

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

			foreach (var dirName in folders) {
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
                        viewBag.AddValue("ExerciseTitle", Settings.ExerciseTitle);
                        viewBag.AddValue("DisplayPace", Settings.DisplayPace);
                        viewBag.AddValue("DisplaySpeed", Settings.DisplaySpeed);
                        string page;
						if (!individualRunCompiled) {
							page = Engine.Razor.RunCompile(pageTemplate, "IndividualRun", typeof(RunStatistics), runStats, viewBag);
							individualRunCompiled = true;
						} else {
							page = Engine.Razor.Run("IndividualRun", typeof(RunStatistics), runStats, viewBag);
						}
						page = MinifyPage(page, fileName);
						using (var output = File.CreateText(outputFileName)) {
							output.Write(page);
						}
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
				viewBag.AddValue("DisplayPace", Settings.DisplayPace);
				viewBag.AddValue("DisplaySpeed", Settings.DisplaySpeed);
				string page;
				if (!monthlyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "MonthlyStatistics", typeof(AggregateStatistics), analyzer.MonthlyStats[month], viewBag);
					monthlyCompiled = true;
				} else {
					page = Engine.Razor.Run("MonthlyStatistics", typeof(AggregateStatistics), analyzer.MonthlyStats[month], viewBag);
				}
				page = MinifyPage(page, String.Format("the monthly report {0:yyyy-MM}", month));
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
				viewBag.AddValue("DisplayPace", Settings.DisplayPace);
				viewBag.AddValue("DisplaySpeed", Settings.DisplaySpeed);
				string page;
				if (!weeklyCompiled) {
					page = Engine.Razor.RunCompile(pageTemplate, "WeeklyStatistics", typeof(AggregateStatistics), analyzer.WeeklyStats[week], viewBag);
					weeklyCompiled = true;
				} else {
					page = Engine.Razor.Run("WeeklyStatistics", typeof(AggregateStatistics), analyzer.WeeklyStats[week], viewBag);
				}
				page = MinifyPage(page, String.Format("weekly report {0:D2}/{1:yyyy}",
					calendar.GetWeekOfYear(week, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday), week));
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
				indexPage = MinifyPage(indexPage, "the index page");
				using (var output = File.CreateText("Index.html")) {
					output.Write(indexPage);
				}
			}

			cache.SaveCache(Settings.RunStatsCacheFile);
		}

		private static readonly HtmlMinifier _minifier = new HtmlMinifier();

		private static string MinifyPage(string page, string fileName)
		{
#if !DEBUG
			if (Settings.MinifyHtmlFiles) {
				var minifyResult = _minifier.Minify(page);
				if (minifyResult.Errors.Any()) {
					foreach (var error in minifyResult.Errors) {
						Console.Error.WriteLine("Error minifying the HTML result for {0}: Line {1}, column {2}: {3}", fileName, error.LineNumber,
												error.ColumnNumber, error.Message);
					}
				} else {
					page = minifyResult.MinifiedContent;
				}
			}
#endif
			return page;
		}
	}
}
