using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QuickArmorSets;

[HarmonyPatch]
public class VQuickUseWheelUIPatch
{
    private const string QuickUseActionsName = "QuickUseActions";
    private const string PrimarySlotName = "PrimarySlot";
    private const string MainBackgroundName = "MainBackground";

    private const float WheelDiameter = 400;
    private const float VerticalShift = 370;
    private const float Scale = 0.9f;

    [HarmonyPatch(typeof(VQuickUseWheelUI), "OnInitialize")]
    [HarmonyPostfix]
    public static void OnInitializePostfix(VQuickUseWheelUI __instance)
    {
        try
        {
            Plugin.Log?.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Start");

            if (!VCQuickArmorSet.HasAnyArmorSets)
            {
                Plugin.Log?.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | No armor sets found, returning early.");
                return;
            }

            Transform oldWheel = __instance.transform.Find(QuickUseActionsName);
            if (oldWheel == null)
            {
                Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Couldn't find {QuickUseActionsName} wheel.");
                return;
            }

            Transform newWheel = UnityEngine.Object.Instantiate(oldWheel, oldWheel.parent);
            newWheel.name = "QuickArmorSetActions";

            RectTransform oldWheelRect = oldWheel.GetComponent<RectTransform>();
            RectTransform newWheelRect = newWheel.GetComponent<RectTransform>();

            // Origin is bottom left
            float scaleShift = WheelDiameter * (1 - Scale) / 2;
            newWheelRect.anchoredPosition += new Vector2(scaleShift, (VerticalShift / 2) + scaleShift);
            oldWheelRect.anchoredPosition += new Vector2(0, -(VerticalShift / 2) + scaleShift);

            Vector3 scale = newWheelRect.localScale;
            scale.x *= Scale;
            scale.y *= Scale;
            newWheelRect.localScale = scale;

#if DEBUG
            LogHierarchy(newWheel, $"Hierarchy of {nameof(newWheel)} at start.");
#endif

            VCQuickItemBase defaultItem = null;
            foreach (Transform child in newWheel)
            {
                VCQuickItemBase action = child.GetComponent<VCQuickItemBase>();
                if (action == null)
                    continue;

                defaultItem = action;
                break;
            }

            if (defaultItem == null)
            {
                Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Unable to find {nameof(VCQuickItemBase)} to use as default.");
                return;
            }

            int index = 0;
            List<VCQuickUseAction> toDestroy = []; // Delay destroy to keep defaultItem around
            List<VCQuickArmorSet> newItems = [];
            foreach (Transform child in newWheel)
            {
                VCQuickUseAction oldAction = child.GetComponent<VCQuickUseAction>();
                if (oldAction == null)
                    continue;

                VCQuickUseActionMouseHelper helper = child.GetComponent<VCQuickUseActionMouseHelper>();
                if (helper == null)
                {
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {child.name} has {nameof(VCQuickUseAction)} but no {nameof(VCQuickUseActionMouseHelper)}.");
                    continue;
                }

                ItemSlotUI oldSlot;
                if (oldAction is VCQuickItemBase oldItem)
                {
                    if (oldItem.slot == null)
                    {
                        Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {child.name}.{nameof(oldItem.slot)} == null.");
                        continue;
                    }
                    oldSlot = oldItem.slot;
                }
                else
                {
                    oldSlot = CloneItemSlotUI(oldAction, defaultItem);
                    if (oldSlot == null)
                    {
                        Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(CloneItemSlotUI)} returned null for {child.name}.");
                        continue;
                    }
                }

                VCQuickArmorSet newItem = child.gameObject.AddComponent<VCQuickArmorSet>();
                child.name = $"{nameof(VCQuickArmorSet)}{nameof(Transform)}[{index}]";

                toDestroy.Add(oldAction);

                Image previewImage = newItem.transform.Find("PreviewIcon")?.GetComponent<Image>();
                if (previewImage == null)
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(previewImage)} is null.");
                else
                    previewImage.enabled = false;

                Image previewIconImage = newItem.transform.Find("PreviewIcon")?.Find("Icon")?.GetComponent<Image>();
                if (previewIconImage == null)
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(previewIconImage)} is null.");
                else
                    previewIconImage.enabled = false;

                Image favouriteImage = newItem.transform.Find("PrimarySlot")?.Find("TopRightCorner")?.Find("Favourite")?.Find("FavouriteIcon")?.GetComponent<Image>();
                if (favouriteImage == null)
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(favouriteImage)} is null.");
                else
                    favouriteImage.enabled = false;

                try { newItem.Initialize(oldAction, oldSlot, helper, index++); }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Failed to initialize {child.name} with error: {ex}");
                    continue;
                }

                try { newItem.Attach(__instance.Services, __instance.Target, __instance); }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Failed to attach {child.name} with error: {ex}");
                    continue;
                }

                newItems.Add(newItem);
            }

            // Delay to not mess with defaultItem
            foreach (VCQuickArmorSet newItem in newItems)
                if (newItem.IsSelected)
                {
                    Sprite slotActiveSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "slot_active");
                    if (slotActiveSprite == null)
                        Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(slotActiveSprite)} is null.");
                    else
                    {
                        Image slotImage = newItem.transform.Find("PrimarySlot")?.Find("Backgrounds")?.Find("Main")?.Find("MainBackground")?.GetComponent<Image>();
                        if (slotImage == null)
                            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(slotImage)} is null.");
                        else
                        {
                            slotImage.sprite = slotActiveSprite;
                            slotImage.color = Color.white;
                        }
                    }
                }

            foreach (VCQuickUseAction oldAction in toDestroy)
                UnityEngine.Object.DestroyImmediate(oldAction); // Immediate to get it out of hierarchy for logging

