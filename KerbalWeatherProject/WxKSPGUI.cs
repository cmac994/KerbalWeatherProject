using UnityEngine;
using KSP.Localization;
using System;
using System.Collections.Generic;
using ToolbarControl_NS;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using static KerbalWeatherProject.KerbalWx;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Expansions.Missions.Tests;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WxKSPGUI : MonoBehaviour
    {

        //public KerbalWx kwx;
        // GUI bits
        public static Rect titleRect = new Rect(0, 0, 10000, 10000);
        public static Rect windowPos = new Rect(100, 100, 250, 250);
        public KerbalWx _kwx;
        Vector3d kwind; Vector3d ktherm;
        public KeyCode key = KeyCode.I;
        public bool useModifier = true;
        public bool winterOwlModeOff = true;
        private ToolbarControl toolbarController;
        private bool toolbarButtonAdded = false;
        private bool guiIsUp = false;
        public bool wx_enabled = true;
        public static Vector2 posFlightMainWindow = new Vector2(Screen.width / 2f, Screen.height / 2f);
        private string wstr;

        const int nvars = 8;
        const int nsvars = 9;
        List<double> vel_list = new List<double>();
        List<double> wx_list2d = new List<double>();
        List<double> wx_list3d = new List<double>();

        private static DialogGUIVerticalLayout info_page;
        private static DialogGUIVerticalLayout thermo_page;

        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIBox page_box;

        private const float width = 285.0f;
        private const float height = 350.0f;
        private const float button_width = 50.0f;
        private const float button_height = 25.0f;

        private static DialogGUILabel uwind_label;
        private static DialogGUILabel vwind_label;
        private static DialogGUILabel wwind_label;
        private static DialogGUILabel wspd_label;
        private static DialogGUILabel wdir_label;

        private static DialogGUILabel velocity_ias_label;
        private static DialogGUILabel velocity_tas_label;
        private static DialogGUILabel velocity_ground_label;
        private static DialogGUILabel tailwind_label;
        private static DialogGUILabel crosswind_label;

        private static DialogGUILabel pressure_label;
        private static DialogGUILabel temperature_label;
        private static DialogGUILabel humidity_label;

        private static string wspd_txt = "";
        private static string wdir_txt = "";
        private static string velocity_ias_txt = "";
        private static string velocity_tas_txt = "";
        private static string velocity_ground_txt = "";
        private static string tailwind_txt = "";
        private static string crosswind_txt = "";

        private static string uwind_txt = "";
        private static string vwind_txt = "";
        private static string wwind_txt = "";

        private static string pressure_txt = "";
        private static string temperature_txt = "";
        private static string humidity_txt = "";

        private const int page_padding = 10;

        public Vector2 MainGUIWindowPos;
        public int MainGUICurrentPage;
        //public bool GUIEnabled = false;
        public bool MainGUIEnabled = false;
        public Rect MapGUIWindowPos;
        private static bool visible = false;
        private static int spawned = 0;

        // Health Monitor window

        // page type enum
        private enum PageType
        {
            INFO = 0,
            THERMO
        }

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
                    KerbalWx.MODID,
                    "368859",
                    "KerbalWeatherProject/Textures/KWP_minimal_large",
                    "KerbalWeatherProject/Textures/KWP_minimal_small",
                    Localizer.Format(KerbalWx.MODNAME));
                toolbarButtonAdded = true;
            }
        }

        private void RemoveToolbarButton()
        {
            if (toolbarButtonAdded)
            {
                toolbarController.OnDestroy();
                Destroy(toolbarController);
                toolbarButtonAdded = false;
            }
        }

        private void ToolbarButtonOnTrue()
        {
            MainGUIEnabled = true;
            Show();
            //guiIsUp = true;
        }

        private void ToolbarButtonOnFalse()
        {
            MainGUIEnabled = false;
            Hide(); 
            //guiIsUp = false;
        }

        // display update timer
        private static double update_timer = Util.Clocks;
        private const double update_fps = 10;

        void Start()
        {
            wx_enabled = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams>().WxEnabled;
            Debug.Log("[KerbalWxGUI]: Wx Enabled, " + wx_enabled.ToString());

            MainGUIWindowPos.x = 0.803645849f;
            MainGUIWindowPos.y = 0.497222215f;

            MapGUIWindowPos.xMin = 0;
            MapGUIWindowPos.xMax = 1;
            MapGUIWindowPos.yMin = 0;
            MapGUIWindowPos.yMax = 0;
            MainGUICurrentPage = 0;

            if (multi_dialog == null)
                Allocate();

            // set page padding
            info_page.padding.left = page_padding;
            info_page.padding.right = page_padding;
            thermo_page.padding.left = page_padding;
            thermo_page.padding.right = page_padding;

            kwind.x = 0.0f;
            kwind.y = 0.0f;
            kwind.z = 0.0f;

            for (int i=0; i < 6; i++)
            {
                vel_list.Add(0);
            }
            for (int i=0; i < nvars; i++)
            {
                wx_list3d.Add(0);
            }
            for (int i = 0; i < nsvars; i++)
            {
                wx_list2d.Add(0);
            }

            wstr = HighLogic.CurrentGame.Parameters.CustomParams<KerbalWxCustomParams>().windstrs;
            //set data field labels justification

            //_kwx = GetComponent<KerbalWx>();
            _kwx = (KerbalWx)FindObjectOfType(typeof(KerbalWx));
            AddToolbarButton();
            Debug.Log("[KerbalWxGUI]: Instantiate Toolbar and KWx");
        }

        void FixedUpdate()
        {
            kwind = _kwx.get3DWind();
            ktherm = _kwx.get3DThermo();
            vel_list = _kwx.getAero();

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (spawned != 2 && popup_dialog != null)
                {
                    if (spawned == 0)
                    {
                        popup_dialog.gameObject.SetActive(true);
                        spawned = 1;
                    }
                    else
                    {
                        Hide();
                        spawned = 2;
                    }
                    return;
                }

                // hide or show the dialog box
                if ((!MainGUIEnabled || PlanetariumCamera.Camera == null) && visible)
                {
                    Hide();
                    return;
                }
                else if (MainGUIEnabled && (!visible || popup_dialog == null))
                {
                    Show();
                }
                UpdatePages();
            } 
        }

        public void Show()
        {
            if (popup_dialog == null)
            {
                SpawnDialog();
            }
            visible = true;
            popup_dialog.gameObject.SetActive(true);
        }

        /// <summary> Hides window. </summary>
        public void Hide()
        {
            DeSpawn();
            if (popup_dialog != null)
            {
                visible = false;
                popup_dialog.gameObject.SetActive(false);
            }
        }

        private void DeSpawn()
        {
            if (popup_dialog != null)
                popup_dialog.Dismiss();

            popup_dialog = null;
        }

        private void SpawnDialog()
        {
            if (multi_dialog != null)
            {
                if (ClampToScreen())
                    multi_dialog.dialogRect.Set(MainGUIWindowPos.x, MainGUIWindowPos.y, width, height);

                popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, false, HighLogic.UISkin, false, "");
                popup_dialog.onDestroy.AddListener(new UnityAction(OnPopupDialogDestroy));
                popup_dialog.SetDraggable(true);
            }
        }
        
        private void OnPopupDialogDestroy()
        {
            // save popup position. Note. PopupDialog.RTrf is an offset from the center of the screen.
            if (popup_dialog != null)
            {
                MainGUIWindowPos = new Vector2(
                    ((Screen.width / 2) + (popup_dialog.RTrf.position.x / GameSettings.UI_SCALE)) / Screen.width,
                    ((Screen.height / 2) + (popup_dialog.RTrf.position.y / GameSettings.UI_SCALE)) / Screen.height);
                //Util.DebugLog("Saving MainGUI window position as {0}", Settings.MainGUIWindowPos.ToString("F4"));
                multi_dialog.dialogRect.Set(MainGUIWindowPos.x, MainGUIWindowPos.y, width, height);
            }
            visible = false;
        }

        private void UpdatePages()
        {
            // skip updates for a smoother display and increased performance
            if (Util.Clocks - update_timer <= System.Diagnostics.Stopwatch.Frequency / update_fps)
                return;
            update_timer = Util.Clocks;

            switch ((PageType)MainGUICurrentPage)
            {
                case PageType.INFO:
                    UpdateInfoPage();
                    return;
                case PageType.THERMO:
                    UpdateThermoPage();
                    return;
            }
        }
        private void SetDataFieldJustification()
        {
            if (MainGUICurrentPage == (int)PageType.INFO)
            {
                uwind_label.text.alignment = TextAlignmentOptions.MidlineRight;
                vwind_label.text.alignment = TextAlignmentOptions.MidlineRight;
                wwind_label.text.alignment = TextAlignmentOptions.MidlineRight;
                wspd_label.text.alignment = TextAlignmentOptions.MidlineRight;
                wdir_label.text.alignment = TextAlignmentOptions.MidlineRight;

                velocity_ias_label.text.alignment = TextAlignmentOptions.MidlineRight;
                velocity_tas_label.text.alignment = TextAlignmentOptions.MidlineRight;
                velocity_ground_label.text.alignment = TextAlignmentOptions.MidlineRight;
                tailwind_label.text.alignment = TextAlignmentOptions.MidlineRight;
                crosswind_label.text.alignment = TextAlignmentOptions.MidlineRight;
            }
            else if (MainGUICurrentPage == (int)PageType.THERMO)
            {
                pressure_label.text.alignment = TextAlignmentOptions.MidlineRight;
                temperature_label.text.alignment = TextAlignmentOptions.MidlineRight;
                humidity_label.text.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        private void UpdateInfoPage()
        {

            int wdir2 = (int)Math.Round(((180.0 / Math.PI) * Math.Atan2(-1.0 * kwind.z, -1.0 * kwind.x)), 0); // Direction wind is blowing from.
            if (wdir2 < 0)
            {
                wdir2 += 360;
            }
            string wdir_str = get_wind_card(wdir2, wstr);
            double wspd = Math.Sqrt(Math.Pow(kwind.x, 2) + Math.Pow(kwind.y, 2) + Math.Pow(kwind.z, 2));

            if (wx_enabled)
            {
                uwind_txt = string.Format("{0:F2} {1}", kwind.z, Localizer.Format("m/s"));
                vwind_txt = string.Format("{0:F2} {1}", kwind.x, Localizer.Format("m/s"));
                wwind_txt = string.Format("{0:F2} {1}", kwind.y * 1000.0, Localizer.Format("mm/s"));
                wspd_txt = string.Format("{0:F2} {1}", wspd, Localizer.Format("m/s"));
                wdir_txt = string.Format("{0:F1} {1}", wdir2, Localizer.Format("°"))+" ("+wdir_str+")";

                velocity_ias_txt = string.Format("{0:F2} {1}", vel_list[0], Localizer.Format("m/s"));
                velocity_tas_txt = string.Format("{0:F2} {1}", vel_list[1], Localizer.Format("m/s"));
                velocity_ground_txt = string.Format("{0:F2} {1}", vel_list[3], Localizer.Format("m/s"));
                tailwind_txt = string.Format("{0:F2} {1}", vel_list[4], Localizer.Format("m/s"));
                crosswind_txt = string.Format("{0:F2} {1}", vel_list[5], Localizer.Format("m/s"));
            } else
            {
                uwind_txt = "-,- " + Localizer.Format("m/s'");
                vwind_txt = "-,- " + Localizer.Format("m/s'");
                wwind_txt = "-,- " + Localizer.Format("mm/s'");
                wspd_txt = "-,- " + Localizer.Format("m/s'");
                wdir_txt = "-,- " + Localizer.Format("°");

                velocity_ias_txt = "-,- " + Localizer.Format("m/s'");
                velocity_tas_txt = "-,- " + Localizer.Format("m/s'");
                velocity_ground_txt = "-,- " + Localizer.Format("m/s'");
                tailwind_txt = "-,- " + Localizer.Format("m/s'");
                crosswind_txt = "-,- " + Localizer.Format("m/s'");
            }
        }
        
        private void UpdateThermoPage()
        {
            if (wx_enabled)
            {
                pressure_txt = string.Format("{0:F2} {1}", (ktherm.x / 100.0), Localizer.Format("hPa"));
                temperature_txt = string.Format("{0:F2} {1}", ktherm.y, Localizer.Format("K"));
                humidity_txt = string.Format("{0:F1} {1}", ktherm.z, Localizer.Format("%"));
            } 
            else
            {
                pressure_txt = "-,- " + Localizer.Format("hPa");
                temperature_txt = "-,- " + Localizer.Format("K");
                humidity_txt = "-,- " + Localizer.Format("%");
            }
        }
        
        public void Allocate()
        {
            //DialogGUILayoutBase layout = new DialogGUIVerticalLayout(true, true);

            uwind_label = new DialogGUILabel(() => { return uwind_txt; }, 65);
            vwind_label = new DialogGUILabel(() => { return vwind_txt; }, 65);
            wwind_label = new DialogGUILabel(() => { return wwind_txt; }, 65);
            wspd_label = new DialogGUILabel(() => { return wspd_txt; }, 65);
            wdir_label = new DialogGUILabel(() => { return wdir_txt; }, 65);

            //Velocity
            velocity_ias_label = new DialogGUILabel(() => { return velocity_ias_txt; }, 65);
            velocity_tas_label = new DialogGUILabel(() => { return velocity_tas_txt; }, 65);
            velocity_ground_label = new DialogGUILabel(() => { return velocity_ground_txt; }, 65);

            tailwind_label = new DialogGUILabel(() => { return tailwind_txt; }, 65);
            crosswind_label = new DialogGUILabel(() => { return crosswind_txt; }, 65);

            //Thermo
            pressure_label = new DialogGUILabel(() => { return pressure_txt; }, 65);
            temperature_label = new DialogGUILabel(() => { return temperature_txt; }, 65);
            humidity_label = new DialogGUILabel(() => { return humidity_txt; }, 65);

            Debug.Log("[KerbalWxGUI]: Toggle Wx Enabled, " + wx_enabled.ToString());

            // create pages
            info_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return wx_enabled; },
                        Localizer.Format("Toggle Weather"), OnButtonClick_ToggleWx)),

                //new DialogGUILabel("Wind Speed and Direction", Util.Style_Label_Normal_Center_White, true),
                new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("Wind Speed and Direction") + "</color></b>", true),//Name

                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Zonal Wind Speed (U)"), true),
                    new DialogGUILabel(() => { return uwind_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Meridional Wind Speed (V)"), true),
                    new DialogGUILabel(() => { return vwind_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Vertical Wind Speed (W): "), true),
                    new DialogGUILabel(() => { return wwind_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Horizontal Wind Speed"), true),
                    new DialogGUILabel(() => { return wspd_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Wind Direction"), true),
                    new DialogGUILabel(() => { return wdir_txt; })),

                //new DialogGUILabel("Vehicle Speed and Relative Wind", Util.Style_Label_Normal_Center_White, true),
                new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("Vehicle Speed and Relative Wind") + "</color></b>", true),//Name

                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Tailwind (+) | Headwind (-)"), true),
                    new DialogGUILabel(() => { return tailwind_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Crosswind"), true),
                    new DialogGUILabel(() => { return crosswind_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Indicated Airspeed"), true),
                    new DialogGUILabel(() => { return velocity_ias_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("True Airspeed"), true),
                    new DialogGUILabel(() => { return velocity_tas_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Ground speed"), true),
                    new DialogGUILabel(() => { return velocity_ground_txt; }))
                ); 

            thermo_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return wx_enabled; },
                        Localizer.Format("Toggle Weather"), OnButtonClick_ToggleWx)),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Atmospheric Pressure"), true),
                    new DialogGUILabel(() => { return pressure_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Ambient Temperature"), true),
                    new DialogGUILabel(() => { return temperature_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("Relative Humidity"), true),
                    new DialogGUILabel(() => { return humidity_txt; }))
                );

            switch ((PageType) MainGUICurrentPage)
            {
                case PageType.THERMO:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, thermo_page);
                    break;
                default:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, info_page);
                    break;
            }

            //Debug.Log("[KerbalWxGUI] X: " + MainGUIWindowPos.x.ToString() + ", Y: " + MainGUIWindowPos.y.ToString());
            // create base window for popup dialog
            multi_dialog = new MultiOptionDialog(
                "KWPMainGUI",
                "",
                Localizer.Format("Kerbal Weather Project") + " - 1.0",
                HighLogic.UISkin,
                // window origin is center of rect, position is offset from lower left corner of screen and normalized
                // i.e (0.5, 0.5 is screen center)

                new Rect(MainGUIWindowPos.x, MainGUIWindowPos.y, width, height),
                new DialogGUIBase[]
                {
                    // create page select buttons
                    new DialogGUIHorizontalLayout(true, false, 2, new RectOffset(), TextAnchor.MiddleCenter,
                        new DialogGUIButton(Localizer.Format("Kinematics"),
                            OnButtonClick_Info, ButtonEnabler_Info, button_width, button_height, false),
                        new DialogGUIButton(Localizer.Format("Thermodynamics"),
                            OnButtonClick_Thermo, ButtonEnabler_Thermo, button_width, button_height, false)), 
                    page_box
                }
            );
        }
        private void OnButtonClick_Info() => ChangePage(PageType.INFO);
        private void OnButtonClick_Thermo() => ChangePage(PageType.THERMO);

        private bool ButtonEnabler_Info()
        {
            if ((PageType)MainGUICurrentPage == PageType.INFO)
                return false;
            return true;
        }

        private bool ButtonEnabler_Thermo()
        {
            if ((PageType)MainGUICurrentPage == PageType.THERMO)
                return false;
            return true;
        }

        private void ChangePage(PageType inpage)
        {
            MainGUICurrentPage = (int)inpage;

            // remove current page from page box
            page_box.children[0].uiItem.gameObject.DestroyGameObjectImmediate();
            page_box.children.Clear();

            // insert desired page into page box
            switch (inpage)
            {
                case PageType.THERMO:
                    page_box.children.Add(thermo_page);
                    break;
                default:
                    page_box.children.Add(info_page);
                    break;
            }

            // required to force the Gui to update
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(page_box.uiItem.gameObject.transform);
            page_box.children[0].Create(ref stack, HighLogic.UISkin);

            //set data field label justification
            SetDataFieldJustification();
        }

        private bool ClampToScreen()
        {
            float border = 50f;
            bool adjusted = false;

            //Debug.Log("[KerbalWxGUI] x position: " + MainGUIWindowPos.x.ToString() + ", y position: " + MainGUIWindowPos.y.ToString());
            if (MainGUIWindowPos.x <= 0.0f || MainGUIWindowPos.y <= 0.0f)
            {
                // default window to center of screen
                MainGUIWindowPos = new Vector2(0.5f, 0.5f);
                adjusted = true;
            }
            else
            {
                // ensure window remains within the screen bounds
                Vector2 pos = new Vector2(((MainGUIWindowPos.x * Screen.width) - (Screen.width / 2)) * GameSettings.UI_SCALE,
                                          ((MainGUIWindowPos.y * Screen.height) - (Screen.height / 2)) * GameSettings.UI_SCALE);

                if (pos.x > (Screen.width / 2) - border)
                {
                    pos.x = (Screen.width / 2) - (border + (width / 2));
                    adjusted = true;
                }
                else if (pos.x < ((Screen.width / 2) - border) * -1f)
                {
                    pos.x = ((Screen.width / 2) - (border + (width / 2))) * -1f;
                    adjusted = true;
                }

                if (pos.y > (Screen.height / 2) - border)
                {
                    pos.y = (Screen.height / 2) - (border + (height / 2));
                    adjusted = true;
                }
                else if (pos.y < ((Screen.height / 2) - border) * -1f)
                {
                    pos.y = ((Screen.height / 2) - (border + (height / 2))) * -1f;
                    adjusted = true;
                }

                if (adjusted)
                {
                    MainGUIWindowPos = new Vector2(
                        ((Screen.width / 2) + (pos.x / GameSettings.UI_SCALE)) / Screen.width,
                        ((Screen.height / 2) + (pos.y / GameSettings.UI_SCALE)) / Screen.height);
                }
                //Debug.Log("[KerbalWxGUI] x2 position: " + MainGUIWindowPos.x.ToString() + ", y2 position: " + MainGUIWindowPos.y.ToString());
            }
            return adjusted;
        }

        void Destroy()
        {
            RemoveToolbarButton();
            DeSpawn();
            multi_dialog = null;
            //GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbarButton);
        }

        /*public void OnGUI()
        {
            if (guiIsUp)
            {
                windowPos = GUILayout.Window("KerbalWeatherProject".GetHashCode(), windowPos, DrawWindow, "KerbalWeatherProject");
            }
        }*/

        private string get_wind_card(double wdir, string wstr)
        {
            Debug.Log("[KerbalWxGUI]: "+wstr);
            string wdir_str = "N/A";
            if (wstr == "N,S,E,W")
            {
                //Cardinal Wind Directions
                if ((wdir > 315) && (wdir <= 45))
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
                if ((wdir > 337.5) && (wdir <= 22.5))
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
                if ((wdir > 348.75) && (wdir <= 11.25))
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

        public void OnButtonClick_ToggleWx(bool inState)
        {
            Debug.Log("[KerbalWxGUI]: InState, " + inState.ToString() + ", wx_enabled: " + wx_enabled.ToString());
            wx_enabled = inState;
            //wx_enabled = !wx_enabled;
            _kwx.setWindBool(wx_enabled);
        }

        /*
        public void DrawWindow(int windowID)
        {
            // Enable closing of the window tih "x"
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(10, 10, 6, 0);
            buttonStyle.margin = new RectOffset(2, 2, 2, 2);
            buttonStyle.stretchWidth = false;
            buttonStyle.stretchHeight = false;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.wordWrap = false;

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle WX", buttonStyle))
            {
                wx_enabled = !wx_enabled;
                _kwx.setWindBool(wx_enabled);

                Debug.Log("[KerbalWxGUI]: Wind button clicked wind is: " + wx_enabled.ToString());
            }
            GUILayout.EndHorizontal();
            // Now the basic craft stats
            // see above in the method where they are calculated for explanations.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!wx_enabled)
                {
                    kwind = Vector3.zero;
                    ktherm = Vector3.zero;
                    for (int v = 0; v < vel_list.Count; v++)
                    {
                        vel_list[v] = 0;
                    }
                }

                int wdir2 = (int)Math.Round(((180.0 / Math.PI) * Math.Atan2(-1.0 * kwind.z, -1.0 * kwind.x)), 0); // Direction wind is blowing from.
                if (wdir2 < 0)
                {
                    wdir2 += 360;
                }

                string wdir_str = get_wind_card(wdir2, wstr);
                double wspd = Math.Sqrt(Math.Pow(kwind.x, 2) + Math.Pow(kwind.y, 2) + Math.Pow(kwind.z, 2));

                velocity_ias_txt = string.Format("{0:F1} {1}", vel_list[0], Localizer.Format("m/s"));
                velocity_tas_txt = string.Format("{0:F1} {1}", vel_list[1], Localizer.Format("m/s"));
                velocity_eas_txt = string.Format("{0:F1} {1}", vel_list[2], Localizer.Format("m/s"));
                velocity_ground_txt = string.Format("{0:F1} {1}", vel_list[3], Localizer.Format("m/s"));
                velocity_ias_label.text.alignment = TextAlignmentOptions.MidlineRight;

                GUILayout.BeginHorizontal();
                GUILayout.Label("U: " + kwind.z.ToString("N2") + " m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("V: " + kwind.x.ToString("N2") + " m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("W: " + (kwind.y * 1000.0).ToString("N2") + " mm/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Wind Direction: " + wdir2.ToString("N2") + "° (" + wdir_str + ")", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Wind Speed: " + wspd.ToString("N2") + " m/s", labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Tailwind (+) | Headwind (-): " + vel_list[4].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Crosswind: " + vel_list[5].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Velocity (IAS): " + vel_list[0].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Velocity (TAS): " + vel_list[1].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Velocity (EAS): " + vel_list[2].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Velocity (Ground): " + vel_list[3].ToString("N2") + "m/s", labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Pressure: " + (ktherm.x / 100.0).ToString("N2") + " hPa", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Temperature: " + ktherm.y.ToString("N2") + " K", labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Relative Humidity: " + ktherm.z.ToString("N2") + "%", labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }*/
    }
}