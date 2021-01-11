using System.Collections.Generic;

namespace KerbalWeatherProject_Lite
{
    public static class climate_api
    {

        // Retrieve dictionary of atmospheric variables available in KWP //

        //Retrieve list of 3-D atmospheric variables (Key = Variable Name, Value = Index in Array)
        public static Dictionary<string, int> get_vars3D()
        {
            return weather_data.Vars3d;
        }
        //Retrieve dictionary of 2-D atmospheric variables (Key = Variable Name, Value = Index in Array)
        public static Dictionary<string, int> get_vars2D()
        {
            return weather_data.Vars2d;
        }

        /// <summary>
        ///  Returns climatological wind data (in m/s) at specified point in space and time.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns>

        //Retrieve zonal (east-west) wind component (m/s)
        public static double uwind(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "u");
        }

        //Retrieve meridional (north-south) wind component (m/s)
        public static double vwind(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "v");
        }

        //Retrieve vertical wind component (m/s)
        public static double zwind(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "w");
        }

        /// <summary>
        ///  Returns climatological ambient weather data at specified point in space and time.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns>

        //Retrieve atmospheric pressure (hPa)
        public static double pressure(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "p");
        }

        //Retrieve atmospheric temperature (K)
        public static double temperature(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "t");
        }

        //Retrieve atmospheric relative humidity (%)
        public static double relative_humidity(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "rh");
        }

        //Retrieve visibility (km) 
        public static double visibility(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "vis");
        }

        //Retrieve cloud cover (%) above a given altitude at a specific time and position.
        public static double cloud_cover(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get3DVar(latitude, longitude, altitude, ut, "cld");
        }

        //Retrieve atmospheric density (hPa)
        public static double density(double latitude, double longitude, double altitude, double ut)
        {
            double pp = pressure(latitude, longitude, altitude, ut); //Compute air pressure
            double tt = temperature(latitude, longitude, altitude, ut); //Compute air temperature
            //Compute air density using ideal gas law
            double rho = (pp / (Util.Rd * tt));
            return rho;
        }

        /// <summary>
        ///  Returns climatological surface weather data at specified point in space and time.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// 
        /// 
        //Retrieve outgoing longwave radiation (W/m^2): used in infrared satellite imagery to see cloud tops without visible light.
        public static double OLR(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "olr");
        }

        //Retrieve total cloud cover: maximum cloud cover above the surface.
        public static double total_cloud_cover(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "tcld");
        }

        //Retrieve percipitable water (mm): liquid water equivalent if all of the moisture in the atmospheric column above lat,lng was condensed.
        public static double precitable_water(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "pw");
        }

        //Retrieve precipitation rate (mm/hr): liquid water equivelent average precipitation rate 
        public static double prate(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "prate");
        }

        //Retrieve mslp (hPa): Mean Sea Level Pressure (Low = warm/wet/windy, high = cold/dry/calm) 
        public static double mslp(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "mslp");
        }

        //Retrieve skin surface temperature: Over land = land surface temperature, Over water: sea surface temperature
        public static double sst(double latitude, double longitude, double altitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "sst");
        }
    }
}