#if DEBUG
            __instance.StartCoroutine(LogHierarchyDelayed(newWheel, $"Hierarchy of {nameof(newWheel)} at end."));
#endif

            Plugin.Log?.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | End");
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} error: {ex}");
        }
    }

    private static ItemSlotUI CloneItemSlotUI(VCQuickUseAction quickUseAction, VCQuickItemBase defaultQuickItemBase)
    {
        Transform oldPrimarySlot = quickUseAction.transform.Find(PrimarySlotName);
        if (oldPrimarySlot == null)
        {
            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {quickUseAction.transform.name} has no {PrimarySlotName}");
            return null;
        }

        Transform defaultPrimarySlot = defaultQuickItemBase.transform.Find(PrimarySlotName);
        if (defaultPrimarySlot == null)
        {
            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {defaultQuickItemBase.transform.name} has no {PrimarySlotName}");
            return null;
        }

        Transform newPrimarySlot = UnityEngine.Object.Instantiate(defaultPrimarySlot, quickUseAction.transform);
        newPrimarySlot.name = PrimarySlotName;

        Rotate(newPrimarySlot);

        CanvasGroup canvasGroup = newPrimarySlot.Find("IconSlot")?.Find("Icon")?.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {nameof(newPrimarySlot)} has no {nameof(CanvasGroup)}");
        else
            canvasGroup.alpha = 1;

        UnityEngine.Object.DestroyImmediate(oldPrimarySlot.gameObject); // Immediate to get it out of hierarchy for logging

        return newPrimarySlot.GetComponent<ItemSlotUI>();
    }

    private static void Rotate(Transform primarySlot)
    {
        List<Transform> toRotate = [];

        string[] componentsToUnrotate = ["IconSlot", "TopRightCorner"];
        foreach (string component in componentsToUnrotate)
        {
            Transform transform = primarySlot.Find(component);
            if (transform == null)
            {
                Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(Rotate)} | {component} not found.");
                continue;
            }
            toRotate.Add(transform);
        }

        Transform slot = primarySlot.Find("Backgrounds")?.Find("Main")?.Find(MainBackgroundName);
        if (slot == null)
            Plugin.Log?.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(Rotate)} | {MainBackgroundName} not found.");
        else
            toRotate.Add(slot);

        foreach (Transform transform in toRotate)
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

#if DEBUG
    private static IEnumerator LogHierarchyDelayed(Transform transform, string title = null, float delay = 0.1f)
    {
        yield return new WaitForSeconds(delay);
        LogHierarchy(transform, title);
    }

    private static void LogHierarchy(Transform transform, string title = null, string indent = "")
    {
        if (transform == null)
            return;

        if (!string.IsNullOrEmpty(title))
            Plugin.Log?.LogInfo($"{nameof(VQuickUseWheelUIPatch)}.{nameof(LogHierarchy)} | {title}");

        var componentStrings = new List<string>();
        foreach (Component component in transform.GetComponents<Component>())
        {
            if (component == null)
                continue;

            string spriteName = string.Empty;
            bool disabled = false;
            bool hidden = false;
            switch (component)
            {
                case Image image:
                    if (image.sprite != null)
                        spriteName = $"{image.sprite.name} [{image.color.r}, {image.color.g}, {image.color.b}, {image.color.a}]";
                    else
                        spriteName = "NULL";

                    if (!image.enabled)
                        disabled = true;
                    break;
                case CanvasGroup canvasGroup:
                    if (canvasGroup.alpha <= 0)
                        hidden = true;
                    break;
                case Behaviour behaviour:
                    if (!behaviour.enabled)
                        disabled = true;
                    break;
                default:
                    break;
            }

            List<string> states = [];
            if (!component.gameObject.activeSelf)
                states.Add("INACTIVE");
            if (disabled)
                states.Add("DISABLED");
            if (hidden)
                states.Add("HIDDEN");

            string componentString = component.GetType().Name;
            if (!string.IsNullOrEmpty(spriteName))
                componentString += $" ({spriteName})";
            if (states.Any())
                componentString += $" [{string.Join(", ", states)}]";

            componentStrings.Add(componentString);
        }

        string inactiveString = !transform.gameObject.activeSelf ? " [INACTIVE]" : string.Empty;
        Plugin.Log?.LogInfo($"{nameof(VQuickUseWheelUIPatch)}.{nameof(LogHierarchy)} | {indent}{transform.name}{inactiveString} | Components: {string.Join(", ", componentStrings)}");

        foreach (Transform child in transform)
            LogHierarchy(child, indent: indent + "  ");
    }
#endif
}
