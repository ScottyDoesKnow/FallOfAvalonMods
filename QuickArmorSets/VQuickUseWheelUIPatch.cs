using System;
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
    private const string SlotActiveSpriteName = "slot_active";

    private const float WheelDiameter = 400;
    private const float VerticalShift = 370;
    private const float Scale = 0.9f;

    private static Sprite SlotActiveSprite
    {
        get
        {
            if (_slotActiveSprite == null)
            {
                _slotActiveSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == SlotActiveSpriteName);
                if (_slotActiveSprite != null)
                    Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(SlotActiveSprite)} | Found {SlotActiveSpriteName} sprite.");
            }

            return _slotActiveSprite;
        }
    }
    private static Sprite _slotActiveSprite;

    [HarmonyPatch(typeof(VQuickUseWheelUI), nameof(VQuickUseWheelUI.OnInitialize))]
    [HarmonyPostfix]
    public static void OnInitializePostfix(VQuickUseWheelUI __instance)
    {
        try
        {
            Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Start");

            if (!VCQuickArmorSet.HasAnyArmorSets)
            {
                Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | No armor sets found, returning early.");
                return;
            }

            Transform oldWheel = __instance.transform.Find(QuickUseActionsName);
            if (oldWheel == null)
            {
                Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Couldn't find {QuickUseActionsName} wheel.");
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

            Utils.LogHierarchy(newWheel, $"Hierarchy of {nameof(newWheel)} at start.");

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
                Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Unable to find {nameof(VCQuickItemBase)} to use as default.");
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
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {child.name} has {nameof(VCQuickUseAction)} but no {nameof(VCQuickUseActionMouseHelper)}.");
                    continue;
                }

                ItemSlotUI oldSlot;
                if (oldAction is VCQuickItemBase oldItem)
                {
                    if (oldItem.slot == null)
                    {
                        Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {child.name}.{nameof(oldItem.slot)} == null.");
                        continue;
                    }
                    oldSlot = oldItem.slot;
                }
                else
                {
                    oldSlot = CloneItemSlotUI(oldAction, defaultItem);
                    if (oldSlot == null)
                    {
                        Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(CloneItemSlotUI)} returned null for {child.name}.");
                        continue;
                    }
                }

                VCQuickArmorSet newItem = child.gameObject.AddComponent<VCQuickArmorSet>();
                child.name = $"{nameof(VCQuickArmorSet)}{nameof(Transform)}[{index}]";

                toDestroy.Add(oldAction);

                Image previewImage = newItem.transform.Find("PreviewIcon")?.GetComponent<Image>();
                if (previewImage == null)
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(previewImage)} is null.");
                else
                    previewImage.enabled = false;

                Image previewIconImage = newItem.transform.Find("PreviewIcon")?.Find("Icon")?.GetComponent<Image>();
                if (previewIconImage == null)
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(previewIconImage)} is null.");
                else
                    previewIconImage.enabled = false;

                Image favouriteImage = newItem.transform.Find("PrimarySlot")?.Find("TopRightCorner")?.Find("Favourite")?.Find("FavouriteIcon")?.GetComponent<Image>();
                if (favouriteImage == null)
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(favouriteImage)} is null.");
                else
                    favouriteImage.enabled = false;

                try { newItem.Initialize(oldAction, oldSlot, helper, index++); }
                catch (Exception ex)
                {
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Failed to initialize {child.name} with error: {ex.Message}");
                    continue;
                }

                try { newItem.Attach(__instance.Services, __instance.Target, __instance); }
                catch (Exception ex)
                {
                    Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Failed to attach {child.name} with error: {ex.Message}");
                    continue;
                }

                newItems.Add(newItem);
            }

            // Do this after to not mess with defaultItem
            foreach (VCQuickArmorSet newItem in newItems)
                if (newItem.IsSelected)
                    if (SlotActiveSprite == null)
                        Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(SlotActiveSprite)} is null.");
                    else
                    {
                        Image slotImage = newItem.transform.Find("PrimarySlot")?.Find("Backgrounds")?.Find("Main")?.Find("MainBackground")?.GetComponent<Image>();
                        if (slotImage == null)
                            Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | {nameof(slotImage)} is null.");
                        else
                        {
                            slotImage.sprite = SlotActiveSprite;
                            slotImage.color = Color.white;
                        }
                    }

            foreach (VCQuickUseAction oldAction in toDestroy)
                UnityEngine.Object.DestroyImmediate(oldAction); // Immediate to get it out of hierarchy for logging

#if DEBUG
            __instance.StartCoroutine(Utils.LogHierarchyDelayed(newWheel, $"Hierarchy of {nameof(newWheel)} at end."));
#endif

            Plugin.LogDebug($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | End");
        }
        catch (Exception ex)
        {
            Plugin.LogError($"{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} | Error: {ex}");
            Debug.LogError($"{PluginConsts.PLUGIN_NAME}.{nameof(VQuickUseWheelUIPatch)}.{nameof(OnInitializePostfix)} error: {ex}");
        }
    }

    private static ItemSlotUI CloneItemSlotUI(VCQuickUseAction quickUseAction, VCQuickItemBase defaultQuickItemBase)
    {
        Transform oldPrimarySlot = quickUseAction.transform.Find(PrimarySlotName);
        if (oldPrimarySlot == null)
        {
            Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {quickUseAction.transform.name} has no {PrimarySlotName}");
            return null;
        }

        Transform defaultPrimarySlot = defaultQuickItemBase.transform.Find(PrimarySlotName);
        if (defaultPrimarySlot == null)
        {
            Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {defaultQuickItemBase.transform.name} has no {PrimarySlotName}");
            return null;
        }

        Transform newPrimarySlot = UnityEngine.Object.Instantiate(defaultPrimarySlot, quickUseAction.transform);
        newPrimarySlot.name = PrimarySlotName;

        Rotate(newPrimarySlot);

        CanvasGroup canvasGroup = newPrimarySlot.Find("IconSlot")?.Find("Icon")?.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(CloneItemSlotUI)} | {nameof(newPrimarySlot)} has no {nameof(CanvasGroup)}");
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
                Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(Rotate)} | {component} not found.");
                continue;
            }
            toRotate.Add(transform);
        }

        Transform slot = primarySlot.Find("Backgrounds")?.Find("Main")?.Find(MainBackgroundName);
        if (slot == null)
            Plugin.LogWarning($"{nameof(VQuickUseWheelUIPatch)}.{nameof(Rotate)} | {MainBackgroundName} not found.");
        else
            toRotate.Add(slot);

        foreach (Transform transform in toRotate)
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }
}
