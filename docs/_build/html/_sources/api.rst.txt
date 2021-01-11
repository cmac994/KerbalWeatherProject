API Documentation
=================

Climate API
--------------------

.. function:: climate_api.temperature(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: Climatological temperature at a given point in space and time.

Weather API
--------------------

.. function::  weather_api.temperature(altitude, ut)
 
	Parameters 
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: temperature: Temperature (K) at a given time and height ASL. 

