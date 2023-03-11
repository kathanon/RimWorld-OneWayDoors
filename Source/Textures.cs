using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace OneWayDoors {
    [StaticConstructorOnStartup]
    public static class Textures {
        private const string Prefix = Strings.ID + "/";

        public static readonly Texture2D North      = ContentFinder<Texture2D>.Get(Prefix + "North");
        public static readonly Texture2D South      = ContentFinder<Texture2D>.Get(Prefix + "South");
        public static readonly Texture2D East       = ContentFinder<Texture2D>.Get(Prefix + "East");
        public static readonly Texture2D West       = ContentFinder<Texture2D>.Get(Prefix + "West");
        public static readonly Texture2D NorthSouth = ContentFinder<Texture2D>.Get(Prefix + "NorthSouth");
        public static readonly Texture2D EastWest   = ContentFinder<Texture2D>.Get(Prefix + "EastWest");
    }
}
