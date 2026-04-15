using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using HarmonyLib;
using UnityEngine;

namespace QuickArmorSets;

[HarmonyPatch]
public class HeroArmorSetPatch
{
    [HarmonyPatch(typeof(HeroArmorSet), "ValidateSetOnSlotChanged")]
    [HarmonyPrefix]
    public static bool ValidateSetOnSlotChangedPrefix(EquipmentSlotType slot)
    {
        try
        {
            Plugin.Log?.LogDebug($"{nameof(HeroArmorSetPatch)}.{nameof(ValidateSetOnSlotChangedPrefix)} | Called");
            return VCQuickArmorSet.ArmorSlotTypes.Contains(slot);
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"{nameof(HeroArmorSetPatch)}.{nameof(ValidateSetOnSlotChangedPrefix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(HeroArmorSetPatch)}.{nameof(ValidateSetOnSlotChangedPrefix)} error: {ex}");
            return true;
        }
    }
}
