using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalWxPoint : MonoBehaviour
    {
        private const string Message = "[KerbalWeatherProject]: Initiated";
        internal const string MODID = "KerbalWeatherProject_NS";
        internal const string MODNAME = "KerbalWeatherProject";

        // wind
        public static Vector3 windVectorWS; // final wind direction and magnitude in world space
        Vector3 windVector; // wind in "map" space, y = north, x = east (?)
        bool use_point;
        bool aero;
        bool thermo;

        const int nvars = 8; //Variable dimension (number of 3D, full-atmosphere variables)
        const int nsvars = 6; //Surface Variable dimension (number of 2D surface variables)

        List<double> vel_list = new List<double>();
        List<double> wx_list2d = new List<double>();
        List<double> wx_list3d = new List<double>();

        CelestialBody kerbin = Util.getbody();
        public bool wx_enabled = true;
        public bool gotMPAS = false;
        // book keeping data
        Matrix4x4 worldframe = Matrix4x4.identity; // orientation of the planet surface under the vessel in world space
        public weather_api _wx_api;
        public climate_api _clim_api;

        public bool cnst_wnd;
        public bool power_wnd;
        public int wspd_prof;
        public int wdir_prof;

        public bool haveFAR = false;
        public bool disable_surface_wind;
        public string lsite = "KSC";

        List<string> lsites = new List<string>() { "KSC", "DLS", "WLS", "BKN", "BKN", "KHV", "KAT", "CKR", "SLK", "KRS" };
        List<string> lsites_name = new List<string>()
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

        List<double> lsites_lat = new List<double>() { -0.0972, -6.5603, 45.290, 20.6397, 6.0744, -37.1457, 24.9062, -37.25, -90.0 };
        List<double> lsites_lng = new List<double>() { -74.5571, -143.95, 136.1101, -146.4786, -142.0487, -71.0359, -83.6232, 52.70, 113.04703 };

        bool CheckFAR()
        {
            try
            {
                Type FARWind = null;
                Type WindFunction = null;

                foreach (var assembly in AssemblyLoader.loadedAssemblies)
                {
                    if (assembly.name == "FerramAerospaceResearch")
                    {
                        var types = assembly.assembly.GetExportedTypes();

                        foreach (Type t in types)
                        {
                            if (t.FullName.Equals("FerramAerospaceResearch.FARWind"))
                            {
                                FARWind = t;
                            }
                            if (t.FullName.Equals("FerramAerospaceResearch.FARWind+WindFunction"))
                            {
                                WindFunction = t;
                            }
                        }
                    }
                }
                if (FARWind == null)
                {
                    return false;
                }
                if (WindFunction == null)
                {
                    return false;
                }
                MethodInfo SetWindFunction = FARWind.GetMethod("SetWindFunction");
                if (SetWindFunction == null)
                {
                    return false;
                }
                var del = Delegate.CreateDelegate(WindFunction, this, typeof(KerbalWxPoint).GetMethod("GetTheWindPoint"), true); // typeof(KerbalWxPoint).GetMethod("GetTheWindPoint"), true);
                SetWindFunction.Invoke(null, new object[] { del });
                return true; // jump out
            }
            catch (Exception e)
            {
                Debug.LogError("KerbalWeatherProject: unable to register with FerramAerospaceResearch. Exception thrown: " + e.ToString());
            }
            return false;
        }

        void check_settings()
        {
            //Check to see if weather or climate data is to be used.
            use_point = Util.useWX();

            //Check to see if aero or thermo effects have been turned off
            aero = Util.allowAero();
            thermo = Util.allowThermo();

            //Determine if KWP is enabled
            wx_enabled = Util.getWindBool();

            //Get surface wind boolean
            disable_surface_wind = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().disable_surface_wind;

            //Retrieve wind profile booleans and parameters
            cnst_wnd = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().use_cnstprofile;
            wspd_prof = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().set_wspeed;
            wdir_prof = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec2>().set_wdir;

        }

        void Awake()
        {
            windVectorWS = Vector3.zero;

            check_settings();
            //Initialize weather api
            _wx_api = new weather_api();

            //Register with FAR
            haveFAR = CheckFAR();

            //Initialize vel data
            for (int i = 0; i < nvars; i++)
            {
                vel_list.Add(0);
            }

            //Initialize Wx list 2D
            for (int i = 0; i < nsvars + 2; i++)
            {
                wx_list2d.Add(0);
            }

            //Initialize Wx list 3D
            for (int i = 0; i < nvars+1; i++)
            {
                wx_list3d.Add(0);
            }
        }

        bool UpdateCoords()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null) return false;
            Vector3 east = vessel.east;
            Vector3 north = vessel.north;
            Vector3 up = vessel.upAxis;

            worldframe[0, 2] = east.x;
            worldframe[1, 2] = east.y;
            worldframe[2, 2] = east.z;
            worldframe[0, 1] = up.x;
            worldframe[1, 1] = up.y;
            worldframe[2, 1] = up.z;
            worldframe[0, 0] = north.x;
            worldframe[1, 0] = north.y;
            worldframe[2, 0] = north.z;
            return true;
        }

        void FixedUpdate()
        {

            check_settings();
            //If were not using point MPAS data or were not in flight return void
            if ((!HighLogic.LoadedSceneIsFlight) || (!use_point))
            {
                return;
            }
            Vessel vessel = FlightGlobals.ActiveVessel;
            //Get vehicle position
            double vheight = vessel.altitude;
            if ((wx_enabled) && (use_point))
            {
                UpdateCoords();

                if ((FlightGlobals.ActiveVessel.mainBody != kerbin))
                {
                    return;
                }

                string lsite0 = Util.get_last_lsite();
                //Check to see if launch site has changed
                string lsite = vessel.launchedFrom;
                //Util.Log("Currrent Launch Site: " + lsite);
                //Rename launchpad to KSC
                if (lsite != lsite0)
                {
                    //Util.Log("Launch site changed, update weather data | New: " + lsite + ", Old: " + lsite0);
                    gotMPAS = false;
                }

                int lidx;
                if (lsites_name.Contains(lsite))
                {
                    lidx = lsites_name.IndexOf(lsite);
                    //Util.Log("Launch site confirmed: " + lsites[lidx]);
                }
                else
                {
                    double mlat = vessel.latitude;
                    double mlng = vessel.longitude;
                    List<double> ddist = new List<double>();
                    for (int l = 0; l < lsites_lat.Count(); l++)
                    {
                        ddist.Add(Math.Pow((lsites_lat[l] - mlat), 2) + Math.Pow((lsites_lng[l] - mlng), 2));
                    }
                    lidx = ddist.IndexOf(ddist.Min());
                    //Util.Log("Retrieve longitude of nearest launch site: " + lsites[lidx]);
                }

                //Get current launch site
                Util.save_lsite(lsite);
                Util.save_lsite_short(lsites[lidx]);
                //If launch site has changed retrieved weather data for new launch site
                if (!gotMPAS)
                {
                    //Debug.Log("Current launch site: " + lsite);
                    //Util.Log("Retrieve weather data for new launch site");
                    if (lsites_name.Contains(lsite)) { 
                        //If launch site is known (i.e., in KSP or Kerbinside remastered)
                        lidx = lsites_name.IndexOf(lsite);
                        //Util.Log("Retrieve weather data for: " + lsites[lidx]);
                        _wx_api.Refresh();
                        gotMPAS = true;
                    } else { 
                        double mlat = vessel.latitude;
                        double mlng = vessel.longitude;
                        List<double> ddist = new List<double>();
                        for (int l = 0; l < lsites_lat.Count(); l++)
                        {
                            ddist.Add(Math.Pow((lsites_lat[l] - mlat), 2) + Math.Pow((lsites_lng[l] - mlng), 2));
                        }
                        int midx = ddist.IndexOf(ddist.Min());
                        //Util.Log("Retrieve nearby weather data for: " + lsites[midx]);
                        _wx_api.Refresh();
                        gotMPAS = true;
                    }
                    //Util.Log("GotMPAS: " + gotMPAS.ToString());
                }

                if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (vessel.altitude >= 70000))
                {

                    //Get 2D meteorological fields
                    double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
                    weather_api.wx_srf weather_data2D = _wx_api.getAmbientWx2D();
                    olr = weather_data2D.olr; tcld = weather_data2D.cloudcover; mslp = weather_data2D.mslp; precipw = weather_data2D.precitable_water; rain = weather_data2D.precipitation_rate;
                    sst = weather_data2D.sst; tsfc = weather_data2D.temperature; rhsfc = weather_data2D.humidity; uwnd_sfc = weather_data2D.wind_x; vwnd_sfc = weather_data2D.wind_y;
                    double wspd_sfc = Math.Sqrt(Math.Pow(uwnd_sfc, 2) + Math.Pow(vwnd_sfc, 2));

                    if (tcld < 0.0)
                    {
                        tcld = 0.0;
                    }

                    //Save 2D surface meteorological fields to list
                    wx_list2d[0] = olr;
                    wx_list2d[1] = tcld;
                    wx_list2d[2] = precipw;
                    wx_list2d[3] = rain;
                    wx_list2d[4] = mslp;
                    wx_list2d[5] = tsfc;
                    wx_list2d[6] = rhsfc;
                    wx_list2d[7] = wspd_sfc;
                    return;
                }
                if (FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {
                    double u; double v; double w; double t; double p; double rh; double vis; double cldfrac; double d;
                    //Retrieve 3D meteorological fields
                    weather_api.wx_atm weather_data3D = _wx_api.getAmbientWx3D(vheight);
                    u = weather_data3D.wind_x; v = weather_data3D.wind_y; w = weather_data3D.wind_z; t = weather_data3D.temperature; d = weather_data3D.density;
                    p = weather_data3D.pressure; rh = weather_data3D.humidity; vis = weather_data3D.visibility; cldfrac = weather_data3D.cloudcover;
                    if (((vessel.LandedOrSplashed) || (vessel.heightFromTerrain < 50)) && (disable_surface_wind))
                    {
                        u = 0; v = 0; w = 0;
                    }

                    //Override MPAS wind if a wind profile is selected/enabled.
                    if (cnst_wnd)
                    {
                        //Compute wind components
                        u = -wspd_prof * Math.Sin((Math.PI / 180.0) * wdir_prof);
                        v = -wspd_prof * Math.Cos((Math.PI / 180.0) * wdir_prof);
                        w = 0; //no vertical motion
                    }

                    //Retrieve 2D meteorological fields
                    double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
                    weather_api.wx_srf weather_data2D = _wx_api.getAmbientWx2D();

                    olr = weather_data2D.olr; tcld = weather_data2D.cloudcover; mslp = weather_data2D.mslp; precipw = weather_data2D.precitable_water; rain = weather_data2D.precipitation_rate;
                    sst = weather_data2D.sst; tsfc = weather_data2D.temperature; rhsfc = weather_data2D.humidity; uwnd_sfc = weather_data2D.wind_x; vwnd_sfc = weather_data2D.wind_y;
                    windVector.x = (float)v; //North
                    windVector.y = (float)w; //Up
                    windVector.z = (float)u; //East
                    windVectorWS = worldframe * windVector;
                    //Compute horizontal wind speed
                    double wspd_sfc = Math.Sqrt(Math.Pow(uwnd_sfc, 2) + Math.Pow(vwnd_sfc, 2));

                    if (tcld < 0)
                    {
                        tcld = 0;
                    }

                    //3D atmospheric fields
                    wx_list3d[0] = v;
                    wx_list3d[1] = u;
                    wx_list3d[2] = w;
                    wx_list3d[3] = p;
                    wx_list3d[4] = t;
                    wx_list3d[5] = rh;
                    wx_list3d[6] = vis;
                    wx_list3d[7] = cldfrac;
                    wx_list3d[8] = d;

                    //2D atmospheric fields
                    wx_list2d[0] = olr;
                    wx_list2d[1] = tcld;
                    wx_list2d[2] = precipw;
                    wx_list2d[3] = rain;
                    wx_list2d[4] = mslp;
                    wx_list2d[5] = tsfc;
                    wx_list2d[6] = rhsfc;
                    wx_list2d[7] = wspd_sfc;

                    //Get vehicle/wind relative velocity 
                    double vel_ias; double vel_tas; double vel_eas; double vel_grnd; double vel_par; double vel_prp;
                    weather_api.wx_vel vdata;
                    if (cnst_wnd)
                    {
                       vdata = _wx_api.getVehicleVelCnst(vessel, u, v, w, wx_enabled);
                    }
                    else
                    {
                        vdata = _wx_api.getVehicleVel(vessel, vheight, wx_enabled);
                    }

                    vel_ias = vdata.vel_ias; vel_tas = vdata.vel_tas; vel_eas = vdata.vel_eas; vel_grnd = vdata.vel_grnd; vel_par = vdata.vel_par; vel_prp = vdata.vel_prp;
                    vel_list[0] = vel_ias; //Indicated airspeed
                    vel_list[1] = vel_tas; //True airspeed (accounts for FAR Wind)
                    vel_list[2] = vel_eas; //Equivalent Airspeed 
                    vel_list[3] = vel_grnd; //Surface velocity (i.e. ground speed)
                    vel_list[4] = vel_par; //Component of wind perpendicular to aircraft
                    vel_list[5] = vel_prp; //Component of wind parallel to aircraft

                    Vector3d vwrld = vessel.GetWorldPos3D();
                    if (aero == false)
                    {
                        //3D atmospheric fields
                        wx_list3d[0] = 0;
                        wx_list3d[1] = 0;
                        wx_list3d[2] = 0;
                    }
                    if (thermo == false)
                    {
                        //3D atmospheric fields
                        wx_list3d[3] = Util.GetPressure(vwrld) * 1000;
                        wx_list3d[4] = Util.GetTemperature(vwrld);
                        wx_list3d[5] = 0;
                        wx_list3d[6] = 0;
                        wx_list3d[7] = 0;
                        wx_list3d[8] = Util.GetDensity(vwrld);
                    }
                }
                else
                {
                    windVectorWS = Vector3.zero;
                }
            }
            else
            {
                //Get vehicle/wind relative velocity 
                double vel_ias; double vel_tas; double vel_eas; double vel_grnd; double vel_par; double vel_prp;
                weather_api.wx_vel vdata;
                if (cnst_wnd)
                {
                    vdata = _wx_api.getVehicleVelCnst(vessel, 0, 0, 0, wx_enabled);
                }
                else
                {
                    vdata = _wx_api.getVehicleVel(vessel, vheight, wx_enabled);
                }

                vel_ias = vdata.vel_ias; vel_tas = vdata.vel_tas; vel_eas = vdata.vel_eas; vel_grnd = vdata.vel_grnd; vel_par = vdata.vel_par; vel_prp = vdata.vel_prp;
                vel_list[0] = vel_ias; //Indicated airspeed
                vel_list[1] = vel_tas; //True airspeed (accounts for FAR Wind)
                vel_list[2] = vel_eas; //Equivalent Airspeed 
                vel_list[3] = vel_grnd; //Surface velocity (i.e. ground speed)
                vel_list[4] = vel_par; //Component of wind perpendicular to aircraft
                vel_list[5] = vel_prp; //Component of wind parallel to aircraft

                Vector3d vwrld = vessel.GetWorldPos3D();
                //3D atmospheric fields
                wx_list3d[0] = 0;
                wx_list3d[1] = 0;
                wx_list3d[2] = 0;
                wx_list3d[3] = Util.GetPressure(vwrld)*1000;
                wx_list3d[4] = Util.GetTemperature(vwrld);
                wx_list3d[5] = 0;
                wx_list3d[6] = 0;
                wx_list3d[7] = 0;
                wx_list3d[8] = Util.GetDensity(vwrld);

                //2D atmospheric fields
                wx_list2d[0] = 0;
                wx_list2d[1] = 0;
                wx_list2d[2] = 0;
                wx_list2d[3] = 0;
                wx_list2d[4] = 0;
                wx_list2d[5] = 0;
                wx_list2d[6] = 0;
                wx_list2d[7] = 0;

                windVectorWS = Vector3.zero;
            }
        }
        public List<double> getAero()
        {
            return vel_list;
        }
        public Vector3d get3DWind()
        {
            return windVector;
        }

        public List<double> getWx3D()
        {
            return wx_list3d;
        }
        public static Vector3 getWSWind()
        {
            return windVectorWS;
        }

        public List<double> getWx2D()
        {
            return wx_list2d;
        }

        //Called by FAR. Returns wind vector.
        public Vector3 GetTheWindPoint(CelestialBody body, Part part, Vector3 position)
        {
            if (!part  || (part.partBuoyancy && part.partBuoyancy.splashed))
            {
                return Vector3.zero;
            }
            else
            {
                if (part.vessel == FlightGlobals.ActiveVessel)
                    return windVectorWS;
                else
                    return windVectorWS;
            }
        }

    }
}