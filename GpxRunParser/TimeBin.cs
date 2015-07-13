using System;
using System.Collections.Generic;
using System.Linq;

namespace GpxRunParser
{
	public class TimeBin<T> where T : IComparable
	{
		public IList<T> Bins { get; private set; }

		public IList<TimeSpan> Values { get; private set; }

		/// <summary>
		/// This constructor should _only_ be used for JSON deserialization
		/// </summary>
		public TimeBin()
		{
			Bins = new List<T>();
			Values = new List<TimeSpan>();
		}

		public TimeBin(T[] boundaries)
		{
			Bins = new List<T>(boundaries.Length);
			Values = new List<TimeSpan>(boundaries.Length + 1);
			var i = 0;
			Values[0] = TimeSpan.Zero;
			foreach (var b in boundaries.OrderBy(bnd => bnd)) {
				Bins[i] = b;
				Values[i + 1] = TimeSpan.Zero;
				i++;
			}
		}

		public void Record(T value, TimeSpan time)
		{
			var i = Bins.Count - 1;
			while (i >= 0 && Bins[i].CompareTo(value) > 0) {
				i--;
			}
			Values[i + 1] += time;
		}
	}
}
