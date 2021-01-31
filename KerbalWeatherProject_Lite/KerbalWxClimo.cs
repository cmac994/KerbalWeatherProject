using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KerbalWeatherProject_Lite
{
    
    using WindDelegate = Func<CelestialBody, Part, Vector3, Vector3>;
    using PropertyDelegate = Func<CelestialBody, Vector3d, double, double>;

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    //Main Class for processing climatological data
    public class KerbalWxClimo : MonoBehaviour
    {
        internal const string MODID = "KerbalWeatherProject_Lite_NS";
        internal const string MODNAME = "KerbalWeatherProject_Lite";

        // wind
        public static Vector3 windVectorWS; // final wind direction and magnitude in world space
        public Vector3 windVector; // wind in "map" space, y = north, x = east (?)
        //Temp & Pres
        public static double presWS;
        public static double tempWS;

        const int nsvars = 6;
        const int nvars = 8;
        List<double> vel_list = new List<double>();
        List<double> wx_list2d = new List<double>();
        List<double> wx_list3d = new List<double>();
        CelestialBody kerbin;
        public bool haveFAR = false;

        // book keeping data
        Matrix4x4 worldframe = Matrix4x4.identity; // orientation of the planet surface under the vessel in world space
        public climate_data _clim_api;

        //Check if FAR is available
        public bool CheckFAR()
        {
            try
            {
                //Define type methods
                Type WindFunction2 = null;
                Type FARAtm = null;

                foreach (var assembly in AssemblyLoader.loadedAssemblies)
                {
                    if (assembly.name == "FerramAerospaceResearch")
                    {
                        var types = assembly.assembly.GetExportedTypes();

                        foreach (Type t in types)
                        {
                            //Util.Log("Type: " + t.FullName);
                            if (t.FullName.Equals("FerramAerospaceResearch.FARWind"))
                            {
                                FARAtm = t;
                            }
                            if (t.FullName.Equals("FerramAerospaceResearch.FARWind+WindFunction"))
                            {
                                WindFunction2 = t;
                            }
                            if (t.FullName.Equals("FerramAerospaceResearch.FARAtmosphere"))
                            {
                                FARAtm = t;
                            }
                        }
                    }
                }

                //If no wind or atmosphere cs available return false
                if (FARAtm == null)
                {
                    return false;
                }

                //Check if Old Version of FAR is installed
                if (WindFunction2 != null)
                {
                    //Get FAR Wind Method
                    MethodInfo SetWindFunction = FARAtm.GetMethod("SetWindFunction");
                    if (SetWindFunction == null)
                    {
                        return false;
                    }
                    //Set FARWind function
                    var del = Delegate.CreateDelegate(WindFunction2, this, typeof(KerbalWxClimo).GetMethod("GetTheWind"), true);
                    SetWindFunction.Invoke(null, new object[] { del });
                    //Util.Log("SetWindFunc: " + SetWindFunction);

                }
                else
                {

                    //Get FAR Atmosphere Methods 
                    MethodInfo SetWindFunction = FARAtm.GetMethod("SetWindFunction");
                    MethodInfo SetTempFunction = FARAtm.GetMethod("SetTemperatureFunction");
                    MethodInfo SetPresFunction = FARAtm.GetMethod("SetPressureFunction");

                    //If no wind function available return false
                    if (SetWindFunction == null)
                    {
                        return false;
                    }

                    // Set FAR Atmosphere functions
                    WindDelegate del1 = GetTheWind;
                    //var del1 = Delegate.CreateDelegate(WindFunction, this, typeof(KerbalWxClimo).GetMethod("GetTheWind"), true); // typeof(KerbalWxPoint).GetMethod("GetTheWindPoint"), true);                                                                                                                                      //Util.Log("del1: " + del1);
                    SetWindFunction.Invoke(null, new object[] { del1 });

                    PropertyDelegate del2 = GetTheTemperature;
                    //var del2 = Delegate.CreateDelegate(TempFunction, this, typeof(KerbalWxClimo).GetMethod("GetTheTemperature"), true); // typeof(KerbalWxPoint).GetMethod("GetTheWindPoint"), true);
                    SetTempFunction.Invoke(null, new object[] { del2 });

                    PropertyDelegate del3 = GetThePressure;
                    //var del3 = Delegate.CreateDelegate(PresFunction, this, typeof(KerbalWxClimo).GetMethod("GetThePressure"), true); // typeof(KerbalWxPoint).GetMethod("GetTheWindPoint"), true);
                    SetPresFunction.Invoke(null, new object[] { del3 });

                    //Util.Log("SetWindFunc: " + SetWindFunction);
                }
                return true; // jump out
            }
            catch (Exception e)
            {
                Debug.LogError("KerbalWeatherProject_Lite: unable to register with FerramAerospaceResearch. Exception thrown: " + e.ToString());
            }
            return false;
        }

        void Awake()
        {
            windVectorWS = Vector3.zero;

            //Check settings
            Util.check_settings();

            //Register FAR
            CheckFAR();

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

            Util.check_settings();

            //Check if in flight or using point weather
            if ((!HighLogic.LoadedSceneIsFlight) || (!Util.use_climo)) 
            {
                return;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            //Get vehicle position
            double vheight = vessel.altitude;
            double vlat = vessel.latitude;
            double vlng = vessel.longitude;
            if ((Util.wx_enabled) && (Util.use_climo))
            {
                //Update reference frame
                UpdateCoords();

                //Just Kerbin weather for now
                if ((FlightGlobals.ActiveVessel.mainBody != kerbin))
                {
                    return;
                }
                //In space above Kerbin
                if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (vessel.altitude >= 70000)) {

                    //Get 2D meteorological fields
                    double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
                    climate_data.wx_srf climate_data2D = climate_data.getAmbientWx2D(vlat, vlng);
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
                //In Kerbin's atmosphere
                if (FlightGlobals.ready && FlightGlobals.ActiveVessel != null)
                {

                    double u; double v; double w; double t; double p; double d;  double rh; double vis; double cldfrac;
                    //Retrieve 3D meteorological fields
                    
                    climate_data.wx_atm climate_data3D = climate_data.getAmbientWx3D(vlat, vlng, vheight);
                    u = climate_data3D.wind_x; v = climate_data3D.wind_y; w = climate_data3D.wind_z; t = climate_data3D.temperature; d = climate_data3D.density;
                    p = climate_data3D.pressure; rh = climate_data3D.humidity; vis = climate_data3D.visibility; cldfrac = climate_data3D.cloudcover;

                    if (((vessel.LandedOrSplashed) || (vessel.heightFromTerrain < 50)) && (Util.disable_surface_wind))
                    {
                        u = 0; v = 0; w = 0;
                    }

                    //Override MPAS wind if a wind profile is selected/enabled.
                    if (Util.cnst_wnd)
                    {
                        //Compute wind components
                        u = -Util.wspd_prof * Math.Sin((Math.PI / 180.0) * Util.wdir_prof);
                        v = -Util.wspd_prof * Math.Cos((Math.PI / 180.0) * Util.wdir_prof);
                        w = 0; //no vertical motion
                    }

                    //Retrieve 2D meteorological fields
                    double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
                    climate_data.wx_srf climate_data2D = climate_data.getAmbientWx2D(vlat, vlng);
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
                    climate_data.wx_vel vdata;
                    if (Util.cnst_wnd)
                    {
                        vdata = climate_data.getVehicleVelCnst(vessel, u, v, w, Util.wx_enabled);
                    }
                    else
                    {
                        vdata = climate_data.getVehicleVel(vessel, vlat, vlng, vheight, Util.wx_enabled);
                    }

                    vel_ias = vdata.vel_ias; vel_tas = vdata.vel_tas; vel_eas = vdata.vel_eas; vel_grnd = vdata.vel_grnd; vel_par = vdata.vel_par; vel_prp = vdata.vel_prp;
                    vel_list[0] = vel_ias; //Indicated airspeed
                    vel_list[1] = vel_tas; //True airspeed (accounts for FAR Wind)
                    vel_list[2] = vel_eas; //Equivalent Airspeed 
                    vel_list[3] = vel_grnd; //Surface velocity (i.e. ground speed)
                    vel_list[4] = vel_par; //Component of wind perpendicular to aircraft
                    vel_list[5] = vel_prp; //Component of wind parallel to aircraft

                    Vector3d vwrld = vessel.GetWorldPos3D();
                    if (Util.allow_aero ==false)
                    {
                        //3D atmospheric fields
                        wx_list3d[0] = 0;
                        wx_list3d[1] = 0;
                        wx_list3d[2] = 0;
                    } 
                    if (Util.allow_thermo ==false) 
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
                //KWP Weather disabled use stock atmosphere instead

                //Get vehicle/wind relative velocity
                double vel_ias; double vel_tas; double vel_eas; double vel_grnd; double vel_par; double vel_prp;
                climate_data.wx_vel vdata;
                if (Util.cnst_wnd)
                {
                    vdata = climate_data.getVehicleVelCnst(vessel, 0, 0, 0, Util.wx_enabled);
                }
                else
                {
                    vdata = climate_data.getVehicleVel(vessel, vlat, vlng, vheight, Util.wx_enabled);
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
            tempWS = wx_list3d[4];
            presWS = wx_list3d[3];
        }

        //Series of internal functions for GUI
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
        public Vector3 GetTheWind(CelestialBody body, Part part, Vector3 position)
        {
            if (!part || (part.partBuoyancy && part.partBuoyancy.splashed))
            {
                return Vector3.zero;
            }
            else
            {
                if (Util.use_climo)
                {
                    //Util.Log("CLIMO Windvec: " + windVectorWS.ToString() + ", use_climo: " + Util.use_climo.ToString() + ", use_point: " + Util.use_point.ToString() + ", wx_enabled: " + Util.wx_enabled);
                    return windVectorWS;
                }
                else
                {
                    //Util.Log("Point Windvec: " + KerbalWxPoint.windVectorWS.ToString() + ", use_climo: " + Util.use_climo.ToString() + ", use_point: " + Util.use_point.ToString() + ", wx_enabled: " + Util.wx_enabled);
                    return KerbalWxPoint.windVectorWS;
                }
            }
        }

        //Called by FAR. Returns temperature.
        public double GetTheTemperature(CelestialBody body, Vector3d latlonAltitude, double ut)
        {
            //Retrieve air temperature at vessel location
            if (Util.use_climo)
            {
                return tempWS;
            }
            else
            {
                return KerbalWxPoint.tempWS;
            }
        }

        //Called by FAR. Returns pressure.
        public double GetThePressure(CelestialBody body, Vector3d latlonAltitude, double ut)
        {
            //Retrieve air pressure at vessel location
            if (Util.use_climo)
            {
                return presWS;
            }
            else
            {
                return KerbalWxPoint.presWS;
            }
        }

    }
}