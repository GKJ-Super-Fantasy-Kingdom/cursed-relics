using HarmonyLib;
using SuperFantasyKingdom;

namespace CursedRelics
{
    [HarmonyPatch]
    internal static class CombatRelics
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Health), nameof(Health.ReceiveDamage))]
        static void Covenant(Health __instance, ref Damage __0)
        {
            if (__0.isStatus || __0.damageAmount <= 0 || !(__0.origin is UnitBase) ||
                !(__instance.GetEntity() is MonsterBase) || !Catalog.Held(Catalog.Vulture)) return;
            float life = __instance.GetHealthPercentageAbs();
            if (life < .25f) __0.damageAmount *= 1.35f;
            else if (life > .75f) __0.damageAmount *= .85f;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Health), nameof(Health.ReceiveHeal))]
        static void Mercy(Health __instance, ref Heal __0)
        {
            if (Night.Mercy && __instance.GetEntity() is UnitBase && __0.healAmount > 0 &&
                (__0.healType == HealType.Health || __0.healType == HealType.OverHeal))
                __0.healAmount *= 1.3f;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UnitHealth), "GetShieldRegenAmount")]
        static void EmptyShield(UnitHealth __instance, ref float __result)
        {
            if (Night.Hollow && __instance.GetShieldAmountAbs() <= 0) __result = 0;
        }
    }
}
