using System;

namespace GpxRunParser
{
	public class KalmanFilter
	{
		private const double MinAccuracy = 1.0;

		private double _q_meters_per_second;
		private GpxTrackPoint _point = new GpxTrackPoint();
		private double _variance;
		// P matrix.  Negative means object uninitialised.  NB: units irrelevant, as long as same units used throughout

		public KalmanFilter(double q_meters_per_second)
		{
			_q_meters_per_second = q_meters_per_second;
			_variance = -1.0;
		}

		public DateTime TimeStamp { get { return _point.Time; } }

		public double Latitude { get { return _point.Latitude; } }

		public double Longitude	{ get { return _point.Longitude; } }

		public GpxTrackPoint Point { get { return _point; } }

		public double Accuracy { get { return Math.Sqrt(_variance); } }

		public void SetState(GpxTrackPoint point, float accuracy)
		{
			_point = new GpxTrackPoint(point);
			_variance = accuracy * accuracy;
		}

		/// <summary>
		/// Kalman filter processing for lattitude and longitude
		/// </summary>
		/// <param name="lat_measurement_degrees">new measurement of lattidude</param>
		/// <param name="lng_measurement">new measurement of longitude</param>
		/// <param name="accuracy">measurement of 1 standard deviation error in metres</param>
		/// <param name="TimeStamp_milliseconds">time of measurement</param>
		/// <returns>new state</returns>
		public void Process(GpxTrackPoint rawPoint, double accuracy)
		{
			if (accuracy < MinAccuracy)
				accuracy = MinAccuracy;
			if (_variance < 0.0) {
				// if variance < 0, object is unitialised, so initialise with current values
				_point = new GpxTrackPoint(rawPoint);
				_variance = accuracy * accuracy; 
			} else {
				// else apply Kalman filter methodology
				var TimeInc_milliseconds = (rawPoint.Time - _point.Time).TotalMilliseconds;
				if (TimeInc_milliseconds > 0.0) {
					// time has moved on, so the uncertainty in the current position increases
					_variance += TimeInc_milliseconds * _q_meters_per_second * _q_meters_per_second / 1000;
					_point.Time = rawPoint.Time;
					// TO DO: USE VELOCITY INFORMATION HERE TO GET A BETTER ESTIMATE OF CURRENT POSITION
				}

				// Kalman gain matrix K = Covarariance * Inverse(Covariance + MeasurementVariance)
				// NB: because K is dimensionless, it doesn't matter that variance has different units to lat and lng
				var K = _variance / (_variance + accuracy * accuracy);
				// apply K
				_point.Latitude += K * (rawPoint.Latitude - _point.Latitude);
				_point.Longitude += K * (rawPoint.Longitude - _point.Longitude);
				// new Covarariance  matrix is (IdentityMatrix - K) * Covarariance 
				_variance = (1 - K) * _variance;
			}
		}
	}
}