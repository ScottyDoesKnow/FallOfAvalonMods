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
        try { Chainloader.ManagerObject.GetComponent<Plugin>()?.OnHeroFullyInitialized(); }
        catch (Exception ex)
        {
            Plugin.LogError($"{nameof(HeroPatch)}.{nameof(OnFullyInitializedPostfix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(HeroPatch)}.{nameof(OnFullyInitializedPostfix)} error: {ex}");
        }
    }
}
