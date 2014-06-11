using System;
using System.IO;
using System.Linq;

namespace GpxRunParser
{
	public class TimeBin<T> where T : IComparable
	{
		private T[] Bins { get; set; }

		private TimeSpan[] Values { get; set; }

		public TimeBin(T[] boundaries)
		{
			Bins = new T[boundaries.Length];
			Values = new TimeSpan[boundaries.Length + 1];
			var i = 0;
			Values[0] = TimeSpan.Zero;
			foreach (var b in boundaries.OrderBy(bnd => bnd)) {
				Bins[i] = b;
				Values[i+1] = TimeSpan.Zero;
				i++;
			}
		}

		public void Record(T value, TimeSpan time)
		{
			var i = Bins.Length-1;
			while (i >= 0 && Bins[i].CompareTo(value) > 0) i--;
			Values[i+1] += time;
		}

		public void Output(StreamWriter stream, TimeSpan totalTime)
		{
			stream.WriteLine("< {0,-18}  {1,8:g}  {2,5:P0}", Bins[0], Values[0],
							 Values[0].TotalMilliseconds / totalTime.TotalMilliseconds);
			for (var i = 0; i < Bins.Length - 1; i++) {
				stream.WriteLine("{0,-20}  {1,8:g}  {2,5:P0}", Bins[i] + "..." + Bins[i + 1], Values[i + 1],
								 Values[i+1].TotalMilliseconds / totalTime.TotalMilliseconds);
			}
			stream.WriteLine("> {0,-18}  {1,8:g}  {2,5:P0}", Bins[Bins.Length - 1], Values[Bins.Length],
							 Values[Bins.Length].TotalMilliseconds / totalTime.TotalMilliseconds);
		}
	}
}