using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Dubs_Debug
{

    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance.Create("DubsDebug").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class Butil
    {
        private static List<Thing> tmpThings = new List<Thing>();

        public static IEnumerable<Thing> CalculateWealthItems()
        {
            tmpThings.Clear();
            ThingOwnerUtility.GetAllThingsRecursively<Thing>(Find.CurrentMap, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), tmpThings, false, delegate (IThingHolder x)
            {
                if (x is PassingShip || x is MapComponent)
                {
                    return false;
                }
                Pawn pawn = x as Pawn;
                return pawn == null || pawn.Faction == Faction.OfPlayer;
            }, true);
            float num = 0f;
            for (int i = 0; i < tmpThings.Count; i++)
            {
                if (tmpThings[i].SpawnedOrAnyParentSpawned && !tmpThings[i].PositionHeld.Fogged(Find.CurrentMap))
                {
                    yield return tmpThings[i];
                }
            }
            tmpThings.Clear();

        }
    }

    [HarmonyPatch(typeof(EditWindow_DebugInspector)), HarmonyPatch("CurrentDebugString")]
    internal static class Harmony_CurrentDebugString
    {
        private static List<ThingDef> tempDefs = new List<ThingDef>();
        public static void Postfix(ref string __result)
        {
            tempDefs.Clear();

            var butanebob = Butil.CalculateWealthItems();

            foreach (var item in butanebob)
            {
                if (!tempDefs.Contains(item.def))
                {
                    tempDefs.Add(item.def);
                }

            }

            foreach (var def in tempDefs)
            {
                int bigG = butanebob.Count(x => x.def == def);
                float market = butanebob.Sum(x => x.MarketValue * (float)x.stackCount);
                __result += $"\n {def.LabelCap} x{bigG} {market}";
            }

        }
    }

    [HarmonyPatch(typeof(Thing)), HarmonyPatch("Notify_ColorChanged")]
    internal static class Harmony_Notify_ColorChanged
    {
        public static void Postfix()
        {
            System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            Log.Warning(t.ToString());
        }
    }

    [HarmonyPatch(typeof(SkillRecord)), HarmonyPatch(nameof(SkillRecord.CalculateTotallyDisabled))]
    internal static class Harmony_CalculateTotallyDisabled
    {
        public static bool Prefix(SkillRecord __instance, bool __result)
        {
            __result = false;
            try
            {
                __result = __instance.def.IsDisabled(__instance.pawn.story.CombinedDisabledWorkTags, __instance.pawn.story.DisabledWorkTypes);
            }
            catch (Exception e)
            {
                string b = "DUBBUG_TEST_42_START";
                b += $"\n __instance.def {__instance?.def?.defName}";
                b += $"\n __instance.pawn {__instance?.pawn}";
                b += $"\n CombinedDisabledWorkTags == null { __instance?.pawn?.story?.CombinedDisabledWorkTags == null}";
                b += $"\n DisabledWorkTypes == null { __instance?.pawn?.story?.DisabledWorkTypes == null}";
                b += "\n";
                b += e;
                Log.Error(b);
            }
            return false;
        }
    }
}