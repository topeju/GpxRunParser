using System;
using System.Globalization;
using System.Xml.Linq;

namespace GpxRunParser
{
	public class GpxTrackPoint
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double Elevation { get; set; }
		public DateTime Time { get; set; }
		public double HeartRate { get; set; }
		public double Cadence { get; set; }

		public GpxTrackPoint()
		{
		}

		public GpxTrackPoint(XElement element)
			: this()
		{
			var gpxNamespace = XNamespace.Get("http://www.topografix.com/GPX/1/1");
			var gpxTrackPointNamespace = XNamespace.Get("http://www.garmin.com/xmlschemas/TrackPointExtension/v1");
			Latitude = double.Parse(element.Attribute("lat").Value, CultureInfo.InvariantCulture);
			Longitude = double.Parse(element.Attribute("lon").Value, CultureInfo.InvariantCulture);
			var elevElem = element.Element(gpxNamespace + "ele");
			if (elevElem != null) {
				Elevation = double.Parse(elevElem.Value, CultureInfo.InvariantCulture);
			}
			var timeElem = element.Element(gpxNamespace + "time");
			if (timeElem != null) {
				Time = DateTime.Parse(timeElem.Value, CultureInfo.InvariantCulture);
			}
			var extElem = element.Element(gpxNamespace + "extensions");
			if (extElem != null) {
				var trackPointExtElem = extElem.Element(gpxTrackPointNamespace + "TrackPointExtension");
				if (trackPointExtElem != null) {
					var hrElem = trackPointExtElem.Element(gpxTrackPointNamespace + "hr");
					if (hrElem != null) {
						HeartRate = double.Parse(hrElem.Value, CultureInfo.InvariantCulture);
					}
					var cadenceElem = trackPointExtElem.Element(gpxTrackPointNamespace + "cad");
					if (cadenceElem != null) {
						Cadence = double.Parse(cadenceElem.Value, CultureInfo.InvariantCulture);
					}
				}
			}
		}

		public double DistanceTo(GpxTrackPoint otherPoint)
		{
			const double equatorialRadius = 6378137.0D; // m
			var latDiff = (otherPoint.Latitude - Latitude) * Math.PI / 180.0;
			var lonDiff = (otherPoint.Longitude - Longitude) * Math.PI / 180.0;
			var lat1 = Latitude * Math.PI / 180.0;
			var lat2 = otherPoint.Latitude * Math.PI / 180.0;
			var a = Math.Sin(latDiff / 2.0) * Math.Sin(latDiff / 2.0)
					+ Math.Sin(lonDiff / 2.0) * Math.Sin(lonDiff / 2.0) * Math.Cos(lat1) * Math.Cos(lat2);
			var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
			return equatorialRadius * c;
		}

		public TimeSpan TimeDifference(GpxTrackPoint otherPoint)
		{
			return otherPoint.Time - Time;
		}
	}
}
