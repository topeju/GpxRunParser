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
			var fileName = "";
			var zoneStr = "120,140,173,182";
			var paceBinStr = "8:45,7:15,6:10";
			var help = false;
			var opts = new OptionSet {
				{ "f|file=", "GPX file to parse", s => fileName = s },
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
				Console.Out.WriteLine("Parses the GPX file specified using the -f option to calculate statistics on the file.");
				opts.WriteOptionDescriptions(Console.Out);
				return;
			}
			if (fileName == "") {
				Console.Error.WriteLine("Input GPX file not specified");
				return;
			}
			var zones = (from zone in zoneStr.Split(',')
						 select double.Parse(zone, CultureInfo.InvariantCulture)).ToArray();
			var paces =
				(from paceStr in paceBinStr.Split(',')
				 select new TimeSpan(0, int.Parse(paceStr.Split(':')[0]), int.Parse(paceStr.Split(':')[1]))).ToArray();

			RunAnalyzer analyzer = new RunAnalyzer(zones, paces);

			var runStats = analyzer.Analyze(fileName);

			var extRegexp = new Regex(@"\.gpx$", RegexOptions.IgnoreCase);
			var outputFileName = extRegexp.Replace(fileName, ".html");

			var assembly = Assembly.GetExecutingAssembly();
			var pageTemplate = "";

			using (var stream = assembly.GetManifestResourceStream("GpxRunParser.Templates.index.cshtml"))
			using (var reader = new StreamReader(stream)) pageTemplate = reader.ReadToEnd();

			var page = Razor.Parse(pageTemplate, runStats);

			using (var output = File.CreateText(outputFileName)) output.Write(page);
		}
	}

}
