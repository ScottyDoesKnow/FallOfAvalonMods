using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LootNewItemMarker;

[BepInPlugin(PluginConsts.PLUGIN_GUID, PluginConsts.PLUGIN_NAME, PluginConsts.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    public Harmony HarmonyInstance { get; set; }

    public void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loading...");

        try
        {
            HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Log.LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loaded.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Plugin {PluginConsts.PLUGIN_GUID} failed to load with error: {ex.Message}");
        }
    }

    public void OnDestroy()
    {
        Log.LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is unloading...");

        try
        {
            HarmonyInstance?.UnpatchSelf();
            Log.LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is unloaded.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Plugin {PluginConsts.PLUGIN_GUID} failed to unload with error: {ex.Message}");
        }
    }
}
