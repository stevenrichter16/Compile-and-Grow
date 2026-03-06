using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using CodeEditor.Core;
using CodeEditor.Completion;
using CodeEditor.Editor;
using CodeEditor.Language;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace CodeEditor.View
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CodeEditorView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_InputField _hiddenInput;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentArea;
        [SerializeField] private RectTransform _gutterArea;
        [SerializeField] private RectTransform _textArea;
        [SerializeField] private TMP_FontAsset _monoFont;
        [SerializeField] private Button _fontSizeUpButton;
        [SerializeField] private Button _fontSizeDownButton;
        [SerializeField] private TMP_Text _fontSizeLabel;

        [Header("Config")]
        [SerializeField] private float _fontSize = 16f;
        [SerializeField] private int _indentSize = 4;
        [SerializeField] private bool _showLineNumbers = true;
        [SerializeField] private Color _backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color _gutterColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _caretColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color _selectionColor = new Color(0.25f, 0.47f, 0.77f, 0.35f);
        [SerializeField] private Color _lineNumberColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private Color _currentLineHighlightColor = new Color(1f, 1f, 1f, 0.05f);

        [Header("Syntax Highlighting")]
        [SerializeField] private HighlightTheme _highlightTheme = new HighlightTheme();

        private DocumentModel _doc;
        private EditorController _controller;
        private LinePool _linePool;
        private LineNumberGutter _gutter;
        private CaretRenderer _caret;
        private SelectionRenderer _selection;
        private ScrollManager _scrollManager;
        private InputHandler _inputHandler;
        private MouseHandler _mouseHandler;
        private GutterClickHandler _gutterClickHandler;
        private Image _currentLineHighlight;
        private DefinitionFlashRenderer _definitionFlash;
        private CompletionPopup _completionPopup;
        private ICompletionProvider _completionProvider;
        private SignatureHintPopup _signatureHintPopup;
        private ISignatureHintProvider _signatureHintProvider;
        private ILanguageService _pendingLanguageService;
        private int _lastSyncedVersion = -1;

        // Syntax highlighting cache
        private System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<HighlightToken>> _lineTokenCache;
        private System.Collections.Generic.List<object> _lineStateCache;
        private int _highlightDirtyFrom = int.MaxValue;

        public event Action<string> TextChanged;
        public event Action CtrlEnterPressed;
        public event Action<string, int> CtrlClickWord;

        public string Text
        {
            get => _controller?.GetText() ?? string.Empty;
            set
            {
                if (_controller != null)
                {
                    _controller.SetText(value);
                    SyncView();
                }
            }
        }

        public float FontSize
        {
            get => _fontSize;
            set => SetFontSize(value);
        }

        public void SetFontSize(float fontSize)
        {
            fontSize = Mathf.Max(1f, fontSize);
            if (Mathf.Approximately(fontSize, _fontSize)) return;
            _fontSize = fontSize;
            ApplyFontSize();
            UpdateFontSizeLabel();
        }

        public void SetLanguageService(ILanguageService service)
        {
            if (_controller != null)
                _controller.SetLanguageService(service);
            else
                _pendingLanguageService = service;
            _lineTokenCache = null;
            _lineStateCache = null;
            _highlightDirtyFrom = 0;
        }

        public void SetCompletionProvider(ICompletionProvider provider)
        {
            _completionProvider = provider;
        }

        public void SetSignatureHintProvider(ISignatureHintProvider provider)
        {
            _signatureHintProvider = provider;
        }

        internal CompletionPopup CompletionPopup => _completionPopup;
        internal SignatureHintPopup SignatureHintPopup => _signatureHintPopup;

        public void Focus()
        {
            if (_hiddenInput != null)
                _hiddenInput.ActivateInputField();
        }

        // Test accessors — internal so test assemblies can inspect view state
        internal EditorController Controller => _controller;
        internal CaretRenderer Caret => _caret;
        internal LinePool Lines => _linePool;
        internal SelectionRenderer Selection => _selection;

        public void ScrollToLine(int line)
        {
            _scrollManager?.EnsureLineVisible(line);
        }

        internal void RaiseCtrlClickWord(string word, int line)
        {
            CtrlClickWord?.Invoke(word, line);
        }

        public void FlashLine(int line)
        {
            if (_definitionFlash == null || _linePool == null) return;
            if (line < 0 || line >= _doc.LineCount) return;
            _scrollManager?.EnsureLineVisible(line);
            _definitionFlash.Flash(line, _linePool.LineHeight);
        }

        private void Awake()
        {
            EnsureUiEventInfrastructure();

            _doc = new DocumentModel();
            var config = new EditorConfig { IndentSize = _indentSize };
            _controller = new EditorController(_doc, config);

            if (_pendingLanguageService != null)
            {
                _controller.SetLanguageService(_pendingLanguageService);
                _pendingLanguageService = null;
            }

            if (_monoFont == null)
                _monoFont = TMP_Settings.defaultFontAsset;

            _prevFontSize = _fontSize;
            InitializeSubComponents();

            if (_fontSizeUpButton != null)
                _fontSizeUpButton.onClick.AddListener(() => { SetFontSize(_fontSize + 2f); UpdateFontSizeLabel(); Focus(); });
            if (_fontSizeDownButton != null)
                _fontSizeDownButton.onClick.AddListener(() => { SetFontSize(_fontSize - 2f); UpdateFontSizeLabel(); Focus(); });
            UpdateFontSizeLabel();
        }

        private void UpdateFontSizeLabel()
        {
            if (_fontSizeLabel != null)
                _fontSizeLabel.text = $"{_fontSize:0}pt";
        }

        private void OnEnable()
        {
            if (_doc != null)
            {
                _doc.Changed += OnDocumentChanged;
                _doc.CursorMoved += OnCursorMoved;
            }
            if (_scrollRect != null)
                _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        private float _prevFontSize;

        private void OnValidate()
        {
            // React to inspector changes at runtime
            if (_linePool != null && !Mathf.Approximately(_fontSize, _prevFontSize))
            {
                _prevFontSize = _fontSize;
                _fontSize = Mathf.Max(1f, _fontSize);
                ApplyFontSize();
            }
        }

        private void OnDisable()
        {
            if (_doc != null)
            {
                _doc.Changed -= OnDocumentChanged;
                _doc.CursorMoved -= OnCursorMoved;
            }
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        private int _hiddenInputRaycastCleanupFrames = 3;

        private void LateUpdate()
        {
            // TMP_InputField creates a Caret child lazily; disable its raycastTarget
            if (_hiddenInputRaycastCleanupFrames > 0 && _hiddenInput != null)
            {
                _hiddenInputRaycastCleanupFrames--;
                foreach (var g in _hiddenInput.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;
            }

            if (_doc == null || _doc.Version == _lastSyncedVersion) return;
            SyncView();
        }

        private void InitializeSubComponents()
        {
            // Initialize LinePool
            if (_textArea != null)
            {
                _linePool = _textArea.gameObject.AddComponent<LinePool>();
                _linePool.Initialize(_textArea, _monoFont, _fontSize);
            }

            // Initialize ScrollManager
            if (_scrollRect != null)
            {
                _scrollManager = _scrollRect.gameObject.AddComponent<ScrollManager>();
                float lineHeight = _linePool != null ? _linePool.LineHeight : _fontSize * 1.2f;
                _scrollManager.Initialize(_scrollRect, lineHeight);
            }

            // Initialize LineNumberGutter
            if (_gutterArea != null && _showLineNumbers)
            {
                _gutter = _gutterArea.gameObject.AddComponent<LineNumberGutter>();
                float lineHeight = _linePool != null ? _linePool.LineHeight : _fontSize * 1.2f;
                _gutter.Initialize(_gutterArea, _monoFont, _fontSize * 0.85f, lineHeight);
                _gutter.SetTextColor(_lineNumberColor);
            }

            // Initialize CaretRenderer
            if (_textArea != null)
            {
                var caretGo = new GameObject("Caret", typeof(RectTransform), typeof(Image));
                caretGo.transform.SetParent(_textArea, false);
                var caretRt = caretGo.GetComponent<RectTransform>();
                caretRt.anchorMin = new Vector2(0, 1);
                caretRt.anchorMax = new Vector2(0, 1);
                caretRt.pivot = new Vector2(0, 1);

                _caret = caretGo.AddComponent<CaretRenderer>();
                _caret.Initialize(_controller.Config.CaretBlinkRate);
                _caret.SetColor(_caretColor);
                _caret.SetActive(true);
            }

            // Initialize SelectionRenderer
            if (_textArea != null)
            {
                var selGo = new GameObject("SelectionOverlay", typeof(RectTransform));
                selGo.transform.SetParent(_textArea, false);
                selGo.transform.SetAsFirstSibling(); // render behind text

                var selRt = selGo.GetComponent<RectTransform>();
                selRt.anchorMin = Vector2.zero;
                selRt.anchorMax = Vector2.one;
                selRt.sizeDelta = Vector2.zero;
                selRt.anchoredPosition = Vector2.zero;

                _selection = selGo.AddComponent<SelectionRenderer>();
                _selection.Initialize(selRt);
                _selection.SetSelectionColor(_selectionColor);
            }

            // Initialize CurrentLineHighlight
            if (_textArea != null)
            {
                var hlGo = new GameObject("CurrentLineHighlight", typeof(RectTransform), typeof(Image));
                hlGo.transform.SetParent(_textArea, false);
                hlGo.transform.SetAsFirstSibling(); // render behind everything

                var hlRt = hlGo.GetComponent<RectTransform>();
                hlRt.anchorMin = new Vector2(0, 1);
                hlRt.anchorMax = new Vector2(1, 1);
                hlRt.pivot = new Vector2(0, 1);

                _currentLineHighlight = hlGo.GetComponent<Image>();
                _currentLineHighlight.color = _currentLineHighlightColor;
                _currentLineHighlight.raycastTarget = false;
            }

            // Initialize DefinitionFlashRenderer
            if (_textArea != null)
            {
                var flashGo = new GameObject("DefinitionFlash", typeof(RectTransform), typeof(Image));
                flashGo.transform.SetParent(_textArea, false);
                flashGo.transform.SetAsFirstSibling();

                var flashRt = flashGo.GetComponent<RectTransform>();
                flashRt.anchorMin = new Vector2(0, 1);
                flashRt.anchorMax = new Vector2(1, 1);
                flashRt.pivot = new Vector2(0, 1);

                _definitionFlash = flashGo.AddComponent<DefinitionFlashRenderer>();
                _definitionFlash.Initialize();
            }

            // Initialize InputHandler
            if (_hiddenInput != null)
            {
                _inputHandler = _hiddenInput.gameObject.AddComponent<InputHandler>();
                _inputHandler.Initialize(_controller, _hiddenInput, this);
                _inputHandler.CtrlEnterPressed += () => CtrlEnterPressed?.Invoke();

                // Disable raycastTarget on ALL graphics under HiddenInput
                // (Image, Text, Placeholder, and the runtime Caret created by TMP_InputField)
                foreach (var g in _hiddenInput.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;

                // Hide the input field's text display
                if (_hiddenInput.textComponent != null)
                    _hiddenInput.textComponent.color = new Color(0, 0, 0, 0);
            }

            // Initialize MouseHandler — attach to ScrollArea (which already has a raycastable Image)
            // and pass _textArea for coordinate conversion
            if (_scrollRect != null && _textArea != null)
            {
                RectTransform viewportRt = _scrollRect.viewport != null
                    ? _scrollRect.viewport
                    : _scrollRect.GetComponent<RectTransform>();

                _mouseHandler = _scrollRect.gameObject.AddComponent<MouseHandler>();
                _mouseHandler.Initialize(_controller, this, _linePool, _scrollManager, _textArea, viewportRt);
            }

            // Initialize GutterClickHandler
            if (_gutterArea != null && _showLineNumbers)
            {
                var gutterImage = _gutterArea.GetComponent<Image>();
                if (gutterImage != null)
                    gutterImage.raycastTarget = true;

                float lh = _linePool != null ? _linePool.LineHeight : _fontSize * 1.2f;
                _gutterClickHandler = _gutterArea.gameObject.AddComponent<GutterClickHandler>();
                _gutterClickHandler.Initialize(_controller, this, lh, _gutterArea);
            }

            // Initialize CompletionPopup
            if (_textArea != null)
            {
                var popupGo = new GameObject("CompletionPopupHost", typeof(RectTransform));
                popupGo.transform.SetParent(_textArea, false);
                var popupRt = popupGo.GetComponent<RectTransform>();
                popupRt.anchorMin = Vector2.zero;
                popupRt.anchorMax = Vector2.one;
                popupRt.sizeDelta = Vector2.zero;
                popupRt.anchoredPosition = Vector2.zero;

                _completionPopup = popupGo.AddComponent<CompletionPopup>();
                _completionPopup.Initialize(_textArea, _monoFont, _fontSize);
                _completionPopup.ItemAccepted += OnCompletionAccepted;

                // Signature hint tooltip (reuse same parent)
                var hintGo = new GameObject("SignatureHintHost", typeof(RectTransform));
                hintGo.transform.SetParent(_textArea, false);
                var hintRt = hintGo.GetComponent<RectTransform>();
                hintRt.anchorMin = Vector2.zero;
                hintRt.anchorMax = Vector2.one;
                hintRt.sizeDelta = Vector2.zero;
                hintRt.anchoredPosition = Vector2.zero;

                _signatureHintPopup = hintGo.AddComponent<SignatureHintPopup>();
                _signatureHintPopup.Initialize(_textArea, _monoFont, _fontSize);
            }
        }

        private void ApplyFontSize()
        {
            // 1. LinePool: recompute metrics, update all pooled TMP objects
            if (_linePool != null)
                _linePool.UpdateFontSize(_fontSize);

            float lineHeight = _linePool != null ? _linePool.LineHeight : _fontSize * 1.2f;

            // 2. ScrollManager: update line height for scroll calculations
            if (_scrollManager != null)
                _scrollManager.UpdateLineHeight(lineHeight);

            // 3. Gutter: update font size and line height
            if (_gutter != null)
                _gutter.UpdateFontSize(_fontSize * 0.85f, lineHeight);

            // 4. GutterClickHandler: update line height
            if (_gutterClickHandler != null)
                _gutterClickHandler.UpdateLineHeight(lineHeight);

            // 5. Caret: scale width proportionally (2px at 16pt)
            if (_caret != null)
                _caret.SetCaretWidth(Mathf.Max(1f, _fontSize / 8f));

            // 6. Force full re-sync on next LateUpdate
            _lastSyncedVersion = -1;
        }

        private int _lastSyncedFirstLine = -1;
        private int _lastSyncedLastLine = -1;

        private void OnScrollChanged(Vector2 _)
        {
            if (_scrollManager == null || _linePool == null || _doc == null) return;

            int first = _scrollManager.FirstVisibleLine;
            int last = _scrollManager.LastVisibleLine;

            // Only refresh if the visible range actually changed
            if (first == _lastSyncedFirstLine && last == _lastSyncedLastLine) return;
            _lastSyncedFirstLine = first;
            _lastSyncedLastLine = last;

            _linePool.UpdateVisibleLines(first, last, _doc);
            ApplySyntaxHighlighting(first, last);

            if (_gutter != null && _showLineNumbers)
                _gutter.UpdateVisibleLines(first, last, _doc.LineCount);

            UpdateCaret();
            UpdateSelection(first, last);
            UpdateCurrentLineHighlight();
        }

        private void SyncView()
        {
            if (_scrollManager != null)
            {
                _scrollManager.UpdateContentSize(_doc.LineCount);
                _scrollManager.EnsureLineVisible(_doc.Cursor.Line);
            }

            int first = _scrollManager?.FirstVisibleLine ?? 0;
            int last = _scrollManager?.LastVisibleLine ?? (_doc.LineCount - 1);

            // If layout isn't ready yet (viewport has zero size), don't cache the version
            // so LateUpdate will retry next frame
            if (first == 0 && last <= 0 && _doc.LineCount > 1)
                return;

            _lastSyncedVersion = _doc.Version;
            _lastSyncedFirstLine = first;
            _lastSyncedLastLine = last;

            // Update text lines
            _linePool?.UpdateVisibleLines(first, last, _doc);

            // Apply syntax highlighting
            ApplySyntaxHighlighting(first, last);

            // Update line numbers
            if (_gutter != null && _showLineNumbers)
                _gutter.UpdateVisibleLines(first, last, _doc.LineCount);

            // Update caret
            UpdateCaret();

            // Update selection
            UpdateSelection(first, last);

            // Update current line highlight
            UpdateCurrentLineHighlight();

            // Keep hidden input field in sync with the document
            if (_inputHandler != null)
                _inputHandler.SyncInputFieldFromDocument();

            TextChanged?.Invoke(_doc.GetText());
        }

        private void UpdateCaret()
        {
            if (_caret == null || _linePool == null) return;
            var pixelPos = _linePool.GetPixelPosition(_doc.Cursor);
            _caret.SetPosition(pixelPos, _linePool.LineHeight);
        }

        private void UpdateSelection(int firstVisible, int lastVisible)
        {
            if (_selection == null || _linePool == null) return;
            _selection.UpdateSelection(
                _doc.SelectionRange,
                _linePool,
                _linePool.LineHeight,
                firstVisible,
                lastVisible);
        }

        private void UpdateCurrentLineHighlight()
        {
            if (_currentLineHighlight == null || _linePool == null) return;
            var rt = _currentLineHighlight.rectTransform;
            rt.anchoredPosition = new Vector2(0, -_doc.Cursor.Line * _linePool.LineHeight);
            rt.sizeDelta = new Vector2(0, _linePool.LineHeight);
        }

        // ── Syntax Highlighting ─────────────────────────────────────────

        private void ApplySyntaxHighlighting(int first, int last)
        {
            if (_linePool == null || _controller == null || _highlightTheme == null) return;

            var langService = _controller.LanguageService;
            if (langService is PlainTextLanguageService) return;

            int lineCount = _doc.LineCount;

            // Ensure caches are sized correctly
            if (_lineTokenCache == null)
            {
                _lineTokenCache = new System.Collections.Generic.List<System.Collections.Generic.IReadOnlyList<HighlightToken>>(lineCount);
                _lineStateCache = new System.Collections.Generic.List<object>(lineCount);
                for (int i = 0; i < lineCount; i++)
                {
                    _lineTokenCache.Add(null);
                    _lineStateCache.Add(null);
                }
                _highlightDirtyFrom = 0;
            }

            // Resize caches if line count changed
            while (_lineTokenCache.Count < lineCount)
            {
                _lineTokenCache.Add(null);
                _lineStateCache.Add(null);
            }
            while (_lineTokenCache.Count > lineCount)
            {
                _lineTokenCache.RemoveAt(_lineTokenCache.Count - 1);
                _lineStateCache.RemoveAt(_lineStateCache.Count - 1);
            }

            // Determine range to re-tokenize
            int dirtyStart = Math.Min(_highlightDirtyFrom, first);
            dirtyStart = Math.Max(0, dirtyStart);
            int dirtyEnd = Math.Min(last, lineCount - 1);

            // Re-tokenize dirty lines (from dirtyStart to at least last visible)
            for (int line = dirtyStart; line <= dirtyEnd; line++)
            {
                string lineText = _doc.GetLine(line);
                object startState = line > 0 ? _lineStateCache[line - 1] : null;

                var tokens = langService.TokenizeLine(lineText, startState);
                object endState = langService.GetLineEndState(lineText, startState);

                _lineTokenCache[line] = tokens;

                // If end state changed, cascade dirty to next line
                object oldEndState = _lineStateCache[line];
                _lineStateCache[line] = endState;

                if (line == dirtyEnd && line < lineCount - 1 && !StateEquals(oldEndState, endState))
                    dirtyEnd = Math.Min(dirtyEnd + 1, lineCount - 1);
            }

            _highlightDirtyFrom = int.MaxValue;

            // Apply colors to visible lines
            Color32 defaultColor = _highlightTheme.DefaultColor;
            for (int line = first; line <= last && line < lineCount; line++)
            {
                var tokens = _lineTokenCache[line];
                if (tokens != null && tokens.Count > 0)
                    _linePool.ApplyHighlighting(line, tokens, _highlightTheme.GetColor, defaultColor);
            }
        }

        private static bool StateEquals(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        private void OnDocumentChanged(DocumentChangeEventArgs args)
        {
            // Version change will trigger SyncView in LateUpdate
            int changedLine = args.DeletedRange.Start.Line;
            _highlightDirtyFrom = Math.Min(_highlightDirtyFrom, changedLine);
        }

        private void OnCursorMoved(TextPosition pos)
        {
            if (_scrollManager != null)
                _scrollManager.EnsureLineVisible(pos.Line);

            int first = _scrollManager?.FirstVisibleLine ?? 0;
            int last = _scrollManager?.LastVisibleLine ?? (_doc.LineCount - 1);

            UpdateCaret();
            UpdateSelection(first, last);
            UpdateCurrentLineHighlight();
        }

        // ── Completion ────────────────────────────────────────────────────

        /// <summary>
        /// Triggers the completion popup. Called from InputHandler on Ctrl+Space
        /// or after typing an identifier character.
        /// </summary>
        public void TriggerCompletion(bool force = false)
        {
            if (_completionProvider == null || _completionPopup == null || _linePool == null) return;

            var result = _completionProvider.GetCompletions(_doc, _doc.Cursor);
            if (result == null || result.Items.Count == 0)
            {
                if (!force) _completionPopup.Hide();
                return;
            }

            if (_completionPopup.IsVisible)
            {
                _completionPopup.UpdateFilter(result);
            }
            else
            {
                var caretPx = _linePool.GetPixelPosition(_doc.Cursor);
                _completionPopup.Show(result, caretPx, _linePool.LineHeight);
            }
        }

        /// <summary>
        /// Dismisses the completion popup.
        /// </summary>
        public void DismissCompletion()
        {
            _completionPopup?.Hide();
        }

        /// <summary>
        /// Shows or updates the signature hint tooltip for the function call at the cursor.
        /// Called from InputHandler after typing '(' or ',', and during typing inside a call.
        /// </summary>
        public void TriggerSignatureHint()
        {
            if (_signatureHintProvider == null || _signatureHintPopup == null || _linePool == null) return;

            int activeParam;
            var hint = _signatureHintProvider.GetSignatureHint(_doc, _doc.Cursor, out activeParam);
            if (hint == null)
            {
                _signatureHintPopup.Hide();
                return;
            }

            if (_signatureHintPopup.IsVisible)
            {
                _signatureHintPopup.UpdateHint(hint, activeParam);
            }
            else
            {
                var caretPx = _linePool.GetPixelPosition(_doc.Cursor);
                _signatureHintPopup.Show(hint, activeParam, caretPx, _linePool.LineHeight);
            }
        }

        /// <summary>
        /// Dismisses the signature hint tooltip.
        /// </summary>
        public void DismissSignatureHint()
        {
            _signatureHintPopup?.Hide();
        }

        private void OnCompletionAccepted(CompletionItem item)
        {
            if (_controller == null) return;

            // Use the result captured at accept time (before Hide cleared it)
            var result = _completionPopup?.LastAcceptedResult;
            TextRange range;
            if (result != null && !result.ReplacementRange.IsEmpty)
            {
                range = result.ReplacementRange;
            }
            else
            {
                var (_, prefixRange) = _controller.GetWordPrefixAtCursor();
                range = prefixRange;
            }

            // Replace the prefix with the completion text
            _doc.Delete(range);
            _doc.Insert(range.Start, item.InsertText);
            _doc.SetCursor(new TextPosition(range.Start.Line,
                range.Start.Column + item.InsertText.Length));

            SyncView();
            _inputHandler?.SyncInputFieldFromDocument();
        }

        private void EnsureUiEventInfrastructure()
        {
            if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
                Debug.LogWarning("[View] Auto-created EventSystem because none was found in scene.");
            }

            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.LogWarning("[View] Added missing GraphicRaycaster to parent Canvas.");
            }
        }
    }
}
