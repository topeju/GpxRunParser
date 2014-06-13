using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NDesk.Options;
using RazorEngine;
using RazorEngine.Templating;

namespace GpxRunParser
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			var dirName = "";
			var zoneStr = "120,140,173,182";
			var paceBinStr = "8:45,7:15,6:10";
			var help = false;
			var opts = new OptionSet {
				{ "d|dir=", "Directory with GPX files to parse", s => dirName = s },
				{ "z|zones=", "Heart rate zone limits, comma-separated", s => zoneStr = s },
				{ "p|paces=", "Pace bin limits, comma-separated mm:ss", s => paceBinStr = s },
				{ "h|?|help", "Show this help text", s => help = s != null }
			};
			try {
				opts.Parse(args);
			}
			catch (OptionException e) {
				Console.Error.Write("GpxRunParser: ");
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine("Try GpxRunParser --help for more information");
				return;
			}
			if (help) {
				Console.Out.WriteLine("Usage: GpxRunParser [OPTIONS]+");
				Console.Out.WriteLine("Parses the GPX files in the directory specified using the -d option to calculate statistics on the files.");
				opts.WriteOptionDescriptions(Console.Out);
				return;
			}
			if (dirName == "") {
				Console.Error.WriteLine("Input directory not specified");
				return;
			}
			var zones = (from zone in zoneStr.Split(',')
						 select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			var paces =
				(from paceStr in paceBinStr.Split(',')
				 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1]))).ToArray();

			var analyzer = new RunAnalyzer(zones, paces);

			var assembly = Assembly.GetExecutingAssembly();
			var pageTemplate = "";

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.index.cshtml"))
			using (var reader = new StreamReader(stream))
				pageTemplate = reader.ReadToEnd();

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);
			var gpxFiles = Directory.EnumerateFiles(dirName, "*.gpx");

			foreach (var fileName in gpxFiles) {
				var runStats = analyzer.Analyze(fileName);

				var outputFileName = extRegexp.Replace(fileName, ".html");

				var page = Razor.Parse(pageTemplate, runStats);

				using (var output = File.CreateText(outputFileName))
					output.Write(page);
			}
		}
	}

}
