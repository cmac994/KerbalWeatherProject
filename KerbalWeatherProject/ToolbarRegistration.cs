using UnityEngine;
using ToolbarControl_NS;

namespace KerbalWeatherProject
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    //Toolbar registration class
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(KerbalWxClimo.MODID, KerbalWxClimo.MODNAME);
        }
    }
}