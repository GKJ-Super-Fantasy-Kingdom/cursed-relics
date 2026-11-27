using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using SuperFantasyKingdom;
using UnityEngine;

namespace CursedRelics
{
    [BepInPlugin("gkj.cursedrelics", "Cursed Relics", "0.2.14")]
    [BepInDependency("sfk.weeklycontracts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("gkj.holdtheline", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<int> Chance;
        internal static ConfigEntry<bool> Paid;
        internal static BepInEx.Logging.ManualLogSource Log;
        Harmony patches;

        void Awake()
        {
            Log = Logger;
            Chance = Config.Bind("Rewards", "CursedChancePercent", 35,
                new ConfigDescription("Chance for one cursed option in an eligible three-relic reward.",
                    new AcceptableValueRange<int>(0, 100)));
            Paid = Config.Bind("Rewards", "AllowPaidWager", false,
                "Allow a cooperating Hold the Line relic wager to offer one cursed choice.");
            patches = new Harmony("gkj.cursedrelics");
            try { patches.PatchAll(typeof(Plugin).Assembly); }
            catch { patches.UnpatchSelf(); throw; }
            if (Chainloader.PluginInfos.TryGetValue("sfk.weeklycontracts", out var weekly))
            {
                var kingdom = weekly.Instance.GetType().Assembly.GetType("WeeklyContracts.Kingdom");
                var choices = kingdom == null ? null : AccessTools.Method(kingdom, "Choices", Type.EmptyTypes);
                if (choices?.ReturnType == typeof(string[]))
                    patches.Patch(choices, postfix: new HarmonyMethod(typeof(Rewards), nameof(Rewards.Weekly)));
            }
            if (Chainloader.PluginInfos.TryGetValue("gkj.holdtheline", out var hold))
            {
                var rewards = hold.Instance.GetType().Assembly.GetType("HoldTheLine.RelicRewards");
                var draw = rewards == null ? null : AccessTools.Method(rewards, "Draw");
                if (draw?.ReturnType == typeof(string[]) && draw.GetParameters().Length == 1)
                    patches.Patch(draw, postfix: new HarmonyMethod(typeof(Rewards), nameof(Rewards.Wager)));
            }
            GameManager.OnGameInit += Night.End;
            GameManager.OnGameInit += Rewards.ResetOpenings;
            DaytimeManager.OnSunsetStart += Night.Dusk;
            DaytimeManager.BeforeMorningStart += Morning;
            GameManager.BeforeStateChange += StateChange;
            UnitManager.OnUnitSpawn += Night.ApplyPower;
            UnitManager.OnUnitDeath += Night.UnitDied;
        }
        static void Morning(int _) => Night.End();
        static void StateChange(GameState state)
        {
            if (state == GameState.Survived || state == GameState.Win || state == GameState.Lose || state == GameState.GiveUp) Night.End();
        }
        void Update()
        {
            Night.Update();
            Rewards.KeyboardFocus();
        }
        void OnDestroy()
        {
            GameManager.OnGameInit -= Night.End;
            GameManager.OnGameInit -= Rewards.ResetOpenings;
            DaytimeManager.OnSunsetStart -= Night.Dusk;
            DaytimeManager.BeforeMorningStart -= Morning;
            GameManager.BeforeStateChange -= StateChange;
            UnitManager.OnUnitSpawn -= Night.ApplyPower;
            UnitManager.OnUnitDeath -= Night.UnitDied;
            Night.End();
            patches?.UnpatchSelf();
        }
    }
}
