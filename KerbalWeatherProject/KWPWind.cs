using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalWeatherProject
{
    /// Adapt FAR's set Wind function for KWP
    public static class KWPWind
    {
        /// <summary>
        /// A WindFunction takes the current celestial body and a position (should be the position of the part)
        /// and returns the wind as a Vector3 (in m/s)
        /// </summary>
        public delegate Vector3 WindFunction(CelestialBody body, Part part, Vector3 position);

        private static WindFunction func = ZeroWind;
        //Retrieve the wind vector
        public static Vector3 GetWind(CelestialBody body, Part part, Vector3 position)
        {
            try
            {
                return func(body, part, position);
            }
            catch (Exception e)
            {
                Util.Log("Exception! " + e.Message + e.StackTrace);
                return Vector3.zero;
            }
        }


        /// <summary>
        /// "Set" method for the wind function.
        /// If newFunction is null, it resets the function to ZeroWind.
        /// </summary>
        public static void SetWindFunction(WindFunction newFunction)
        {
            if (newFunction == null)
            {
                Util.Log("Attempted to set a null wind function, using ZeroWind instead.");
                KWPWind.func = ZeroWind;
            }
            else
            {
                Util.Log("Setting wind function to " + newFunction.Method.Name);
                KWPWind.func = newFunction;
            }
        }

        //Zero wind function
        public static Vector3 ZeroWind(CelestialBody body, Part part, Vector3 position)
        {
            return Vector3.zero;
        }
    }
}
