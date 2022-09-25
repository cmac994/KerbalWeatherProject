using KSP.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KerbalWeatherProject
{
    internal static class Util
    {

        public static string lsite_name = "LaunchPad";
        public static string lsite = "KSC";
        const double NT = 12810.0; //Time dimension (length of weather time-series)
        public static bool wx_enabled = true;
        public static bool use_climo = true;
        public static bool use_point = false;
        public static bool allow_aero = true;
        public static bool allow_thermo = true;
        public static bool use_mfi = false;

        public static bool disable_surface_wind;
        public static bool cnst_wnd;
        public static int wspd_prof;
        public static int wdir_prof;

        //Atmospheric Constants
        public static double g = 9.80665; //Gravitational constant m/s^2
        public static double md = 28.9655; //Molar mass, dry air (kg/kmol)
        public static double mw = 18.0153; //Molar mass, water (kg/kmol)
        public static double epsilon = 0.622; //Molecular weight ratio of H20 to dry air
        public static double cp_d = 1004.67; //Specific heat capacity of dry air at constant pressure (J/kg*K)
        public static double cp_v = 717; //Specific heat capacity of dry air at constant volume (J/kg*K)
        public static double cp_w = 4186; //Specific heat capacity of liquid water
        public static double Gamma_d = g / cp_d; //Dry adiabatic lapse rate;
        public static double gamma = cp_d / cp_v; //adiabatic index (i.e. heat capacity ratio)
        public static double Lv = 2.5 * 1e6; //Latent heat of vaporization at 0 C
        public static double R = 8.314427; //Universal gas constant (J/K*mol)
        public static double Rd = 287.047; //Gas constant, dry air (J/kg*K)
        public static double Rv = 461.5; //Gas constant, water vapor (J/kg*K)
        public static double sigma = 5.670374419 * 1e-8; //Stefan-Boltzmann Constant (W/m^2 K-4)

        public static string logTag = "KerbalWeatherProject";

        //Structure defining full-atmosphere variables
        public struct aero_stats
        {
            public double alpha;
            public double sideslip;
            public double soundSpeed;
            public double mach;
            public double lift;
            public double drag;
            public double ldRatio;
            public double Q;
            public double shockTemp;

        };

        public static string getPath(string data_type, string fname)
        {
            // Initialize path to file
            string file_path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", logTag, "Binary", data_type, fname);
            return file_path;
        }


        public static void check_settings()
        {

            //Get surface wind boolean
            disable_surface_wind = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().disable_surface_wind;

            //Retrieve wind profile booleans and parameters
            cnst_wnd = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().use_cnstprofile;
            wspd_prof = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().set_wspeed;
            wdir_prof = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().set_wdir;

            //MFI Settings
            allow_aero = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams>().allow_aero;
            allow_thermo = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams>().allow_thermo;
        }

        public static aero_stats aero_sdata;
        //Log functions: Adapted from Dynamic Battery Storage
        public static void Log(string toLog)
        {
            Debug.Log(string.Format("[{0}]: {1}", logTag, toLog));
        }
        public static void Warn(string toLog)
        {
            Debug.LogWarning(string.Format("[{0}]: {1}", logTag, toLog));
        }
        public static void Error(string toLog)
        {
            Debug.LogError(string.Format("[{0}]: {1}", logTag, toLog));
        }

        public static string get_wind_card(double wdir, string wstr)
        {
            //Debug.Log("[KerbalWxGUI]: " + wstr);
            string wdir_str = "N/A";
            if (wstr == "N,S,E,W")
            {
                //Cardinal Wind Directions
                if ((wdir > 315) || (wdir <= 45))
                {
                    wdir_str = "N";
                }
                else if ((wdir > 45) && (wdir <= 135))
                {
                    wdir_str = "E";
                }
                else if ((wdir > 135) && (wdir <= 225))
                {
                    wdir_str = "S";
                }
                else if ((wdir > 225) && (wdir <= 315))
                {
                    wdir_str = "W";
                }
            }
            else if (wstr == "N,NE,E,SE,...")
            {
                //Principle Wind Directions
                if ((wdir > 337.5) || (wdir <= 22.5))
                {
                    wdir_str = "N";
                }
                else if ((wdir > 22.5) && (wdir <= 67.5))
                {
                    wdir_str = "NE";
                }
                else if ((wdir > 67.5) && (wdir <= 112.5))
                {
                    wdir_str = "E";
                }
                else if ((wdir > 112.5) && (wdir <= 157.5))
                {
                    wdir_str = "SE";
                }
                else if ((wdir > 157.5) && (wdir <= 202.5))
                {
                    wdir_str = "S";
                }
                else if ((wdir > 202.5) && (wdir <= 247.5))
                {
                    wdir_str = "SW";
                }
                else if ((wdir > 247.5) && (wdir <= 292.5))
                {
                    wdir_str = "W";
                }
                else if ((wdir > 292.5) && (wdir <= 337.5))
                {
                    wdir_str = "NW";
                }
            }
            else if (wstr == "N,NNE,NE,ENE,...")
            {
                //Half-wind and Principle Wind Direction
                if ((wdir > 348.75) || (wdir <= 11.25))
                {
                    wdir_str = "N";
                }
                else if ((wdir > 11.25) && (wdir <= 33.75))
                {
                    wdir_str = "NNE";
                }
                else if ((wdir > 33.75) && (wdir <= 56.25))
                {
                    wdir_str = "NE";
                }
                else if ((wdir > 56.25) && (wdir <= 78.75))
                {
                    wdir_str = "ENE";
                }
                else if ((wdir > 78.75) && (wdir <= 101.25))
                {
                    wdir_str = "E";
                }
                else if ((wdir > 101.25) && (wdir <= 123.75))
                {
                    wdir_str = "ESE";
                }
                else if ((wdir > 123.75) && (wdir <= 146.25))
                {
                    wdir_str = "SE";
                }
                else if ((wdir > 146.25) && (wdir <= 168.75))
                {
                    wdir_str = "SSE";
                }
                else if ((wdir > 168.75) && (wdir <= 191.25))
                {
                    wdir_str = "S";
                }
                else if ((wdir > 191.25) && (wdir <= 213.75))
                {
                    wdir_str = "SSW";
                }
                else if ((wdir > 213.75) && (wdir <= 236.25))
                {
                    wdir_str = "SW";
                }
                else if ((wdir > 236.25) && (wdir <= 258.75))
                {
                    wdir_str = "WSW";
                }
                else if ((wdir > 258.75) && (wdir <= 281.25))
                {
                    wdir_str = "W";
                }
                else if ((wdir > 281.25) && (wdir <= 303.75))
                {
                    wdir_str = "WNW";
                }
                else if ((wdir > 303.75) && (wdir <= 326.25))
                {
                    wdir_str = "NW";
                }
                else if ((wdir > 326.25) && (wdir <= 348.75))
                {
                    wdir_str = "NNW";
                }
            }
            return wdir_str;
        }

        public static double convert_wspeed(double val, string sunit)
        {
            switch (sunit)
            {
                //Default is kts
                default:
                    return val * 1.94384;
                case "m/s":
                    return val;
                case "mph":
                    return val * 2.23694;
                case "km/h":
                    return val * 3.6;
            }
        }

        public static double convert_veh_speed(double val, string sunit)
        {
            switch (sunit)
            {
                //default is m/s
                default:
                    return val;
                case "kts":
                    return val * 1.94384;
                case "mph":
                    return val * 2.23694;
                case "km/h":
                    return val * 3.6;
            }
        }

        public static double convert_pres(double val, string sunit)
        {
            switch (sunit)
            {
                //Default is hPa
                default:
                    return val / 100.0;
                case "Pa":
                    return val;
                case "inHg":
                    return val * 0.0002953;
                case "mmHg":
                    return val * 0.00750062;
                case "psi":
                    return val * 0.000145038;
                case "atm":
                    return val / 101325.0;

            }
        }

        public static double convert_temp(double val, string sunit)
        {
            switch (sunit)
            {
                //Default is Kelvin (K)
                default:
                    return val;
                case "C":
                    return val - 273.15;
                case "F":
                    return (val - 273.15) * (9.0 / 5.0) + 32;
                case "Ra":
                    return val * 1.8;
            }
        }

        public static double convert_precip(double val, string sunit)
        {
            switch (sunit)
            {
                //Default is mm
                default:
                    return val;
                case "cm":
                    return val * 0.1;
                case "in":
                    return val * 0.0393701;
            }
        }

        //Adapted from SCANSAT
        private static int getBiomeIndex(CelestialBody body, double lon, double lat)
        {

            CBAttributeMapSO.MapAttribute att = body.BiomeMap.GetAtt(Mathf.Deg2Rad * lat, Mathf.Deg2Rad * lon);
            for (int i = 0; i < body.BiomeMap.Attributes.Length; ++i)
            {
                if (body.BiomeMap.Attributes[i] == att)
                {
                    return i;
                }
            }
            return -1;
        }
        internal static CBAttributeMapSO.MapAttribute getBiome(CelestialBody body, double lon, double lat)
        {
            if (body.BiomeMap == null) return null;
            int i = getBiomeIndex(body, lon, lat);
            if (i == -1)
                return null;
            return body.BiomeMap.Attributes[i];
        }

        internal static string getBiomeName(CelestialBody body, double lon, double lat, bool getdisplayname=false)
        {
            CBAttributeMapSO.MapAttribute a = getBiome(body, lon, lat);
            if (a == null)
                return "unknown";
            if (getdisplayname)
                return a.displayname;
            return a.name;
        }

        //Adpated from FAR
        private static CelestialBody currentBody = null;
        public static CelestialBody CurrentBody
        {
            get
            {
                if ((object)currentBody == null)
                {
                    if (FlightGlobals.Bodies[1] || !FlightGlobals.ActiveVessel)
                        currentBody = FlightGlobals.Bodies[1];
                    else
                        currentBody = FlightGlobals.ActiveVessel.mainBody;

                }
                return currentBody;
            }
        }

        public delegate double FAR_GetMachNumber(CelestialBody body, double altitude, Vector3 speed);
        public static FAR_GetMachNumber far_GetMachNumber = null;
        public static FAR_GetCurrentDensity far_GetCurrentDensity = null;
        public delegate double FAR_GetCurrentDensity(CelestialBody body, double altitude);

        public static bool RegisterWithFAR()
        {
            try
            {
                foreach (var assembly in AssemblyLoader.loadedAssemblies)
                {
                    if (assembly.name == "FerramAerospaceResearch")
                    {
                        var types = assembly.assembly.GetExportedTypes();

                        foreach (Type t in types)
                        {
                            if (t.FullName.Equals("FerramAerospaceResearch.FARAeroUtil"))
                            {
                                far_GetMachNumber = (FAR_GetMachNumber)Delegate.CreateDelegate(typeof(FAR_GetMachNumber), t, "GetMachNumber", false, false);
                                far_GetCurrentDensity = (FAR_GetCurrentDensity)Delegate.CreateDelegate(typeof(FAR_GetCurrentDensity), t, "GetCurrentDensity", false, false);
                            }
                        }
                    }
                }
                if (far_GetMachNumber == null)
                {
                    return false;
                }
                return true; // jump out
            }
            catch (Exception e)
            {
                Debug.LogError("KerbalWeatherProject_Lite: unable to register with FerramAerospaceResearch. Exception thrown: " + e.ToString());
            }
            return false;
        }

        public static void setMAPSDtype(bool use_clim, bool use_pnt)
        {
            use_climo = use_clim;
            use_point = use_pnt;
        }

        public static bool allowAero()
        {
            return allow_thermo;
        }

        public static bool allowThermo()
        {
            return allow_aero;
        }

        public static bool useCLIM()
        {
            return use_climo;
        }
        public static bool useWX()
        {
            return use_point;
        }

        public static void setWindBool(bool wx_allowed)
        {
            wx_enabled = wx_allowed;
        }

        public static bool getWindBool()
        {
            return wx_enabled;
        }

        public static void setAero(aero_stats aero_data)
        {
            aero_sdata = aero_data;
        }

        public static aero_stats getAero()
        {
            return aero_sdata;
        }
        public static string get_last_lsite_short()
        {
            return lsite;
        }

        public static string get_last_lsite()
        {
            return lsite_name;
        }

        public static string save_lsite(string llsite)
        {
            lsite_name = llsite;
            return lsite_name;
        }
        public static string save_lsite_short(string lssite)
        {
            lsite = lssite;
            return lsite;
        }

        internal static int find_min(float[] arr1, double vval)
        {

            double[] adiff = new double[arr1.Length];
            for (int i = 0; i < arr1.Length; i++)
            {
                adiff[i] = Math.Abs(arr1[i] - vval);
            }
            int midx = Array.IndexOf(adiff, adiff.Min());
            if ((arr1[midx] > vval) && (midx > 0))
            {
                midx = midx - 1;
            }
            return midx;
        }

        //Adpated from FAR
        public static double StagnationPressureCalc(double M)
        {
            double gamma = CurrentBody.atmosphereAdiabaticIndex;

            double ratio;
            ratio = M * M;
            ratio *= (gamma - 1);
            ratio *= 0.5;
            ratio++;

            ratio = Math.Pow(ratio, gamma / (gamma - 1));
            return ratio;
        }

        //Adpated from FAR
        public static double RayleighPitotTubeStagPressure(double M)
        {
            if (M <= 1)
                return StagnationPressureCalc(M);

            double gamma = CurrentBody.atmosphereAdiabaticIndex;
            double value;
            value = (gamma + 1) * M;                  //Rayleigh Pitot Tube Formula; gives max stagnation pressure behind shock
            value *= value;
            value /= (4 * gamma * M * M - 2 * (gamma - 1));
            value = Math.Pow(value, gamma / (gamma - 1));

            value *= (1 - gamma + 2 * gamma * M * M);
            value /= (gamma + 1);

            return value;
        }

        //Adpated from FAR
        public static double GetCurrentDensity(Vessel v)
        {
            double density = 0;
            double counter = 0;
            foreach (Part p in v.parts)
            {
                if (p.physicalSignificance == Part.PhysicalSignificance.NONE)
                    continue;

                density += p.dynamicPressurekPa * (1.0 - p.submergedPortion);
                density += p.submergedDynamicPressurekPa * p.submergedPortion;
                counter++;
            }

            if (counter > 0)
                density /= counter;
            density *= 2000; //need answers in Pa, not kPa
            density /= v.srfSpeed * v.srfSpeed;

            return density;
        }

        //Compute distance between points on sphere
        public static class Haversine
        {
            public static double calculate(double lat1, double lon1, double lat2, double lon2, double vhgt)
            {
                var R = 600 * 1000 + vhgt; // 6372.8; // In meters
                var dLat = toRadians(lat2 - lat1);
                var dLon = toRadians(lon2 - lon1);
                lat1 = toRadians(lat1);
                lat2 = toRadians(lat2);

                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
                return R * 2 * Math.Asin(Math.Sqrt(a));
            }

            public static double toRadians(double angle)
            {
                return Math.PI * angle / 180.0;
            }
        }

        //Get Kerbin cbody
        public static CelestialBody getbody()
        {
            List<CelestialBody> cbodies = PSystemManager.Instance.localBodies;
            return cbodies[1];
        }

        //Adapted from FAR
        public static bool NearlyEqual(this float a, float b, float epsilon = 1e-6f)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            // shortcut, handles infinities
            if (a.Equals(b))
                return true;

            // a or b is zero or both are extremely close to it
            // relative error is less meaningful here
            float diff = Math.Abs(a - b);
            if (a == 0 || b == 0 || diff < float.Epsilon)
                return diff < epsilon * float.Epsilon;

            // use relative error
            return diff / (Math.Abs(a) + Math.Abs(b)) < epsilon;

        }

        //Adapted from FAR
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        //Convert from GMT to Local (UT) time. 
        public static double getLocalTime()
        {
            double epoch = Planetarium.GetUniversalTime();
            epoch = (epoch + 3600 * ((((180.0 - 74.724375) / 15.0) / 24.0) * 6));
            //Util.Log("Epoch time: " + epoch_time.ToString());
            epoch = ((epoch / 21600.0) - (int)(epoch / 21600.0)) * 21600.0;
            return epoch;
        }


        //Convert Any Time from GMT to Local (UT) time. 
        public static double getTime(double epoch)
        {
            epoch = (epoch + 3600 * ((((180.0 - 74.724375) / 15.0) / 24.0) * 6));
            //Util.Log("Epoch time: " + epoch_time.ToString());
            epoch = ((epoch / 21600.0) - (int)(epoch / 21600.0)) * 21600.0;
            return epoch;
        }

        //Convert Model time to UT time.
        public static double getTime_Wx(double epoch_time)
        {
            epoch_time = (epoch_time + 3600 * ((((180.0 - 74.724375) / 15.0) / 24.0) * 6)); //Adjust for Local kerbin time @ KSC. 
            int nrun = (int)(epoch_time / (NT * 3600));
            epoch_time = epoch_time - (NT * 3600)*nrun;
            if (epoch_time == NT*3600)
            {
                epoch_time = epoch_time - 1;
            }
            return epoch_time;
        }

        //Convert Model time to UT time.
        public static double getLocalTime_Wx()
        {
            double epoch_time = Planetarium.GetUniversalTime();
            epoch_time = (epoch_time + 3600 * ((((180.0 - 74.724375) / 15.0) / 24.0) * 6)); //Adjust for Local kerbin time @ KSC.                    
            int nrun = (int)(epoch_time / (NT * 3600));
            epoch_time = epoch_time - (NT * 3600)*nrun;
            if (epoch_time == NT*3600)
            {
                epoch_time = epoch_time - 1;
            }
            return epoch_time;
        }

        //Adapted from Ship Manifest
        internal static Dictionary<string, string> KWPTags;
        internal static void CacheKWPLocalization()
        {
            KWPTags = new Dictionary<string, string>();
            IEnumerator tags = Localizer.Tags.Keys.GetEnumerator();
            while (tags.MoveNext())
            {
                if (tags.Current == null) continue;
                if (tags.Current.ToString().Contains("#autoLOC_"))
                {
                    KWPTags.Add(tags.Current.ToString(), Localizer.GetStringByTag(tags.Current.ToString()).Replace("\\n", "\n"));
                }
            }
        }

        public static double GetTemperature(Vector3d position)
        {
            CelestialBody body = getbody();
            if (!body.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3 up = (position - body.position).normalized;
            float polarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            if (polarAngle > Mathf.PI / 2.0f)
            {
                polarAngle = Mathf.PI - polarAngle;
            }
            float time = (Mathf.PI / 2.0f - polarAngle) * 57.29578f;

            Vector3 sunVector = (FlightGlobals.Bodies[0].position - position).normalized;
            float sunAxialDot = Vector3.Dot(sunVector, body.bodyTransform.up);
            float bodyPolarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            float sunPolarAngle = Mathf.Acos(sunAxialDot);
            float sunBodyMaxDot = (1.0f + Mathf.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5f;
            float sunBodyMinDot = (1.0f + Mathf.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5f;
            float sunDotCorrected = (1.0f + Vector3.Dot(sunVector, Quaternion.AngleAxis(45f * Mathf.Sign((float)body.rotationPeriod), body.bodyTransform.up) * up)) * 0.5f;
            float sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = (double)body.latitudeTemperatureBiasCurve.Evaluate(time) + (double)body.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized + (double)body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            double temperature = body.GetTemperature(altitude) + (double)body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;
            return temperature;
        }

        public static double GetDensity(Vector3d position)
        {
            CelestialBody body = getbody();
            if (!body.atmosphere)
                return 0;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);
            double temperature = // body.GetFullTemperature(position);
                GetTemperature(position);

            return body.GetDensity(pressure, temperature);
        }

        public static double GetPressure(Vector3d position)
        {
            CelestialBody body = getbody();
            if (!body.atmosphere)
                return 0;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);
            return pressure;
        }

        public static double round_down(double val, int prec)
        {
            double frac = Math.Pow(10, prec);
            return Math.Floor(val * frac) / frac;
        }

    }
}