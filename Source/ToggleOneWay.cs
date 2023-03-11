using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Verse;

namespace OneWayDoors;

public class ToggleOneWay : Command_Action {
    public ToggleOneWay(ThingComp_DoorOneWay comp) {
        string setting = comp.Setting;
        string description = null;
        if (comp.Valid) {
            description = Strings.DescriptionValid(setting);
        } else {
            description = (comp.AdjacentAreas < 2) ? Strings.DisabledNowhere : Strings.DisabledMany;
        }

        defaultLabel = comp.Active ? Strings.LabelSet(setting) : Strings.LabelOff;
        defaultDesc = description;
        icon = comp.Icon;
        action = comp.Toggle;
        disabled = !comp.Valid;
    }

    protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms) {
        Settings.GizmoRect = butRect;
        return base.GizmoOnGUIInt(butRect, parms);
    }
}
