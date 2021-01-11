using System;
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

        //Retrieve wind speed (m/s)
        public static double wspd(double uwind, double vwind, double zwind)
        {
            //Calculate wind speed
            double wspd = Math.Sqrt(Math.Pow(uwind, 2) + Math.Pow(vwind, 2) + Math.Pow(zwind, 2));
            return wspd;
        }

        //Retrieve wind direction (deg)
        public static double wdir_degrees(double uwind, double vwind)
        {

            //Convert wind components to wind direction
            int wdir2 = (int)Math.Round(((180.0 / Math.PI) * Math.Atan2(-1.0 * uwind, -1.0 * vwind)), 0); // Direction wind is blowing from.
            if (wdir2 < 0)
            {
                wdir2 += 360;
            }

            return wdir2;
        }

        //Retrieve cardinal wind direction
        public static string wdir_cardinal(double wdir_degrees)
        {

            //Get cardinal wind direction from direction in degrees
            string wdir_str = Util.get_wind_card(wdir_degrees, "N,NNE,NE,ENE,...");
            return wdir_str;
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
        public static double density(double pressure, double temperature)
        {
            //Compute air density using ideal gas law
            double rho = (pressure / (Util.Rd * temperature));
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
        public static double OLR(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "olr");
        }

        //Retrieve total cloud cover: maximum cloud cover above the surface.
        public static double total_cloud_cover(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "tcld");
        }

        //Retrieve percipitable water (mm): liquid water equivalent if all of the moisture in the atmospheric column above lat,lng was condensed.
        public static double precitable_water(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "pw");
        }

        //Retrieve precipitation rate (mm/hr): liquid water equivelent average precipitation rate 
        public static double prate(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "prate");
        }

        //Retrieve mslp (hPa): Mean Sea Level Pressure (Low = warm/wet/windy, high = cold/dry/calm) 
        public static double mslp(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "mslp");
        }

        //Retrieve skin surface temperature: Over land = land surface temperature, Over water: sea surface temperature
        public static double sst(double latitude, double longitude, double ut)
        {
            return climate_data.get2DVar(latitude, longitude, ut, "sst");
        }

        //Compute cloud top temperatures using the Stefan-Boltzmann Law.
        public static double cloud_top_temps(double olr)
        {
            return Math.Pow((olr / Util.sigma), 0.25);
        }

    }
}
