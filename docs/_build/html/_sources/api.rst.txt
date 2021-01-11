API Documentation
=================

Climate API
-----------

Utility
#######

Variable lists.

.. function:: climate_api.get_vars3D()
		
	Returns (Dictionary<string,int>): 3D atmospheric variables accessible with the KWP climate API. (Key = variable name, Value = variable index)
	
.. function:: climate_api.get_vars2D()

	Returns (Dictionary<string,int>): 2D atmospheric variables accessible with the KWP climate API. (Key = variable name, Value = variable index)


Wind
####
Retrieve atmospheric wind data

.. function:: climate_api.uwind(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): zonal-wind component (m/s). Wind velocity in east-west direction.
	
.. function:: climate_api.vwind(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): meridional wind component (m/s). Wind velocity in north-south direction.

.. function:: climate_api.zwind(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): vertical wind component (m/s). wind velocity in up-down direction.	

Ambient Conditions
##################

Retrieve column (3D) atmospheric variables

.. function:: climate_api.pressure(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): air pressure (Pa)
	
.. function:: climate_api.temperature(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): air temperature (K)
	
.. function:: climate_api.relative_humidity(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): relative_humidity (%) 
	
.. function:: climate_api.cloud_cover(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): cloud_cover (%) - above altitude. Percentage of sky above covered by clouds.
	
.. function:: climate_api.visibility(latitude, longitude, altitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): visibility (km). Estimate of visibility derived from humidity, cloud cover, and precipitation rate.

Surface Conditions
##################

Retrieve surface (2D) atmospheric variables

.. function:: climate_api.OLR(latitude, longitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): outgoing longwave radiation (w/m^2). Returned from IR satellite imagery and used to view cloud cover in the absence of visible light.

.. function:: climate_api.total_cloud_cover(latitude, longitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): total cloud cover (%). Percentage of sky covered by clouds.

.. function:: climate_api.precipitable_water(latitude, longitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): precipitable water (mm). Amount of liquid water produced by the condensation of all available water vapor in the atmospheric column above a given point. Estimates the moisture content of the atmosphere.

.. function:: climate_api.prate(latitude, longitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): precipitation rate (mm/hr). Liquid water equivalent precipitation rate, derived from convective and stratiform precipitation totals. 

.. function:: climate_api.mslp(latitude, longitude, ut)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)
		
	Returns (double): mean sea level pressure (Pa). Pressure, reduced to sea level, by accounting for the elevation of terrain and diurnal variations in temperature.
	
.. function:: climate_api.sst(latitude, longitude, ut)

	Parameters 
	
		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): skin surface temperature (K). On land = land surface temperature. On water = sea surface temperature (SST).


Derivatives
###########

Derive variables from climate API calls above.

.. function:: climate_api.density(pressure, temperature)

	Parameters 

		* pressure (double) - air pressure (Pa)
		* temperature (double) - air temperature (K)
		
	Returns (double): air density (kg/m^3)

.. function:: climate_api.wspd(uwind, vwind, zwind)

	Parameters 

		* uwind (double) - zonal wind component (m/s)
		* vwind (double) - meridional wind component (m/s)
		* zwind (double) - vertical wind component (m/s)

	Returns (double): wind speed (m/s)
	
.. function:: climate_api.wdir_degrees(uwind, vwind)

	Parameters 

		* uwind (double) - zonal wind component (m/s)
		* vwind (double) - meridional wind component (m/s)

	Returns (double): wind direction (degrees). Direction in which the wind is coming from (e.g. 45 or 225).
	
.. function:: climate_api.wdir_cardinal(wdir_degrees)

	Parameters 

		* wdir_degrees (double) - wind direction (degrees)

	Returns (string): cardinal wind direction. Direction in which the wind is coming from (e.g. NE or SW)
	
.. function:: climate_api.cloud_top_temps(olr)

	Parameters 

		* olr (double) - outgoing longwave radiation (W/m^2)

	Returns (string): cloud top temperatures (K). Cloud top temperature. If skies are clear this is an estimate of the land/sea surface temperature.
	
Weather API
-----------

Utility
#######

List of available launch sites and atmospheric variables

	lsites (List<string>)
		* list of available launch sites (three letter abbreviations)
		
	lsites_name (List<string>)
		* list of available launch sites (full names)

	lsites_lat (List<double>)
		* list of launch site latitudes
		
	lsites_lng (List<double>)
		* list of launch site longitudes

