using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OneWayDoors;

[StaticConstructorOnStartup]
public class ThingComp_DoorOneWay : ThingComp {
    public enum Direction {
        None  = 0,

        North = 1,
        South = 2,
        East  = 4,
        West  = 8,

        NorthOrSouth = North | South,
        NorthOrEast  = North | East,
        EastOrWest   = East  | West,
    }

    private static readonly int[] FromPathDirections = { 0, 6, 4, 2, 7, 5, 3, 1 };
    private static readonly Direction[] nsToggleOrder = { Direction.None, Direction.North, Direction.South };
    private static readonly Direction[] ewToggleOrder = { Direction.None, Direction.East,  Direction.West };
    private static readonly List<int[]> EmptyDirections = new();
    private static readonly Material baseMaterial =
        new(ShaderDatabase.Transparent) { color = new Color(1f, 1f, 1f, 0.5f) };
    private static readonly Material[] materials = {
        new(baseMaterial) { mainTexture = Textures.North },
        new(baseMaterial) { mainTexture = Textures.South },
        new(baseMaterial) { mainTexture = Textures.East },
        new(baseMaterial) { mainTexture = Textures.West },
    };
    private static readonly float altitude = AltitudeLayer.BuildingOnTop.AltitudeFor();

    public Building_Door Door => parent as Building_Door;

    public bool Active => allowedFrom >= 0;

    public bool Valid => valid;

    public bool Applies => parent.Faction == Faction.OfPlayer;

    public int AdjacentAreas => AdjacentRoomDirections.Count;

    public string Setting => Active ? desired.Translate() : Strings.Off;

    public Texture2D Icon {
        get {
            if (desired != iconFor) {
                icon = desired switch {
                    Direction.North => Textures.North,
                    Direction.South => Textures.South,
                    Direction.East  => Textures.East,
                    Direction.West  => Textures.West,
                    _ => northSouth  ? Textures.NorthSouth
                                     : Textures.EastWest,
                };
                iconFor = desired;
            }
            return icon;
        }
    }

    private int Index
        => desired switch {
            Direction.North => 0,
            Direction.South => 1,
            Direction.East => 2,
            Direction.West => 3,
            _ => -1,
        };

    private Direction desired = Direction.None;

    private List<int[]> AdjacentRoomDirections = null;
    private int allowedFrom = -1;
    private bool valid = false;
    private bool northSouth;
    private bool zeroIsNOrE;
    private Texture2D icon = Textures.NorthSouth;
    private Direction iconFor = Direction.NorthOrSouth;
    private readonly int[] tempIsIn = new int[4];
    private float lastUpdate = 0f;

    private void ApplyDesired() {
        if (DesiredValid) {
            allowedFrom = (DesiredNorthOrEast == zeroIsNOrE) ? 0 : 1;
        } else {
            allowedFrom = -1;
        }
    }

    public void Toggle() {
        if (valid) {
            var order = northSouth ? nsToggleOrder : ewToggleOrder;
            int i = order.FirstIndexOf(x => x == desired);
            if (i < 0) i = 0;
            i = (i + 1) % 3;
            desired = order[i];
            ApplyDesired();
        }
    }

    public bool IsAllowedTo(IntVec3 cell) {
        Update();
        if (!Active) return true;

        var adj = cell - parent.Position;
        return AdjacentRoomDirections[1 - allowedFrom].Any(x => GenAdj.AdjacentCellsAround[x] == adj);
    }

    public bool IsAllowedFrom(IntVec3 cell) {
        Update();
        if (!Active) return true;

        var adj = cell - parent.Position;
        return AdjacentRoomDirections[allowedFrom].Any(x => GenAdj.AdjacentCellsAround[x] == adj);
    }

    public bool IsAllowedInPathDirection(int dir) {
        Update();
        if (!Active) return true;

        return AdjacentRoomDirections[allowedFrom].Contains(FromPathDirections[dir]);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra() {
        Update();

        if (Applies) yield return new ToggleOneWay(this);
    }

    public override void PostDraw() {
        Update();

        if (Active && Settings.ShouldDraw(Door)) {
            Vector3 drawPos = parent.DrawPos;
            drawPos.y = altitude;
            Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, materials[Index], 0);
        }
    }

