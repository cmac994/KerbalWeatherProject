using System.IO;

namespace KerbalWeatherProject_Lite
{
    //Class to read climatological data from binary files
    public static class read_climo  
    { 
        //Path to Climatology binary data
        private static string bin_path = KSPUtil.ApplicationRootPath + "\\GameData\\KerbalWeatherProject_Lite\\Binary\\Climatology";

        const int NT = 6; //Number of Times (0-6 hr)
        const int NZ = 17; //Number of vertical levels (0-17)
        const int NLAT = 46; //latitude dim
        const int NLON = 91; //longitude dim
        const int nvars = 8; //Number of climate variables
        const int nsvars = 6; //Number of climate surface variables

        //Initialize arrays
        public static float[,,,,] climo_3d = new float[nvars, NT, NZ, NLAT, NLON];
        public static float[,,,] climo_2d = new float[nsvars, NT, NLAT, NLON];
        public static float[,,] height_3d = new float[NLAT, NLON, NZ];

        //Retrieve full-atmosphere climatological data
        public static float[,,,,] getMPAS_3D(string lstr)
        {
            get_climo_3d_data(lstr);
            return climo_3d;
        }

        //Retrieve surface climatological data
        public static float[,,,] getMPAS_2D(string lstr)
        {
            get_climo_2d_data(lstr + "_surface");
            return climo_2d;
        }

        //Get Time coordinate
        public static float[] getTime()
        {
            return get1d("time", NT);
        }

        //Get latitude coordinates
        public static float[] getLat()
        {
            return get1d("latitude", NLAT);
        }

        //Get longitude coordinates
        public static float[] getLng()
        {
            return get1d("longitude", NLON);
        }


        //Retrieve coordinate data (1D)
        public static float[] get1d(string vvar, int arr1)
        {
            Util.Log("Reading Binary 1-D data: " + vvar);
            float[] vars1d = new float[arr1];
            // open the file
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + vvar + ".bin")))
            {
                for (int i = 0; i < arr1; i++)
                {
                    // read the doubles out of the byte buffer into the two dimensional array
                    // note this assumes machine-endian byte order
                    vars1d[i] = reader.ReadSingle();
                }
            }
            return vars1d;
        }

        //Read full-atmosphere climatological data 
        public static void get_climo_3d_data(string mon)
        {
            Util.Log("Reading Binary CLIMO 3D data: " + mon);
            // open the binary file for reading
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + mon + "_wx.bin")))
            {
                //Loop through atmospheric variablesc by variable, time, vertical level, latitude, and longitude
                for (int v = 0; v < nvars; v++)
                {
                    for (int i = 0; i < NT; i++)
                    {
                        for (int j = 0; j < NZ; j++)
                        {
                            for (int k = 0; k < NLAT; k++)
                            {
                                for (int l = 0; l < NLON; l++)
                                {
                                    climo_3d[v, i, j, k, l] = reader.ReadSingle(); //Read binary climatological data into a float array
                                }
                            }
                        }
                    }
                }
            }
        }

        //Retrieve 3D height data
        public static float[,,] get_height_data()
        {
            Util.Log("Reading Binary Height 3D data");
            // open the binary file for reading
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\height.bin")))
            {
                //Loop through height by vertical level, latitude, and longitude
                for (int j = 0; j < NLAT; j++)
                {
                    for (int k = 0; k < NLON; k++)
                    {
                        for (int l = 0; l < NZ; l++)
                        {
                            height_3d[j, k, l] = reader.ReadSingle(); //Read binary climatological data into a float array
                        }
                    }
                }
            }
            return height_3d;
        }

        //Retrieve surface climatological data.
        public static void get_climo_2d_data(string mon)
        {
            Util.Log("Reading Binary CLIMO 2D data: " + mon);
            // open the file binary file for reading
            using (BinaryReader reader = new BinaryReader(File.OpenRead(bin_path + "\\" + mon + "_wx.bin")))
            {
                for (int v = 0; v < nsvars; v++)
                {
                    for (int i = 0; i < NT; i++)
                    {
                        for (int j = 0; j < NLAT; j++)
                        {
                            for (int k = 0; k < NLON; k++)
                            {
                                climo_2d[v, i, j, k] = reader.ReadSingle(); //read binary climatological data into a float array
                            }
                        }
                    }
                }
            }
        }
    }
}
