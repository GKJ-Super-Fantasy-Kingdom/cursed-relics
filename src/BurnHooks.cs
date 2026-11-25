using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Status;
using UnityEngine;

namespace CursedRelics
{
    [HarmonyPatch]
    internal static class BurnHooks
    {
        sealed class Contribution { internal float Share; }
        static readonly ConditionalWeakTable<BurnStatus, Contribution> Sources = new ConditionalWeakTable<BurnStatus, Contribution>();
        [ThreadStatic] static bool playerRefresh;

        // The native scriptable path writes expiration again after OnEntry, using duration.
        // Extend both representations once. The direct Status overload keeps OnEntry's timer.
        [HarmonyPostfix, HarmonyPatch(typeof(StatusManager), "AddStatusInternally")]
        static void Applied(Status status, Entity origin)
        {
            if (!(status is BurnStatus burn) || !(origin is UnitBase) || !Catalog.Held(Catalog.Gospel)) return;
            Sources.GetOrCreateValue(burn).Share = 1f;
            if (burn.duration > 0) burn.duration *= 2f;
            Extend(burn);
        }

        static void Extend(BurnStatus burn)
        {
            float remaining = burn.expiration - Time.time;
            if (remaining > 0) burn.expiration += remaining;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StatusManager), "CanAddStatus")]
        static void Source(Entity origin, out bool __state)
        {
            __state = playerRefresh;
            playerRefresh = origin is UnitBase && Catalog.Held(Catalog.Gospel);
        }

        [HarmonyFinalizer, HarmonyPatch(typeof(StatusManager), "CanAddStatus")]
        static void Restore(bool __state) => playerRefresh = __state;

        [HarmonyPrefix, HarmonyPatch(typeof(BurnStatus), "Stack")]
        static void BeforeStack(BurnStatus __instance, out float __state) => __state = __instance.amount;

        [HarmonyPostfix, HarmonyPatch(typeof(BurnStatus), "Stack")]
        static void AfterStack(BurnStatus __instance, float __state, StatusScriptableObject newStatus, float modifier, Stats stats)
        {
            bool tracked = Sources.TryGetValue(__instance, out var part);
            if (!playerRefresh && !tracked) return;
            if (playerRefresh && __instance.duration > 0 && newStatus.GetDuration() > 0) Extend(__instance);
            part ??= Sources.GetOrCreateValue(__instance);
            float incoming = newStatus.GetAmount() * modifier;
            float newShare = playerRefresh ? 1f : 0f;
            if (stats is MonsterStats && CombatManager.Instance.CanBurnStack() && __instance.amount > 0)
                part.Share = Mathf.Clamp01((__state * part.Share + incoming * newShare) / __instance.amount);
            else if (incoming > __state) part.Share = newShare;
        }

        // Scale the final tick after native extra damage, endless Burn and tick multipliers.
        [HarmonyTranspiler, HarmonyPatch(typeof(BurnStatus), "OnTick", typeof(Entity), typeof(Stats))]
        static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var field = AccessTools.Field(typeof(Damage), "damageAmount");
            int index = code.FindIndex(i => i.StoresField(field));
            if (index < 0 || code.Count(i => i.StoresField(field)) != 1) throw new InvalidOperationException("Unknown Burn tick layout.");
            code.InsertRange(index, new[] { new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code[index]),
                CodeInstruction.Call(typeof(BurnHooks), nameof(TickDamage)) });
            return code;
        }

        static float TickDamage(float damage, BurnStatus burn) =>
            Sources.TryGetValue(burn, out var part) ? damage * (1f - .30f * part.Share) : damage;
    }
}
