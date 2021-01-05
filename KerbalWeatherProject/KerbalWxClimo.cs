using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    //Main Class for processing climatological data
    public class KerbalWxClimo : MonoBehaviour
    {
        private const string Message = "[KerbalWeatherProject]: Initiated";
        internal const string MODID = "KerbalWeatherProject_NS";
        internal const string MODNAME = "KerbalWeatherProject";

        // wind
        public static Vector3 windVectorWS; // final wind direction and magnitude in world space
        public Vector3 windVector; // wind in "map" space, y = north, x = east (?)

        bool use_climo;
        bool aero;
        bool thermo;

        const int nsvars = 6;
        const int nvars = 8;
        List<double> vel_list = new List<double>();
        List<double> wx_list2d = new List<double>();
        List<double> wx_list3d = new List<double>();
        CelestialBody kerbin;
        public bool wx_enabled = true;
        public bool haveFAR = false;
        public bool disable_surface_wind;
        public bool cnst_wnd;
        public bool power_wnd;
        public int wspd_prof;
        public int wdir_prof;
        // book keeping data
        Matrix4x4 worldframe = Matrix4x4.identity; // orientation of the planet surface under the vessel in world space
        public climate_api _clim_api;

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
                var del = Delegate.CreateDelegate(WindFunction, this, typeof(KerbalWxClimo).GetMethod("GetTheWindClimo"), true);
                //var del = Delegate.CreateDelegate(WindFunction, this, typeof(KerbalWxClimo).GetMethod("GetTheWind"), true);
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
            //Determine if climatology should be used
            use_climo = Util.useCLIM();

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

            if (haveFAR)
            {
                CheckFAR();
            }

        }

        void Awake()
        {
            windVectorWS = Vector3.zero;

            check_settings();
            //Initialize climate api
            _clim_api = new climate_api();

            //Register with FAR
            //haveFAR = CheckFAR();
            //Util.Log("Have FAR: " + haveFAR);

            //Define Kerbin (only have weather data for Kerbin)
            kerbin = Util.getbody();

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
            //}
        }

        bool UpdateCoords()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null) return false;
            Vector3 east = vessel.east;
            Vector3 north = vessel.north;
            Vector3 up = vessel.upAxis;

            worldframe[0, 0] = north.x;
            worldframe[1, 0] = north.y;
            worldframe[2, 0] = north.z;
            worldframe[0, 1] = up.x;
            worldframe[1, 1] = up.y;
            worldframe[2, 1] = up.z;
            worldframe[0, 2] = east.x;
            worldframe[1, 2] = east.y;
            worldframe[2, 2] = east.z;
            return true;
        }

        void FixedUpdate()
        {

            check_settings();

            if ((!HighLogic.LoadedSceneIsFlight) || (!use_climo)) 
            {
                return;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            //Get vehicle position
            double vheight = vessel.altitude;
            double vlat = vessel.latitude;
            double vlng = vessel.longitude;
            if ((wx_enabled) && (use_climo))
            {
                UpdateCoords();

                if ((FlightGlobals.ActiveVessel.mainBody != kerbin))
                {
                    return;
                }
                if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (vessel.altitude >= 70000)) {

                    //Get 2D meteorological fields
                    double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
                    climate_api.wx_srf climate_data2D = _clim_api.getAmbientWx2D(vlat, vlng);
                    olr = climate_data2D.olr; tcld = climate_data2D.cloudcover; mslp = climate_data2D.mslp; precipw = climate_data2D.precitable_water; rain = climate_data2D.precipitation_rate;
                    sst = climate_data2D.sst; tsfc = climate_data2D.temperature; rhsfc = climate_data2D.humidity; uwnd_sfc = climate_data2D.wind_x; vwnd_sfc = climate_data2D.wind_y;
                    double wspd_sfc = Math.Sqrt(Math.Pow(uwnd_sfc, 2) + Math.Pow(vwnd_sfc, 2));

                    string bname = Util.getBiomeName(kerbin, vlng, vlat);
                    //Set surface temperature to SST over water
                    if (bname == "Water")
                    {
                        tsfc = sst;
                    }
                    if (tcld*100.0 < 0.0)
                    {
                        tcld = 0.0;
                    }
                    //Get 2D surface meteorological fields
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

                    double u; double v; double w; double t; double p; double d;  double rh; double vis; double cldfrac;
                    //Retrieve 3D meteorological fields
                    climate_api.wx_atm climate_data3D = _clim_api.getAmbientWx3D(vlat, vlng, vheight);
                    u = climate_data3D.wind_x; v = climate_data3D.wind_y; w = climate_data3D.wind_z; t = climate_data3D.temperature; d = climate_data3D.density;
                    p = climate_data3D.pressure; rh = climate_data3D.humidity; vis = climate_data3D.visibility; cldfrac = climate_data3D.cloudcover;

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
                    climate_api.wx_srf climate_data2D = _clim_api.getAmbientWx2D(vlat, vlng);
                    olr = climate_data2D.olr; tcld = climate_data2D.cloudcover; mslp = climate_data2D.mslp; precipw = climate_data2D.precitable_water; rain = climate_data2D.precipitation_rate;
                    sst = climate_data2D.sst; tsfc = climate_data2D.temperature; rhsfc = climate_data2D.humidity; uwnd_sfc = climate_data2D.wind_x; vwnd_sfc = climate_data2D.wind_y;
                    windVector.x = (float)v; //North
                    windVector.y = (float)w; //Up
                    windVector.z = (float)u; //East
                    windVectorWS = worldframe * windVector;

                    //Compute horizontal wind speed
                    double wspd_sfc = Math.Sqrt(Math.Pow(uwnd_sfc, 2) + Math.Pow(vwnd_sfc, 2));

                    string bname = Util.getBiomeName(kerbin, vlng, vlat);
                    if (bname == "water")
                    {
                        tsfc = sst;
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
                    climate_api.wx_vel vdata;
                    if (cnst_wnd)
                    {
                        vdata = _clim_api.getVehicleVelCnst(vessel, u, v, w, wx_enabled);
                    }
                    else
                    {
                        vdata = _clim_api.getVehicleVel(vessel, vlat, vlng, vheight, wx_enabled);
                    }

                    vel_ias = vdata.vel_ias; vel_tas = vdata.vel_tas; vel_eas = vdata.vel_eas; vel_grnd = vdata.vel_grnd; vel_par = vdata.vel_par; vel_prp = vdata.vel_prp;
                    vel_list[0] = vel_ias; //Indicated airspeed
                    vel_list[1] = vel_tas; //True airspeed (accounts for FAR Wind)
                    vel_list[2] = vel_eas; //Equivalent Airspeed 
                    vel_list[3] = vel_grnd; //Surface velocity (i.e. ground speed)
                    vel_list[4] = vel_par; //Component of wind perpendicular to aircraft
                    vel_list[5] = vel_prp; //Component of wind parallel to aircraft

                    Vector3d vwrld = vessel.GetWorldPos3D();
                    if (aero==false)
                    {
                        //3D atmospheric fields
                        wx_list3d[0] = 0;
                        wx_list3d[1] = 0;
                        wx_list3d[2] = 0;
                    } 
                    if (thermo==false) 
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
                climate_api.wx_vel vdata;
                if (cnst_wnd)
                {
                    vdata = _clim_api.getVehicleVelCnst(vessel, 0, 0, 0, wx_enabled);
                }
                else
                {
                    vdata = _clim_api.getVehicleVel(vessel, vlat, vlng, vheight, wx_enabled);
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
        public Vector3 GetTheWindClimo (CelestialBody body, Part part, Vector3 position)
        {

            if (!part || (part.partBuoyancy && part.partBuoyancy.splashed))
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