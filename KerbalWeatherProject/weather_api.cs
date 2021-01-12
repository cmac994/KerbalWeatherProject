using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalWeatherProject
{
    public static class weather_api
    {

        //Define launch sites (including those from Kerbinside)
        public static List<string> lsites = new List<string>() { "KSC", "DLS", "WLS", "BKN", "BKN", "KHV", "KAT", "CKR", "SLK", "KRS" };
        public static List<string> lsites_name = new List<string>()
            {
                "LaunchPad",
                "Desert_Launch_Site",
                "Woomerang_Launch_Site",
                "Baikerbanur Launch Pad",
                "KSC2",
                "Kojave Sands Launch Pad",
                "Kerman Atoll Launch Pad",
                "Cape Kerman_KSC_LaunchPad_level_3_0",
                "South Lake",
                "Kermundsen Research Station"
            };

        //Define location of launch sites
        public static List<double> lsites_lat = new List<double>() { -0.0972, -6.5603, 45.290, 20.6397, 6.0744, -37.1457, 24.9062, -37.25, -90.0 };
        public static List<double> lsites_lng = new List<double>() { -74.5571, -143.95, 136.1101, -146.4786, -142.0487, -71.0359, -83.6232, 52.70, 113.04703 };

        //Get launch site closest to position (lat,lng)
        public static string get_nearest_lsite(double latitude, double longitude)
        {
            int lidx = get_nearest_lsite_idx(latitude, longitude);
            return lsites[lidx];
        }

        //Get index of launch site closest to position (lat,lng)
        public static int get_nearest_lsite_idx(double latitude, double longitude)
        {
            List<double> ddist = new List<double>();
            for (int l = 0; l < lsites_lat.Count(); l++)
            {
                ddist.Add(Math.Pow((lsites_lat[l] - latitude), 2) + Math.Pow((lsites_lng[l] - longitude), 2));
            }
            int lidx = ddist.IndexOf(ddist.Min());
            return lidx;
        }

        // Retrieve dictionary of atmospheric variables available in KWP //

        //Retrieve list of 3-D atmospheric variables (Key = Variable Name, Value = Index in Array)
        public static Dictionary<string,int> get_vars3D() 
        {
            return weather_data.Vars3d;
        }
        //Retrieve dictionary of 2-D atmospheric variables (Key = Variable Name, Value = Index in Array)
        public static Dictionary<string, int> get_vars2D()
        {
            return weather_data.Vars2d;
        }

        /// <summary>
        ///  Returns point wind data (in m/s) at specified time and height above launch site.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns>

        //Set Launch site (choose from list below):
        //Stock: "KSC", "DLS" | DLC:  "WLS" | Kerbinside: "BKN", "BKN", "KHV", "KAT", "CKR", "SLK", "KRS"
        public static void set_datasource(string launch_site)
        {
            if (lsites.Contains(launch_site))
            {
                weather_data.get_wxdata(launch_site); //Set datasource to specified launch site
            } else
            {
                weather_data.get_wxdata("KSC"); //default datasource to KSC if launch site not found
            }
        }

        //Retrieve zonal (east-west) wind component (m/s)
        public static double uwind(double altitude, double ut)
        {
            return weather_data.get3DVar(altitude, ut, "u");
        }

        //Retrieve meridional (north-south) wind component (m/s)
        public static double vwind(double altitude, double ut)
        {
            return weather_data.get3DVar(altitude, ut, "v");
        }

        //Retrieve vertical wind component (m/s)
        public static double zwind(double altitude, double ut)
        {
            return weather_data.get3DVar(altitude, ut, "w");
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
        ///  Returns point ambient weather data at a specified time and height above launch site.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns>

        //Retrieve atmospheric pressure (hPa)
        public static double pressure(double altitude, double ut)
        {
            return weather_data.get3DVar(altitude, ut, "p");
        }

        //Retrieve atmospheric temperature (K)
        public static double temperature(double altitude, double ut)
        {
            return weather_data.get3DVar(altitude, ut, "t");
        }

        //Retrieve atmospheric relative humidity (%)
        public static double relative_humidity(double altitude, double ut)
        {
            double rh = weather_data.get3DVar(altitude, ut, "rh");
            if (rh < 0)
            {
                rh = 0;
            }
            else if (rh > 100)
            {
                rh = 100;
            }
            return rh;
        }

        //Retrieve visibility (km) 
        public static double visibility(double altitude, double ut)
        {
            double vv = weather_data.get3DVar(altitude, ut, "vis");
            if (vv < 0)
            {
                vv = 0.0;
            }
            return vv;
        }

        //Retrieve cloud cover (%) above a given altitude at a specific time.
        public static double cloud_cover(double altitude, double ut)
        {
            double cc = weather_data.get3DVar(altitude, ut, "cld") * 100.0;
            if (cc < 0.0)
            {
                cc = 0.0;
            }
            else if (cc > 100)
            {
                cc = 100;
            }
            return cc;
        }

        //Retrieve atmospheric density (hPa)
        public static double density(double pressure, double temperature)
        {
            //Compute air density using ideal gas law
            double rho = (pressure / (Util.Rd * temperature));
            return rho;
        }

        /// <summary>
        ///  Returns point surface weather data at a specific time and launch site.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// 
        /// 
        //Retrieve outgoing longwave radiation (W/m^2): used in infrared satellite imagery to see cloud tops without visible light.
        public static double OLR(double ut)
        {
            return weather_data.get2DVar(ut, "olr");
        }

        //Retrieve total cloud cover: maximum cloud cover above the surface.
        public static double total_cloud_cover(double ut)
        {
            double tcc = weather_data.get2DVar(ut, "tcld") * 100;
            if (tcc < 0.0)
            {
                tcc = 0;
            }
            else if (tcc > 100)
            {
                tcc = 100;
            }
            return tcc;
        }

        //Retrieve percipitable water (mm): liquid water equivalent if all of the moisture in the atmospheric column above lat,lng was condensed.
        public static double precitable_water(double ut)
        {
            return weather_data.get2DVar(ut, "pw");
        }

        //Retrieve precipitation rate (mm/hr): liquid water equivelent average precipitation rate 
        public static double prate(double ut)
        {
            return weather_data.get2DVar(ut, "prate");
        }

        //Retrieve mslp (hPa): Mean Sea Level Pressure (Low = warm/wet/windy, high = cold/dry/calm) 
        public static double mslp(double ut)
        {
            return weather_data.get2DVar(ut, "mslp");
        }

        //Retrieve skin surface temperature: Over land = land surface temperature, Over water: sea surface temperature
        public static double sst(double ut)
        {
            return weather_data.get2DVar(ut, "sst");
        }

        //Compute cloud top temperatures using the Stefan-Boltzmann Law.
        public static double cloud_top_temps(double olr)
        {
            return Math.Pow((olr / Util.sigma), 0.25);
        }

    }
}
