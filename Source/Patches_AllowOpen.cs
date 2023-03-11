using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace OneWayDoors;

[HarmonyPatch]
public static class Patches_AllowOpen {
    public static bool invertPos = false;
    public static bool useCell = false;
    public static IntVec3 fromCell;
    public static int fromPathDir = -1;

    public static void FromCell(IntVec3 cell) {
        fromCell = cell;
        useCell = true;
    }

    public static void FromCellList(List<IntVec3> list, int i) => FromCell(list[i]);

    public static void FromRegionLink(RegionLink link) {
        fromCell = link.span.root;
        var region = link.regions[link.regions[0].IsDoorway ? 1 : 0];
        bool use = true;
        if (fromCell.GetRegion(region.Map) != region) {
            use = false;
            for (int i = 0; i < 4; i++) {
                var cell = fromCell + GenAdj.AdjacentCells[i];
                if (cell.GetRegion(region.Map) == region) {
                    fromCell = cell;
                    use = true;
                    break;
                }
            }
        }
        useCell = use;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    public static void PawnCanOpen(Pawn p, Building_Door __instance, ref bool __result) {
        if (!__result) return;
        var comp = __instance.TryGetComp<ThingComp_DoorOneWay>();
        if (comp == null) return;
        if (fromPathDir >= 0) {
            __result = comp.IsAllowedInPathDirection(fromPathDir);
            return;
        }
        var pos = useCell ? fromCell : p.Position;
        if (useCell || pos.AdjacentTo8Way(__instance.Position)) {
            __result = invertPos ? comp.IsAllowedTo(pos) : comp.IsAllowedFrom(pos);
            return;
        }
    }


    private static readonly MethodInfo getBuildingCost = 
        AccessTools.Method(typeof(PathFinder), nameof(PathFinder.GetBuildingCost));
    private static readonly MethodInfo canPhysicallyPass = 
        AccessTools.Method(typeof(Building_Door), nameof(Building_Door.CanPhysicallyPass));
    private static readonly MethodInfo pawnCanOpen = 
        AccessTools.Method(typeof(Building_Door), nameof(Building_Door.PawnCanOpen));

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath),
        typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode), typeof(PathFinderCostTuning))]
    public static IEnumerable<CodeInstruction> FindPath_Trans(IEnumerable<CodeInstruction> original) {
        foreach (var instr in original) {
            if (instr.Calls(getBuildingCost)) {
                yield return new CodeInstruction(OpCodes.Ldloc_S, 40);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(fromPathDir));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(fromPathDir));
            } else {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(RegionCostCalculator), nameof(RegionCostCalculator.GetRegionDistance))]
    public static IEnumerable<CodeInstruction> GetRegionDistance_Trans(IEnumerable<CodeInstruction> original) {
        var entry = typeof(RegionCostCalculator).GetNestedType("RegionLinkQueueEntry", AccessTools.all);
        var link = AccessTools.PropertyGetter(entry, "Link");

        foreach (var instr in original) {
            if (instr.Calls(getBuildingCost)) {
                yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                yield return new CodeInstruction(OpCodes.Call, link);
                yield return CodeInstruction.Call(typeof(Patches_AllowOpen), nameof(FromRegionLink));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(useCell));
            } else {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PawnPathUtility), nameof(PawnPathUtility.TryFindLastCellBeforeBlockingDoor))]
    public static IEnumerable<CodeInstruction> TryFindLastCellBeforeBlockingDoor_Trans(IEnumerable<CodeInstruction> original) {
        foreach (var instr in original) {
            if (instr.Calls(canPhysicallyPass)) {
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Add);
                yield return CodeInstruction.Call(typeof(Patches_AllowOpen), nameof(FromCellList));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(useCell));
            } else {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PawnPathUtility), nameof(PawnPathUtility.FirstBlockingBuilding))]
    public static IEnumerable<CodeInstruction> FirstBlockingBuilding_Trans(IEnumerable<CodeInstruction> original) {
        foreach (var instr in original) {
            if (instr.Calls(pawnCanOpen)) {
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldloc_3);
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Add);
                yield return CodeInstruction.Call(typeof(Patches_AllowOpen), nameof(FromCellList));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(useCell));
            } else {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Pawn_PathFollower), "NeedNewPath")]
    public static IEnumerable<CodeInstruction> NeedNewPath_Trans(IEnumerable<CodeInstruction> original) {
        foreach (var instr in original) {
            if (instr.Calls(canPhysicallyPass)) {
                yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                yield return CodeInstruction.Call(typeof(Patches_AllowOpen), nameof(FromCell));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(useCell));
            } else {
                yield return instr;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static IEnumerable<CodeInstruction> TryEnterNextPathCellh_Trans(IEnumerable<CodeInstruction> original) {
        foreach (var instr in original) {
            if (instr.Calls(pawnCanOpen)) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(invertPos));
                yield return instr;
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return CodeInstruction.StoreField(typeof(Patches_AllowOpen), nameof(invertPos));
            } else {
                yield return instr;
            }
        }
    }
}
