API Documentation
=================

Climate API
-----------

Wind
####

Ambient Conditions
##################

.. function:: climate_api.pressure(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: air pressure (Pa)
	

.. function:: climate_api.temperature(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: air temperature (K)
	
.. function:: climate_api.density(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: air density (kg/m^3)
	
.. function:: climate_api.relative_humidity(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: relative_humidity (%) 
	
.. function:: climate_api.cloud_cover(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: cloud_cover (%) - above altitude. Percentage of sky above covered by clouds.
	
.. function:: climate_api.visibility(latitude, longitude, altitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: visibility (km). Estimate of visibility derived from humidity, cloud cover, and precipitation rate.

Surface Conditions
##################

.. function:: climate_api.OLR(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: outgoing longwave radiation (w/m^2). Returned from IR satellite imagery and used to view cloud cover in the absence of visible light.

.. function:: climate_api.total_cloud_cover(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: total cloud cover (%). Percentage of sky covered by clouds.

.. function:: climate_api.precipitable_water(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: precipitable water (mm). Amount of liquid water produced by the condensation of all available water vapor in the atmospheric column above a given point. Estimates the moisture content of the atmosphere.

.. function:: climate_api.prate(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: precipitation rate (mm/hr). Liquid water equivalent precipitation rate, derived from convective and stratiform precipitation totals. 

.. function:: climate_api.mslp(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: mean sea level pressure (Pa). Pressure, reduced to sea level, by accounting for the elevation of terrain and diurnal variations in temperature.
	
.. function:: climate_api.sst(latitude, longitude, ut)

	Parameters 

		* latitude - decimal degrees
		* longitude - decimal degrees
		* ut - universal time in seconds (time since game began)

	Returns: skin surface temperature (K). On land = land surface temperature. On water = sea surface temperature (SST).

Weather API
-----------

.. function::  weather_api.temperature(altitude, ut)
 
	Parameters 

		* altitude - meters above sea level
		* ut - universal time in seconds (time since game began)

	Returns: temperature: Temperature (K) at a given time and height ASL. 

