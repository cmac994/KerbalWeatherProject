using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class ModularFI_Registerer : MonoBehaviour
    {

        //Initialize vars
        public bool haveFAR;
        public double mach;

        //Initialize climate api class reference
        public climate_api _clim_api;
        //Initialize weather api class reference
        public weather_api _wx_api;

        //Get Kerbin cbody
        CelestialBody kerbin;

        //Set weather/climate boolean
        public bool wx_enabled = true;

        //Booleans for type of MPAS data to use
        private bool use_climo;
        private bool use_point;
        private bool aero;
        private bool thermo;

        //Detect if FAR is present
        bool RegisterWithFAR()
        {
            try
            {
                Type FARWind = null;

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
                        }
                    }
                }
                if (FARWind == null)
                {
                    return false;
                }
                return true; // jump out
            }
            catch (Exception e)
            {
                Debug.LogError("KerbalWeatherProject: unable to register with FerramAerospaceResearch. Exception thrown: " + e.ToString());
            }
            return false;
        }

        internal void SetWindBool(bool wind_bool)
        {
            Util.Log("Set WindBool: " + wind_bool);
            wx_enabled = wind_bool;
        }

        void Start()
        {
            //Check if KWP is enabled by default
            wx_enabled = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams>().WxEnabled;
            Util.Log("MFI: wx_enabled: " + wx_enabled);
            //Determine MPAS data set
            use_climo = Util.useCLIM();
            use_point = Util.useWX();
            aero = Util.allowAero();
            thermo = Util.allowThermo();

            //Check if FAR is available
            haveFAR = RegisterWithFAR();
            //Get instance of climate api
            _clim_api = new climate_api();
            _wx_api = new weather_api();

            if (!haveFAR)
            {
                Vessel v = FlightGlobals.ActiveVessel;

                //Override KSP's FlightIntegrator using the ModularFlightIntegrator 
                Util.Log("Register Modular FlightIntegrator");
                if (aero)
                {
                    ModularFI.ModularFlightIntegrator.RegisterUpdateAerodynamicsOverride(UpdateAerodynamics);
                }
                if (thermo)
                {
                    ModularFI.ModularFlightIntegrator.RegisterUpdateThermodynamicsPre(UpdateThermodynamicsPre);
                }

                if ((aero) || (thermo))
                {
                    //Util.Log("MFI On");
                    Util.setMFI(true);
                }

            }
            GameObject.Destroy(this);
        }

        //Update aerodynamics
        //Adapted from KSP Trajectories, Kerbal Wind Tunnel, FAR, and AeroGUI 
        void UpdateAerodynamics(ModularFI.ModularFlightIntegrator fi, Part part)
        {

            Vessel v = FlightGlobals.ActiveVessel;
            wx_enabled = Util.getWindBool(); //Is weather enabled?
            use_climo = Util.useCLIM(); //Are we using the climatology 
            use_point = Util.useWX(); //Are we using MPAS point time-series

            //Get main vessel and get reference to Kerbin (i.e. celestial body)
            kerbin = Util.getbody();

            //Check to see if root part is in list. If not do not perform aero update. 
            //This avoids updating aerodynamics of parts that have been decoupled and are no longer part of the active vessel.
            bool hasPart = false;
            for (int i = 0; i < fi.PartThermalDataCount; i++)
            {
                PartThermalData pttd = fi.partThermalDataList[i];
                Part ppart = pttd.part;
                if (ppart == v.rootPart)
                {
                    hasPart = true;
                }
            }

            if (!hasPart)
            {
                return;
            }

            //Get position of current vessel
            double vheight = v.altitude;
            double vlat = v.latitude;
            double vlng = v.longitude;

            double air_pressure; double air_density; double soundSpeed;
            //Define gamma (i.e. ratio between specific heat of dry air at constant pressure and specific heat of dry air at constant volume: cp/cv).
            double gamma = 1.4;

            bool inatmos = false;
            if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (v.altitude <= 70000) && (wx_enabled))
            {
                if (use_climo)
                {
                    //Retrieve air pressure/density at vessel location
                    climate_api.wx_aero ptd = _clim_api.getPTD(vlat, vlng, vheight);
                    air_density = ptd.density;
                    air_pressure = ptd.pressure;
                }
                else 
                {
                    //Retrieve air pressure/density at vessel location
                    weather_api.wx_aero ptd = _wx_api.getPTD(vheight);
                    air_density = ptd.density;
                    air_pressure = ptd.pressure;
                }
                //Compute speed of sound based on air pressure and air density.
                soundSpeed = Math.Sqrt(gamma * (air_pressure / air_density));
                inatmos = true;
            }
            else
            {
                soundSpeed = v.speedOfSound;
            }
            //Get height from surface
            double hsfc = v.heightFromTerrain;
            //Determine if vessel is grounded (i.e. on the ground)
            bool isgrounded = true;
            if (hsfc >= 10)
            {
                isgrounded = false;
            }

            //If FAR is present FAR will handle aerodynamic updates or if we're not on Kerbin (i.e. in a different atmosphere)
            if (part.Modules.Contains<ModuleAeroSurface>() || (part.Modules.Contains("MissileLauncher") && part.vessel.rootPart == part) || (haveFAR) || (v.mainBody != kerbin) || (!inatmos) )
            {
                fi.BaseFIUpdateAerodynamics(part);

                if (((!part.DragCubes.None) && (!part.hasLiftModule)) || (part.name.Contains("kerbalEVA")))
                {
                    double drag = part.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * fi.pseudoReDragMult;
                    float pscal = (float)(part.dynamicPressurekPa * drag * PhysicsGlobals.DragMultiplier);

                    //Estimate lift force from body lift
                    Vector3 lforce = part.transform.rotation * (part.bodyLiftScalar * part.DragCubes.LiftForce);
                    lforce = Vector3.ProjectOnPlane(lforce, -part.dragVectorDir);
                }
                return;
            }
            else
            {

                fi.BaseFIUpdateAerodynamics(part); //Run base aerodynamic update first (fix bug where parachutes experience multiplicative acceleration)

                // Get rigid body
                Rigidbody rb = part.rb;
                if (rb)
                {
                    if (part == v.rootPart)
                    {
                        v.mach = 0;
                    }
                    //Retrieve wind vector at vessel location
                    Vector3d windVec = KWPWind.GetWind(FlightGlobals.currentMainBody, part, rb.position);

                    //Retrieve world velocity without wind vector
                    Vector3 velocity_nowind = rb.velocity + Krakensbane.GetFrameVelocity();
                    //Compute mach number
                    double mach_nowind = velocity_nowind.magnitude / soundSpeed;

                    Vector3 drag_sum = Vector3.zero;
                    Vector3 lift_sum = Vector3.zero;

                    if (((!part.DragCubes.None) && (!part.hasLiftModule)) || (part.name.Contains("kerbalEVA")))
                    {
                        //Estimate Drag force
                        double pseudoreynolds = part.atmDensity * Math.Abs(velocity_nowind.magnitude);
                        double pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate((float)pseudoreynolds);
                        double drag = part.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoReDragMult;
                        float pscal = (float)(part.dynamicPressurekPa * drag * PhysicsGlobals.DragMultiplier);
                        drag_sum += -part.dragVectorDir * pscal;

                        //Estimate lift force from body lift
                        float pbodyLiftScalar = (float)(part.dynamicPressurekPa * part.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier) * PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)part.machNumber);
                        Vector3 lforce2 = part.transform.rotation * (pbodyLiftScalar * part.DragCubes.LiftForce);
                        lforce2 = Vector3.ProjectOnPlane(lforce2, -part.dragVectorDir);
                        lift_sum += lforce2;
                    }

                    //Util.Log("BodyLift Default: " + part.bodyLiftScalar + ", Lift Force: " + lift_force);
                    //Loop through each part module to look for lifting surfaces
                    for (int j = 0; j < part.Modules.Count; ++j)
                    {
                        var m = part.Modules[j];
                        if (m is ModuleLiftingSurface)
                        {
                            //Initialize force vectors
                            Vector3 lforce = Vector3.zero;
                            Vector3 dforce = Vector3.zero;
                            //Convert kPa to Pa
                            double liftQ = part.dynamicPressurekPa * 1000;
                            ModuleLiftingSurface wing = (ModuleLiftingSurface)m;

                            //Get wing coefficients
                            Vector3 nVel = Vector3.zero;
                            Vector3 liftVector = Vector3.zero;
                            float liftdot;
                            float absdot;
                            wing.SetupCoefficients(velocity_nowind, out nVel, out liftVector, out liftdot, out absdot);

                            //Get Lift/drag force
                            lforce = wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, (float)part.machNumber);
                            dforce = wing.GetDragVector(nVel, absdot, liftQ);

                            if (part.name.Contains("kerbalEVA"))
                            {
                                continue;
                            }
                            drag_sum += dforce;
                            //lift_sum += lforce;
                            if (isgrounded) { 
                                lift_sum += lforce;
                            }
                        }
                    }

                    //Retrieve world velocity and subtract off wind vector
                    Vector3 velocity_wind = rb.velocity + Krakensbane.GetFrameVelocity() - windVec;
                    //Compute mach number
                    double mach_wind = velocity_wind.magnitude / soundSpeed;
                    //Set mach number
                    part.machNumber = mach_wind; // *mdiv;
                    if (part == v.rootPart)
                    {
                        fi.mach = mach_wind;
                    }
                    //Get drag and lift forces with wind 
                    List<Vector3> total_forces = GetDragLiftForce(fi, part, velocity_wind, windVec, part.machNumber, isgrounded);

                    //Compute difference in drag/lift with and without wind.
                    Vector3 total_drag = total_forces[0] - drag_sum;
                    Vector3 total_lift = total_forces[1] - lift_sum;

                    //Calculate force due to wind
                    Vector3 force = total_lift + total_drag;

                    if (double.IsNaN(force.sqrMagnitude) || (((float)force.magnitude).NearlyEqual(0.0f)))
                    {
                        force = Vector3d.zero;
                    }
                    else
                    {
                        //Adapted from FAR - apply numerical control factor
                        float numericalControlFactor = (float)(part.rb.mass * velocity_wind.magnitude * 0.67 / (force.magnitude * TimeWarp.fixedDeltaTime));
                        force *= Math.Min(numericalControlFactor, 1);
                        part.AddForce(force);
                    }
                    v.mach = fi.mach;
                }
            }
        }


        //Retrieve the vector sum of the lift and drag forces on the part when wind is present.
        public List<Vector3> GetDragLiftForce(ModularFI.ModularFlightIntegrator fi, Part p, Vector3d vel, Vector3d windVec, double mach, bool isgrounded)
        {
            //Initialize forces
            Vector3 total_drag = Vector3d.zero;
            Vector3 total_lift = Vector3d.zero;

            List<Vector3> total = new List<Vector3>();
            //Set Part drag based on new air velocity 
            p.dragVector = vel;
            p.dragVectorSqrMag = p.dragVector.sqrMagnitude;
            //Update dynamic pressure
            p.dynamicPressurekPa = 0.0005 * p.atmDensity * p.dragVectorSqrMag;
            //If part darg is near zero or part is shielded do not calculate force sum
            if (p.dragVectorSqrMag.NearlyEqual(0) || p.ShieldedFromAirstream)
            {
                p.dragVectorMag = 0f;
                p.dragVectorDir = Vector3.zero;
                p.dragVectorDirLocal = Vector3.zero;
                p.dragScalar = 0f;
            }
            //Update drag vector
            p.dragVectorMag = (float)Math.Sqrt(p.dragVectorSqrMag);
            p.dragVectorDir = p.dragVector / p.dragVectorMag;
            p.dragVectorDirLocal = -p.transform.InverseTransformDirection(p.dragVectorDir);

            if (((!p.DragCubes.None) && (!p.hasLiftModule)) || (p.name.Contains("kerbalEVA")))
            {
                //Apply drag vector direction to drag cube
                Vector3 drag_force = Vector3.zero;
                //Update angular drag (adapated from FAR)
                p.DragCubes.SetDrag(p.dragVectorDirLocal, (float)mach);
                //Calculate drag force and set dragScalar (adapated from Trajectories mod)
                double pseudoreynolds = p.atmDensity * Math.Abs(vel.magnitude);
                double pseudoReDragMult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate((float)pseudoreynolds);
                double drag = p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoReDragMult;
                p.dragScalar = (float)(p.dynamicPressurekPa * drag * PhysicsGlobals.DragMultiplier);
                //Add drag force to total drag
                drag_force = -p.dragVectorDir * p.dragScalar;
                total_drag += drag_force;

                //Estimate lift force from body lift
                p.bodyLiftScalar = (float)(p.dynamicPressurekPa * p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier) * PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)mach);
                Vector3 lforce = p.transform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                lforce = Vector3.ProjectOnPlane(lforce, -p.dragVectorDir);
                //Add lift force if vessel is on the ground
                total_lift += lforce;
            }

            for (int j = 0; j < p.Modules.Count; ++j)
            {
                var m = p.Modules[j];
                if (m is ModuleLiftingSurface)
                {
                    //Initialize force vectors
                    Vector3 lforce3 = Vector3.zero;
                    Vector3 dforce3 = Vector3.zero;
                    //Convert kPa to Pa
                    double liftQ = p.dynamicPressurekPa * 1000;
                    ModuleLiftingSurface wing = (ModuleLiftingSurface)m;

                    //Get wing coefficients
                    Vector3 nVel = Vector3.zero;
                    Vector3 liftVector = Vector3.zero;
                    float liftdot;
                    float absdot;
                    wing.SetupCoefficients(vel, out nVel, out liftVector, out liftdot, out absdot);

                    //Get Lift/drag force
                    lforce3 = wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, (float)mach);
                    dforce3 = wing.GetDragVector(nVel, absdot, liftQ);

                    //wing.dragForce = dforce3;
                    if (p.name.Contains("kerbalEVA"))
                    {
                        continue;
                    }
                    total_drag += dforce3;
                    if (isgrounded) { 
                        total_lift += lforce3;
                    }
                }
            }
            total.Add(total_drag);
            total.Add(total_lift);
            return total;
        }

        public void UpdateThermodynamicsPre(ModularFI.ModularFlightIntegrator fi)
        {

            wx_enabled = Util.getWindBool();
            use_climo = Util.useCLIM();
            use_point = Util.useWX();

            //Get reference to Kerbin (i.e. celestial body)
            kerbin = Util.getbody();

            //Get vessel position
            Vessel v = FlightGlobals.ActiveVessel;
            double vheight = v.altitude;
            double vlat = v.latitude;
            double vlng = v.longitude;

            //Check to see if root part is in list. If not do not perform thermo update. 
            //This avoids updating thermodynamics of parts that have been decoupled and are no longer part of the active vessel.

            bool hasPart = false;
            for (int i = 0; i < fi.PartThermalDataCount; i++)
            {
                PartThermalData pttd = fi.partThermalDataList[i];
                Part part = pttd.part;
                if (part == v.rootPart)
                {
                    hasPart = true;
                }
            }

            if (!hasPart)
            {
                return;
            }

            //Start out with Stock Thermodynamics before performing adjustments due to wind/weather.
            fi.BaseFIUpdateThermodynamics();

            // Initialize External Temp and part drag/lift sum for GUI. //
            // This ensures GUI updates are smooth and don't bounce back and forth between stock aero and KWP aero updates //
            v.externalTemperature = fi.externalTemperature;

            if ((fi.CurrentMainBody != kerbin) || (v.altitude >= 70000))
            {
                return;
            }
            //Check if in atmosphere
            if (wx_enabled)
            {
                //Define gamma (ratio of cp/cv)
                double gamma = 1.4;

                if (use_climo)
                {
                    climate_api.wx_aero ptd = _clim_api.getPTD(vlat, vlng, vheight); //Retrieve pressure,temperature,and density from climate API
                    //Adjust atmospheric constants
                    CalculateConstantsAtmosphere_CLIMO(fi, ptd, v);
                }
                else 
                {
                    weather_api.wx_aero ptd = _wx_api.getPTD(vheight); //Retrieve pressure,temperature,and density from climate API
                    //Adjust atmospheric constants
                    CalculateConstantsAtmosphere_WX(fi, ptd, v);
                }
                // change density lerp
                double shockDensity = GetShockDensity(fi.density, fi.mach, gamma);
                fi.DensityThermalLerp = CalculateDensityThermalLerp(shockDensity);
                double lerpVal = fi.dynamicPressurekPa;
                if (lerpVal < 1d)
                    fi.convectiveCoefficient *= UtilMath.LerpUnclamped(0.1d, 1d, lerpVal);

                // reset background temps
                fi.backgroundRadiationTemp = CalculateBackgroundRadiationTemperature(fi.atmosphericTemperature, fi.DensityThermalLerp);
                fi.backgroundRadiationTempExposed = CalculateBackgroundRadiationTemperature(fi.externalTemperature, fi.DensityThermalLerp);
            }
            else if (!wx_enabled)
            {
                fi.BaseFIUpdateThermodynamics();
            }
        }

        //Adapted from Real Heat
        public static double GetShockDensity(double density, double mach, double gamma)
        {
            if (mach > 1d)
            {
                double M2 = mach * mach;
                density = (gamma + 1d) * M2 / (2d + (gamma - 1d) * M2) * density;
            }
            return density;
        }

        //Adapted from Real Heat
        public static double CalculateDensityThermalLerp(double density)
        {
            // calculate lerp
            if (density < 0.0625d)
                return 1d - Math.Sqrt(Math.Sqrt(density));
            if (density < 0.25d)
                return 0.75d - Math.Sqrt(density);
            return 0.0625d / density;
        }

        //Update atmospheric constants to reflect MPAS climatology
        public static double CalculateConstantsAtmosphere_CLIMO(ModularFI.ModularFlightIntegrator fi, climate_api.wx_aero ptd, Vessel v)
        {

            //Get environmental conditions
            double air_density = ptd.density;
            double air_temperature = ptd.temperature;
            double air_pressure = ptd.pressure;
            double orig_temp = fi.atmosphericTemperature;

            //Set environmental data for current vessel
            v.staticPressurekPa = air_pressure / 1000.0;
            v.atmDensity = air_density;
            v.atmosphericTemperature = air_temperature;
            v.speedOfSound = Math.Sqrt(1.4 * (air_pressure / air_density));
            v.atmosphericTemperature = air_temperature;
            v.externalTemperature = (air_temperature + (fi.externalTemperature - orig_temp));
            return air_temperature;
        }

        //Update atmospheric constants to reflect MPAS Point Data
        public static double CalculateConstantsAtmosphere_WX(ModularFI.ModularFlightIntegrator fi, weather_api.wx_aero ptd, Vessel v)
        {

            //Get environmental conditions
            double air_density = ptd.density;
            double air_temperature = ptd.temperature;
            double air_pressure = ptd.pressure;
            double orig_temp = fi.atmosphericTemperature;

            //Set environmental data for current vessel
            v.staticPressurekPa = air_pressure / 1000.0;
            v.atmDensity = air_density;
            v.atmosphericTemperature = air_temperature;
            v.speedOfSound = Math.Sqrt(1.4 * (air_pressure / air_density));
            v.atmosphericTemperature = air_temperature;
            v.externalTemperature = (air_temperature + (fi.externalTemperature - orig_temp));
            return air_temperature;
        }

        //Adapted from Real Heat
        public static double CalculateBackgroundRadiationTemperature(double ambientTemp, double densityThermalLerp)
        {

            return UtilMath.Lerp(
                ambientTemp,
                PhysicsGlobals.SpaceTemperature,
                densityThermalLerp);
        }
    }
}
