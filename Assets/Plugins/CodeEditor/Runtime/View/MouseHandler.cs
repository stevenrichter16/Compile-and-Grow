using UnityEngine;
using UnityEngine.EventSystems;
using CodeEditor.Core;
using CodeEditor.Editor;

namespace CodeEditor.View
{
    public sealed class MouseHandler : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private EditorController _controller;
        private CodeEditorView _editorView;
        private LinePool _linePool;
        private ScrollManager _scrollManager;
        private RectTransform _textAreaRt;
        private RectTransform _viewportRt;

        // Click detection
        private float _lastClickTime;
        private Vector2 _lastClickLocalPos;
        private int _clickCount;

        // Drag state
        private bool _isDragging;
        private TextPosition _dragAnchor;
        private TextRange _dragAnchorWordRange;
        private int _dragClickCount;
        private Vector2 _lastPointerScreenPos;
        private Camera _lastEventCamera;

        // Constants
        private const float DoubleClickThreshold = 0.3f;
        private const float ClickPositionTolerance = 5f;
        private const float AutoScrollBaseSpeed = 3f;
        private const float AutoScrollMaxSpeed = 20f;

        public void Initialize(
            EditorController controller,
            CodeEditorView editorView,
            LinePool linePool,
            ScrollManager scrollManager,
            RectTransform textAreaRt,
            RectTransform viewportRt)
        {
            _controller = controller;
            _editorView = editorView;
            _linePool = linePool;
            _scrollManager = scrollManager;
            _textAreaRt = textAreaRt;
            _viewportRt = viewportRt;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _textAreaRt, eventData.position, eventData.pressEventCamera, out var localPos);

            var pos = _linePool.GetTextPosition(localPos, _controller.Document);
            _clickCount = DetectClickCount(localPos);

            _dragClickCount = _clickCount;
            _lastPointerScreenPos = eventData.position;
            _lastEventCamera = eventData.pressEventCamera;

            switch (_clickCount)
            {
                case 1:
                    _controller.Document.SetCursor(pos);
                    _dragAnchor = pos;
                    break;
                case 2:
                    var wordRange = _controller.GetWordBoundsAt(pos);
                    _controller.Document.SetSelection(wordRange.Start, wordRange.End);
                    _dragAnchor = wordRange.Start;
                    _dragAnchorWordRange = wordRange;
                    break;
                case 3:
                    var lineRange = GetLineBounds(pos.Line);
                    _controller.Document.SetSelection(lineRange.Start, lineRange.End);
                    _dragAnchor = lineRange.Start;
                    break;
            }

            _isDragging = true;
            _editorView.Focus();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.button != PointerEventData.InputButton.Left) return;

            _lastPointerScreenPos = eventData.position;
            _lastEventCamera = eventData.pressEventCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _textAreaRt, eventData.position, eventData.pressEventCamera, out var localPos);

            var pos = _linePool.GetTextPosition(localPos, _controller.Document);
            HandleDrag(pos);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _isDragging = false;
        }

        private void Update()
        {
            if (!_isDragging || _viewportRt == null) return;

            var corners = new Vector3[4];
            _viewportRt.GetWorldCorners(corners);
            float viewportBottom = corners[0].y;
            float viewportTop = corners[1].y;

            float scrollDelta = 0f;

            if (_lastPointerScreenPos.y > viewportTop)
            {
                float overshoot = _lastPointerScreenPos.y - viewportTop;
                scrollDelta = -Mathf.Lerp(AutoScrollBaseSpeed, AutoScrollMaxSpeed,
                    Mathf.Clamp01(overshoot / 100f));
            }
            else if (_lastPointerScreenPos.y < viewportBottom)
            {
                float overshoot = viewportBottom - _lastPointerScreenPos.y;
                scrollDelta = Mathf.Lerp(AutoScrollBaseSpeed, AutoScrollMaxSpeed,
                    Mathf.Clamp01(overshoot / 100f));
            }

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _scrollManager.ScrollByLines(scrollDelta * Time.unscaledDeltaTime);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _textAreaRt, _lastPointerScreenPos, _lastEventCamera, out var localPos);
                var pos = _linePool.GetTextPosition(localPos, _controller.Document);
                HandleDrag(pos);
            }
        }

        private void HandleDrag(TextPosition pos)
        {
            var doc = _controller.Document;

            switch (_dragClickCount)
            {
                case 1:
                    doc.SetSelection(_dragAnchor, pos);
                    break;
                case 2:
                    // Extend by word bounds
                    var currentWord = _controller.GetWordBoundsAt(pos);
                    if (pos < _dragAnchorWordRange.Start)
                        doc.SetSelection(_dragAnchorWordRange.End, currentWord.Start);
                    else
                        doc.SetSelection(_dragAnchorWordRange.Start, currentWord.End);
                    break;
                case 3:
                    // Extend by lines
                    var anchorLine = _dragAnchor.Line;
                    var currentLine = pos.Line;
                    if (currentLine < anchorLine)
                    {
                        var end = GetLineBounds(anchorLine).End;
                        doc.SetSelection(end, new TextPosition(currentLine, 0));
                    }
                    else
                    {
                        var lineEnd = GetLineBounds(currentLine).End;
                        doc.SetSelection(new TextPosition(anchorLine, 0), lineEnd);
                    }
                    break;
            }
        }

        private int DetectClickCount(Vector2 localPos)
        {
            float now = Time.unscaledTime;
            if (now - _lastClickTime < DoubleClickThreshold
                && Vector2.Distance(localPos, _lastClickLocalPos) < ClickPositionTolerance)
            {
                _clickCount = Mathf.Min(_clickCount + 1, 3);
            }
            else
            {
                _clickCount = 1;
            }

            _lastClickTime = now;
            _lastClickLocalPos = localPos;
            return _clickCount;
        }

        private TextRange GetLineBounds(int line)
        {
            var doc = _controller.Document;
            var lineStart = new TextPosition(line, 0);
            var lineEnd = line < doc.LineCount - 1
                ? new TextPosition(line + 1, 0)
                : new TextPosition(line, doc.GetLineLength(line));
            return new TextRange(lineStart, lineEnd);
        }
    }
}
