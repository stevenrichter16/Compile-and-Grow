using UnityEngine;
using UnityEngine.UI;

namespace CodeEditor.View
{
    public sealed class DefinitionFlashRenderer : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rt;
        private float _timer;
        private bool _flashing;

        private static readonly Color FlashColor = new Color(1f, 0.85f, 0.3f, 0.35f);
        private const float HoldDuration = 0.25f;
        private const float FadeDuration = 0.5f;
        private const float TotalDuration = HoldDuration + FadeDuration;

        public void Initialize()
        {
            _image = GetComponent<Image>();
            _rt = GetComponent<RectTransform>();
            _image.raycastTarget = false;
            _image.enabled = false;
        }

        public void Flash(int lineIndex, float lineHeight)
        {
            _rt.anchoredPosition = new Vector2(0, -lineIndex * lineHeight);
            _rt.sizeDelta = new Vector2(0, lineHeight);
            _image.color = FlashColor;
            _image.enabled = true;
            _timer = 0f;
            _flashing = true;
        }

        private void Update()
        {
            if (!_flashing) return;

            _timer += Time.unscaledDeltaTime;

            if (_timer >= TotalDuration)
            {
                _image.enabled = false;
                _flashing = false;
                return;
            }

            if (_timer > HoldDuration)
            {
                float fadeProgress = (_timer - HoldDuration) / FadeDuration;
                float alpha = Mathf.Lerp(FlashColor.a, 0f, fadeProgress);
                _image.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, alpha);
            }
        }
    }
}
