using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    //API for accessing Kerbin climatogical data.
    public class climate_data : MonoBehaviour
    {
        const int NT = 6; //Time Dimension (hours in Kerbin Day)
        const int NZ = 17; //Height dimension (vertical levels) 
        const int NLAT = 91; //Latitude dimension (y-grid)
        const int NLON = 181; //Longitude dimension (x-grid)

        private static double invKerbinSLDensity = 0.8319f;

        //Structure defining vehicle velocity stats
        public struct wx_vel
        {
            public double vel_ias; //Indicated airspeed
            public double vel_tas; //True airspeed 
            public double vel_eas; //Equivalent airspeed
            public double vel_grnd; //Ground speed
            public double vel_par; //Component of wind perpendicular to flight path
            public double vel_prp; //Component of wind parallel to flight path 
        };

        //Structure defining full-atmosphere variables
        public struct wx_atm
        {
            public double wind_x;
            public double wind_y;
            public double wind_z;
            public double pressure;
            public double temperature;
            public double humidity;
            public double density;
            public double visibility;
            public double cloudcover;
        };


        //Structure defining surface climatological variables
        public struct wx_srf
        {
            public double wind_x;
            public double wind_y;
            public double olr;
            public double mslp;
            public double humidity;
            public double temperature;
            public double precipitation_rate;
            public double precitable_water;
            public double sst;
            public double cloudcover;
        };

        //Struct for aerodynamic variables
        public struct wx_aero
        {
            public double temperature;
            public double pressure;
            public double density;
        }

        //Struct for wind data
        public struct wx_wind
        {
            public double uwind;
            public double vwind;
            public double zwind;
        }

        //Initialize coordinates of climatological data
        public static float[] times = new float[NT];
        public static float[] heights = new float[NZ];
        public static float[] longitude = new float[NLON];
        public static float[] latitude = new float[NLAT];
        
        //Initialize MPAS data arrays [Variable,Times,Height,Latitude,Longitude]
        public static float[,,,,] climo_3d; //= new float[nvars, NT, NZ, NLAT, NLON];
        public static float[,,,] climo_2d; //= new float[nsvars, NT, NLAT, NLON];
        public static float[,,] heights_3d;

        public static Dictionary<string, int> Vars3d = new Dictionary<string, int>();
        public static Dictionary<string, int> Vars2d = new Dictionary<string, int>();
        
        //Retrieve coordinates of climatological data and adjust time.
        public static void get_dims()
        {
            //Read in coordinates from binary files
            heights_3d = read_climo.get_height_data();
            times = read_climo.getTime();

            // get list of times;
            for (int i = 0; i < NT; i++)
            {
                times[i] = times[i] * 900.0f; //Multiply time by 900 to account for fact that kerbin day is 6 hours
            }
            //Get lat/lng coords from binary files
            latitude = read_climo.getLat();
            longitude = read_climo.getLng();
        }

        //Define a dictionary to store the index of the coordinate that most costly matches the position of the current vessel
        public static Dictionary<string,int> get_dim_idx(double vlat, double vlng, double vheight, double epoch_time)
        {
            int lat_idx = Util.find_min(latitude, vlat); //index of nearest lat
            int lng_idx = Util.find_min(longitude, vlng); //index of nearest lng
            int tidx = Util.find_min(times, epoch_time); //index of nearest time

            for (int j = 0; j < NZ; j++)
            {
                heights[j] = heights_3d[lat_idx, lng_idx, j];
            }
            heights[NZ-1] = 70000;
            int hidx = Util.find_min(heights, vheight); //index of nearest height
                    
            if (hidx >= heights.Length)
            {
                hidx = heights.Length-1;
            }
            //Return dictionary of coordinate indices
            Dictionary<String, int> idx = new Dictionary<string, int>();
            idx.Add("y", lat_idx);
            idx.Add("x", lng_idx);
            idx.Add("t", tidx);
            idx.Add("z", hidx);
            return idx;
        }

        void Start()
        {
            //Read Kerbin Climatology data
            get_dims(); //Get coordinates of data
            //Retrieve climatological data for the full atmosphere (3D) and at the surface (2D)
            climo_3d = read_climo.getMPAS_3D("year");
            climo_2d = read_climo.getMPAS_2D("year");

            Vars3d.Add("w",0);
            Vars3d.Add("u",1);
            Vars3d.Add("v",2);
            Vars3d.Add("t",3);
            Vars3d.Add("p",4);
            Vars3d.Add("rh",5);
            Vars3d.Add("vis",6);
            Vars3d.Add("cld",7);

            Vars2d.Add("olr",0);
            Vars2d.Add("tcld",1);
            Vars2d.Add("pw",2);
            Vars2d.Add("prate",3);
            Vars2d.Add("mslp",4);
            Vars2d.Add("sst",5);
        }

        void Update()
        {
            if (climo_3d == null)
            {
                Destroy(this.gameObject);
            }
        }

        public static void get_climdata(string lsite)
        {
            //Read Kerbin Climatology data
            get_dims(); //Get coordinates of data
            //Retrieve climatological data for the full atmosphere (3D) and at the surface (2D)
            climo_3d = read_climo.getMPAS_3D("year");
            climo_2d = read_climo.getMPAS_2D("year");
        }


        //Generic function to retrieve select 3D atmospheric variable
        public static double get3DVar(double vlat, double vlng, double vheight, double ut, string vname)
        {
            double epoch_time = Util.getTime(ut); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, vheight, epoch_time);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            int vnum = Vars3d[vname];
            double vv = interp_var3d(vnum, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get climate data at vessel location
            return vv;
        }

        //Retrieve select 2-D atmospheric variable
        public static double get2DVar(double vlat, double vlng, double ut, string vname)
        {
            double epoch_time = Util.getTime(ut); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, 0, epoch_time);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int x = idx["x"]; int y = idx["y"];
            int vnum = Vars2d[vname];
            double v2d = trilinear_interp_sfc(vnum, t, y, x, epoch_time, vlat, vlng); //Retrieve outgoing longwave radiation (i.e. OLR) - (indicative of surface and cloud top temps)
            return v2d;
        }


        //Retrieve ambient temperature at vessel location
        public static double getAmbientPressure(double vlat, double vlng, double vheight)
        {
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, vheight, epoch_time);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            double pp = interp_var3d(4, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get climate data at vessel location
            return pp;
        }

        //Retrieve ambient temperature at vessel location
        public static double getAmbientTemperature(double vlat, double vlng, double vheight) { 
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, vheight, epoch_time);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            double tt = interp_var3d(3, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get climate data at vessel location
            return tt;
        }

        //Retrieve aerodynamic data at vessel location
        public static wx_aero getPTD(double vlat, double vlng, double vheight)
        {
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, vheight, epoch_time); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            double pp = interp_var3d(4, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get ambient pressure at vessel location
            double tt = interp_var3d(3, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get ambient temperature at vessel location
            //P = rho*RT; rho = P/RT  |  Gas constat for Dry air is 287.053 J/kg*K
            double rho = pp / (Util.Rd * tt); //Compute air density

            //Return data as struct
            wx_aero aero_data = new wx_aero();
            aero_data.temperature = tt;
            aero_data.pressure = pp;
            aero_data.density = rho;

            return aero_data;
        }

        //Retrieve wind data at vessel location
        public static wx_wind getWind(double vlat, double vlng, double vheight)
        {
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, vheight, epoch_time); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            double uu; double vv; double ww;
            ww = interp_var3d(0, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get vertical wind component (w) at vessel location
            uu = interp_var3d(1, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get zonal wind component (u) at vessel location
            vv = interp_var3d(2, t, z, y, x, epoch_time, vheight, vlat, vlng); //Get meridional wind component (v) at vessel location
            //Return data as struct
            wx_wind wind_data = new wx_wind();
            wind_data.uwind = uu;
            wind_data.vwind = vv;
            wind_data.zwind = ww;

            return wind_data;
        }
        //Interpolate data in space-time and return all 3D climatogical variables
        public static wx_atm getAmbientWx3D(double vlat, double vlng, double vheight)
        {
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat,vlng,vheight,epoch_time); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"]; int x = idx["x"]; int y = idx["y"];
            double uu; double vv; double ww; double tt; double pp; double rh; double vis; double cldfrac;
            ww = interp_var3d(0, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve vertical wind component
            uu = interp_var3d(1, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve zonal wind component
            vv = interp_var3d(2, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve meridional wind component
            tt = interp_var3d(3, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve ambient temperature
            pp = interp_var3d(4, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve ambient pressure
            rh = interp_var3d(5, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve relative humidity
            vis = interp_var3d(6, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve visibility
            cldfrac = interp_var3d(7, t, z, y, x, epoch_time, vheight, vlat, vlng); //Retrieve cloud fraction 
            //Compute air density
            double rho = (pp / (Util.Rd * tt));
            //Perform validity checks
            if (vis < 0)
            {
                vis = 0.0;
            }
            if (rh < 0)
            {
                rh = 0.0;
            } else if (rh > 100)
            {
                rh = 100.0;
            }
            //Return all fields as struct
            wx_atm wx_data = new wx_atm();
            wx_data.wind_x = uu;
            wx_data.wind_y = vv;
            wx_data.wind_z = ww;
            wx_data.temperature = tt;
            wx_data.pressure = pp;
            wx_data.density = rho;
            wx_data.humidity = rh;
            wx_data.visibility = vis;
            wx_data.cloudcover = cldfrac;
            return wx_data;
        }

        //Interpolate data in space-time and return all 2D climatogical  variables
        public static wx_srf getAmbientWx2D(double vlat, double vlng)
        {
            double epoch_time = Util.getLocalTime(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vlat, vlng, 0,epoch_time);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int x = idx["x"]; int y = idx["y"];

            double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
            olr = trilinear_interp_sfc(0, t, y, x, epoch_time, vlat, vlng); //Retrieve outgoing longwave radiation (i.e. OLR) - (indicative of surface and cloud top temps)
            tcld = trilinear_interp_sfc(1, t, y, x, epoch_time, vlat, vlng); //Get total cloud cover (i.e. vertically integrated cloud fraction)
            precipw = trilinear_interp_sfc(2, t, y, x, epoch_time, vlat, vlng); //Get precipitable water (i.e. the amount of liquid water that could be condensed out of the entire atmospheric column)
            rain = trilinear_interp_sfc(3, t, y, x, epoch_time, vlat, vlng); //Retrieve the precipitation rate (for snow this is liquid water equivalent)
            mslp = trilinear_interp_sfc(4, t, y, x, epoch_time, vlat, vlng); //Retrieve mean sea level pressure
            sst = trilinear_interp_sfc(5, t, y, x, epoch_time, vlat, vlng); //Retrieve skin surface temperature (over the ocean this is the sea surface temperature)
            // If precipitation rate is trace set it to zero.
            if (rain <= 0.1)
            {
                rain = 0;
            }
            tsfc = trilinear_interp_sfc3D(3, t, y, x, epoch_time, vlat, vlng); //Retrieve surface temperature
            rhsfc = trilinear_interp_sfc3D(5, t, y, x, epoch_time, vlat, vlng); //Retrieve relative humidity at surface
            uwnd_sfc = trilinear_interp_sfc3D(0, t, y, x, epoch_time, vlat, vlng); //Retrieve zonal wind component at surface
            vwnd_sfc = trilinear_interp_sfc3D(1, t, y, x, epoch_time, vlat, vlng); //Retrieve meridional wind component at surface

            //Return surface meteorological fields as struct
            wx_srf wx_data = new wx_srf();
            wx_data.olr = olr;
            wx_data.cloudcover = tcld;
            wx_data.precipitation_rate = rain;
            wx_data.precitable_water = precipw;
            wx_data.mslp = mslp;
            wx_data.humidity = rhsfc;
            wx_data.sst = sst;
            wx_data.temperature = tsfc;
            wx_data.wind_x = uwnd_sfc;
            wx_data.wind_y = vwnd_sfc;
            return wx_data;
        }

        public static wx_vel getVehicleVel(Vessel vessel, double vlat, double vlng, double vheight, bool wxenabled)
        {

            Vector3 windVector = Vector3.zero; //Wind in map space
            if (wxenabled)
            {
                //Get wind vector (in meteorological coordinates)
                wx_wind windvec = getWind(vlat, vlng, vheight);
                windVector.x = (float)windvec.vwind; //North
                windVector.y = (float)windvec.zwind; //Up
                windVector.z = (float)windvec.uwind; //East
            }

            //Retrieve mach
            double vel_mach;
            if (Util.far_GetMachNumber != null)
            {
                vel_mach = Util.far_GetMachNumber(vessel.mainBody, vessel.altitude, vessel.srf_velocity);
            }
            else
            {
                vel_mach = vessel.mach;
            }
            //Compute indicated airspeed
            double vel_ias = Math.Sqrt(((vessel.staticPressurekPa * 1000 * 2) * (Util.RayleighPitotTubeStagPressure(vel_mach) - 1)) / 1.225);
            //Get density ratio
            double d_ratio;
            if (Util.far_GetCurrentDensity != null)
            {
                d_ratio = Util.far_GetCurrentDensity(vessel.mainBody, vessel.altitude) * invKerbinSLDensity;
            }
            else
            {
                d_ratio = vessel.atmDensity / 1.225;
            }

            //Compute equivalent air speed
            double easCoeff = Math.Sqrt(d_ratio);
            double vel_eas = vessel.srfSpeed * easCoeff;

            if (vessel.staticPressurekPa <= 0)
            {
                vel_eas = 0;
            }

            //Get vessel orientation
            Vector3d CoM = vessel.CoMD;
            Vector3d orbitalVelocity = vessel.obt_velocity;
            Vector3d orbitalPosition = CoM - vessel.mainBody.position;

            Vector3d up = orbitalPosition.normalized;
            Vector3d north = vessel.north;
            Vector3d east = vessel.east;

            //Compute vessel surface velocity
            Vector3d surfaceVelocity = orbitalVelocity - vessel.mainBody.getRFrmVel(CoM);
            
            //Get components of vessel velocity
            double speedVertical = Vector3d.Dot(surfaceVelocity, up);
            double speedNorth = Vector3d.Dot(vessel.srf_velocity, north);
            double speedEast = Vector3d.Dot(vessel.srf_velocity, east);
            
            //Compute vessel ground speed
            double vel_grnd = Math.Sqrt(Math.Pow(speedEast, 2) + Math.Pow(speedNorth, 2)); 
            //Get true air speed w.r.t surface
            double vel_tas_sfc = Math.Sqrt(Math.Pow((windVector.z - speedEast), 2) + Math.Pow((windVector.x - speedNorth), 2));
            //Get true air speed (including vertical component)
            double vel_tas = Math.Sqrt(Math.Pow((windVector.z - speedEast), 2) + Math.Pow((windVector.x - speedNorth), 2) + Math.Pow((windVector.y - speedVertical), 2));
            //Get component of wind perpendicular and parallel to the vessel's flight path.  
            double projvu_sc = ((windVector.z * speedEast + windVector.x * speedNorth + windVector.y * speedVertical) / (Math.Pow(speedEast, 2) + Math.Pow(speedNorth, 2) + Math.Pow(speedVertical, 2)));
            double vel_par = Math.Sqrt(Math.Pow(projvu_sc * speedEast, 2) + Math.Pow(projvu_sc * speedNorth, 2) + Math.Pow(projvu_sc * speedVertical, 2));
            double vel_prp = Math.Sqrt(Math.Pow(windVector.z - projvu_sc * speedEast, 2) + Math.Pow(windVector.x - projvu_sc * speedNorth, 2) + Math.Pow(windVector.y - projvu_sc * speedVertical, 2));
            
            //If ground speed is slower than horizontal component of true air speed then there is a headwind.
            if (vel_grnd < vel_tas_sfc)
            {
                vel_par = vel_par * -1;
            }

            //Return all fields as struct
            wx_vel wx_data = new wx_vel();
            wx_data.vel_ias = vel_ias; //Indiciated airspeed
            wx_data.vel_tas = vel_tas; //True airspeed
            wx_data.vel_eas = vel_eas; //Equivalent airspeed 
            wx_data.vel_grnd = vel_grnd; //Surface velocity (i.e. ground speed)
            wx_data.vel_par = vel_par; //Component of wind perpendicular to aircraft
            wx_data.vel_prp = vel_prp; //Component of wind parallel to aircraft
            return wx_data;
        }

        public static wx_vel getVehicleVelCnst(Vessel vessel, double uwnd, double vwnd, double zwnd, bool wxenabled)
        {

            Vector3 windVector = Vector3.zero; //Wind in map space
            if (wxenabled)
            {
                //Get wind vector (in meteorological coordinates)
                windVector.x = (float)vwnd; //North
                windVector.y = (float)zwnd; //Up
                windVector.z = (float)uwnd; //East
            }

            //Retrieve mach
            double vel_mach;
            if (Util.far_GetMachNumber != null)
            {
                vel_mach = Util.far_GetMachNumber(vessel.mainBody, vessel.altitude, vessel.srf_velocity);
            }
            else
            {
                vel_mach = vessel.mach;
            }
            //Compute indicated airspeed
            double vel_ias = Math.Sqrt(((vessel.staticPressurekPa * 1000 * 2) * (Util.RayleighPitotTubeStagPressure(vel_mach) - 1)) / 1.225);
            //Get density ratio
            double d_ratio;
            if (Util.far_GetCurrentDensity != null)
            {
                d_ratio = Util.far_GetCurrentDensity(vessel.mainBody, vessel.altitude) * invKerbinSLDensity;
            }
            else
            {
                d_ratio = vessel.atmDensity / 1.225;
            }

            //Compute equivalent air speed
            double easCoeff = Math.Sqrt(d_ratio);
            double vel_eas = vessel.srfSpeed * easCoeff;

            if (vessel.staticPressurekPa <= 0)
            {
                vel_eas = 0;
            }

            //Get vessel orientation
            Vector3d CoM = vessel.CoMD;
            Vector3d orbitalVelocity = vessel.obt_velocity;
            Vector3d orbitalPosition = CoM - vessel.mainBody.position;

            Vector3d up = orbitalPosition.normalized;
            Vector3d north = vessel.north;
            Vector3d east = vessel.east;

            //Compute vessel surface velocity
            Vector3d surfaceVelocity = orbitalVelocity - vessel.mainBody.getRFrmVel(CoM);

            //Get components of vessel velocity
            double speedVertical = Vector3d.Dot(surfaceVelocity, up);
            double speedNorth = Vector3d.Dot(vessel.srf_velocity, north);
            double speedEast = Vector3d.Dot(vessel.srf_velocity, east);

            //Compute vessel ground speed
            double vel_grnd = Math.Sqrt(Math.Pow(speedEast, 2) + Math.Pow(speedNorth, 2));
            //Get true air speed w.r.t surface
            double vel_tas_sfc = Math.Sqrt(Math.Pow((windVector.z - speedEast), 2) + Math.Pow((windVector.x - speedNorth), 2));
            //Get true air speed (including vertical component)
            double vel_tas = Math.Sqrt(Math.Pow((windVector.z - speedEast), 2) + Math.Pow((windVector.x - speedNorth), 2) + Math.Pow((windVector.y - speedVertical), 2));
            //Get component of wind perpendicular and parallel to the vessel's flight path.  
            double projvu_sc = ((windVector.z * speedEast + windVector.x * speedNorth + windVector.y * speedVertical) / (Math.Pow(speedEast, 2) + Math.Pow(speedNorth, 2) + Math.Pow(speedVertical, 2)));
            double vel_par = Math.Sqrt(Math.Pow(projvu_sc * speedEast, 2) + Math.Pow(projvu_sc * speedNorth, 2) + Math.Pow(projvu_sc * speedVertical, 2));
            double vel_prp = Math.Sqrt(Math.Pow(windVector.z - projvu_sc * speedEast, 2) + Math.Pow(windVector.x - projvu_sc * speedNorth, 2) + Math.Pow(windVector.y - projvu_sc * speedVertical, 2));

            //If ground speed is slower than horizontal component of true air speed then there is a headwind.
            if (vel_grnd < vel_tas_sfc)
            {
                vel_par = vel_par * -1;
            }

            //Return all fields as struct
            wx_vel wx_data = new wx_vel();
            wx_data.vel_ias = vel_ias; //Indiciated airspeed
            wx_data.vel_tas = vel_tas; //True airspeed
            wx_data.vel_eas = vel_eas; //Equivalent airspeed 
            wx_data.vel_grnd = vel_grnd; //Surface velocity (i.e. ground speed)
            wx_data.vel_par = vel_par; //Component of wind perpendicular to aircraft
            wx_data.vel_prp = vel_prp; //Component of wind parallel to aircraft
            return wx_data;
        }

        //Perform 1-D linear interpolation in time.
        public static double linear_interp(double[] arr_2d, int tidx, double epoch_time)
        {
            /* Climate data is averaged by hour. Since there are only 6-h in one Kerbin day 
             * temporal interpolation between days is performed between the last hour of the previous day and the first hour of the following day*/
            int tidx2;
            if (tidx == 5)
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }
            //Perform 1-d linear interpolation in time
            double y1 = arr_2d[0] + (epoch_time - times[tidx]) * ((arr_2d[1] - arr_2d[0]) / (times[tidx2] - times[tidx]));
            return y1;
        }

        //Perform trilinear interpolation to get surface meteorological fields at a given time and position on Kerbin's surface (i.e. interpolate in longitude, latitude, and time)
        public static double trilinear_interp_sfc(int vidx, int tidx, int lat_idx, int lng_idx, double epoch_time, double lat, double lng)
        {
            //Get temporal indices to interpolate between (loop back to zeroth index when temporal window extends into the next day)
            int tidx2;
            if (tidx == NT - 1)
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }

            //Interpolate surface meteorological fields in space and time
            double xd = (lng - longitude[lng_idx]) / (longitude[lng_idx + 1] - longitude[lng_idx]);
            double yd = (lat - latitude[lat_idx]) / (latitude[lat_idx + 1] - latitude[lat_idx]);
            double zd = (epoch_time - times[tidx]) / (times[tidx2] - times[tidx]);

            double c00 = climo_2d[vidx, tidx, lat_idx, lng_idx] * (1 - xd) + climo_2d[vidx, tidx, lat_idx, lng_idx + 1] * xd;
            double c01 = climo_2d[vidx, tidx2, lat_idx, lng_idx] * (1 - xd) + climo_2d[vidx, tidx2, lat_idx, lng_idx + 1] * xd;
            double c10 = climo_2d[vidx, tidx, lat_idx + 1, lng_idx] * (1 - xd) + climo_2d[vidx, tidx, lat_idx + 1, lng_idx + 1] * xd;
            double c11 = climo_2d[vidx, tidx2, lat_idx + 1, lng_idx] * (1 - xd) + climo_2d[vidx, tidx2, lat_idx + 1, lng_idx + 1] * xd;

            double c0 = c00 * (1 - yd) + c10 * yd;
            double c1 = c01 * (1 - yd) + c11 * yd;

            double c = c0 * (1 - zd) + c1 * zd;
            return c;
        }

        //Perform trilinear interpolation to get surface meteorological fields at a given time and position on Kerbin's surface (i.e. interpolate in longitude, latitude, and time)
        public static double trilinear_interp_sfc3D(int vidx, int tidx, int lat_idx, int lng_idx, double epoch_time, double lat, double lng)
        {
            //Get temporal indices to interpolate between (loop back to zeroth index when temporal window extends into the next day)
            int tidx2;
            if (tidx == NT - 1)
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }

            //Interpolate lowest model level of 3D atmospheric fields in space and time
            double xd = (lng - longitude[lng_idx]) / (longitude[lng_idx + 1] - longitude[lng_idx]);
            double yd = (lat - latitude[lat_idx]) / (latitude[lat_idx + 1] - latitude[lat_idx]);
            double zd = (epoch_time - times[tidx]) / (times[tidx2] - times[tidx]);

            double c00 = climo_3d[vidx, tidx, 0, lat_idx, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, 0, lat_idx, lng_idx + 1] * xd;
            double c01 = climo_3d[vidx, tidx2, 0, lat_idx, lng_idx] * (1 - xd) + climo_3d[vidx, tidx2, 0, lat_idx, lng_idx + 1] * xd;
            double c10 = climo_3d[vidx, tidx, 0, lat_idx + 1, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, 0, lat_idx + 1, lng_idx + 1] * xd;
            double c11 = climo_3d[vidx, tidx2, 0, lat_idx + 1, lng_idx] * (1 - xd) + climo_3d[vidx, tidx2, 0, lat_idx + 1, lng_idx + 1] * xd;

            double c0 = c00 * (1 - yd) + c10 * yd;
            double c1 = c01 * (1 - yd) + c11 * yd;

            double c = c0 * (1 - zd) + c1 * zd;
            return c;
        }

        //Perform trilinear interpolation to retrieve 3D atmospheric fields at a point in 3D space (i.e. longitude, latitude, height ASL)
        public static double trilinear_interp(int vidx, int tidx, int hidx, int lat_idx, int lng_idx, double height, double lat, double lng)
        {
            //Interpolate in 3D space
            double xd = (lng - longitude[lng_idx]) / (longitude[lng_idx + 1] - longitude[lng_idx]);
            double yd = (lat - latitude[lat_idx]) / (latitude[lat_idx + 1] - latitude[lat_idx]);
            double zd = (height - heights[hidx]) / (heights[hidx + 1] - heights[hidx]);

            //Time is fixed here (temporal interpolatin is performed after trilinear interpolation in 3-D space, for full atmosphere fields)
            double c00 = climo_3d[vidx, tidx, hidx, lat_idx, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, hidx, lat_idx, lng_idx + 1] * xd;
            double c01 = climo_3d[vidx, tidx, hidx + 1, lat_idx, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, hidx + 1, lat_idx, lng_idx + 1] * xd;
            double c10 = climo_3d[vidx, tidx, hidx, lat_idx + 1, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, hidx, lat_idx + 1, lng_idx + 1] * xd;
            double c11 = climo_3d[vidx, tidx, hidx + 1, lat_idx + 1, lng_idx] * (1 - xd) + climo_3d[vidx, tidx, hidx + 1, lat_idx + 1, lng_idx + 1] * xd;

            double c0 = c00 * (1 - yd) + c10 * yd;
            double c1 = c01 * (1 - yd) + c11 * yd;

            double c = c0 * (1 - zd) + c1 * zd;
            return c;
        }

        //Interpolate 3D atmospheric fields in 4D (i.e. longitude, latitude, height ASL, and time)
        public static double interp_var3d(int vidx, int tidx, int hidx, int lat_idx, int lng_idx, double epoch_time, double height, double lat, double lng)
        {
            //Get temporal indices to interpolate between (loop back to zeroth index when temporal window extends into the next day)
            int tidx2;
            if (tidx == 5)
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }
            //Perform trilinear interpolation to interpolate atmospheric fields in 3D space (lat,lng,height)
            double[] tarr = new double[2];
            tarr[0] = trilinear_interp(vidx, tidx, hidx, lat_idx, lng_idx, height, lat, lng);
            tarr[1] = trilinear_interp(vidx, tidx2, hidx, lat_idx, lng_idx, height, lat, lng);

            //Interpolate interpolant results in time
            double fxy = linear_interp(tarr, tidx, epoch_time);

            //Return 4D interpolant
            return fxy;
        }
        
    }
}
