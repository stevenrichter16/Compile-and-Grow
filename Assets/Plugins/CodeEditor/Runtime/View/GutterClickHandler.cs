using UnityEngine;
using UnityEngine.EventSystems;
using CodeEditor.Core;
using CodeEditor.Editor;

namespace CodeEditor.View
{
    public sealed class GutterClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private EditorController _controller;
        private CodeEditorView _editorView;
        private float _lineHeight;
        private RectTransform _gutterRt;

        public void Initialize(
            EditorController controller,
            CodeEditorView editorView,
            float lineHeight,
            RectTransform gutterRt)
        {
            _controller = controller;
            _editorView = editorView;
            _lineHeight = lineHeight;
            _gutterRt = gutterRt;
        }

        public void UpdateLineHeight(float lineHeight)
        {
            _lineHeight = Mathf.Max(1f, lineHeight);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gutterRt, eventData.position, eventData.pressEventCamera, out var localPos);

            int line = Mathf.FloorToInt(-localPos.y / _lineHeight);
            var doc = _controller.Document;
            line = Mathf.Clamp(line, 0, doc.LineCount - 1);

            var lineStart = new TextPosition(line, 0);
            var lineEnd = line < doc.LineCount - 1
                ? new TextPosition(line + 1, 0)
                : new TextPosition(line, doc.GetLineLength(line));

            doc.SetSelection(lineStart, lineEnd);
            _editorView.Focus();
        }
    }
}
