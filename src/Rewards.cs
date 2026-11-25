using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Clickable;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CursedRelics
{
    public static class RewardIntegration
    {
        public static bool IsCursed(string id) => Array.IndexOf(Catalog.Ids, id) >= 0;

        // Paid providers call once when committing the three choices, then save the returned IDs.
        public static string[] PaidWagerChoices(string[] ordinary, string rewardKey) =>
            Plugin.Paid.Value ? Rewards.Mix(ordinary, rewardKey) : ordinary;
    }

    [HarmonyPatch]
    internal static class Rewards
    {
        static readonly AccessTools.FieldRef<UIOverlayMerchant,Func<Relic>> Generator =
            AccessTools.FieldRefAccess<UIOverlayMerchant,Func<Relic>>("m_GetRelicCallback");
        static readonly AccessTools.FieldRef<DropBossChest,List<string>> Pool =
            AccessTools.FieldRefAccess<DropBossChest,List<string>>("RelicPool");
        sealed class Visit { internal string Key; internal int Roll; }
        static readonly ConditionalWeakTable<DropBossChest,Visit> Visits = new ConditionalWeakTable<DropBossChest,Visit>();
        static readonly Dictionary<string,int> Openings = new Dictionary<string,int>();
        internal static void ResetOpenings() => Openings.Clear();

        static readonly AccessTools.FieldRef<UICardRelic,string> CardId =
            AccessTools.FieldRefAccess<UICardRelic,string>("m_RelicIdentifier");

        // The native overlay seeds focus only for gamepads. Tab enters the same
        // selectable card row in keyboard mode, after which native Enter submits.
        internal static void KeyboardFocus()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;
            var overlays = OverlayManager.Instance;
            var events = EventSystem.current;
            if (!events || !overlays || !overlays.IsOpen() || !GamepadManager.Instance ||
                GamepadManager.Instance.IsGamepadInUse()) return;
            var merchant = overlays.GetMerchant();
            if (!merchant || !merchant.isActiveAndEnabled) return;
            var cards = merchant.GetComponentsInChildren<UICardRelic>();
            if (cards.Length != 3 || !cards.Any(card => RewardIntegration.IsCursed(CardId(card)))) return;
            var buttons = cards.Select(card => card.GetCardButton()).Where(button =>
                button && button.isActiveAndEnabled && button.IsInteractable()).ToArray();
            if (buttons.Length == 0) return;
            int current = Array.FindIndex(buttons, button => button.gameObject == events.currentSelectedGameObject);
            bool back = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            int next = current < 0 ? (back ? buttons.Length - 1 : 0) :
                (current + (back ? -1 : 1) + buttons.Length) % buttons.Length;
            events.SetSelectedGameObject(buttons[next].gameObject);
        }

        static uint Hash(string key)
        {
            uint hash = 2166136261;
            foreach (char c in key) hash = unchecked((hash ^ c) * 16777619);
            return hash;
        }

        internal static string[] Mix(string[] ordinary, string key, HashSet<string> reserved = null)
        {
            if (ordinary == null || ordinary.Length != 3 || ordinary.Any(string.IsNullOrEmpty) ||
                ordinary.Any(RewardIntegration.IsCursed) || ordinary.Distinct().Count() != 3) return ordinary;
            if (Hash(key + "/chance") % 100 >= Plugin.Chance.Value) return ordinary;
            var options = Catalog.Ids.Where(id => Catalog.Available(id) && reserved?.Contains(id) != true).ToArray();
            if (options.Length == 0) return ordinary;
            var mixed = (string[])ordinary.Clone();
            mixed[Hash(key + "/slot") % 3] = options[Hash(key + "/item") % options.Length];
            return mixed;
        }

        internal static void Weekly(ref string[] __result)
        {
            __result = Mix(__result, GameDataManager.Instance.GetRunID() + "/weekly/" + DaytimeManager.Instance.CurrentDay());
        }

        internal static void Wager(object __0, ref string[] __result)
        {
            if (!Plugin.Paid.Value) return;
            var wager = Traverse.Create(__0);
            var pending = wager.Property("Unclaimed").GetValue() as IEnumerable;
            if (pending == null) return;
            var reserved = new HashSet<string>();
            foreach (var prize in pending)
            {
                var choices = Traverse.Create(prize).Property("Choices").GetValue<string[]>();
                if (choices == null) return;
                reserved.UnionWith(choices);
            }
            var key = wager.Property("Run").GetValue<string>() + "/wager/" + wager.Property("Day").GetValue<int>();
            __result = Mix(__result, key, reserved);
        }

        // Run after the native unique/eligible roll and before the existing card rendering loop.
        [HarmonyTranspiler, HarmonyPatch(typeof(UIOverlayMerchant), "GenerateRelics")]
        static IEnumerable<CodeInstruction> Cards(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var enumerator = AccessTools.Method(typeof(List<string>), "GetEnumerator");
            int i = code.FindIndex(c => c.Calls(enumerator));
            if (i < 0 || code.Count(c => c.Calls(enumerator)) != 1) throw new InvalidOperationException("Unknown reward renderer.");
            code.InsertRange(i, new[] { new CodeInstruction(OpCodes.Dup).MoveLabelsFrom(code[i]),
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
                CodeInstruction.Call(typeof(Rewards), nameof(Prepare)) });
            return code;
        }

        static void Prepare(List<string> choices, UIOverlayMerchant picker, bool reroll)
        {
            if (!reroll || choices.Count != 3 || !(Generator(picker)?.Target is DropBossChest chest)) return;
            var kind = chest.GetType();
            if (kind != typeof(DropBossChest) && kind != typeof(DropKingdomChest) && kind != typeof(DropWorkerChest)) return;
            if (!Guid.TryParse(GameDataManager.Instance.GetRunID(), out var run)) return;
            var visit = Visits.GetValue(chest, c => {
                var p = c.transform.position;
                // Bosses can die at a different position when replaying a saved day.
                string key = FormattableString.Invariant($"{run:N}/{DaytimeManager.Instance.CurrentDay()}/{kind.Name}");
                if (kind != typeof(DropBossChest)) key += FormattableString.Invariant($"/{p.x:R}/{p.y:R}");
                Openings.TryGetValue(key, out int prior);
                Openings[key] = prior + 1;
                if (prior > 0) key += "/" + prior.ToString(CultureInfo.InvariantCulture);
                return new Visit { Key = key };
            });
            var path = Path.Combine(Application.persistentDataPath, "CursedRelics", run.ToString("N"),
                Hash(visit.Key).ToString("x8", CultureInfo.InvariantCulture) + ".json");
            var pool = Pool(chest);
            string[] row = null;
            try
            {
                if (visit.Roll == 0 && File.Exists(path))
                {
                    var saved = JsonConvert.DeserializeObject<Offer>(File.ReadAllText(path));
                    if (saved?.Key == visit.Key && saved.Roll > 0 && Valid(saved.Choices, pool))
                    {
                        row = saved.Choices;
                        visit.Roll = saved.Roll;
                    }
                }
                if (row == null)
                {
                    row = Mix(choices.ToArray(), visit.Key + "/" + visit.Roll++);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    File.WriteAllText(temp, JsonConvert.SerializeObject(new Offer { Key = visit.Key, Roll = visit.Roll, Choices = row }));
                    if (File.Exists(path)) File.Replace(temp, path, null);
                    else File.Move(temp, path);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException)
            {
                Plugin.Log.LogWarning("Could not read or write the chest checkpoint: " + error.Message);
            }
            if (row != null) for (int i = 0; i < 3; ++i) choices[i] = row[i];
        }

        sealed class Offer { public string Key; public int Roll; public string[] Choices; }
        static bool Valid(string[] row, List<string> pool) =>
            row?.Length == 3 && row.Distinct().Count() == 3 && row.Count(RewardIntegration.IsCursed) <= 1 &&
            row.All(id => !string.IsNullOrEmpty(id) && (RewardIntegration.IsCursed(id) ? Catalog.Available(id)
                : pool.Contains(id) && RelicManager.Instance.IsUnlocked(id) && RelicManager.Instance.CanAddRelic(id)));
    }
}
