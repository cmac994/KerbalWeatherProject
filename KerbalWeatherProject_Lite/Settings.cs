using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace KerbalWeatherProject_Lite
{
    public class KerbalWxCustomParams : GameParameters.CustomParameterNode
    {

        //Define mod name
        public override string Title { get { return "#autoLOC_030"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Kerbal Weather Project"; } }
        public override string DisplaySection { get { return "Kerbal Weather Project"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        public bool switched = false;

        //Setting to enable/disable weather by default (can always enable wx in game using the GUI toggle button)
        [GameParameters.CustomParameterUI("#autoLOC_001",
            toolTip = "#autoLOC_002")]
        public bool WxEnabled = true;

        //Use climatology or point forecasts
        [GameParameters.CustomParameterUI("#autoLOC_015",
            toolTip = "#autoLOC_016")]
        public bool use_climo = true;

        [GameParameters.CustomParameterUI("#autoLOC_017",
            toolTip = "#autoLOC_018")]
        public bool use_point = false;

        //Enable or disable thermodynamics effects
        [GameParameters.CustomParameterUI("#autoLOC_021",
            toolTip = "#autoLOC_022")]
        public bool allow_thermo = true;

        //Enable or disable aerodynamic effects.
        [GameParameters.CustomParameterUI("#autoLOC_023",
            toolTip = "#autoLOC_024")]
        public bool allow_aero = true;


        //Switch between climatology and point forecast booleans (only one can be selected at a time)
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        { 
            if (use_climo)
            {
                if ((use_point) && (!switched))
                {
                    use_climo = false;
                    switched = true; 
                }
            }
            if (use_point) { 
                if ((use_climo) && (switched))
                {
                    use_point = false;
                    switched = false;
                }
            }
            return true;
        }
    }

    public class KerbalWxCustomParams_Sec2 : GameParameters.CustomParameterNode

    {
        //Define mod name
        public override string Title { get { return "#autoLOC_031"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Kerbal Weather Project"; } }
        public override string DisplaySection { get { return "Kerbal Weather Project"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        public bool wind_switched = false;
        public bool wind_switched2 = false;
        public bool wind_switched3 = false;

        //Use wind from MPAS
        [GameParameters.CustomParameterUI("#autoLOC_029")]
        public bool use_mpaswind = true;

        //Enable or disable aerodynamic effects.
        [GameParameters.CustomParameterUI("#autoLOC_025",
            toolTip = "#autoLOC_026")]
        public bool use_cnstprofile = false;

        // Sliders for overriding MPAS wind speed with power law profile //

        //Set wind speed at 10 m ASL
        [GameParameters.CustomIntParameterUI("#autoLOC_027", minValue = 0, maxValue = 100, stepSize = 5, autoPersistance = true)]
        public int set_wspeed = 20;

        //Set Fixed Wind direction
        [GameParameters.CustomIntParameterUI("#autoLOC_028", minValue = 0, maxValue = 360, stepSize = 15, autoPersistance = true)]
        public int set_wdir = 90;

        //Disable wind near terrain for easier takeoff/landing
        [GameParameters.CustomParameterUI("#autoLOC_019",
            toolTip = "#autoLOC_020")]

        public bool disable_surface_wind = false;

        //Switch between wind profile booleans (only one can be selected at a time)
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (use_mpaswind)
            {
                if ((use_cnstprofile) && (!wind_switched))
                {
                    use_mpaswind = false;
                    wind_switched = true;
                }
            }

            if (use_cnstprofile)
            {
                if ((use_mpaswind) && (wind_switched))
                {
                    use_cnstprofile = false;
                    wind_switched = false;
                }
            }

            return true;
        }
    }

    public class KerbalWxCustomParams_Sec3 : GameParameters.CustomParameterNode

    {

        //Define mod name
        public override string Title { get { return "#autoLOC_032"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Kerbal Weather Project"; } }
        public override string DisplaySection { get { return "Kerbal Weather Project"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        public bool wind_switched = false;

        //Define specificity of wind direction
        [GameParameters.CustomStringParameterUI("#autoLOC_003", autoPersistance = true,
            toolTip = "#autoLOC_004")]
        public string windstrs = "N,NNE,NE,ENE,...";

        //Select velocity units
        [GameParameters.CustomStringParameterUI("#autoLOC_005", autoPersistance = true,
            toolTip = "#autoLOC_006")]
        public string velunit = "m/s";

        //Select wind units
        [GameParameters.CustomStringParameterUI("#autoLOC_007", autoPersistance = true,
            toolTip = "#autoLOC_008")]
        public string windunit = "m/s";

        //Select temperature units
        [GameParameters.CustomStringParameterUI("#autoLOC_009", autoPersistance = true,
            toolTip = "#autoLOC_010")]
        public string tempunit = "K";

        //Select pressure units
        [GameParameters.CustomStringParameterUI("#autoLOC_011", autoPersistance = true,
            toolTip = "#autoLOC_012")]
        public string presunit = "hPa";

        //Select precip units
        [GameParameters.CustomStringParameterUI("#autoLOC_013", autoPersistance = true,
            toolTip = "#autoLOC_014")]
        public string precipunit = "mm";


        //Switch between wind profile booleans (only one can be selected at a time)
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        //Provide list of unit options to chose from
        public override IList ValidValues(MemberInfo member)
        {
            if (member.Name == "windstrs")
            {
                List<string> wlist = new List<string>();
                wlist.Add("N,NNE,NE,ENE,...");
                wlist.Add("N,NE,E,SE,...");
                wlist.Add("N,S,E,W");
                IList windstrs = wlist;
                return windstrs;
            }

            if (member.Name == "velunit")
            {
                List<string> vunit_list = new List<string>();
                vunit_list.Add("m/s");
                vunit_list.Add("kts");
                vunit_list.Add("mph");
                vunit_list.Add("km/h");
                IList velunit = vunit_list;
                return velunit;
            }

            if (member.Name == "windunit")
            {
                List<string> wunit_list = new List<string>();
                wunit_list.Add("m/s");
                wunit_list.Add("kts");
                wunit_list.Add("mph");
                wunit_list.Add("km/h");
                IList windunit = wunit_list;
                return windunit;
            }

            if (member.Name == "tempunit")
            {
                List<string> tunit_list = new List<string>();
                tunit_list.Add("K");
                tunit_list.Add("C");
                tunit_list.Add("F");
                tunit_list.Add("Ra");
                IList tempunit = tunit_list;
                return tempunit;
            }

            if (member.Name == "presunit")
            {
                List<string> punit_list = new List<string>();
                punit_list.Add("Pa");
                punit_list.Add("hPa");
                punit_list.Add("inHg");
                punit_list.Add("mmHg");
                punit_list.Add("psi");
                punit_list.Add("atm");
                IList presunit = punit_list;
                return presunit;
            }

            if (member.Name == "precipunit")
            {
                List<string> preunit_list = new List<string>();
                preunit_list.Add("mm");
                preunit_list.Add("cm");
                preunit_list.Add("in");
                IList precipunit = preunit_list;
                return precipunit;
            }

            return null;
        }
    }

}