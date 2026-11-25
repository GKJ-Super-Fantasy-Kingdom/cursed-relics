using System;
using System.IO;
using System.Text.RegularExpressions;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CursedRelics
{
    [HarmonyPatch(typeof(UICardRelic), nameof(UICardRelic.Init))]
    internal static class CardFrames
    {
        const string TrimName = "Cursed Relic Trim";
        const string SigilName = "Cursed Relic Sigil";
        const string FieldName = "Cursed Relic Icon Field";
        const string BannerName = "Cursed Relic Banner";
        static readonly Regex SignedPercent = new Regex(@"[+-][0-9]+%");
        static Sprite trim, sigil, iconField, ribbon;

        [HarmonyPostfix]
        static void SetFrame(Relic __0, Image ___card, TextMeshProUGUI ___title, TextMeshProUGUI ___description)
        {
            if (!___card) return;

            var surface = ___card.rectTransform;
            var field = surface.Find("BG") as RectTransform;
            var banner = ___title ? ___title.transform.parent as RectTransform : null;
            bool cursed = RewardIntegration.IsCursed(__0.GetIdentifier());
            if (cursed)
            {
                // The original face still supplies the round rim and all three interaction states.
                SetLayer(surface, TrimName, trim ??= ReadSprite("frame.relic.png"), Vector2.zero, Vector2.one)
                    .transform.SetAsFirstSibling();
                if (field)
                    SetLayer(field, FieldName, iconField ??= ReadSprite("frame.relic-icon-field.png"), Vector2.zero, Vector2.one);
                SetLayer(surface, SigilName, sigil ??= ReadSprite("frame.relic-sigil.png"),
                    new Vector2(.81f, .76f), new Vector2(.90f, .85f));
            }
            else
            {
                HideLayer(surface, TrimName);
                HideLayer(surface, SigilName);
                HideLayer(field, FieldName);
            }
            SetRibbon(banner, cursed);
            SetDescriptionPadding(surface, ___description, cursed);
        }

        static void SetDescriptionPadding(RectTransform face, TextMeshProUGUI description, bool cursed)
        {
            if (!description) return;
            description.TryGetComponent<RelicDescriptionPadding>(out var padding);
            if (cursed)
            {
                if (!padding) padding = description.gameObject.AddComponent<RelicDescriptionPadding>();
                padding.Apply(face, description);
                description.text = SignedPercent.Replace(description.text, "<nobr>$0</nobr>");
            }
            else if (padding)
            {
                padding.Restore(description);
            }
        }

        static void SetRibbon(RectTransform banner, bool cursed)
        {
            if (!banner || !banner.TryGetComponent<Image>(out var backing)) return;
            var layer = banner.Find(BannerName);
            if (!cursed)
            {
                if (!layer) return;
                layer.GetComponent<RelicBannerTint>().Restore(backing);
                layer.gameObject.SetActive(false);
                return;
            }

            // The art fills the visible ribbon inside the native folded ends. The title stays above it.
            var art = SetLayer(banner, BannerName, ribbon ??= ReadSprite("frame.relic-banner.png"),
                new Vector2(.025f, .355f), new Vector2(.975f, .786f));
            art.transform.SetAsFirstSibling();
            var tint = layer ? layer.GetComponent<RelicBannerTint>() : art.gameObject.AddComponent<RelicBannerTint>();
            tint.Apply(backing);
        }

        static void HideLayer(RectTransform parent, string name)
        {
            if (!parent) return;
            var layer = parent.Find(name);
            if (layer) layer.gameObject.SetActive(false);
        }

        static Image SetLayer(RectTransform parent, string name, Sprite sprite, Vector2 from, Vector2 to)
        {
            var existing = parent.Find(name);
            if (existing)
            {
                existing.gameObject.SetActive(true);
                return existing.GetComponent<Image>();
            }

            var layer = new GameObject(name, typeof(RectTransform), typeof(Image));
            var bounds = (RectTransform)layer.transform;
            bounds.SetParent(parent, false);
            bounds.anchorMin = from;
            bounds.anchorMax = to;
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            var image = layer.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        static Sprite ReadSprite(string resource)
        {
            using var stream = typeof(CardFrames).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("Missing embedded card art " + resource);
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            var texture = new Texture2D(2, 2) { name = resource, filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            if (!texture.LoadImage(bytes.ToArray())) throw new InvalidDataException(resource);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
            sprite.name = resource;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    internal sealed class RelicDescriptionPadding : MonoBehaviour
    {
        Vector4 original;
        bool applied;

        internal void Apply(RectTransform face, TextMeshProUGUI description)
        {
            if (applied) return;
            original = description.margin;
            // TextMeshPro margins are in the description's scaled local space.
            var units = description.rectTransform.InverseTransformVector(face.TransformVector(new Vector3(1f, 1f, 0f)));
            // The native description is two units right of the card center.
            description.margin = original + new Vector4(8f * units.x, 2f * units.y, 12f * units.x, 6f * units.y);
            applied = true;
        }

        internal void Restore(TextMeshProUGUI description)
        {
            if (!applied) return;
            description.margin = original;
            applied = false;
        }
    }

    internal sealed class RelicBannerTint : MonoBehaviour
    {
        Color original;
        bool applied;

        internal void Apply(Image backing)
        {
            if (applied) return;
            original = backing.color;
            backing.color = original * new Color(.67f, .68f, .92f, 1f);
            applied = true;
        }

        internal void Restore(Image backing)
        {
            if (!applied) return;
            backing.color = original;
            applied = false;
        }
    }
}
