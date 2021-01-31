using UnityEngine;
using KSP.Localization;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
namespace KerbalWeatherProject_Lite
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WxUnityGUI : MonoBehaviour
    {

        // Default GUI dimensions and position
        private static float xpos = 100f;
        private static float ypos = 100f;
        private static float xwidth = 285.0f;
        private static float yheight = 60.0f;
        public static Rect titleRect = new Rect(0, 0, 10000, 10000);
        public Rect windowPos = new Rect(xpos, ypos, xwidth, yheight);
        public Rect wpos;
        //References to Climatology and Point Forecast classes
        public KerbalWxClimo _kwx_climo;
        public KerbalWxPoint _kwx_point;

        //Initialize toolbar controler
        private ToolbarControl toolbarController;
        private bool toolbarButtonAdded = false;
        private bool guiIsUp = false;

        //Unit strings
        private string wstr;
        private string wind_unit;
        private string temp_unit;
        private string pres_unit;
        private string precip_unit;
        private string vel_unit;

        //Initialize booleans for GUI tabs
        private bool showAmbient = false;
        private bool showWind = false;
        private bool showWindRelative = false;
        private bool showSfc = false;
        private bool showPos = false;
        private bool showVelocity = false;
        private bool showSat = false;
        private bool showAero = false;

        //Set nvars depending on MPAS data type
        CelestialBody kerbin;
        private bool inspace = false;
        private bool gui_removed = false;
        bool inspace_orig;
        const int nvars = 8;
        const int nsvars = 8;

        //Initialize data lists
        List<double> vel_list = new List<double>();
        List<double> wx_list2d = new List<double>();
        List<double> wx_list3d = new List<double>();

        //Initialize text (GUI) strings
        private static string wspd_txt = "";
        private static string wdir_txt = "";
        private static string wdirstr_txt = "";

        private static string velocity_ias_txt = "";
        private static string velocity_eas_txt = "";
        private static string velocity_tas_txt = "";
        private static string velocity_ground_txt = "";

        private static string tailwind_txt = "";
        private static string headwind_txt = "";
        private static string crosswind_txt = "";
        private static string wwind_txt = "";

        private static string biome_txt = "";
        private static string lat_txt = "";
        private static string lng_txt = "";
        private static string ter_txt = "";

        private static string pressure_txt = "";
        private static string temperature_txt = "";
        private static string density_txt = "";
        private static string humidity_txt = "";

        private static string cldfrac_txt = "";
        private static string vis_txt = "";

        private static string olr_txt = "";
        private static string tcld_txt = "";
        private static string tpw_txt = "";
        private static string rainsfc_txt = "";

        private static string mslp_txt = "";
        private static string tmpsfc_txt = "";
        private static string rhsfc_txt = "";
        private static string wspdsfc_txt = "";

        private static string dyn_pres_txt = "";
        private static string mach_txt = "";
        private static string sound_spd_txt = "";
        private static string shock_tmp_txt = "";
        private static string aoa_txt = "";
        private static string side_slip_txt = "";
        private static string tlift_txt = "";
        private static string tdrag_txt = "";
        private static string ld_ratio_txt = "";

        //Set Default site (KSC)
        public string lsite = "KSC";

        GUIStyle buttonStyle;
        GUIStyle labelStyle;

        // aero variables
        const double RAD2DEG = 180.0 / Math.PI;

        Util.aero_stats aero_sdata;

        //Add KWP to toolbar using toolbar controller
        private void AddToolbarButton()
        {
            KSP.UI.Screens.ApplicationLauncher.AppScenes scenes =
                KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT
                | KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW;

            if (!toolbarButtonAdded)
            {
                toolbarController = gameObject.AddComponent<ToolbarControl>();

                toolbarController.AddToAllToolbars(
                    ToolbarButtonOnTrue,
                    ToolbarButtonOnFalse,
                    scenes,
                    KerbalWxClimo.MODID,
                    "368859",
                    "KerbalWeatherProject_Lite/Textures/KWP_minimal_large",
                    "KerbalWeatherProject_Lite/Textures/KWP_minimal_small",
                    Localizer.Format(KerbalWxClimo.MODNAME));
                toolbarButtonAdded = true;
            }
        }

        //Remove from toolbar
        private void RemoveToolbarButton()
        {
            if (toolbarButtonAdded)
            {
                toolbarController.OnDestroy();
                Destroy(toolbarController);
                toolbarButtonAdded = false;
            }
        }

        //Check if Added to Toolbar
        private void ToolbarButtonOnTrue()
        {
            guiIsUp = true;
        }

        private void ToolbarButtonOnFalse()
        {
            guiIsUp = false;
        }

        void Start()
        {

            //Get primary celestial body (i.e. Kerbin)
            kerbin = Util.getbody();
            //Check to see if weather is enabled
            //Util.Log("Wx Enabled, " + wx_enabled.ToString());
            Util.CacheKWPLocalization();

            //Initialize variable lists
            for (int i = 0; i < 6; i++)
            {
                vel_list.Add(0);
            }
            for (int i = 0; i < nvars+1; i++)
            {
                wx_list3d.Add(0);
            }
            for (int i = 0; i < nsvars; i++)
            {
                wx_list2d.Add(0);
            }

            //Initialize class instance based on Wx type

            _kwx_climo = (KerbalWxClimo)FindObjectOfType(typeof(KerbalWxClimo));
            _kwx_point = (KerbalWxPoint)FindObjectOfType(typeof(KerbalWxPoint));

            //Add to toolbar
            AddToolbarButton();
            //Util.Log("Instantiate Toolbar and KWP");
        }

        //Check units
        void Update()
        {
            //Get wind direction specificity
            wstr = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().windstrs;

            //Get variable units
            wind_unit = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().windunit;
            temp_unit = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().tempunit;
            pres_unit = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().presunit;
            precip_unit = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().precipunit;
            vel_unit = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams_Sec3>().velunit;
        }
        void FixedUpdate()
        {
            if (!gui_removed)
            {
                if (Util.use_climo)
                {
                    //Retrieve climatological data
                    vel_list = _kwx_climo.getAero();
                    if (showAmbient)
                    {
                        wx_list3d = _kwx_climo.getWx3D();
                    }
                    if (showSfc || showSat)
                    {
                        wx_list2d = _kwx_climo.getWx2D();
                    }
                }
                else if (Util.use_point)
                {
                    //Retrieve point forecast data
                    lsite = Util.get_last_lsite_short();
                    if (showVelocity)
                    {
                        vel_list = _kwx_point.getAero();
                    }
                    if (showWind || showWindRelative || showAmbient)
                    {
                        wx_list3d = _kwx_point.getWx3D();
                    }
                    if (showSfc || showSat)
                    {
                        wx_list2d = _kwx_point.getWx2D();
                    }
                }
                if (showAero)
                {
                    aero_sdata = GetAeroStats();
                }
            }

            //Check to see if outside of Kerbin's SOI
            if (((FlightGlobals.ActiveVessel.mainBody != kerbin)))
            {
                //Util.Log("Destroy toolbar as we're outside Kerbin's SOI");
                //Don't display toolbar button or GUI when out of Kerbin SOI
                ToolbarButtonOnFalse();
                Destroy();
                gui_removed = true;
            }
            else
            {
                //If we're back in Kerbin's SOI add the toolbar button and enable GUI oncemore.
                if (gui_removed)
                {
                    //Util.Log("Re-add toolbar as we're back in Kerbin's SOI");
                    AddToolbarButton();
                    gui_removed = false;
                }
            }
        }

        Util.aero_stats GetAeroStats()
        {
            //Get current vessel
            Vessel v = FlightGlobals.ActiveVessel;

            Util.aero_stats aero_data = new Util.aero_stats();
            Vector3d nVel = v.srf_velocity.normalized; //normalized velocity
            double alpha = Vector3d.Dot(v.transform.forward, nVel);
            alpha = Math.Asin(alpha) * RAD2DEG; // angle of attack re: velocity
            double sideslip = Vector3d.Dot(v.transform.up, Vector3d.Exclude(v.transform.forward, nVel).normalized);
            sideslip = Math.Acos(sideslip) * RAD2DEG; // sideslip re: velocity
            if (double.IsNaN(sideslip))
                sideslip = 0;
            if (sideslip < 0)
                sideslip = 180 + sideslip;
            double shockTemp = v.externalTemperature; //External (air) temperature

            //Get current vessel
            Vector3d vLift = Vector3d.zero; // the sum of lift from all parts
            Vector3d vDrag = Vector3d.zero; // the sum of drag from all parts
            double sqrMag = v.srf_velocity.sqrMagnitude; //Square magnitude of vessel surface velocity
            double Q = 0.5 * v.atmDensity * sqrMag; // dynamic pressure, aka Q
            // i.e. your airspeed at sea level with the same Q
            double soundSpeed = v.speedOfSound; //Speed of sound (m/s)
            double mach = v.mach;
            
            // Loop through all parts, checking modules in each part, to get sum of drag and lift.
            for (int i = 0; i < v.Parts.Count; ++i)
            {
                Part p = v.Parts[i];
                // get part drag (but not wing/surface drag)
                vDrag += -p.dragVectorDir * p.dragScalar;
                if (!p.hasLiftModule)
                {
                    //Get body lift
                    Vector3 bodyLift = p.transform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                    bodyLift = Vector3.ProjectOnPlane(bodyLift, -p.dragVectorDir);
                    vLift += bodyLift;
                }
                // now find modules
                for (int j = 0; j < p.Modules.Count; ++j)
                {
                    var m = p.Modules[j];
                    if (m is ModuleLiftingSurface) // control surface derives from this
                    {
                        //Retrieve wind lift/drag
                        ModuleLiftingSurface wing = (ModuleLiftingSurface)m;
                        vLift += wing.liftForce;
                        vDrag += wing.dragForce;
                    }
                }
            }
            Vector3d force = vLift + vDrag; // sum of all forces on the craft
            Vector3d liftDir = -Vector3d.Cross(v.transform.right, nVel); // we need the "lift" direction, which
            // is "up" from our current velocity vector and roll angle.

            // Now we can compute the dots.
            double lift = Vector3d.Dot(force, liftDir); // just the force in the 'lift' direction
            double drag = Vector3d.Dot(force, -nVel); // drag force, = pDrag + lift-induced drag
            double ldRatio = lift / drag; // Lift / Drag ratio

            aero_data.alpha = alpha;
            aero_data.sideslip = sideslip;
            aero_data.soundSpeed = soundSpeed;
            aero_data.mach = mach;
            aero_data.Q = Q;
            aero_data.lift = lift;
            aero_data.drag = drag;
            aero_data.ldRatio = ldRatio;
            aero_data.shockTemp = shockTemp;
            return aero_data;
        }

        //Destroy toolbar button
        void Destroy()
        {
            RemoveToolbarButton();
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbarButton);
        }


        //Add GUI Window
        void OnGUI()
        {
            if (guiIsUp)
            {
                windowPos = GUILayout.Window("KerbalWeatherProject_Lite".GetHashCode(), windowPos, DrawWindow, "KerbalWeatherProject_Lite");
            }
        }

        //Draw GUI
        public void DrawWindow(int windowID)
        {

            //Define button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(10, 10, 6, 0);
            buttonStyle.margin = new RectOffset(2, 2, 2, 2);
            buttonStyle.stretchWidth = true;
            buttonStyle.stretchHeight = false;

            //Adust label style
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.wordWrap = false;

            //Get current vessel
            Vessel vessel = FlightGlobals.ActiveVessel;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            //Define Weather Toggle button based on location (satellite outside atmosphere, vessel inside atmosphere, etc.)
            string tws;
            if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (vessel.altitude >= 70000) && (Util.use_climo))
            {
                tws = Util.KWPTags["#autoLOC_tglclim"];
            }
            else if ((FlightGlobals.ActiveVessel.mainBody == kerbin) && (Util.use_point))
            {
                tws = Util.KWPTags["#autoLOC_tglpnt"] + " " + lsite;
            }
            else
            {
                tws = Util.KWPTags["#autoLOC_tglwx"];
            }

            //Add GUI button to disable or enable weather (i.e. toggle weather)
            if (GUILayout.Button(tws, buttonStyle))
            {
                Util.wx_enabled = !Util.wx_enabled;
                //Util.setWindBool(Util.wx_enabled);
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            //Define Weather Toggle button based on location (satellite outside atmosphere, vessel inside atmosphere, etc.)
            string use_climo_str;
            string use_point_str;

            use_climo_str = Util.KWPTags["#autoLOC_useclim"];
            use_point_str = Util.KWPTags["#autoLOC_usepnt"];

            // Add GUI buttons to select MPAS data source (i.e. toggle weather) //

            //GUI Button to select MPAS climatology as data source (i.e. spatially varying wx conditions with diurnal cycle)
            if (GUILayout.Button(use_climo_str, buttonStyle))
            {
                //Util.Log("Enable Climo");
                Util.use_climo = true;
                Util.use_point = false;
                Util.setMAPSDtype(Util.use_climo, Util.use_point);
            }
            //GUI Button to select MPAS Point Time-series (i.e. local weather at nearest launch site, which varies in height-time)
            if (GUILayout.Button(use_point_str, buttonStyle))
            {
                //Util.Log("Enable Point");
                Util.use_climo = false;
                Util.use_point = true;
                Util.setMAPSDtype(Util.use_climo, Util.use_point);
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            //Add GUI button to show the lat,lng position along a vessels ground track (in space)
            if (GUILayout.Button(Util.KWPTags["#autoLOC_gtrack"], buttonStyle))
            {
                Rect windowPos0 = windowPos;
                windowPos0.width = xwidth;
                windowPos0.height = yheight;
                showPos = !showPos;
                //OnGUI();
                windowPos = windowPos0;
            }

            //If we entered or exited the atmosphere update UI 
            if (inspace_orig != inspace)
            {
                Rect windowPos0 = windowPos;
                windowPos0.width = xwidth;
                windowPos0.height = yheight;
                showPos = !showPos; //Set showPos false on update (now that UI has been resized)
                //OnGUI();
                windowPos = windowPos0;
            }

            //check to see if the previous drawWindow was called in a different world space (i.e. atmosphere or low-space)
            inspace_orig = inspace;
            GUILayout.EndHorizontal();
            //Select buttons to show when vessel is in Kerbin's atmosphere
            if (FlightGlobals.ActiveVessel.mainBody == kerbin)
            {
                if (vessel.altitude < 70000)
                {
                    //If in space, reset GUI position (GUI needs to update when going from atmosphere to space - so check to see if it's in space)
                    if (inspace)
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showPos = true; //Set showPos to true to fill in GUI when switching scenes (set showPos false on update)
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    inspace = false;
                    GUILayout.BeginHorizontal();
                    //Add flight info button
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_flight"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showVelocity = !showVelocity;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    //Add wind info button
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_wind"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showWind = !showWind;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    //Add button for cross/headwind info
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_xwind"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showWindRelative = !showWindRelative;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    //Add button to overlay ambient environmental data
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_enviro"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showAmbient = !showAmbient;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    //Add button for aero data (adapted from AeroGUI)
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_aero"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showAero = !showAero;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    GUILayout.EndHorizontal();
                    showSat = false;
                    showSfc = false;

                }
                else
                {
                    //Check when no longer in space to update GUI upon atmospheric entry
                    if (!inspace)
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showPos = true; //Set showPos to true to fill in GUI when switching scenes (set showPos false on update)
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    inspace = true;
                    GUILayout.BeginHorizontal();
                    //Add button to display surface meteorological conditions (along ground track)
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_srf"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showSfc = !showSfc;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    //Add button to display satellite meteorological data (from space)
                    if (GUILayout.Button(Util.KWPTags["#autoLOC_sat"], buttonStyle))
                    {
                        Rect windowPos0 = windowPos;
                        windowPos0.width = xwidth;
                        windowPos0.height = yheight;
                        showSat = !showSat;
                        //OnGUI();
                        windowPos = windowPos0;
                    }
                    GUILayout.EndHorizontal();
                    showAmbient = false;
                    showAero = false;
                    showWind = false;
                    showWindRelative = false;
                    showVelocity = false;
                }
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                //Convert wind components to wind direction
                int wdir2 = (int)Math.Round(((180.0 / Math.PI) * Math.Atan2(-1.0 * wx_list3d[1], -1.0 * wx_list3d[0])), 0); // Direction wind is blowing from.
                if (wdir2 < 0)
                {
                    wdir2 += 360;
                }

                //Get vessel position
                double vlng = vessel.longitude;
                double vlat = vessel.latitude;
                double ter_height = vessel.terrainAltitude;
                string bname = Util.getBiomeName(kerbin, vlng, vlat);

                //Get cardinal wind direction from direction in degrees
                string wdir_str = Util.get_wind_card(wdir2, wstr);
                //Calculate wind speed
                double wspd = Math.Sqrt(Math.Pow(wx_list3d[0], 2) + Math.Pow(wx_list3d[1], 2));

                //Position text
                biome_txt = Util.KWPTags["#autoLOC_biome"] + ": " + bname;
                lat_txt = string.Format("{0:F2} {1}", vlat, Localizer.Format("°"));
                lng_txt = string.Format("{0:F2} {1}", vlng, Localizer.Format("°"));
                ter_txt = string.Format("{0:F2} {1}", ter_height, Localizer.Format("m"));

                //Wind text formatting
                wwind_txt = string.Format("{0:F2} {1}", wx_list3d[2] * 1000.0, Localizer.Format("mm/s"));
                wspd_txt = string.Format("{0:F2} {1}", Util.convert_wspeed(wspd, wind_unit), Localizer.Format(wind_unit));
                wdir_txt = string.Format("{0:F1} {1}", wdir2, Localizer.Format("°") + "(" + wdir_str + ")");
                wdir_txt = string.Format("{0:F1} {1}", wdir2, Localizer.Format("°"));
                wdirstr_txt = wdir_str;

                //Velocity text formating
                velocity_ias_txt = string.Format("{0:F2} {1}", Util.convert_veh_speed(vel_list[0], vel_unit), Localizer.Format(vel_unit));
                velocity_eas_txt = string.Format("{0:F2} {1}", Util.convert_veh_speed(vel_list[2], vel_unit), Localizer.Format(vel_unit));
                velocity_tas_txt = string.Format("{0:F2} {1}", Util.convert_veh_speed(vel_list[1], vel_unit), Localizer.Format(vel_unit));
                velocity_ground_txt = string.Format("{0:F2} {1}", Util.convert_veh_speed(vel_list[3], vel_unit), Localizer.Format(vel_unit));
                crosswind_txt = string.Format("{0:F2} {1}", Util.convert_wspeed(vel_list[5], wind_unit), Localizer.Format(wind_unit));

                pressure_txt = string.Format("{0:F2} {1}", Util.convert_pres(wx_list3d[3], pres_unit), Localizer.Format(pres_unit));
                temperature_txt = string.Format("{0:F2} {1}", Util.convert_temp(wx_list3d[4], temp_unit), Localizer.Format(temp_unit));
                density_txt = string.Format("{0:F2} {1}", wx_list3d[8], Localizer.Format("kg/m^3"));

                //Format aero GUI data
                dyn_pres_txt = string.Format("{0:F2} {1}", Util.convert_pres(aero_sdata.Q, pres_unit), Localizer.Format(pres_unit));
                mach_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.mach, 2), Localizer.Format(""));
                sound_spd_txt = string.Format("{0:F2} {1}", Util.convert_veh_speed(aero_sdata.soundSpeed, vel_unit), Localizer.Format(vel_unit));
                shock_tmp_txt = string.Format("{0:F2} {1}", Util.round_down(Util.convert_temp(aero_sdata.shockTemp, temp_unit),2), Localizer.Format(temp_unit));
                aoa_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.alpha, 2), Localizer.Format("°"));
                side_slip_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.sideslip, 2), Localizer.Format("°"));
                tlift_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.lift, 2), Localizer.Format("kN"));
                tdrag_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.drag, 2), Localizer.Format("kN"));
                ld_ratio_txt = string.Format("{0:F2} {1}", Util.round_down(aero_sdata.ldRatio, 2), Localizer.Format(""));

                if (Util.wx_enabled)
                {

                    //Format text for headwind, tailwind, and crosswind
                    if ((vel_list[4]) < 0)
                    {
                        headwind_txt = string.Format("{0:F2} {1}", Math.Abs(Util.convert_wspeed(vel_list[4], wind_unit)), Localizer.Format(wind_unit));
                        tailwind_txt = string.Format("{0:F2} {1}", 0, Localizer.Format(wind_unit));
                    }
                    else
                    {
                        tailwind_txt = string.Format("{0:F2} {1}", Util.convert_wspeed(vel_list[4], wind_unit), Localizer.Format(wind_unit));
                        headwind_txt = string.Format("{0:F2} {1}", 0, Localizer.Format(wind_unit));
                    }
                    humidity_txt = string.Format("{0:F1} {1}", wx_list3d[5], Localizer.Format("%"));
                    //Format visibility/cldfrac text string
                    if (wx_list3d[6] > 20)
                    {
                        vis_txt = string.Format("> {0:F1} {1}", wx_list3d[6], Localizer.Format("km"));
                    }
                    else
                    {
                        vis_txt = string.Format("{0:F1} {1}", wx_list3d[6], Localizer.Format("km"));
                    }
                    cldfrac_txt = string.Format("{0:F2} {1}", wx_list3d[7] * 100.0, Localizer.Format("%"));

                    //Format surface satellite meteorological text
                    olr_txt = string.Format("{0:F2} {1}", wx_list2d[0], Localizer.Format("W/m\xB2 "));
                    tcld_txt = string.Format("{0:F2} {1}", wx_list2d[1] * 100, Localizer.Format("%"));
                    tpw_txt = string.Format("{0:F2} {1}", Util.convert_precip(wx_list2d[2], precip_unit), Localizer.Format(precip_unit));
                    rainsfc_txt = string.Format("{0:F1} {1}", Util.convert_precip(wx_list2d[3], precip_unit), Localizer.Format(precip_unit + "/hr"));

                    //Format surface meteological text
                    mslp_txt = string.Format("{0:F2} {1}", Util.convert_pres(wx_list2d[4], pres_unit), Localizer.Format(pres_unit));
                    tmpsfc_txt = string.Format("{0:F2} {1}", Util.convert_temp(wx_list2d[5], temp_unit), Localizer.Format(temp_unit));
                    rhsfc_txt = string.Format("{0:F1} {1}", wx_list2d[6], Localizer.Format("%"));
                    wspdsfc_txt = string.Format("{0:F2} {1}", Util.convert_wspeed(wx_list2d[7], wind_unit), Localizer.Format(wind_unit));
                    
                }
                else
                {

                    tailwind_txt = string.Format("{0:F2} {1}", 0, Localizer.Format(wind_unit));
                    headwind_txt = string.Format("{0:F2} {1}", 0, Localizer.Format(wind_unit));

                    humidity_txt = "-,- " + Localizer.Format("%");
                    cldfrac_txt = "-,- " + Localizer.Format("%");
                    vis_txt = "-,- " + Localizer.Format("km");

                    olr_txt = "-,- " + Localizer.Format("W/m\xB2 ");
                    tpw_txt = "-,- " + Localizer.Format(precip_unit);
                    tcld_txt = "-,- " + Localizer.Format("%");
                    rainsfc_txt = "-,- " + Localizer.Format(precip_unit);

                    mslp_txt = "-,- " + Localizer.Format(pres_unit);
                    tmpsfc_txt = "-,- " + Localizer.Format(temp_unit);
                    rhsfc_txt = "-,- " + Localizer.Format("%");
                    wspdsfc_txt = "-,- " + Localizer.Format(wind_unit);

                }
                //Show ground position (in space)
                if (showPos)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_gtrack"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_lat"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(lat_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_lng"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(lng_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_hgt"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(ter_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_biome"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(biome_txt);
                    GUILayout.EndHorizontal();
                }
                //Show velocity data
                if (showVelocity)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_vvel"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_ias"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(velocity_ias_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_eas"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(velocity_eas_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_tas"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(velocity_tas_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_gs"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(velocity_ground_txt);
                    GUILayout.EndHorizontal();
                }
                //Show Wind data
                if (showWind)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_wsdr"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_hzwnd"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(wspd_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_vwnd"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(wwind_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_wdird"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(wdir_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_wdirc"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(wdirstr_txt);
                    GUILayout.EndHorizontal();
                }
                // Show relative winds (i.e crosswinds, headwinds, etc.)
                if (showWindRelative)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_vrw"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_twind"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tailwind_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_hwind"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(headwind_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_cwind"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(crosswind_txt);
                    GUILayout.EndHorizontal();
                }
                //Show satellite obs
                if (showSat)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_rs"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_olr"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(olr_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_tcc"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tcld_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_pw"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tpw_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_prate"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(rainsfc_txt);
                    GUILayout.EndHorizontal();
                }
                //Show space-borne surface obs
                if (showSfc)
                {

                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_srfwx"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_mslp"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(mslp_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_temp"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tmpsfc_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_rh"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(rhsfc_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_wspd"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(wspdsfc_txt);
                    GUILayout.EndHorizontal();

                }
                //Show ambient environmental conditions
                if (showAmbient)
                {
                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_amb"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_prs"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(pressure_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_den"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(density_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_temp"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(temperature_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_rh"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(humidity_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_cc"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(cldfrac_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_vis"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(vis_txt);
                    GUILayout.EndHorizontal();

                }
                //Show aerodynamic data
                if (showAero)
                {

                    GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.fontStyle = FontStyle.Bold;
                    GUILayout.Label(Util.KWPTags["#autoLOC_adyn"]);
                    GUI.skin.label.fontStyle = FontStyle.Normal;
                    GUILayout.EndHorizontal();
                    GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_mach"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(mach_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_sos"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(sound_spd_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_shock"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(shock_tmp_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_dynp"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(dyn_pres_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_aoa"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(aoa_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_sang"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(side_slip_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_tlift"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tlift_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_tdrag"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(tdrag_txt);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    GUILayout.Label(Util.KWPTags["#autoLOC_ldr"]);
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUILayout.Label(ld_ratio_txt);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }
    }
}