using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerBlock
{
    public sealed class EndMenuReturnButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        public CombatHud TargetHud;

        public void OnPointerClick(PointerEventData eventData)
        {
            Activate();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Activate();
        }

        private void Activate()
        {
            if (TargetHud != null)
            {
                TargetHud.ReturnToMainMenu();
                return;
            }

            var hud = CombatHud.Instance;
            if (hud != null)
            {
                hud.ReturnToMainMenu();
            }
        }
    }
}
