using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tenyu.Audio
{
    [RequireComponent(typeof(Button))]
    public sealed class TenyuButtonSound : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        [SerializeField] private TenyuSceneAudio sceneAudio;
        private Button button;
        private void Awake() { button = GetComponent<Button>(); }
        private void Play()
        {
            if (button.IsActive() && button.IsInteractable() && sceneAudio != null)
                sceneAudio.PlayButtonSound();
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Play();
        }
        public void OnSubmit(BaseEventData data) { Play(); }
    }
}
