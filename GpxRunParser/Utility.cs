using System;

namespace GpxRunParser
{
	public static class Utility
	{
		public static long ToUnixMilliseconds(this DateTime tm)
		{
			return (long)((tm - new DateTime(1970, 1, 1).ToLocalTime()).TotalMilliseconds);
		}
	}
}
	