using HarmonyLib;
using SuperFantasyKingdom;
using UnityEngine;

namespace CursedRelics
{
    [HarmonyPatch]
    internal static class Night
    {
        const int Origin = -18430672;
        static readonly AccessTools.FieldRef<WallManager,float> Hp = AccessTools.FieldRefAccess<WallManager,float>("m_Hitpoints");
        static readonly AccessTools.FieldRef<WallManager,float> Max = AccessTools.FieldRefAccess<WallManager,float>("m_MaxHitpoints");
        static int day = -1;
        static float crown, bellUntil;
        static bool rang, bell, rampart, glass, hourglass, afterMidnight, ledger, paidFerryman;
        static WallManager wall;
        static float withheld, wallScale;
        internal static bool Hollow { get; private set; }
        internal static bool Mercy { get; private set; }

        internal static void Dusk(int currentDay)
        {
            if (day == currentDay) return;
            End();
            day = currentDay;
            glass = Catalog.Held(Catalog.Glass);
            hourglass = Catalog.Held(Catalog.Hourglass);
            ledger = Catalog.Held(Catalog.Ledger);
            Hollow = Catalog.Held(Catalog.Hollow);
            Mercy = Catalog.Held(Catalog.Mercy);
            string report = "";
            if (Catalog.Held(Catalog.Crown))
            {
                int coins = ResourceManager.Instance.GetCoins();
                crown = coins <= 5 ? .25f : -.10f;
                string balance = coins == 1 ? "1 coin" : coins + " coins";
                report = balance + " at dusk. Unit Power " + (crown > 0 ? "+25%" : "−10%") + " tonight.";
            }
            var fort = WallManager.Instance;
            if (fort && fort.HasWall() && Max(fort) > 0)
            {
                bell = Catalog.Held(Catalog.Bell);
                rampart = Catalog.Held(Catalog.Rampart);
                if (bell || rampart)
                {
                    wall = fort;
                    wallScale = (bell ? .8f : 1f) * (rampart ? 1.25f : 1f);
                    SetWallCapacity();
                    if (report.Length > 0) report += "\n";
                    report += $"Wall capacity {(Max(wall) + withheld):0.##} → {Max(wall):0.##} tonight.";
                }
            }
            Refresh();
            if (report.Length > 0)
                AlertManager.Instance.DisplayMessage("Cursed Relics", report);
        }

        internal static void Hit(float before, float after)
        {
            if (!wall || !bell || rang || !(before > after)) return;
            rang = true;
            bellUntil = Time.time + 12f;
            Refresh();
        }

        // The native evening timer expires at midnight. This still runs if another
        // mod keeps the clock in evening overtime while reinforcements are alive.
        [HarmonyPrefix, HarmonyPriority(Priority.First), HarmonyPatch(typeof(DaytimeManager), "SwitchState")]
        static void AtMidnight(DaytimeManager __instance)
        {
            if (!hourglass || afterMidnight || !__instance.IsEvening() || __instance.CurrentDay() != day) return;
            afterMidnight = true;
            Refresh();
            AlertManager.Instance.DisplayMessage("Hourglass of Ash", "Midnight. Units lose 20% Power for the rest of battle.");
        }

        internal static void UnitDied(UnitBase unit)
        {
            if (!unit || !ledger || paidFerryman) return;
            paidFerryman = true;
            var purse = ResourceManager.Instance;
            purse.AddToCoins(3);
            purse.ResourceAmountChanged(ResourceType.Coins);
            Refresh();
            AlertManager.Instance.DisplayMessage("Ferryman’s Ledger", "First ally lost. Gain 3 coins and lose 20% Power tonight.");
        }

        internal static void Update()
        {
            if (bellUntil > 0 && Time.time >= bellUntil) { bellUntil = 0; Refresh(); }
        }

        // Native capacity multipliers also scale the portion withheld for this night.
        // Track their observed result so removal at dawn preserves other relics' changes.
        [HarmonyPrefix, HarmonyPatch(typeof(WallManager), "AddToHitpointMultiplier")]
        static void BeforeScale(WallManager __instance, out float __state) =>
            __state = wall == __instance ? Max(__instance) : 0f;

        [HarmonyPostfix, HarmonyPatch(typeof(WallManager), "AddToHitpointMultiplier")]
        static void AfterScale(WallManager __instance, float __state)
        {
            if (__state > 0 && wall == __instance) withheld *= Max(wall) / __state;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WallManager), "SetAdditionalHitpoints")]
        static void ReplacementCapacity(WallManager __instance, int val)
        {
            if (val <= 0 || wall != __instance) return;
            SetWallCapacity();
        }

        static void SetWallCapacity()
        {
            withheld = Max(wall) * (1f - wallScale);
            Max(wall) *= wallScale;
            Hp(wall) *= wallScale;
        }

        internal static void End()
        {
            if (day < 0) return;
            if (wall && Max(wall) > 0)
            {
                float ratio = Hp(wall) / Max(wall);
                Max(wall) += withheld;
                Hp(wall) = Max(wall) * ratio;
            }
            wall = null;
            day = -1;
            crown = bellUntil = withheld = 0;
            rang = bell = rampart = glass = hourglass = afterMidnight = ledger = paidFerryman = false;
            Hollow = Mercy = false;
            Refresh();
        }

        static void Refresh()
        {
            if (!UnitManager.Instance) return;
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                var damage = unit ? unit.GetUnitStats()?.damage : null;
                if (damage == null) continue;
                float previous = damage.Value;
                ApplyPower(unit);
                // The unit card refreshes its displayed Power on the ability's native stats event.
                if (!Mathf.Approximately(previous, damage.Value)) unit.GetCombatAbility()?.GetStats();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UnitBase), "AfterGridChange")]
        internal static void ApplyPower(UnitBase __instance)
        {
            if (!__instance) return;
            var stats = __instance.GetUnitStats();
            if (stats == null) return;
            var damage = stats.damage;
            damage.RemoveAllModifiersFromOrigin(Origin);
            AddPower(damage, crown);
            if (bellUntil > Time.time) AddPower(damage, .4f);
            if (hourglass) AddPower(damage, afterMidnight ? -.20f : .35f);
            if (glass) AddPower(damage, .30f);
            if (rampart) AddPower(damage, -.15f);
            if (Mercy) AddPower(damage, -.20f);
            if (paidFerryman) AddPower(damage, -.20f);

            var health = __instance.GetHealth();
            if (health == null) return;
            float maxHp = stats.maxHealth.Value, maxShield = stats.maxShield.Value;
            float healthRatio = health.GetHealthPercentage() * Mathf.Sign(maxHp);
            stats.maxHealth.RemoveAllModifiersFromOrigin(Origin);
            stats.maxShield.RemoveAllModifiersFromOrigin(Origin);
            if (glass) stats.maxHealth.AddModifier(new StatModifier(-.25f, StatModifierType.PercentMult, Origin));
            if (Hollow) stats.maxShield.AddModifier(new StatModifier(.50f, StatModifierType.PercentMult, Origin));
            if (!Mathf.Approximately(maxHp, stats.maxHealth.Value) && maxHp != 0)
            {
                health.SetHealthPercentage(healthRatio);
                health.SetCurrentHealth();
            }
            if (!Mathf.Approximately(maxShield, stats.maxShield.Value) && maxShield > 0)
            {
                health.SetShieldPercentage(health.GetShieldAmountAbs() / maxShield);
                health.SetCurrentShield();
            }
        }

        static void AddPower(Stat damage, float factor)
        {
            if (factor != 0) damage.AddModifier(new StatModifier(factor, StatModifierType.PercentMult, Origin));
        }
    }
}
