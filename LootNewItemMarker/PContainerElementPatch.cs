using System;
using System.Linq;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.MVC;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace LootNewItemMarker;

[HarmonyPatch]
public class PContainerElementPatch
{
    private const string NewThingMarkerName = "ScottyDoesKnow_LootNewItemMarker_new_thing_marker";
    private const string NewThingSpriteName = "icon_menu_focus_color";

    private static Sprite NewThingSprite
    {
        get
        {
            if (_newThingSprite == null)
            {
                _newThingSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == NewThingSpriteName);
                if (_newThingSprite != null)
                    Plugin.LogDebug($"{nameof(PContainerElementPatch)}.{nameof(NewThingSprite)} | Found {NewThingSpriteName} sprite.");
            }

            return _newThingSprite;
        }
    }
    private static Sprite _newThingSprite;

    [HarmonyPatch(typeof(PContainerElement), nameof(PContainerElement.CacheVisualElements))]
    [HarmonyPostfix]
    public static void CacheVisualElementsPostfix(PContainerElement __instance)
    {
        try
        {
            Plugin.LogDebug($"{nameof(PContainerElementPatch)}.{nameof(CacheVisualElementsPostfix)} | Start");

            VisualElement itemIcon = __instance._icon?._itemIcon;
            if (itemIcon == null)
            {
                Plugin.LogWarning($"{nameof(PContainerElementPatch)}.{nameof(CacheVisualElementsPostfix)} | {nameof(itemIcon)} is null.");
                return;
            }

            var newThingMarker = new VisualElement { name = NewThingMarkerName };

            newThingMarker.style.position = Position.Absolute;
            newThingMarker.style.right = Length.Percent(5);
            newThingMarker.style.top = Length.Percent(5);
            newThingMarker.style.width = Length.Percent(16);
            newThingMarker.style.height = Length.Percent(28);
            newThingMarker.style.backgroundImage = new StyleBackground(NewThingSprite);
            newThingMarker.style.display = DisplayStyle.None;

            itemIcon.Add(newThingMarker);

            Plugin.LogDebug($"{nameof(PContainerElementPatch)}.{nameof(CacheVisualElementsPostfix)} | End");
        }
        catch (Exception ex)
        {
            Plugin.LogError($"{nameof(PContainerElementPatch)}.{nameof(CacheVisualElementsPostfix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(PContainerElementPatch)}.{nameof(CacheVisualElementsPostfix)} error: {ex}");
        }
    }

    [HarmonyPatch(typeof(PContainerElement), nameof(PContainerElement.SetData))]
    [HarmonyPostfix]
    public static void SetDataPostfix(PContainerElement __instance, [HarmonyArgument(1)] Item item)
    {
        try
        {
            Plugin.LogDebug($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | Start");

            if (item == null)
            {
                Plugin.LogWarning($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | {nameof(item)} is null.");
                return;
            }

            VisualElement itemIcon = __instance._icon?._itemIcon;
            if (itemIcon == null)
            {
                Plugin.LogWarning($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | {nameof(itemIcon)} is null.");
                return;
            }

            VisualElement newThingMarker = itemIcon.Q(NewThingMarkerName);
            if (newThingMarker == null)
            {
                Plugin.LogWarning($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | {nameof(newThingMarker)} is null.");
                return;
            }

            NewThingsTracker newThingsTracker = World.Services.Get<NewThingsTracker>();
            if (newThingsTracker == null)
            {
                Plugin.LogWarning($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | {nameof(newThingsTracker)} is null.");
                return;
            }

            bool isNewThing = !item.IsFish && !newThingsTracker.WasSeen(item.NewThingId);
            if (isNewThing && item.HasElement<ItemToCurrency>())
                isNewThing = false;

            newThingMarker.style.display = isNewThing ? DisplayStyle.Flex : DisplayStyle.None;

            Plugin.LogDebug($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | End");
        }
        catch (Exception ex)
        {
            Plugin.LogError($"{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(PContainerElementPatch)}.{nameof(SetDataPostfix)} error: {ex}");
        }
    }
}
