using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LootNewItemMarker;

[BepInPlugin(PluginConsts.PLUGIN_GUID, PluginConsts.PLUGIN_NAME, PluginConsts.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    #region Logging

    private static ManualLogSource Log;

    internal static void LogDebug(object data)
    {
#if DEBUG
        Log?.LogInfo(data);
#else
        Log?.LogDebug(data);
#endif
    }

    internal static void LogInfo(object data) => Log?.LogInfo(data);

    internal static void LogWarning(object data) => Log?.LogWarning(data);

    internal static void LogError(object data) => Log?.LogError(data);

    #endregion

    public Harmony HarmonyInstance { get; set; }

    public void Awake()
    {
        Log = Logger;
        LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loading...");

        try
        {
            HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loaded.");
        }
        catch (Exception ex)
        {
            LogError($"Plugin {PluginConsts.PLUGIN_GUID} failed to load with error: {ex.Message}");
        }
    }

    public void OnDestroy()
    {
        LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is unloading...");

        try
        {
            HarmonyInstance?.UnpatchSelf();
            LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is unloaded.");
        }
        catch (Exception ex)
        {
            LogError($"Plugin {PluginConsts.PLUGIN_GUID} failed to unload with error: {ex.Message}");
        }
    }
}
