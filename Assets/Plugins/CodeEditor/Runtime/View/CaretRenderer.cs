using UnityEngine;
using UnityEngine.UI;

namespace CodeEditor.View
{
    public sealed class CaretRenderer : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rt;
        private float _blinkRate = 0.53f;
        private float _caretWidth = 2f;
        private float _timer;
        private bool _visible = true;
        private bool _active;

        internal Vector2 CurrentPosition => _rt != null ? _rt.anchoredPosition : Vector2.zero;

        public void Initialize(float blinkRate = 0.53f)
        {
            _blinkRate = blinkRate;
            _image = GetComponent<Image>();
            if (_image == null)
                _image = gameObject.AddComponent<Image>();
            _rt = GetComponent<RectTransform>();
            _image.color = new Color(1f, 1f, 1f, 0.9f);
            _rt.sizeDelta = new Vector2(_caretWidth, 20f);
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (!active)
            {
                _image.enabled = false;
                return;
            }
            ResetBlink();
        }

        public void SetPosition(Vector2 localPos, float lineHeight)
        {
            _rt.anchoredPosition = localPos;
            _rt.sizeDelta = new Vector2(_caretWidth, lineHeight);
            ResetBlink();
        }

        public void SetCaretWidth(float width)
        {
            _caretWidth = Mathf.Max(1f, width);
            if (_rt != null)
                _rt.sizeDelta = new Vector2(_caretWidth, _rt.sizeDelta.y);
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }

        private void Update()
        {
            if (!_active) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer >= _blinkRate)
            {
                _timer -= _blinkRate;
                _visible = !_visible;
                _image.enabled = _visible;
            }
        }

        public void ResetBlink()
        {
            _timer = 0f;
            _visible = true;
            if (_image != null)
                _image.enabled = true;
        }
    }
}
