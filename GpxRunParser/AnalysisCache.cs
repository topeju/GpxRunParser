using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GpxRunParser
{
	public static class AnalysisCache
	{
		private struct CacheRecord 
		{
			public DateTime LastModified { get; set; }
			public RunStatistics Statistics { get; set; }
		}

		private static readonly IDictionary<string, CacheRecord> _cache;

		static AnalysisCache()
		{
			if (File.Exists(Settings.RunStatsCacheFile)) {
				var cacheFile = File.ReadAllText(Settings.RunStatsCacheFile);
				var settings = new JsonSerializerSettings();
				settings.Converters.Add(new JavaScriptDateTimeConverter());
				_cache = JsonConvert.DeserializeObject<Dictionary<string, CacheRecord>>(cacheFile, settings);
			} else {
				_cache = new Dictionary<string, CacheRecord>();
			}
		}

		public static RunStatistics Fetch(string fileName)
		{
			if (!_cache.ContainsKey(fileName)) {
				return null;
			}
			var cacheRecord = _cache[fileName];
			var fileInfo = new FileInfo(fileName);
			if (fileInfo.LastWriteTimeUtc > cacheRecord.LastModified) {
				return null;
			}
			cacheRecord.Statistics.RefreshCalculatedProperties();
			return cacheRecord.Statistics;
		}

		public static void Store(string fileName, RunStatistics stats)
		{
			var fileInfo = new FileInfo(fileName);
			_cache[fileName] = new CacheRecord {
				LastModified = fileInfo.LastWriteTimeUtc,
				Statistics = stats
			};
		}

		public static void SaveCache()
		{
			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Formatting.Indented;
			using (var sw = new StreamWriter(Settings.RunStatsCacheFile)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					serializer.Serialize(writer, _cache);
				}
			}
		}
	}
}

