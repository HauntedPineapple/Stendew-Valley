﻿/// Project: Stendew Valley
/// File: ModConfig.cs
/// Description: Class to hold mod tuning variables
/// Author: Team Stendew Valley

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StendewValley
{
    class ModConfig
    {
        public bool PassiveMobs { get; set; }
        public int MinSlimesPerDay { get; set; } = 1;
        public int MaxSlimesPerDay { get; set; } = 3;
        public int MaxTotalSlimesHouse { get; set; } = 10;
        public int MaxTotalSlimesCave { get; set; } = 10;
    }
}
