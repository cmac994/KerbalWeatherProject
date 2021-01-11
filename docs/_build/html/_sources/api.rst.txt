KerbalWeatherProject API Documentation
======================================

The “climate” module
--------------------

.. function:: public static double temperature(double latitude, double longitude, double altitude, double ut)

   :module: climate_api.temperature

   API call to retrieve climatological temperature given position and UT Time.

The “weather” module
--------------------

.. function::  public static double temperature(double altitude, double ut)

   :module: weather_api.temperature
   
   API call to retrieve temperature given height ASL and UT Time.
