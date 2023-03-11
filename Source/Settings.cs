using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace OneWayDoors;
public class Settings : ModSettings {
    public static Rect GizmoRect;

    private static readonly Color dimmed = new(1f, 1f, 1f, 0.5f);
    private static readonly string[] checkLabels = { Strings.Always, Strings.Select, Strings.HoverDoor, Strings.HoverGizmo, Strings.Ctrl,
    };
    private static float checkWidth = 0f;

    private bool always = true;
    private bool select = true;
    private bool hoverDoor = true;
    private bool hoverGizmo = true;
    private bool ctrl = true;

    private static Settings instance;

    public Settings() {
        instance = this;
    }

    public static bool ShouldDraw(Building_Door door)
        => (instance ?? Main.Instance.Settings).ShouldDrawInt(door);

    private bool ShouldDrawInt(Building_Door door) 
        => always
        || select 
            && Find.Selector.IsSelected(door)
        || hoverDoor 
            && Mouse.IsOver(door.Position.ToUIRect())
        || hoverGizmo 
            && Find.Selector.SelectedObjects.OfType<Building_Door>().Any() 
            && Mouse.IsOver(GizmoRect)
        || ctrl 
            && (Event.current.control || Event.current.command);

    public void DoUI(Rect rect) {
        rect.height = Text.LineHeight + 4f;
        Widgets.Label(rect, Strings.ShowArrows);
        rect.x += 30f;
        if (checkWidth == 0f) {
            checkWidth = checkLabels.Select(Text.CalcSize).Max(s => s.x) + Widgets.CheckboxSize + 10f;
        }
        rect.width = checkWidth;

        rect.y += rect.height;
        Widgets.CheckboxLabeled(rect, Strings.Always, ref always);
        if (always) GUI.color = dimmed;

        rect.y += rect.height;
        Widgets.CheckboxLabeled(rect, Strings.Select, ref select, always);

        rect.y += rect.height;
        Widgets.CheckboxLabeled(rect, Strings.HoverDoor, ref hoverDoor, always);

        rect.y += rect.height;
        Widgets.CheckboxLabeled(rect, Strings.HoverGizmo, ref hoverGizmo, always);

        rect.y += rect.height;
        Widgets.CheckboxLabeled(rect, Strings.Ctrl, ref ctrl, always);

        GUI.color = Color.white;
    }

    public override void ExposeData() {
        Scribe_Values.Look(ref always,     "always");
        Scribe_Values.Look(ref select,     "select");
        Scribe_Values.Look(ref hoverDoor,  "hoverDoor");
        Scribe_Values.Look(ref hoverGizmo, "hoverGizmo");
        Scribe_Values.Look(ref ctrl,       "ctrl");
    }
}
