using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;

namespace OneWayDoors {
    [StaticConstructorOnStartup]
    public class Main : Mod {
        public static Main Instance { get; private set; }

        public Settings Settings => GetSettings<Settings>();

        static Main() {
            var harmony = new Harmony(Strings.ID);
            harmony.PatchAll();
        }

        public Main(ModContentPack content) : base(content) {
            Instance = this;
        }

        public override void DoSettingsWindowContents(Rect inRect) 
            => Settings.DoUI(inRect);

        public override string SettingsCategory() 
            => Strings.Name;
    }
}
