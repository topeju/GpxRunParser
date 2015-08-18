using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GpxRunParser
{
	public class AnalysisCache
	{
		private struct CacheRecord 
		{
			public DateTime LastModified { get; set; }
			public RunStatistics Statistics { get; set; }
		}

		private readonly IDictionary<string, CacheRecord> _cache;

		public AnalysisCache()
		{
			_cache = new Dictionary<string, CacheRecord>();
		}

		public AnalysisCache(string cacheFileName) : this() {
			if (File.Exists(cacheFileName)) {
				var cacheFile = File.ReadAllText(cacheFileName);
				var settings = new JsonSerializerSettings();
				settings.Converters.Add(new JavaScriptDateTimeConverter());
				_cache = JsonConvert.DeserializeObject<Dictionary<string, CacheRecord>>(cacheFile, settings);
			}
		}

		public RunStatistics Fetch(string fileName)
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

		public void Store(string fileName, RunStatistics stats)
		{
			var fileInfo = new FileInfo(fileName);
			_cache[fileName] = new CacheRecord {
				LastModified = fileInfo.LastWriteTimeUtc,
				Statistics = stats
			};
		}

		public void SaveCache(string cacheFileName)
		{
			var serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Formatting.Indented;
			using (var sw = new StreamWriter(cacheFileName)) {
				using (JsonWriter writer = new JsonTextWriter(sw)) {
					serializer.Serialize(writer, _cache);
				}
			}
		}
	}
}

