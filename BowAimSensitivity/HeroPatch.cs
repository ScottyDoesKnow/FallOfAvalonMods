using System;
using Awaken.TG.Main.Heroes;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace BowAimSensitivity;

[HarmonyPatch]
public class HeroPatch
{
    [HarmonyPatch(typeof(Hero), nameof(Hero.OnFullyInitialized))]
    [HarmonyPostfix]
    public static void OnFullyInitializedPostfix()
    {
        try
        {
            Plugin plugin = Chainloader.ManagerObject.GetComponent<Plugin>();
            plugin?.OnHeroFullyInitialized();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(HeroPatch)} error: {ex}");
        }
    }
}
