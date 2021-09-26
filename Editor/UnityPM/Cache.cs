using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityUtils.UnityPM
{
    class Cache
    {
        public string CacheDirectory { get; set; }

        public bool HasHash(string hash)
        {
            return false;
        }

        public string GetPath(string hash)
        {
            return "";
        }

        public string this[string index]
        {
            get
            {
                return "";
            }
        }

        public Cache(string cacheDirectory)
        {
            CacheDirectory = cacheDirectory;
        }
    }
}
