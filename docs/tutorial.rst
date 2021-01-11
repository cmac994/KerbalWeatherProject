Tutorial
========

This module demonstrates how to instantiate the KerbalWeatherProject (KWP) API

Copy KerbalWeatherProject.dll to your KSP_x64_Data/Managed Folder in the KSP Game Directory. 

Add KerbalWeatherProject.dll as a project reference. 
   In Visual Studio this can be accomplished by clicking `Project` then `add Reference`. Browse and select KerbalWeatherProject.dll.

Include KerbalWeatherProject as an assembly dependency in your project

>>> [assembly: KSPAssemblyDependency("KerbalWeatherProject", 1, 0)]

Open a class in which you'd like to reference the KWP API and add the following:

>>> using KerbalWeatherProject

Utilize the API to retrieve climate data.

.. testcode::

  //Set UT Time
  epoch = 3600;

  //Set position for climate API test
  double mlat = 25.0; // 25 N
  double mlng = -60.0; // 60 W
  double malt = 5000; // 5-km ASL

  double uwind_climo = climate_api.uwind(mlat, mlng, malt, epoch);
  double vwind_climo = climate_api.vwind(mlat, mlng, malt, epoch);
  double zwind_climo = climate_api.zwind(mlat, mlng, malt, epoch);

  Debug.Log("Climatological U-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + uwind_climo);
  Debug.Log("Climatological V-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + vwind_climo);
  Debug.Log("Climatological Z-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + zwind_climo);

.. testoutput::

  Climatological U-Wind 5 km ASL at (25N, 60W) 21.4549880545088
  Climatological V-Wind 5 km ASL at (25N, 60W) -1.55983404053068
  Climatological Z-Wind 5 km ASL at (25N, 60W) -0.0169466099952593

Utilize the API to retrieve point weather data at select launch sites

.. testcode:: 

  //Altitude above sea level
  double altitude = 0.0;

  //Get list of launch sites with weather data 
  List<string> lsites = weather_api.lsites;

  //Loop through launch sites
  for (int l = 0; l < 3; l++)
  {

     //Set launch site
     lsite = lsites[l];

     //Read weather data from launch site
     weather_api.set_datasource(lsite);

     //Get temperature data for launch site
     double tmp_ls = weather_api.temperature(altitude, epoch);
     Debug.Log("Temperature at " + lsite + " "+altitude+" m ASL: " + tmp_ls);
  }

.. testoutput::

  Temperature at KSC: 300.649475097656
  Temperature at DLS: 288.496887207031
  Temperature at WLS: 243.553863525391

