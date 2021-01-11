KerbalWeatherProjet API: Tutorial
=================================

This module demonstrates how to instantiate the KerbalWeatherProject (KWP) API

Add KerbalWeatherProject.dll to your KSP_x64_Data/Managed Folder in the KSP Game Directory. Add KerbalWeatherProject.dll as a project reference. In Visual Studio this can be accomplished by clicking `Project` then `add Reference`, after which you can browse for and select the KerbalWeatherProject.dll.


With the reference added add an assembly dependency to your project

>>> [assembly: KSPAssemblyDependency("KerbalWeatherProject", 1, 0)]

Next open the class file in which you'd like to reference the KWP API. Add the following before the namespace.

>>> using KerbalWeatherProject

Add code to check whether KWP is available

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
                    if (assembly.name == "KerbalWeatherProject")
                    {
                        //Get assembly methods
                        var types = assembly.assembly.GetExportedTypes();

                        //Search for climate and weather api 
                        foreach (Type t in types)
                        {
                            if (t.FullName.Equals("KerbalWeatherProject.climate_api"))
                            {
                                climate = t;
                            }
                            if (t.FullName.Equals("KerbalWeatherProject.weather_api"))
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
                Debug.LogError("[WxAPI]: unable to find KerbalWeatherProject_Lite. Exception thrown: " + e.ToString());
            }
            return false;
        }

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

.. testoutput::

	Temperature at KSC: 300.649475097656
	Temperature at DLS: 288.496887207031
	Temperature at WLS: 243.553863525391

