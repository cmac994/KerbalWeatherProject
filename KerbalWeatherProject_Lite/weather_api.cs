using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalWeatherProject_Lite
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    //API for accessing Kerbin climatogical data.
    public class weather_api : MonoBehaviour
    {

        public read_wx _kwx_read;
        const int NT = 2556; //Time dimension (length of weather time-series)
        const int NZ = 17; //Height dimension (number of vertical levels)
        const int nvars = 8; //Variable dimension (number of 3D, full-atmosphere variables)
        const int nsvars = 6; //Surface Variable dimension (number of 2D surface variables)

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

        //Initialize coordinates of weather time-series data
        public static float[] times = new float[NT];
        public static float[] heights = new float[NZ];

        //Initialize 2-d and 3-d weather (i.e. meteorological) fields
        public static float[,,] point_3d = new float[nvars, NT, NZ];
        public static float[,] point_2d = new float[nsvars, NT];

        //Retrieve coordinates of climatological data and adjust time.
        public void get_dims()
        {
            //Read in coordinates from binary files
            heights = _kwx_read.getHeight();
            times = _kwx_read.getTime();
        }

        //Define a dictionary to store the index of the coordinate that most costly matches the position of the current vessel
        public Dictionary<string, int> get_dim_idx(double vheight)
        {
            double epoch_time = Util.getLocalTime_Wx();
            int tidx = Util.find_min(times, epoch_time); //index of nearest time
            int hidx = Util.find_min(heights, vheight); //index of nearest height

            if (hidx >= heights.Length)
            {
                hidx = heights.Length - 1;
            }
            //Return dictionary of coordinate indices
            Dictionary<String, int> idx = new Dictionary<string, int>();
            idx.Add("t", tidx);
            idx.Add("z", hidx);
            return idx;
        }

        void Start()
        {
            //Read Kerbin Climatology data
            _kwx_read = new read_wx();
            get_dims(); //Get coordinates of data
            //Retrieve meteorological data for the full atmosphere (3D) and at the surface (2D)
            string lsite = Util.get_last_lsite_short();
            //Util.Log("Weather API: Last Launch Site: " + lsite);
            point_3d = _kwx_read.getMPAS_3D(lsite);
            point_2d = _kwx_read.getMPAS_2D(lsite);
        }

        public void Refresh()
        {
            //Read Kerbin Climatology data
            _kwx_read = new read_wx();
            get_dims(); //Get coordinates of data
            //Retrieve meteorological data for the full atmosphere (3D) and at the surface (2D)
            string lsite = Util.get_last_lsite_short();
            //Util.Log("Weather API: Last Launch Site: " + lsite);
            point_3d = _kwx_read.getMPAS_3D(lsite);
            point_2d = _kwx_read.getMPAS_2D(lsite);
        }

        void Update()
        {
            if (point_3d == null)
            {
                Destroy(this.gameObject);
            }
        }

        //Retrieve ambient temperature at vessel location
        public double getAmbientTemperature(double vheight)
        {
            double epoch_time = Util.getLocalTime_Wx(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vheight);  //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"];
            double tt = bilinear_interp(3, t, z, epoch_time, vheight); //Get current ambient air temperature at vessel location
            return tt;
        }

        //Retrieve aerodynamic data at vessel location
        public wx_aero getPTD(double vheight)
        {
            double epoch_time = Util.getLocalTime_Wx(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vheight); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"];
            double pp = bilinear_interp(4, t, z, epoch_time, vheight); //Get current atmospheric pressure at vessel location
            double tt = bilinear_interp(3, t, z, epoch_time, vheight); //Get current ambient temperature at vessel location
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
        public wx_wind getWind(double vheight)
        {
            double epoch_time = Util.getLocalTime_Wx(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vheight); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"];
            double uu; double vv; double ww;
            ww = bilinear_interp(0, t, z, epoch_time, vheight); //Get vertical wind component (w) at vessel location
            uu = bilinear_interp(1, t, z, epoch_time, vheight); //Get zonal wind component (u) at vessel location
            vv = bilinear_interp(2, t, z, epoch_time, vheight); //Get meridional wind component (v) at vessel location
            //Return data as struct
            wx_wind wind_data = new wx_wind();
            wind_data.uwind = uu;
            wind_data.vwind = vv;
            wind_data.zwind = ww;

            return wind_data;
        }
        //Interpolate data in space-time and return all 3D climatogical variables
        public wx_atm getAmbientWx3D(double vheight)
        {
            double epoch_time = Util.getLocalTime_Wx(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(vheight); //Get index of coordinates closest to vessel location
            int t = idx["t"]; int z = idx["z"];
            double uu; double vv; double ww; double tt; double pp; double rh; double vis; double cldfrac;
            ww = bilinear_interp(0, t, z, epoch_time, vheight); //Retrieve vertical wind component
            uu = bilinear_interp(1, t, z, epoch_time, vheight); //Retrieve zonal wind component
            vv = bilinear_interp(2, t, z, epoch_time, vheight); //Retrieve meridional wind component
            tt = bilinear_interp(3, t, z, epoch_time, vheight); //Retrieve ambient temperature
            pp = bilinear_interp(4, t, z, epoch_time, vheight); //Retrieve ambient pressure
            rh = bilinear_interp(5, t, z, epoch_time, vheight); //Retrieve relative humidity
            vis = bilinear_interp(6, t, z, epoch_time, vheight); //Retrieve visibility
            cldfrac = bilinear_interp(7, t, z, epoch_time, vheight); //Retrieve cloud fraction 

            //Perform validity checks
            /*if (cldfrac < 0)
            {
                cldfrac = 0.0;
            }*/
            if (vis < 0)
            {
                vis = 0.0;
            }
            if (rh < 0)
            {
                rh = 0.0;
            }
            else if (rh > 100)
            {
                rh = 100.0;
            }

            //Compute air density
            double rho = (pp / (Util.Rd * tt));
            //Util.Log("Pressure: " + pp + ", zidx: " + z);
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
        public wx_srf getAmbientWx2D()
        {
            double epoch_time = Util.getLocalTime_Wx(); //Get local time (i.e. UT time)
            Dictionary<string, int> idx = get_dim_idx(0);  //Get index of coordinates closest to vessel location
            int t = idx["t"];

            double olr; double precipw; double mslp; double sst; double tcld; double rain; double tsfc; double rhsfc; double uwnd_sfc; double vwnd_sfc;
            olr = linear_interp(0, t, epoch_time); //Retrieve outgoing longwave radiation (i.e. OLR) - (indicative of surface and cloud top temps)
            tcld = linear_interp(1, t, epoch_time); //Get total cloud cover (i.e. vertically integrated cloud fraction)
            precipw = linear_interp(2, t, epoch_time); //Get precipitable water (i.e. the amount of liquid water that could be condensed out of the entire atmospheric column)
            rain = linear_interp(3, t, epoch_time); //Retrieve the precipitation rate (for snow this is liquid water equivalent)
            mslp = linear_interp(4, t, epoch_time); //Retrieve mean sea level pressure
            sst = linear_interp(5, t, epoch_time); //Retrieve skin surface temperature (over the ocean this is the sea surface temperature)
            // If precipitation rate is trace set it to zero.
            if (rain <= 0.1)
            {
                rain = 0;
            }
            tsfc = linear_interp3d(3, t, epoch_time); //Retrieve surface temperature
            rhsfc = linear_interp3d(5, t, epoch_time); //Retrieve relative humidity at surface
            uwnd_sfc = linear_interp3d(0, t, epoch_time); //Retrieve zonal wind component at surface
            vwnd_sfc = linear_interp3d(1, t, epoch_time); //Retrieve meridional wind component at surface

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

        public wx_vel getVehicleVel(Vessel vessel, double vheight, bool wxenabled)
        {

            Vector3 windVector = Vector3.zero; //Wind in map space
            if (wxenabled)
            {
                //Get wind vector (in meteorological coordinates)
                wx_wind windvec = getWind(vheight);
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


        public wx_vel getVehicleVelCnst(Vessel vessel, double uwnd, double vwnd, double zwnd, bool wxenabled)
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
        double linear_interp(int vidx, int tidx, double epoch_time)
        {
            // Weather data is available for 5 years on Kerbin, after which the weather time-series will repeat.
            int tidx2;
            if (tidx == (NT - 1))
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }

            //Linear interpolation
            double y0 = point_2d[vidx, tidx];
            double y1 = point_2d[vidx, tidx2];
            double yt = y0 + (epoch_time - times[tidx]) * ((y1 - y0) / (times[tidx2] - times[tidx]));
            return yt;
        }

        //Perform bilinear interpolation to retrieve meteorological fields at a given time and height (ASL).
        double bilinear_interp(int vidx, int tidx, int hidx, double epoch_time, double radarAltitude)
        {
            double x1 = (times[tidx + 1] - epoch_time) / (times[tidx + 1] - times[tidx]);
            double x2 = (epoch_time - times[tidx]) / (times[tidx + 1] - times[tidx]);

            double xy1 = x1 * point_3d[vidx, tidx, hidx] + x2 * point_3d[vidx, tidx + 1, hidx];
            double xy2 = x1 * point_3d[vidx, tidx, hidx + 1] + x2 * point_3d[vidx, tidx + 1, hidx + 1];

            double fxy = ((heights[hidx + 1] - radarAltitude) / (heights[hidx + 1] - heights[hidx])) * xy1 + ((radarAltitude - heights[hidx]) / (heights[hidx + 1] - heights[hidx])) * xy2;
            return fxy;
        }

        double linear_interp3d(int vidx, int tidx, double epoch_time)
        {
            //Get temporal indices to interpolate between (loop back to zeroth index when temporal window extends beyond length of 5-year time-series)
            int tidx2;
            if (tidx == (NT - 1))
            {
                tidx2 = 0;
            }
            else
            {
                tidx2 = tidx + 1;
            }

            //Linearlly interpolate lowest model level of 3D atmospheric fields in time
            double y0 = point_3d[vidx, tidx, 0];
            double y1 = point_3d[vidx, tidx2, 0];
            double yt;
            if (tidx == (NT - 1))
            {
                yt = y0 + (epoch_time - times[tidx]) * ((y1 - y0) / (times[1] - times[0]));
            }
            else
            {
                yt = y0 + (epoch_time - times[tidx]) * ((y1 - y0) / (times[tidx2] - times[tidx]));
            }
            return yt;
        }
    }
}
