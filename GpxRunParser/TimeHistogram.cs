using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace GpxRunParser
{
	public class TimeHistogram<T> where T : IComparable
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public IDictionary<T, TimeSpan> Data { get; private set; }

		[JsonIgnore]
		public ICollection<T> Keys { get { return Data.Keys; } }

		public TimeSpan Value(T key)
		{
			return Data[key];
		}
		
		public TimeHistogram()
		{
			Data = new SortedDictionary<T, TimeSpan>();
		}

		public void Record(T value, TimeSpan time)
		{
			if (!Data.ContainsKey(value)) {
				Data[value] = time;
			} else {
				Data[value] += time;
			}
		}

		public void Record(TimeHistogram<T> values)
		{
			foreach (var key in values.Keys) {
				Record(key, values.Value(key));
			}
		}

		public TimeSpan TimeInRange(T t1, T t2) {
			var matchingRange = Data.Where(kvp => t1.CompareTo(kvp.Key) <= 0 && t2.CompareTo(kvp.Key) > 0);
			var values = matchingRange.Select(kvp => kvp.Value);
			return new TimeSpan(values.Sum(v => v.Ticks));
		}

	}
}
