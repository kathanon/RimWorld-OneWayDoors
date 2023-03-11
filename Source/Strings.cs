using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace OneWayDoors;

public static class Strings {
    public const string ID = "kathanon.OneWayDoors";
    public const string Name = "One-Way Doors";

    public static readonly string Off             = (ID + ".Off"            ).Translate();
    public static readonly string LabelOff        = (ID + ".LabelOff"       ).Translate();
    public static readonly string DisabledNowhere = (ID + ".DisabledNowhere").Translate();
    public static readonly string DisabledMany    = (ID + ".DisabledMany"   ).Translate();
    public static readonly string ShowArrows      = (ID + ".ShowArrows"     ).Translate();
    public static readonly string Always          = (ID + ".Always"         ).Translate();
    public static readonly string Select          = (ID + ".Select"         ).Translate();
    public static readonly string HoverDoor       = (ID + ".HoverDoor"      ).Translate();
    public static readonly string HoverGizmo      = (ID + ".HoverGizmo"     ).Translate();
    public static readonly string Ctrl            = (ID + ".Ctrl"           ).Translate();


    private const string DirectionKey = ID + ".Direction.";
    public static string Translate(this ThingComp_DoorOneWay.Direction dir) 
        => (DirectionKey + dir).Translate();


    private const string DescriptionValidKey = ID + ".DescriptionValid";
    public static string DescriptionValid(string setting) 
        => DescriptionValidKey.Translate(setting);

    private const string LabelSetKey = ID + ".LabelSet";
    public static string LabelSet(string setting) 
        => LabelSetKey.Translate(setting);

    private const string InspectExtraKey = ID + ".InspectExtra";
    public static string InspectExtra(ThingComp_DoorOneWay.Direction dir) 
        => InspectExtraKey.Translate(dir.Translate());
}