.. function:: weather_api.get_nearest_lsite_idx(latitude, longitude)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		
	Returns (int): Index of nearest launch site in list (int).
	
.. function:: weather_api.get_nearest_lsite(latitude, longitude)

	Parameters 

		* latitude (double) - decimal degrees
		* longitude (double) - decimal degrees
		
	Returns (string): Nearest launch site.
	
.. function:: weather_api.get_vars3D()
		
	Returns (Dictionary<string,int>): 3D atmospheric variables accessible with the KWP weather API. (Key = variable name, Value = variable index)
	
.. function:: weather_api.get_vars2D()

	Returns (Dictionary<string,int>): 2D atmospheric variables accessible with the KWP weather API. (Key = variable name, Value = variable index)

Wind
####

Retrieve atmospheric wind data

.. function:: weather_api.uwind(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): zonal-wind component (m/s). Wind velocity in east-west direction.
	
.. function:: weather_api.vwind(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): meridional wind component (m/s). Wind velocity in north-south direction.

.. function:: weather_api.zwind(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): vertical wind component (m/s). wind velocity in up-down direction.	

Ambient Conditions
##################

Retrieve column (3D) atmospheric variables

.. function:: weather_api.pressure(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): air pressure (Pa)
	
.. function:: weather_api.temperature(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): air temperature (K)
		
.. function:: weather_api.relative_humidity(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): relative_humidity (%) 
	
.. function:: weather_api.cloud_cover(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): cloud_cover (%) - above altitude. Percentage of sky above covered by clouds.
	
.. function:: weather_api.visibility(altitude, ut)

	Parameters 

		* altitude (double) - meters above sea level
		* ut (double) - universal time in seconds (time since game began)

	Returns (double): visibility (km). Estimate of visibility derived from humidity, cloud cover, and precipitation rate.

Surface Conditions
##################

Retrieve surface (2D) atmospheric variables

.. function:: weather_api.OLR(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): outgoing longwave radiation (w/m^2). Returned from IR satellite imagery and used to view cloud cover in the absence of visible light.

.. function:: weather_api.total_cloud_cover(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): total cloud cover (%). Percentage of sky covered by clouds.

.. function:: weather_api.precipitable_water(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): precipitable water (mm). Amount of liquid water produced by the condensation of all available water vapor in the atmospheric column above a given point. Estimates the moisture content of the atmosphere.

.. function:: weather_api.prate(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): precipitation rate (mm/hr). Liquid water equivalent precipitation rate, derived from convective and stratiform precipitation totals. 

.. function:: weather_api.mslp(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): mean sea level pressure (Pa). Pressure, reduced to sea level, by accounting for the elevation of terrain and diurnal variations in temperature.
	
.. function:: weather_api.sst(ut)

	Parameters 

		* ut(double) - universal time in seconds (time since game began)

	Returns (double): skin surface temperature (K). On land = land surface temperature. On water = sea surface temperature (SST).
	
Derivatives
###########

Derive variables from weather API calls above.

.. function:: weather_api.density(pressure, temperature)

	Parameters 

		* pressure (double) - air pressure (Pa)
		* temperature (double) - air temperature (K)
		
	Returns (double): air density (kg/m^3)

.. function:: weather_api.wspd(uwind, vwind, zwind)

	Parameters 

		* uwind (double) - zonal wind component (m/s)
		* vwind (double) - meridional wind component (m/s)
		* zwind (double) - vertical wind component (m/s)

	Returns (double): wind speed (m/s)
	
.. function:: weather_api.wdir_degrees(uwind, vwind)

	Parameters 

		* uwind (double) - zonal wind component (m/s)
		* vwind (double) - meridional wind component (m/s)

	Returns (double): wind direction (degrees). Direction in which the wind is coming from (e.g. 45 or 225).
	
.. function:: weather_api.wdir_cardinal(wdir_degrees)

	Parameters 

		* wdir_degrees (double) - wind direction (degrees)

	Returns (string): cardinal wind direction. Direction in which the wind is coming from (e.g. NE or SW)

.. function:: weather_api.cloud_top_temps(olr)

	Parameters 

		* olr (double) - outgoing longwave radiation (W/m^2)

	Returns (string): cloud top temperatures (K). Cloud top temperature. If skies are clear this is an estimate of the land/sea surface temperature.

