using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Buildings;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CursedRelics
{
    [HarmonyPatch]
    internal static class Catalog
    {
        internal const string Crown = "gkj_CR_PaupersCrown";
        internal const string Bell = "gkj_CR_LastWatch";
        internal const string Gospel = "gkj_CR_CinderGospel";
        internal const string Hourglass = "gkj_CR_HourglassOfAsh";
        internal const string Glass = "gkj_CR_GlassCrown";
        internal const string Hollow = "gkj_CR_HollowBulwark";
        internal const string Ledger = "gkj_CR_FerrymansLedger";
        internal const string Vulture = "gkj_CR_VulturesCovenant";
        internal const string Rampart = "gkj_CR_GildedRampart";
        internal const string Mercy = "gkj_CR_RedMercy";

        static readonly (string Id, string Title, string Gift, string Curse)[] Entries = {
            (Crown, "Pauper's Crown", "At dusk, 5 coins or fewer grant +25% Power.", "With 6+, lose 10% Power that night."),
            (Bell, "Bell of the Last Watch", "First wall hit grants +40% Power for 12s.", "Start each night with 20% less wall health capacity."),
            (Gospel, "Cinder Gospel", "Your Burns last twice as long.", "Each tick deals 30% less damage."),
            (Hourglass, "Hourglass of Ash", "+35% Power from dusk to midnight.", "-20% Power from midnight to the end of battle."),
            (Glass, "Glass Crown", "+30% Power at night.", "Units have 25% less max HP that night."),
            (Hollow, "Hollow Bulwark", "+50% Shield capacity at night.", "Empty Shields cannot regenerate on their own that night."),
            (Ledger, "Ferryman's Ledger", "The first ally to fall each night earns 3 coins.", "Then allies lose 20% Power that night."),
            (Vulture, "Vulture's Covenant", "Hits deal +35% damage below 25% enemy HP.", "Hits deal -15% above 75% enemy HP."),
            (Rampart, "Gilded Rampart", "+25% wall health capacity at night.", "Units lose 15% Power that night."),
            (Mercy, "Red Mercy", "Units receive 30% more HP healing at night.", "Units lose 20% Power that night.")
        };
        internal static readonly string[] Ids = Entries.Select(entry => entry.Id).ToArray();
        static readonly Dictionary<string, Relic> Prototypes = new Dictionary<string, Relic>();
        static AddressablesManager registered;

        [HarmonyPostfix, HarmonyPatch(typeof(AddressablesManager), "LoadCatalog")]
        static IEnumerator Loaded(IEnumerator __result, AddressablesManager __instance)
        {
            while (__result.MoveNext()) yield return __result.Current;
            if (__instance.IsLoaded()) Register(__instance);
        }

        static void Register(AddressablesManager __instance)
        {
            if (registered == __instance) return;
            registered = __instance;
            Prototypes.Clear();
            var donor = __instance.GetRelic("GreenHouse") ?? __instance.GetRelic("BetterBerries");
            if (!donor) throw new InvalidOperationException("Missing base relic prefab.");
            foreach (var id in Ids)
            {
                if (__instance.GetAllRelics().ContainsKey(id)) continue;
                var clone = Object.Instantiate(donor);
                clone.gameObject.SetActive(false);
                Object.DontDestroyOnLoad(clone.gameObject);
                clone.name = id;
                var data = Traverse.Create(clone);
                data.Field("identifier").SetValue(id);
                data.Field("kingdom").SetValue(Races.None);
                data.Field("relatedUnit").SetValue("");
                data.Field("requiredBuilding").SetValue(BuildingType.None);
                data.Field("requiredKingdomLevel").SetValue(0);
                data.Field("upgrade").SetValue(null);
                data.Field("rarity").SetValue(Rarity.Epic);
                foreach (string field in new[] { "law", "generic", "racial", "soldAtCartographer1", "soldAtCartographer2",
                    "soldAtCartographer3", "soldAtCartographer4", "triggerOnLoad", "triggerLater" })
                    data.Field(field).SetValue(false);
                data.Field("keywords").SetValue(Array.Empty<EffectKeywordType>());
                data.Field("attack").SetValue(new Attack());
                data.Field("cooldown").SetValue(1E9f);
                using var stream = typeof(Catalog).Assembly.GetManifestResourceStream(id + ".png");
                using var bytes = new MemoryStream();
                stream.CopyTo(bytes);
                var texture = new Texture2D(2, 2) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                texture.LoadImage(bytes.ToArray());
                var icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f),
                    donor.GetIcon().pixelsPerUnit * texture.width / donor.GetIcon().rect.width);
                icon.hideFlags = HideFlags.HideAndDontSave;
                data.Field("icon").SetValue(icon);
                var image = clone.GetComponent<Image>();
                if (image != null) image.sprite = icon;
                Prototypes.Add(id, clone);
            }
            Plugin.Log.LogInfo("Registered " + Prototypes.Count + " cursed relics.");
        }

        // Native lookup and owned inventory resolve the IDs, while unrelated random-pool
        // enumerators (including paid rewards and shops) keep their original candidates.
        [HarmonyPostfix, HarmonyPatch(typeof(AddressablesManager), "GetRelic")]
        static void Resolve(string prefabName, ref Relic __result)
        {
            if (!__result && prefabName != null && Prototypes.TryGetValue(prefabName, out var relic)) __result = relic;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RelicManager), "IsUnlocked")]
        static void Unlock(string relic, ref bool __result) => __result |= relic != null && Prototypes.ContainsKey(relic);

        [HarmonyPostfix, HarmonyPatch(typeof(RelicManager), "SpawnRelic", typeof(Relic))]
        static void ShowInstance(Relic __result)
        {
            if (__result && Prototypes.ContainsKey(__result.name)) __result.gameObject.SetActive(true);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Item), "GetTitle")]
        static bool Title(Item __instance, ref string __result) => Text(__instance, true, ref __result);

        [HarmonyPrefix, HarmonyPatch(typeof(Item), "GetDescription")]
        static bool Description(Item __instance, ref string __result) => Text(__instance, false, ref __result);

        static bool Text(Item item, bool title, ref string result)
        {
            var index = Array.IndexOf(Ids, item.GetIdentifier());
            if (index < 0 || !Prototypes.ContainsKey(Ids[index])) return true;
            var entry = Entries[index];
            result = title ? entry.Title : $"Gift: {entry.Gift}\n<color=#991F29>Curse: {entry.Curse}</color>";
            return false;
        }

        internal static bool Available(string id) => Prototypes.ContainsKey(id) && RelicManager.Instance.IsUnlocked(id)
            && RelicManager.Instance.CanAddRelic(id) &&
            ((id != Bell && id != Rampart) || (WallManager.Instance && WallManager.Instance.HasWall()));
        internal static bool Held(string id) => RelicManager.Instance && RelicManager.Instance.HasRelic(id);
    }
}
