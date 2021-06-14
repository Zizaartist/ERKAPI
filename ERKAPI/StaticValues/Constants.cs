using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.StaticValues
{
    public class Constants
    {
        public const int MAX_SIZE = 500;

        public static Uri IMAGE_BLOB_PATH(string storageName) 
        {
            return new Uri(string.Format("https://{0}.blob.core.windows.net/erkimages/", storageName));
        }
    }
}
