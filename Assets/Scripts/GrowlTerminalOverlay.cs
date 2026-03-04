using System.Collections.Generic;
using UnityEngine;
using GrowlLanguage.Runtime;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GrowlTerminalOverlay : MonoBehaviour
{
    private const string SourceEditorControlName = "GrowlSourceEditor";

    private enum TerminalUiSizePreset
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        ExtraLarge = 3,
    }

    private enum TerminalTemplate
    {
        Welcome = 0,
        OrganismStructLifecycle = 1,
        OrganismHostBridgePulse = 2,
    }

    private static readonly string[] SizePresetLabels = { "Small", "Medium", "Large", "XL" };
    private static readonly string[] TemplateLabels = { "Welcome", "Struct Organism", "Host Organism" };

    [SerializeField] private bool visible = false;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.T;

    public bool IsVisible => visible;
    public event System.Action OnClosed;
    [SerializeField] private TerminalUiSizePreset uiSizePreset = TerminalUiSizePreset.ExtraLarge;
    [SerializeField] private bool showSizePresetSelector = true;
    [SerializeField] private bool showTemplateSelector = true;
    [SerializeField] private TerminalTemplate selectedTemplate = TerminalTemplate.OrganismStructLifecycle;
    [SerializeField] private int indentSize = 4;
    [SerializeField] private bool allowResize = true;
    [SerializeField] private float minWindowWidth = 520f;
    [SerializeField] private float minWindowHeight = 360f;
    [SerializeField] private float resizeHandleSize = 18f;
    [SerializeField] private float screenEdgePadding = 8f;

    [Header("Runtime")]
    [SerializeField] private bool autoInvokeEntryFunction = true;
    [SerializeField] private string entryFunctionName = "main";
    [SerializeField] private int maxLoopIterations = 100000;
    [SerializeField] private MonoBehaviour runtimeHostComponent;

    private Rect _windowRect = new Rect(20f, 20f, 760f, 560f);
    private string _source = BuildTemplateSource(TerminalTemplate.OrganismStructLifecycle);
    private string _output = string.Empty;
    private Vector2 _sourceScroll;
    private Vector2 _outputScroll;
    private bool _isResizing;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;
    private int _resizeControlId;
    private int _sourceKeyboardControl;
    private bool _queuedTabIndent;
    private bool _queuedTabOutdent;
    private bool _queuedAutoIndentEnter;
    private string _queuedEnterSourceSnapshot;
    private int _queuedEnterSelectionStart = -1;
    private int _queuedEnterSelectionEnd = -1;
    private int _pendingCursorIndex = -1;
    private int _pendingSelectIndex = -1;
    private readonly List<int> _lineStartCache = new List<int>();
    private bool _lineCacheDirty = true;
    private float _lineAdvance = 14f; // per-line advance in pixels, cached when style changes
    private GUIStyle _labelStyle;
    private GUIStyle _textAreaStyle;
    private GUIStyle _outputStyle;
    private GUIStyle _lineNumberStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _toolbarStyle;
    private GUIStyle _footerStyle;

    private void Awake()
    {
        if (hideOnStart)
            visible = false;

        // Migrate older inspector instances that were using the previous default.
        if (toggleKey == KeyCode.BackQuote)
            toggleKey = KeyCode.T;

        if (runtimeHostComponent == null)
            runtimeHostComponent = GrowlRuntimeHostResolver.GetOrCreateHostBridge();
    }

    public void SetVisible(bool show)
    {
        visible = show;
    }

    private void Update()
    {
        bool wasVisible = visible;

        if (IsTogglePressed())
            visible = !visible;

        if (wasVisible && !visible)
            OnClosed?.Invoke();
    }

    private void OnDisable()
    {
        if (_isResizing && GUIUtility.hotControl == _resizeControlId)
            GUIUtility.hotControl = 0;

        _isResizing = false;
        _resizeControlId = 0;
        _queuedTabIndent = false;
        _queuedTabOutdent = false;
        _queuedAutoIndentEnter = false;
        _queuedEnterSourceSnapshot = null;
        _queuedEnterSelectionStart = -1;
        _queuedEnterSelectionEnd = -1;
        _pendingCursorIndex = -1;
        _pendingSelectIndex = -1;
    }

    private bool IsTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool cmdOrCtrl = keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed ||
                             keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (!cmdOrCtrl)
                return false;

            switch (toggleKey)
            {
                case KeyCode.T:
                    if (keyboard.tKey.wasPressedThisFrame)
                        return true;
                    break;
                case KeyCode.BackQuote:
                    if (keyboard.backquoteKey.wasPressedThisFrame)
                        return true;
                    break;
            }
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        bool legacyCmdOrCtrl = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                               Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        return legacyCmdOrCtrl && Input.GetKeyDown(toggleKey);
#else
        return false;
