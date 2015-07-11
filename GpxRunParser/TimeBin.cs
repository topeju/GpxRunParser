using System;
using System.Linq;

namespace GpxRunParser
{
	public class TimeBin<T> where T : IComparable
	{
		public T[] Bins { get; private set; }

		public TimeSpan[] Values { get; private set; }

		public TimeBin(T[] boundaries)
		{
			Bins = new T[boundaries.Length];
			Values = new TimeSpan[boundaries.Length + 1];
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
			var i = Bins.Length - 1;
			while (i >= 0 && Bins[i].CompareTo(value) > 0) {
				i--;
			}
			Values[i + 1] += time;
		}
	}
}
