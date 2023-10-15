using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StendewValley
{
    internal class Globals
    {
        public static IManifest Manifest { get; set; }
        public static IModHelper Helper { get; set; }
        public static IMonitor Monitor { get; set; }
        public static ModInfo Info { get; set; }
    }
}