    public override string CompInspectStringExtra()
        => Active ? Strings.InspectExtra(desired) : null;

    public override void PostExposeData() {
        Scribe_Values.Look(ref desired, Strings.ID, Direction.None);
    }

    private bool DesiredEastOrWest => (desired & Direction.EastOrWest) > 0;

    private bool DesiredNorthOrSouth => (desired & Direction.NorthOrSouth) > 0;

    private bool DesiredNorthOrEast => (desired & Direction.NorthOrEast) > 0;

    private bool DesiredValid => northSouth ? DesiredNorthOrSouth : DesiredEastOrWest;

    public void Update() {
        if (AdjacentRoomDirections != null && Time.realtimeSinceStartup - lastUpdate < 0.5f) return;
        lastUpdate = Time.realtimeSinceStartup;

        if (!Applies) {
            allowedFrom = -1;
            valid = false;
            AdjacentRoomDirections = EmptyDirections;
            return;
        }

        var old = AdjacentRoomDirections;
        var New = CalculateAdjacentRoomDirections();
        bool replace = old == null || New.Count != old.Count;
        for (int i = 0; !replace && i < New.Count; i++) {
            var oldRoom = old[i];
            var newRoom = New[i];
            if (oldRoom.Length == newRoom.Length) {
                for (int j = 0; j < oldRoom.Length; j++) {
                    if (oldRoom[j] != newRoom[j]) replace = true;
                }
            } else {
                replace = true;
            }
        }

        if (replace) {
            AdjacentRoomDirections = New;
            if (New.Count == 2) {
                for (int i = 0; i < 4; i++) {
                    tempIsIn[i] = -1;
                }
                for (int i = 0; i < 2; i++) {
                    var room = New[i];
                    for (int j = 0; j < room.Length; j++) {
                        if (room[j] % 2 == 0) tempIsIn[room[j] / 2] = i;
                    }
                }

                bool ns = tempIsIn[0] != -1 && tempIsIn[2] != -1 && tempIsIn[0] != tempIsIn[2];
                bool ew = tempIsIn[1] != -1 && tempIsIn[3] != -1 && tempIsIn[1] != tempIsIn[3];
                northSouth = ns;
                if (ns && ew && DesiredEastOrWest) {
                    int i = (desired == Direction.East) ? 1 : 3;
                    desired = (tempIsIn[i] == tempIsIn[0]) ? Direction.North : Direction.South;
                } else if (ns != ew && !DesiredValid) {
                    desired = Direction.None;
                }
                valid = ns || ew;
                zeroIsNOrE = tempIsIn[ns ? 2 : 3] == 0;

                ApplyDesired();
            } else {
                northSouth = true;
                valid = false;
                allowedFrom = -1;
            }
            // Ensure icon is recalculated.
            iconFor = Direction.NorthOrSouth;
        }
    }

    private List<int[]> CalculateAdjacentRoomDirections() {
        var map = parent.Map;
        var pos = parent.Position;
        List<int[]> res = new();
        IntVec3[] cells = new IntVec3[8];
        bool[] walkable = new bool[8];

        for (int i = 0; i < 8; i++) {
            cells[i] = pos + GenAdj.AdjacentCellsAround[i];
            walkable[i] = cells[i].WalkableByNormal(map);
        }
        int first = walkable.FirstIndexOf(x => !x);

        if (first < 0) {
            res.Add(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        } else {
            int curStart = -1;
            Room curRoom = null;
            for (int i = first; i <= first + 8; i++) {
                int j = i % 8;
                bool start = false;
                bool stop = !walkable[j];
                if (!stop) {
                    var room = cells[j].GetRoom(map);
                    bool empty = curRoom == null;
                    bool same = room == curRoom;
                    start = empty || !same;
                    stop = !empty && !same;
                    if (start) curRoom = room;
                }
                if (stop) {
                    if (curStart >= 0) {
                        int n = i - curStart;
                        var dirs = new int[n];
                        for (int k = 0; k < n; k++) {
                            dirs[k] = (curStart + k) % 8;
                        }
                        res.Add(dirs);
                    }

                    curStart = -1;
                    curRoom = null;
                }
                if (start) curStart = i;
            }
        }
        return res;
    }
}
