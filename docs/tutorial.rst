Tutorial
========

This tutorial demonstrates how to use the KerbalWeatherProject (KWP) API in a C# plugin for KSP. 

Copy `KerbalWeatherProject.dll` to your KSP_x64_Data/Managed Folder in the KSP Game Directory. 

Add `KerbalWeatherProject.dll` as a project reference. 

   - In Visual Studio this can be accomplished by clicking `Project` then `add Reference`. Browse and select `KerbalWeatherProject.dll`.

Include KWP as an assembly dependency in your project

>>> [assembly: KSPAssemblyDependency("KerbalWeatherProject", 1, 0)]

Open a class in which you'd like to reference the KWP API and add the following:

>>> using KerbalWeatherProject

Check to see if KWP is available

.. testcode::

	//Boolean to check for KWP in assembly
	bool CheckKWP()
	{
	    try
	    {
		//Define null type references
		Type weather = null;
		Type climate = null;
		//Sort through assemblies
		foreach (var assembly in AssemblyLoader.loadedAssemblies)
		{
		    //Search for KWP
		    if (assembly.name == "KerbalWeather_Project")
		    {
			//Get assembly methods
			var types = assembly.assembly.GetExportedTypes();

			//Search for climate and weather api 
			foreach (Type t in types)
			{
			    if (t.FullName.Equals("KerbalWeather_Project.climate_api"))
			    {
				climate = t;
			    }
			    if (t.FullName.Equals("KerbalWeather_Project.weather_api"))
			    {
				weather = t;
			    }
			}
		    }
		}

		//Ensure API exists
		if (weather == null || climate == null)
		{
		    return false;
		}
		return true; // jump out
	    }
	    catch (Exception e)
	    {
		Debug.LogError("[WxAPI]: unable to find KerbalWeather_Project. Exception thrown: " + e.ToString());
	    }
	    return false;
	}

Use the climate API to retrieve climatological data at a specific point in time and space.

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

	Debug.Log("Climatological U-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + uwind_climo + " m/s");
	Debug.Log("Climatological V-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + vwind_climo + " m/s");
	Debug.Log("Climatological Z-Wind " + (malt / 1e3) + " km ASL at (" + mlat + "N, " + Math.Abs(mlng) + "W) " + zwind_climo + " m/s");

.. testoutput::

	Climatological U-Wind 5 km ASL at (25N, 60W) 21.4549880545088 m/s
	Climatological V-Wind 5 km ASL at (25N, 60W) -1.55983404053068 m/s
	Climatological Z-Wind 5 km ASL at (25N, 60W) -0.0169466099952593 m/s

Use the weather API to retrieve point weather data at a given time and height (above each launch site).

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
		Debug.Log("Temperature at " + lsite + " "+altitude+" m ASL: " + tmp_ls+" K");
	}

.. testoutput::

  Temperature at KSC: 300.649475097656 K
  Temperature at DLS: 288.496887207031 K 
  Temperature at WLS: 243.553863525391 K

Note: If using the Lite version of KerbalWeatherProject replace `KerbalWeatherProject` with `KerbalWeatherProject_Lite` for all references above.