#endif
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        ClampWindowSizeForPreset();
        ClampWindowToScreen();
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Growl Terminal");
        ClampWindowToScreen();
    }

    private void DrawWindow(int windowId)
    {
        float uiScale = GetUiScale();
        EnsureStyles(uiScale);
        CaptureSourceEditorKeyOverrides();

        float sectionGap = 5f * uiScale;
        float buttonHeight = 30f * uiScale;
        float footerHeight = 18f * uiScale;
        float toolbarHeight = 26f * uiScale;
        float labelHeight = 20f * uiScale;
        float layoutReserve = (2f * labelHeight) + buttonHeight + footerHeight + (3.5f * sectionGap);
        if (showSizePresetSelector)
            layoutReserve += toolbarHeight + (0.5f * sectionGap);
        if (showTemplateSelector)
            layoutReserve += toolbarHeight + (0.5f * sectionGap);

        float availablePanelHeight = Mathf.Max(140f * uiScale, _windowRect.height - layoutReserve);
        float sourceHeight = Mathf.Max(78f * uiScale, availablePanelHeight * 0.55f);
        float outputHeight = Mathf.Max(80f * uiScale, availablePanelHeight - sourceHeight);

        GUILayout.BeginVertical();

        if (showSizePresetSelector)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI Size", _labelStyle, GUILayout.Width(72f * uiScale), GUILayout.Height(labelHeight));
            int selected = GUILayout.Toolbar((int)uiSizePreset, SizePresetLabels, _toolbarStyle, GUILayout.Height(toolbarHeight));
            if (selected != (int)uiSizePreset)
                uiSizePreset = (TerminalUiSizePreset)selected;
            GUILayout.EndHorizontal();
            GUILayout.Space(sectionGap * 0.5f);
        }

        if (showTemplateSelector)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Template", _labelStyle, GUILayout.Width(72f * uiScale), GUILayout.Height(labelHeight));
            int templateIndex = GUILayout.Toolbar((int)selectedTemplate, TemplateLabels, _toolbarStyle, GUILayout.Height(toolbarHeight));
            if (templateIndex != (int)selectedTemplate)
                selectedTemplate = (TerminalTemplate)templateIndex;
            if (GUILayout.Button("Load", _buttonStyle, GUILayout.Width(88f * uiScale), GUILayout.Height(toolbarHeight)))
                LoadSelectedTemplate();
            GUILayout.EndHorizontal();
            GUILayout.Space(sectionGap * 0.5f);
        }

        GUILayout.Label("Source", _labelStyle, GUILayout.Height(labelHeight));
        Rect sourceRect = GUILayoutUtility.GetRect(10f, 100000f, sourceHeight, sourceHeight, GUILayout.ExpandWidth(true));
        DrawSourceEditor(sourceRect, uiScale);

        GUILayout.Space(sectionGap);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Run", _buttonStyle, GUILayout.Width(140f * uiScale), GUILayout.Height(buttonHeight)))
            Run();
        if (GUILayout.Button("Clear Output", _buttonStyle, GUILayout.Width(140f * uiScale), GUILayout.Height(buttonHeight)))
            _output = string.Empty;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(sectionGap);
        GUILayout.Label("Output", _labelStyle, GUILayout.Height(labelHeight));
        _outputScroll = GUILayout.BeginScrollView(_outputScroll, GUILayout.Height(outputHeight));
        string outputText = string.IsNullOrEmpty(_output) ? " " : _output;
        GUILayout.Label(outputText, _outputStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.Space(sectionGap * 0.25f);
        GUILayout.Label("Toggle: Cmd/Ctrl+" + toggleKey, _footerStyle, GUILayout.Height(footerHeight));

        GUILayout.EndVertical();

        HandleResize(uiScale);
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
    }

    private void CaptureSourceEditorKeyOverrides()
    {
        Event evt = Event.current;
        bool isTab = evt.keyCode == KeyCode.Tab;
        bool isEnter = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter;
        if (!isTab && !isEnter)
            return;

        if (!IsSourceEditorFocused())
            return;

        if (evt.type == EventType.KeyDown)
        {
            if (isTab)
            {
                _queuedTabIndent = true;
                _queuedTabOutdent = evt.shift;
            }
            else if (isEnter)
            {
                _queuedAutoIndentEnter = true;
                QueueAutoIndentSnapshot();
            }

            evt.Use();
            return;
        }

        if (evt.type == EventType.KeyUp)
            evt.Use();
    }

    private void DrawSourceEditor(Rect containerRect, float uiScale)
    {
        float gutterWidth = Mathf.Max(44f * uiScale, 42f);
        float contentPadding = Mathf.Max(4f, 4f * uiScale);
        float lineAdvance = _lineAdvance;

        EnsureLineStartCache();
        // Style vertical padding is applied once to the whole TextArea, not per line.
        float styleVertPad = _textAreaStyle.padding.top + _textAreaStyle.padding.bottom;
        float contentHeight = Mathf.Max(containerRect.height,
            styleVertPad + (_lineStartCache.Count * lineAdvance) + (2f * contentPadding));
        float contentWidth = Mathf.Max(containerRect.width - 2f, gutterWidth + 180f);

        Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
        _sourceScroll = GUI.BeginScrollView(containerRect, _sourceScroll, viewRect, false, true);

        Rect gutterRect = new Rect(0f, 0f, gutterWidth, viewRect.height);
        DrawSolidRect(gutterRect, new Color(0f, 0f, 0f, 0.18f));
        DrawLineNumbers(gutterRect, lineAdvance, contentPadding);

        Rect editorRect = new Rect(
            gutterWidth + contentPadding,
            contentPadding,
            Mathf.Max(100f, viewRect.width - gutterWidth - (2f * contentPadding)),
            Mathf.Max(lineAdvance + 8f, viewRect.height - (2f * contentPadding)));

        GUI.SetNextControlName(SourceEditorControlName);
        string newSource = GUI.TextArea(editorRect, _source, _textAreaStyle);

        // Highlight drawn after TextArea so graphicalCursorPos reflects the current frame's cursor.
        DrawCurrentLineHighlight(editorRect);

        if (!string.Equals(newSource, _source))
        {
            _source = newSource;
            MarkLineCacheDirty();
        }

        if (GUI.GetNameOfFocusedControl() == SourceEditorControlName)
            _sourceKeyboardControl = GUIUtility.keyboardControl;

        ApplyQueuedEditorActions();
        ApplyPendingSourceEditorState();

        GUI.EndScrollView();
    }

    private bool IsSourceEditorFocused()
    {
        return GUI.GetNameOfFocusedControl() == SourceEditorControlName ||
               (_sourceKeyboardControl != 0 && GUIUtility.keyboardControl == _sourceKeyboardControl);
    }

    private void QueueAutoIndentSnapshot()
    {
        string snapshot = _source ?? string.Empty;
        int start;
        int end;

        TextEditor textEditor = GetSourceTextEditor();
        if (textEditor != null)
        {
            start = Mathf.Min(textEditor.cursorIndex, textEditor.selectIndex);
            end = Mathf.Max(textEditor.cursorIndex, textEditor.selectIndex);
        }
        else
        {
            int fallback = _pendingCursorIndex >= 0 ? _pendingCursorIndex : snapshot.Length;
            start = Mathf.Clamp(fallback, 0, snapshot.Length);
            end = start;
        }

        _queuedEnterSourceSnapshot = snapshot;
        _queuedEnterSelectionStart = Mathf.Clamp(start, 0, snapshot.Length);
        _queuedEnterSelectionEnd = Mathf.Clamp(end, 0, snapshot.Length);
    }

    private void ApplyQueuedEditorActions()
    {
        TextEditor textEditor = GetSourceTextEditor();

        if (_queuedAutoIndentEnter)
        {
            ApplyAutoIndentOnEnter(textEditor);
            _queuedAutoIndentEnter = false;
        }

        if (textEditor == null)
        {
            _queuedTabIndent = false;
            _queuedTabOutdent = false;
            return;
        }

        if (_queuedTabIndent)
        {
            ApplyQueuedTabIndent(textEditor);
            _queuedTabIndent = false;
            _queuedTabOutdent = false;
        }
    }

    private void ApplyQueuedTabIndent(TextEditor textEditor)
    {
        TerminalTextEditResult result = GrowlTerminalTextOps.ApplyTabIndentation(
            _source,
            textEditor.cursorIndex,
            textEditor.selectIndex,
            indentSize,
            _queuedTabOutdent);

        _source = result.Text;
        MarkLineCacheDirty();

        textEditor.cursorIndex = result.CursorIndex;
        textEditor.selectIndex = result.SelectIndex;
        _pendingCursorIndex = result.CursorIndex;
        _pendingSelectIndex = result.SelectIndex;
    }

    private void ApplyAutoIndentOnEnter(TextEditor textEditor)
    {
        TerminalTextEditResult result = GrowlTerminalTextOps.ApplyEnterAutoIndent(
            _queuedEnterSourceSnapshot ?? _source,
            _queuedEnterSelectionStart,
            _queuedEnterSelectionEnd,
            indentSize);

        _source = result.Text;
        MarkLineCacheDirty();

        int newCaret = result.CursorIndex;
        if (textEditor != null)
        {
            textEditor.cursorIndex = newCaret;
            textEditor.selectIndex = newCaret;
        }
        _pendingCursorIndex = newCaret;
        _pendingSelectIndex = newCaret;

        _queuedEnterSourceSnapshot = null;
        _queuedEnterSelectionStart = -1;
        _queuedEnterSelectionEnd = -1;
    }

    private void ApplyPendingSourceEditorState()
    {
        if (_pendingCursorIndex < 0 || _pendingSelectIndex < 0)
            return;

        TextEditor textEditor = GetSourceTextEditor();
        if (textEditor == null)
            return;

        int max = _source == null ? 0 : _source.Length;
        textEditor.cursorIndex = Mathf.Clamp(_pendingCursorIndex, 0, max);
        textEditor.selectIndex = Mathf.Clamp(_pendingSelectIndex, 0, max);

        if (_sourceKeyboardControl != 0)
            GUIUtility.keyboardControl = _sourceKeyboardControl;

        _pendingCursorIndex = -1;
        _pendingSelectIndex = -1;
    }

    private TextEditor GetSourceTextEditor()
    {
        int editorControlId = _sourceKeyboardControl != 0 ? _sourceKeyboardControl : GUIUtility.keyboardControl;
        if (editorControlId == 0)
            return null;
        return GUIUtility.GetStateObject(typeof(TextEditor), editorControlId) as TextEditor;
    }

    private void LoadSelectedTemplate()
    {
        _source = BuildTemplateSource(selectedTemplate);
        MarkLineCacheDirty();
        _sourceScroll = Vector2.zero;
        _pendingCursorIndex = _source.Length;
        _pendingSelectIndex = _source.Length;
    }

    private void MarkLineCacheDirty()
    {
        _lineCacheDirty = true;
    }

    private void EnsureLineStartCache()
    {
        if (!_lineCacheDirty)
            return;

        _lineStartCache.Clear();
        _lineStartCache.AddRange(GrowlTerminalTextOps.BuildLineStartCache(_source));
        _lineCacheDirty = false;
    }

    private int GetLineIndexAtPosition(int position)
    {
        EnsureLineStartCache();
        return GrowlTerminalTextOps.GetLineIndexAtPosition(_lineStartCache, (_source ?? string.Empty).Length, position);
    }

    // Returns the per-line advance (font line height + leading) that Unity uses internally.
    // Two-line differential avoids relying on explicit padding values, which can vary
    // across Unity versions and skin states. Cached in _lineAdvance on style rebuild.
    private static float ComputeLineAdvance(GUIStyle style)
    {
        float h1 = style.CalcHeight(new GUIContent("A"), 9999f);
        float h2 = style.CalcHeight(new GUIContent("A\nA"), 9999f);
        float diff = h2 - h1;
        if (diff > 0f)
            return Mathf.Max(8f, diff);
        // Fallback for edge cases (e.g. not-yet-loaded font).
        return Mathf.Max(8f, h1 - style.padding.top - style.padding.bottom);
    }

    private void DrawLineNumbers(Rect gutterRect, float lineHeight, float contentPadding)
    {
        EnsureLineStartCache();
        float yOffset = contentPadding + _textAreaStyle.padding.top;
        int lineCount = Mathf.Max(1, _lineStartCache.Count);
        for (int i = 0; i < lineCount; i++)
        {
            Rect lineRect = new Rect(
                gutterRect.x + 2f,
                yOffset + (i * lineHeight),
                Mathf.Max(16f, gutterRect.width - 6f),
                lineHeight);
            GUI.Label(lineRect, (i + 1).ToString(), _lineNumberStyle);
        }
    }

    private void DrawCurrentLineHighlight(Rect editorRect)
    {
        float lineY;
        TextEditor textEditor = GetSourceTextEditor();
        if (textEditor != null)
        {
            // graphicalCursorPos is in editorRect-local space (0,0 = top-left of the rect).
            // Unity computes it from the same layout engine used to render the text, so it
            // is always pixel-accurate regardless of font size, padding, or skin changes.
            lineY = editorRect.y + textEditor.graphicalCursorPos.y;
        }
        else
        {
            // No TextEditor available yet — fall back to character-index math.
            int caretIndex = _pendingCursorIndex >= 0 ? _pendingCursorIndex : 0;
            int lineIndex = GetLineIndexAtPosition(caretIndex);
            lineY = editorRect.y + _textAreaStyle.padding.top + (lineIndex * _lineAdvance);
        }

        Rect highlightRect = new Rect(
            editorRect.x + 1f,
            lineY,
            Mathf.Max(2f, editorRect.width - 2f),
            _lineAdvance);
        DrawSolidRect(highlightRect, new Color(1f, 0.74f, 0.2f, 0.08f));
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, alphaBlend: true);
        GUI.color = previous;
    }

    private void HandleResize(float uiScale)
    {
        if (!allowResize)
            return;

        var evt = Event.current;
        int controlId = GUIUtility.GetControlID("GrowlTerminalResizeHandle".GetHashCode(), FocusType.Passive);
        float clampedHandle = Mathf.Max(10f * uiScale, resizeHandleSize * uiScale);
        var handleRect = new Rect(
            _windowRect.width - clampedHandle - 4f,
            _windowRect.height - clampedHandle - 4f,
            clampedHandle,
            clampedHandle);

        GUI.Box(handleRect, "◢");

        EventType eventType = evt.rawType;
        if (eventType != EventType.MouseDown &&
            eventType != EventType.MouseDrag &&
            eventType != EventType.MouseUp)
        {
            eventType = evt.type;
        }

        switch (eventType)
        {
            case EventType.MouseDown:
                if (evt.button == 0 && handleRect.Contains(evt.mousePosition))
                {
                    _isResizing = true;
                    _resizeControlId = controlId;
                    GUIUtility.hotControl = _resizeControlId;
                    _resizeStartMouse = evt.mousePosition;
                    _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (_isResizing && GUIUtility.hotControl == _resizeControlId)
                {
                    Vector2 delta = evt.mousePosition - _resizeStartMouse;
                    _windowRect.width = Mathf.Clamp(
                        _resizeStartSize.x + delta.x,
                        GetEffectiveMinWidth(uiScale),
                        GetEffectiveMaxWidth());
                    _windowRect.height = Mathf.Clamp(
                        _resizeStartSize.y + delta.y,
                        GetEffectiveMinHeight(uiScale),
                        GetEffectiveMaxHeight());
                    evt.Use();
                }
                break;

            case EventType.MouseUp:
                if (_isResizing && evt.button == 0 && GUIUtility.hotControl == _resizeControlId)
                {
                    _isResizing = false;
                    GUIUtility.hotControl = 0;
                    _resizeControlId = 0;
                    evt.Use();
                }
                break;
        }
    }

    private void ClampWindowSizeForPreset()
    {
        float uiScale = GetUiScale();
        _windowRect.width = Mathf.Clamp(_windowRect.width, GetEffectiveMinWidth(uiScale), GetEffectiveMaxWidth());
        _windowRect.height = Mathf.Clamp(_windowRect.height, GetEffectiveMinHeight(uiScale), GetEffectiveMaxHeight());
    }

    private float GetEffectiveMinWidth(float uiScale)
    {
        return Mathf.Max(minWindowWidth, 520f * uiScale);
    }

    private float GetEffectiveMinHeight(float uiScale)
    {
        return Mathf.Max(minWindowHeight, 360f * uiScale);
    }

    private float GetEffectiveMaxWidth()
    {
        return Mathf.Max(200f, Screen.width - (2f * screenEdgePadding));
    }

    private float GetEffectiveMaxHeight()
    {
        return Mathf.Max(180f, Screen.height - (2f * screenEdgePadding));
    }

    private void ClampWindowToScreen()
    {
        float maxWidth = GetEffectiveMaxWidth();
        float maxHeight = GetEffectiveMaxHeight();
        _windowRect.width = Mathf.Min(_windowRect.width, maxWidth);
        _windowRect.height = Mathf.Min(_windowRect.height, maxHeight);

        float minX = screenEdgePadding;
        float minY = screenEdgePadding;
        float maxX = Mathf.Max(minX, Screen.width - screenEdgePadding - _windowRect.width);
        float maxY = Mathf.Max(minY, Screen.height - screenEdgePadding - _windowRect.height);
        _windowRect.x = Mathf.Clamp(_windowRect.x, minX, maxX);
        _windowRect.y = Mathf.Clamp(_windowRect.y, minY, maxY);
    }

    private float GetUiScale()
    {
        switch (uiSizePreset)
        {
            case TerminalUiSizePreset.Small:
                return 0.9f;
            case TerminalUiSizePreset.Large:
                return 1.2f;
            case TerminalUiSizePreset.ExtraLarge:
                return 1.4f;
            default:
                return 1.0f;
        }
    }

    private void EnsureStyles(float uiScale)
    {
        int bodyFont = Mathf.Max(11, Mathf.RoundToInt(13f * uiScale));
        int labelFont = Mathf.Max(12, Mathf.RoundToInt(14f * uiScale));
        int footerFont = Mathf.Max(10, Mathf.RoundToInt(12f * uiScale));

        if (_labelStyle == null || _labelStyle.fontSize != labelFont)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = labelFont,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
        }

        if (_textAreaStyle == null || _textAreaStyle.fontSize != bodyFont)
        {
            _textAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = bodyFont,
                wordWrap = false,
            };
            _lineAdvance = ComputeLineAdvance(_textAreaStyle);
        }

        if (_outputStyle == null || _outputStyle.fontSize != bodyFont)
        {
            _outputStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = bodyFont,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = false,
            };
        }

        if (_buttonStyle == null || _buttonStyle.fontSize != bodyFont)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFont,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        if (_toolbarStyle == null || _toolbarStyle.fontSize != bodyFont)
        {
            _toolbarStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFont,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        if (_footerStyle == null || _footerStyle.fontSize != footerFont)
        {
            _footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = footerFont,
                alignment = TextAnchor.MiddleLeft,
            };
        }

        if (_lineNumberStyle == null || _lineNumberStyle.fontSize != bodyFont)
        {
            _lineNumberStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = bodyFont,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) },
            };
        }
    }

    private void Run()
    {
        IGrowlRuntimeHost runtimeHost = runtimeHostComponent as IGrowlRuntimeHost;
        if (runtimeHost == null)
        {
            runtimeHostComponent = GrowlRuntimeHostResolver.GetOrCreateHostBridge();
            runtimeHost = runtimeHostComponent as IGrowlRuntimeHost;
        }

        RuntimeResult result = GrowlRuntime.Execute(_source, new RuntimeOptions
        {
            AutoInvokeEntryFunction = autoInvokeEntryFunction,
            EntryFunctionName = entryFunctionName,
            MaxLoopIterations = maxLoopIterations,
            Host = runtimeHost,
        });

        var lines = new List<string>();

        if (result.Messages.Count > 0)
        {
            lines.Add("Errors:");
            for (int i = 0; i < result.Messages.Count; i++)
                lines.Add(result.Messages[i].ToString());
        }

        if (result.OutputLines.Count > 0)
        {
            if (lines.Count > 0)
                lines.Add(string.Empty);

            lines.Add("Output:");
            for (int i = 0; i < result.OutputLines.Count; i++)
                lines.Add(result.OutputLines[i]);
        }

        if (result.Success)
        {
            if (lines.Count > 0)
                lines.Add(string.Empty);

            lines.Add("Result:");
            lines.Add(RuntimeValueFormatter.Format(result.LastValue));
        }

        _output = lines.Count == 0 ? "(no output)" : string.Join("\n", lines);
    }

    private static string BuildTemplateSource(TerminalTemplate template)
    {
        switch (template)
        {
            case TerminalTemplate.Welcome:
                return
                    "fn main():\n" +
                    "    print(\"Growl runtime online\")\n" +
                    "    print(\"Try the organism templates from the Template row\")\n" +
                    "    return 1\n";

            case TerminalTemplate.OrganismStructLifecycle:
                return
                    "struct Organism:\n" +
                    "    name: string = \"Sprout\"\n" +
                    "    energy: float = 12.0\n" +
                    "    health: float = 1.0\n" +
                    "    age: int = 0\n" +
                    "\n" +
                    "fn tick(org):\n" +
                    "    org.age = org.age + 1\n" +
                    "    org.energy = org.energy - 0.5\n" +
                    "    if org.energy < 5:\n" +
                    "        org.health = org.health - 0.1\n" +
                    "    return org\n" +
                    "\n" +
                    "fn main():\n" +
                    "    plant = Organism(name: \"Fernling\", energy: 9.5, age: 2)\n" +
                    "    plant = tick(plant)\n" +
                    "    print(\"organism\", plant.name, \"age\", plant.age, \"energy\", plant.energy, \"health\", plant.health)\n" +
                    "    return plant\n";

            case TerminalTemplate.OrganismHostBridgePulse:
                return
                    "fn pulse():\n" +
                    "    world_add(\"tick\", 1)\n" +
                    "    org_add(\"age\", 1)\n" +
                    "    org_add(\"energy\", -2.5)\n" +
                    "    if org_get(\"energy\", 0) < 8:\n" +
                    "        emit_signal(\"low_energy\", intensity: 0.9, radius: 3)\n" +
                    "        org_memory_set(\"state\", \"seeking_light\")\n" +
                    "    if org_get(\"energy\", 0) < 3:\n" +
                    "        org_damage(0.15)\n" +
                    "    seed_add(\"count\", 1)\n" +
                    "    print(\"tick\", world_get(\"tick\"), \"energy\", org_get(\"energy\"), \"health\", org_get(\"health\"), \"seeds\", seed_get(\"count\"))\n" +
                    "    return org_get(\"health\")\n" +
                    "\n" +
                    "fn main():\n" +
                    "    pulse()\n" +
                    "    pulse()\n" +
                    "    return org_get(\"memory\")\n";

            default:
                return
                    "fn main():\n" +
                    "    print(\"Growl runtime online\")\n" +
                    "    return 1\n";
        }
    }
}
