using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;

namespace QuickArmorSets
{
    public class VCQuickArmorSet : VCQuickItemBase
    {
        private static readonly EquipmentSlotType[] EquipmentSlotTypes = [
            EquipmentSlotType.Helmet,
            EquipmentSlotType.Cuirass,
            EquipmentSlotType.Gauntlets,
            EquipmentSlotType.Greaves,
            EquipmentSlotType.Boots,
            EquipmentSlotType.Back
        ];

        public int Index { get; private set; }

        public bool IsSelected => ArmorSet?.IsEquipped ?? false;

        private HeroArmorSet ArmorSet
        {
            get
            {
                if (HeroItems == null)
                {
                    Plugin.Log?.LogError($"{nameof(VCQuickArmorSet)}.{nameof(ArmorSet)} | {nameof(HeroItems)} is null.");
                    return null;
                }
                return HeroItems.ArmorSetAt(Index);
            }
        }

        private string ActionName => $"Armor Set {Index + 1}";

        private string ActionDescription
        {
            get
            {
                List<string> names = GetItems().Select(x => x.DisplayName).ToList();
                return names.Any() ? string.Join("\r\n", names) : string.Empty;
            }
        }

        public void Initialize(VCQuickUseAction other, ItemSlotUI otherSlot, VCQuickUseActionMouseHelper helper, int index)
        {
            // Not copying previewObject from VCQuickUseAction
            slot = otherSlot; // VCQuickItemBase
            keyBinding = other.keyBinding; // VCQuickUseAction
            chosenIndicator = other.chosenIndicator; // VCQuickUseOption
            isQuickAction = other.isQuickAction; // VCRadialMenuOption<QuickUseWheelUI>
            locID = other.locID; // ViewComponent

            Index = index;
            name = $"{nameof(VCQuickArmorSet)}[{Index}]";

            helper.quickAction = this;
        }

        public override Item RetrieveItem() => GetItems().FirstOrDefault();

        private IEnumerable<Item> GetItems() => EquipmentSlotTypes.Select(x => ArmorSet?.GetItem(x)).Where(x => x != null);

        public override void OnShow()
        {
            SearchForException();
            VQuickUseWheel.Description.ShowCustomAction(ActionName, ActionDescription);
        }

        public override void OnHide()
        {
            SearchForException();
            VQuickUseWheel.Description.HideCustomAction(ActionName);
        }

        public override void UseItemAction()
        {
            Plugin.Log?.LogDebug($"{nameof(VCQuickArmorSet)}[{Index}].{nameof(UseItemAction)} | Called");

            if (VQuickUseWheel == null)
                Plugin.Log?.LogError($"{nameof(VCQuickArmorSet)}[{Index}].{nameof(UseItemAction)}  | {nameof(VQuickUseWheel)} is null.");
            SearchForException();
            HeroItems.ActivateArmorSet(Index);
            RadialMenu.Close();
        }

        private void SearchForException()
        {
            try { HeroItems heroItems = HeroItems; }
            catch
            {
                Plugin.Log?.LogError($"{nameof(VCQuickArmorSet)}[{Index}].{nameof(SearchForException)} | Exception on {nameof(HeroItems)}.");
                throw;
            }

            try { HeroItems.ArmorSetAt(Index); }
            catch
            {
                Plugin.Log?.LogError($"{nameof(VCQuickArmorSet)}[{Index}].{nameof(SearchForException)} " +
                    $"Exception on {nameof(HeroItems)}.{nameof(HeroItems.ArmorSetAt)}[{Index}].");
                throw;
            }

            try { HeroArmorSet armorSet = ArmorSet; }
            catch
            {
                Plugin.Log?.LogError($"{nameof(VCQuickArmorSet)}[{Index}].{nameof(SearchForException)} | Exception on {nameof(ArmorSet)}.");
                throw;
            }
        }
    }
}
