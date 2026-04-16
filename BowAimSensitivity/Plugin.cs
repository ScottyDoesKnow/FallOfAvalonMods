using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BowAimSensitivity;

[BepInPlugin(PluginConsts.PLUGIN_GUID, PluginConsts.PLUGIN_NAME, PluginConsts.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin, IListenerOwner
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

    internal static readonly (string Id, string Name) ExtraZoomTalentInfo = ("482b26f3ee7b25d4a9d635ef1b195a1d", "Eagle Eye");
    internal static readonly (string Id, string Name) SlowTimeTalentInfo = ("79973209408a28440a0e3757ab058059", "Unnatural Focus");

    internal static PluginConfig PluginConfig;

    public Harmony HarmonyInstance { get; set; }

    private float _zoomSensitivityFactor;
    private float _extraZoomSensitivityFactor;
    private float _slowTimeSensitivityFactor;

    private bool _initialized;

    private bool? _hasExtraZoom;
    private bool? _hasSlowTime;

    private float? _originalSensitivity;
    private bool _isZoomed;
    private bool _isDrawn;

    public void Awake()
    {
        Log = Logger;
        LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loading...");

        try
        {
            PluginConfig = new PluginConfig(Config);
            _zoomSensitivityFactor = PluginConfig.ZoomSensitivityFactor.Value;
            _extraZoomSensitivityFactor = PluginConfig.ExtraZoomSensitivityFactor.Value;
            _slowTimeSensitivityFactor = PluginConfig.SlowTimeSensitivityFactor.Value;

            LogDebug($"{nameof(Awake)} | {nameof(_zoomSensitivityFactor)}: {_zoomSensitivityFactor}," +
                $" {nameof(_extraZoomSensitivityFactor)}: {_extraZoomSensitivityFactor}," +
                $" {nameof(_slowTimeSensitivityFactor)}: {_slowTimeSensitivityFactor}");

            HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is loaded.");
        }
        catch (Exception ex)
        {
            LogError($"Plugin {PluginConsts.PLUGIN_GUID} failed to load with error: {ex.Message}");
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => World.EventSystem != null);

        World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, OnTalentChanged);

        World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.OnBowZoomStart, this, OnBowZoomStart);
        World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.OnBowZoomEnd, this, OnBowZoomEnd);

        World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.OnBowDrawStart, this, OnBowDrawStart);
        World.EventSystem.ListenTo(EventSelector.AnySource, ICharacter.Events.OnBowDrawEnd, this, OnBowDrawEnd);
    }

    public void OnHeroFullyInitialized()
    {
        try
        {
            _initialized = false;

            _hasExtraZoom = null;
            _hasSlowTime = null;

            _originalSensitivity = null;
            _isZoomed = false;
            _isDrawn = false;

            if (Hero.Current == null)
            {
                LogWarning($"{nameof(OnHeroFullyInitialized)} | Failed to initialize because {nameof(Hero)}.{nameof(Hero.Current)} is null.");
                return;
            }

            if (!GetHasExtraZoom(Hero.Current, nameof(OnHeroFullyInitialized)))
                return;

            if (!GetHasSlowTime(Hero.Current, nameof(OnHeroFullyInitialized)))
                return;

            _initialized = true;

            LogDebug($"{nameof(OnHeroFullyInitialized)} | State initialized/reset.");
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnHeroFullyInitialized)} | Error: {ex.Message}");
        }
    }

    private bool GetHasExtraZoom(Hero hero, string methodName)
    {
        if (!GetTalent(hero, ExtraZoomTalentInfo, out Talent talent, methodName))
            return false;

        _hasExtraZoom = talent.Level > 0;
        return true;
    }

    private bool GetHasSlowTime(Hero hero, string methodName)
    {
        if (!GetTalent(hero, SlowTimeTalentInfo, out Talent talent, methodName))
            return false;

        _hasSlowTime = talent.Level > 0;
        return true;
    }

    private static bool GetTalent(Hero hero, (string Id, string Name) talentInfo, out Talent talent, string methodName)
    {
        talent = hero.Talents.BaseTalentTables.SelectMany(x => x.talents)
            .FirstOrDefault(x => IsTalent(x, talentInfo));

        if (talent == null)
        {
            LogError($"{methodName} | Couldn't find '{talentInfo.Name}' skill.");
            return false;
        }

        return true;
    }

    private void OnTalentChanged(Talent talent)
    {
        try
        {
            if (!_initialized)
                return;

            if (IsTalent(talent, ExtraZoomTalentInfo))
            {
                _hasExtraZoom = talent.Level > 0;
                LogDebug($"{nameof(OnTalentChanged)} | {nameof(_hasExtraZoom)} = {_hasExtraZoom}");
            }
            else if (IsTalent(talent, SlowTimeTalentInfo))
            {
                _hasSlowTime = talent.Level > 0;
                LogDebug($"{nameof(OnTalentChanged)} | {nameof(_hasSlowTime)} = {_hasSlowTime}");
            }
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnTalentChanged)} | Error: {ex.Message}");
        }
    }

    private static bool IsTalent(Talent talent, (string Id, string Name) talentInfo)
        => string.Equals(talent.Template.GUID, talentInfo.Id, StringComparison.OrdinalIgnoreCase);

    private void OnBowZoomStart(ICharacter character)
    {
        try
        {
            if (!_initialized)
                return;

            LogDebug($"{nameof(OnBowZoomStart)} | Start");

            if (!CheckCharacterAndSensitivity(character, true, out HeroCamera camera, nameof(OnBowZoomStart)))
            {
                LogDebug($"{nameof(OnBowZoomStart)} | Returned early.");
                return;
            }

            _isZoomed = true;
            UpdateSensitivity(camera, nameof(OnBowZoomStart));

            LogDebug($"{nameof(OnBowZoomStart)} | End");
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnBowZoomStart)} | Error: {ex.Message}");
        }
    }

    private void OnBowZoomEnd(ICharacter character)
    {
        try
        {
            if (!_initialized)
                return;

            LogDebug($"{nameof(OnBowZoomEnd)} | Start");

            if (!CheckCharacterAndSensitivity(character, false, out HeroCamera camera, nameof(OnBowZoomEnd)))
            {
                LogDebug($"{nameof(OnBowZoomEnd)} | Returned early.");
                return;
            }

            _isZoomed = false;
            UpdateSensitivity(camera, nameof(OnBowZoomEnd));

            LogDebug($"{nameof(OnBowZoomEnd)} | End");
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnBowZoomEnd)} | Error: {ex.Message}");
        }
    }

    private void OnBowDrawStart(ICharacter character)
    {
        try
        {
            if (!_initialized)
                return;

            LogDebug($"{nameof(OnBowDrawStart)} | Start");

            if (!CheckCharacterAndSensitivity(character, true, out HeroCamera camera, nameof(OnBowDrawStart)))
            {
                LogDebug($"{nameof(OnBowDrawStart)} | Returned early.");
                return;
            }

            if (_hasSlowTime.Value)
            {
                _isDrawn = true;
                if (_isZoomed)
                    UpdateSensitivity(camera, nameof(OnBowDrawStart));
            }

            LogDebug($"{nameof(OnBowDrawStart)} | End");
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnBowDrawStart)} | Error: {ex.Message}");
        }
    }

    private void OnBowDrawEnd(ICharacter character)
    {
        try
        {
            if (!_initialized)
                return;

            LogDebug($"{nameof(OnBowDrawEnd)} | Start");

            if (!CheckCharacterAndSensitivity(character, false, out HeroCamera camera, nameof(OnBowDrawEnd)))
            {
                LogDebug($"{nameof(OnBowDrawEnd)} | Returned early.");
                return;
            }

            if (_hasSlowTime.Value)
            {
                _isDrawn = false;
                if (_isZoomed)
                    UpdateSensitivity(camera, nameof(OnBowDrawEnd));
            }

            LogDebug($"{nameof(OnBowDrawEnd)} | End");
        }
        catch (Exception ex)
        {
            LogError($"{nameof(OnBowDrawEnd)} | Error: {ex.Message}");
        }
    }

    // LogDebug instead of LogWarning because the game tends to call end events at random times to clear states
    private bool CheckCharacterAndSensitivity(ICharacter character, bool isStartEvent, out HeroCamera camera, string methodName)
    {
        camera = null;

        if (character is not Hero hero)
            return false;

        if (hero != Hero.Current)
        {
            LogDebug($"{methodName} | {nameof(hero)} is not equal to {nameof(Hero)}.{nameof(Hero.Current)}");
            return false;
        }

        camera = hero.VHeroController?.HeroCamera;
        if (camera == null)
        {
            LogDebug($"{methodName} | {nameof(HeroCamera)} is null.");
            return false;
        }

        if (isStartEvent && _originalSensitivity == null)
        {
            _originalSensitivity = camera._sensitivity;
            LogDebug($"{methodName} | {nameof(_originalSensitivity)} = {_originalSensitivity}");
        }
        else if (_originalSensitivity == null)
        {
            LogDebug($"{methodName} | {nameof(_originalSensitivity)} hasn't been set.");
            return false;
        }

        return true;
    }

    private void UpdateSensitivity(HeroCamera camera, string methodName)
    {
        float newSensitivity = _originalSensitivity.Value;

        if (_isZoomed)
        {
            if (_hasExtraZoom.Value)
                newSensitivity *= _extraZoomSensitivityFactor;
            else
                newSensitivity *= _zoomSensitivityFactor;

            if (_hasSlowTime.Value && _isDrawn)
                newSensitivity *= _slowTimeSensitivityFactor;
        }

        camera._sensitivity = newSensitivity;

        LogDebug($"{methodName} | {nameof(_hasExtraZoom)}: {_hasExtraZoom}," +
            $" {nameof(_hasSlowTime)}: {_hasSlowTime}," +
            $" {nameof(_isZoomed)}: {_isZoomed}," +
            $" {nameof(_isDrawn)}: {_isDrawn}," +
            $" {nameof(_originalSensitivity)}: {_originalSensitivity}," +
            $" {nameof(newSensitivity)}: {newSensitivity}");

        if (!_isZoomed && !_isDrawn)
        {
            _originalSensitivity = null;
            LogDebug("Reset sensitivity.");
        }
    }

    public void OnDestroy()
    {
        LogInfo($"Plugin {PluginConsts.PLUGIN_GUID} is unloading...");

        _initialized = false;

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
