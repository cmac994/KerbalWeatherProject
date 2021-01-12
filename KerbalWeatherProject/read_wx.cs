using System.IO;

namespace KerbalWeatherProject
{

    public static class read_wx
    {
        //Get Location of point weather data
        private static string bin_path = KSPUtil.ApplicationRootPath + "\\GameData\\KerbalWeatherproject\\Binary\\Point";

        //Define dimensions of binary array
        const int NT = 12810; //Temporal dimension
        const int NZ = 17; //Vertical dimension
        const int nvars = 8; //3D full-atmosphere variables
        const int nsvars = 6; //2D surface weather variables

        //Initialize 2-d and 3-d weather (i.e. meteorological) fields
        public static float[,,] point_3d = new float[nvars, NT, NZ];
        public static float[,] point_2d = new float[nsvars, NT];
        public static bool gotMPAS;

        //Get 3-D point meteorological data data (i.e. lat,lng, height)
        public static float[,,] getMPAS_3D(string lstr)
        {
            get_ts_3d_data(lstr);
            return point_3d;
        }

        //Get 2-d surface meteorological data (i.e. lat,lng)
        public static float[,] getMPAS_2D(string lstr) {
            get_ts_2d_data(lstr + "_surface");
            return point_2d;
        }

        //Get coordinates of data
        public static float[] getHeight()
        {
            return get1d("height", NZ);
        }

        public static float[] getTime()
        {
            return get1d("time", NT);
        }

        //Retrieve 1-d coordinate data from file
        public static float[] get1d(string vvar, int arr1)
        {
            float[] vars1d = new float[arr1];
            // open the file
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + vvar + ".bin")))
            {
                for (int i = 0; i < arr1; i++)
                {
                    // Read float from binary file
                    vars1d[i] = reader.ReadSingle(); //Read coordinate data into float array
                }
            }
            return vars1d;
        }

        //Get Full atmospheric data
        public static void get_ts_3d_data(string mon)
        {
            // open the file
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + mon + "_wx.bin")))
            {
                for (int v = 0; v < nvars; v++)
                {
                    for (int i = 0; i < NT; i++)
                    {
                        // Read from binary file
                        for (int j = 0; j < NZ; j++)
                        {
                            point_3d[v, i, j] = reader.ReadSingle(); //read 3-D point meteorological fields into a float array
                        }
                    }
                }
            }
        }

        //Get surface data
        public static void get_ts_2d_data(string mon)
        {
            // open the file
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + mon + "_wx.bin")))
            {
                for (int v = 0; v < nsvars; v++)
                {
                    //Read from binary file
                    for (int i = 0; i < NT; i++)
                    {
                        point_2d[v, i] = reader.ReadSingle(); //read 2-D point meteorological fields into a float array
                    }
                }
            }
        }
    }
}
