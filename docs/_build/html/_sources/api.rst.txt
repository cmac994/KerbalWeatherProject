API Documentation
=================

The “climate” module
--------------------

.. function:: climate_api.temperature(latitude, longitude, altitude, ut)

   Parameters: * latitude - decimal degrees
               * longitude - decimal degrees
               * altitude - meters above sea level
               * ut - time in seconds since start of game

   Returns: Climatological temperature at a given point in space and time.

The “weather” module
--------------------

.. function::  weather_api.temperature(altitude, ut)
 
   Parameters: * altitude - meters above sea level
               * ut - time in seconds since start of game 

   Returns: temperature: Temperature (K) at a given time and height ASL. 

