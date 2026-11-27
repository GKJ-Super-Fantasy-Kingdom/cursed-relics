using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CursedRelics
{
    // The native button still decides whether a card can receive input or be claimed.
    internal sealed class RelicCardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        Button button;
        Image face;
        Sprite resting, hover, active;
        bool pointerInside, pointerDown, navigationFocus;

        internal static void SetUp(Button button, Image face, Sprite resting, Sprite hover, Sprite active, bool cursed)
        {
            button.TryGetComponent<RelicCardInteraction>(out var cue);
            if (!cursed)
            {
                if (cue) cue.enabled = false;
                return;
            }

            if (!active || !resting) return;
            if (!cue) cue = button.gameObject.AddComponent<RelicCardInteraction>();
            cue.button = button;
            cue.face = face;
            cue.resting = resting;
            cue.hover = hover;
            cue.active = active;
            cue.enabled = true;
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) pointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) pointerDown = false;
        }

        public void OnSelect(BaseEventData eventData) => navigationFocus = !(eventData is PointerEventData);
        public void OnDeselect(BaseEventData eventData) => navigationFocus = false;

        void LateUpdate()
        {
            if (!button || !face) return;
            bool selected = navigationFocus && EventSystem.current &&
                EventSystem.current.currentSelectedGameObject == gameObject;
            if (button.isActiveAndEnabled && button.IsInteractable() && (pointerInside || pointerDown || selected))
            {
                if (face.sprite != active) face.sprite = active;
            }
            else
            {
                RestoreFace();
            }
        }

        void OnDisable()
        {
            pointerInside = pointerDown = navigationFocus = false;
            RestoreFace();
        }

        void RestoreFace()
        {
            if (face && (face.sprite == active || face.sprite == hover)) face.sprite = resting;
        }
    }
}
