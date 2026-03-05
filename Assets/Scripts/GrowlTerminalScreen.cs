using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeEditor.Completion;
using CodeEditor.View;
using GrowlLanguage.Runtime;

/// <summary>
/// Builds the toolbar (Run, Clear, font size) and output panel around a CodeEditorView.
/// Attach to the TerminalCanvas root. Expects a CodeEditorView child.
/// </summary>
public sealed class GrowlTerminalScreen : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private bool autoInvokeEntryFunction = true;
    [SerializeField] private string entryFunctionName = "main";
    [SerializeField] private int maxLoopIterations = 100000;

    private CodeEditorView _editor;
    private TMP_InputField _outputInput;
    private string _output = string.Empty;

    private void Awake()
    {
        _editor = GetComponentInChildren<CodeEditorView>(true);
        if (_editor == null)
        {
            Debug.LogError("[TerminalScreen] No CodeEditorView found in children.");
            return;
        }

        BuildLayout();

        _editor.SetLanguageService(new GrowlLanguageService());
        _editor.SetCompletionProvider(new GrowlCompletionProvider());
        _editor.SetSignatureHintProvider(new GrowlSignatureHintProvider());
        _editor.CtrlEnterPressed += RunCode;
    }

    private void OnDestroy()
    {
        if (_editor != null)
            _editor.CtrlEnterPressed -= RunCode;
    }

    private void BuildLayout()
    {
        // Reorganize: CodeEditor takes top portion, toolbar + output at bottom.
        // We create a parent panel with VerticalLayoutGroup to manage this.

        var canvasRt = GetComponent<RectTransform>();

        // --- Main panel (fills canvas with padding) ---
        var panel = CreateUIObject("Panel", transform);
        var panelRt = panel.GetComponent<RectTransform>();
        Stretch(panelRt);
        panelRt.offsetMin = new Vector2(40, 20);
        panelRt.offsetMax = new Vector2(-40, -20);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        panelImg.raycastTarget = true;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // --- Title bar ---
        var titleBar = CreateUIObject("TitleBar", panel.transform);
        var titleLE = titleBar.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 28;
        titleLE.flexibleWidth = 1;

        var titleHlg = titleBar.AddComponent<HorizontalLayoutGroup>();
        titleHlg.spacing = 8;
        titleHlg.childAlignment = TextAnchor.MiddleLeft;
        titleHlg.childForceExpandWidth = false;
        titleHlg.childForceExpandHeight = false;
        titleHlg.childControlWidth = true;
        titleHlg.childControlHeight = true;

        CreateLabel("Growl Terminal", titleBar.transform, 16, FontStyles.Bold, flexWidth: 1f);

        // Font size controls
        var fontDownBtn = CreateButton("-", titleBar.transform, 28, 26);
        var fontLabel = CreateLabel("16pt", titleBar.transform, 13, FontStyles.Normal, width: 40, height: 26);
        var fontUpBtn = CreateButton("+", titleBar.transform, 28, 26);

        // Wire font size buttons directly
        var fontLabelTmp = fontLabel.GetComponent<TMP_Text>();
        fontDownBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            _editor.SetFontSize(_editor.FontSize - 2f);
            fontLabelTmp.text = $"{_editor.FontSize:0}pt";
            _editor.Focus();
        });
        fontUpBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            _editor.SetFontSize(_editor.FontSize + 2f);
            fontLabelTmp.text = $"{_editor.FontSize:0}pt";
            _editor.Focus();
        });

        // --- Code editor (reparent existing) ---
        _editor.transform.SetParent(panel.transform, false);
        var editorLE = _editor.gameObject.AddComponent<LayoutElement>();
        editorLE.flexibleHeight = 3;
        editorLE.minHeight = 120;

        // --- Separator ---
        CreateSeparator(panel.transform);

        // --- Button bar ---
        var buttonBar = CreateUIObject("ButtonBar", panel.transform);
        var barLE = buttonBar.AddComponent<LayoutElement>();
        barLE.preferredHeight = 36;
        barLE.flexibleWidth = 1;

        var barHlg = buttonBar.AddComponent<HorizontalLayoutGroup>();
        barHlg.spacing = 8;
        barHlg.childAlignment = TextAnchor.MiddleLeft;
        barHlg.childForceExpandWidth = false;
        barHlg.childForceExpandHeight = false;
        barHlg.childControlWidth = true;
        barHlg.childControlHeight = true;

        var runBtn = CreateButton("Run (Ctrl+Enter)", buttonBar.transform, 160, 30);
        runBtn.GetComponent<Button>().onClick.AddListener(RunCode);

        var clearBtn = CreateButton("Clear Output", buttonBar.transform, 130, 30);
        clearBtn.GetComponent<Button>().onClick.AddListener(ClearOutput);

        // --- Separator ---
        CreateSeparator(panel.transform);

        // --- Output label ---
        CreateLabel("Output", panel.transform, 14, FontStyles.Bold, height: 20);

        // --- Output (read-only, selectable TMP_InputField) ---
        var outputArea = CreateUIObject("OutputArea", panel.transform);
        // Deactivate before adding TMP_InputField so OnEnable runs after configuration
        outputArea.SetActive(false);

        var outputLE = outputArea.AddComponent<LayoutElement>();
        outputLE.flexibleHeight = 1;
        outputLE.minHeight = 60;

        var outputImg = outputArea.AddComponent<Image>();
        outputImg.color = new Color(0.09f, 0.09f, 0.11f, 1f);
        outputImg.raycastTarget = true;

        // Text Area child (viewport with mask)
        var textArea = CreateUIObject("Text Area", outputArea.transform);
        var textAreaRt = textArea.GetComponent<RectTransform>();
        Stretch(textAreaRt);
        textAreaRt.offsetMin = new Vector2(6, 4);
        textAreaRt.offsetMax = new Vector2(-6, -4);
        textArea.AddComponent<RectMask2D>();

        // Text child
        var outputTextGo = CreateUIObject("Text", textArea.transform);
        var outputTmp = outputTextGo.AddComponent<TextMeshProUGUI>();
        outputTmp.fontSize = 13;
        outputTmp.fontStyle = FontStyles.Normal;
        outputTmp.color = new Color(0.75f, 0.85f, 0.75f, 1f);
        outputTmp.alignment = TextAlignmentOptions.TopLeft;
        outputTmp.enableWordWrapping = true;
        outputTmp.richText = true;
        var outputTmpRt = outputTextGo.GetComponent<RectTransform>();
        Stretch(outputTmpRt);

        // Wire up TMP_InputField — all properties set before reactivation
        _outputInput = outputArea.AddComponent<TMP_InputField>();
        _outputInput.textComponent = outputTmp;
        _outputInput.textViewport = textAreaRt;
        _outputInput.targetGraphic = outputImg;
        _outputInput.readOnly = true;
        _outputInput.richText = true;
        _outputInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        _outputInput.selectionColor = new Color(0.25f, 0.47f, 0.77f, 0.35f);
        _outputInput.caretColor = new Color(0, 0, 0, 0);
        _outputInput.caretWidth = 0;
        _outputInput.text = "(no output)";

        // Now activate — OnEnable runs with everything properly configured
        outputArea.SetActive(true);

        // --- Footer ---
        CreateLabel("T to open  |  Esc to close  |  Ctrl+Enter to run", panel.transform, 11, FontStyles.Italic, height: 18, color: new Color(1, 1, 1, 0.35f));
    }

    private void RunCode()
    {
        if (_editor == null) return;

        string source = _editor.Text;
        IGrowlRuntimeHost host = GrowlRuntimeHostResolver.GetOrCreateHostBridge();

        RuntimeResult result = GrowlRuntime.Execute(source, new RuntimeOptions
        {
            AutoInvokeEntryFunction = autoInvokeEntryFunction,
            EntryFunctionName = entryFunctionName,
            MaxLoopIterations = maxLoopIterations,
            Host = host,
        });

        var lines = new List<string>();

        if (result.Messages.Count > 0)
        {
            lines.Add("<color=#FF6666>Errors:</color>");
            for (int i = 0; i < result.Messages.Count; i++)
                lines.Add(result.Messages[i].ToString());
        }

        if (result.OutputLines.Count > 0)
        {
            if (lines.Count > 0) lines.Add("");
            lines.Add("<color=#66CCFF>Output:</color>");
            for (int i = 0; i < result.OutputLines.Count; i++)
                lines.Add(result.OutputLines[i]);
        }

        if (result.Success)
        {
            if (lines.Count > 0) lines.Add("");
            lines.Add("<color=#88FF88>Result:</color>");
            lines.Add(RuntimeValueFormatter.Format(result.LastValue));
        }

        _output = lines.Count == 0 ? "(no output)" : string.Join("\n", lines);
        if (_outputInput != null)
            _outputInput.text = _output;
    }

    private void ClearOutput()
    {
        _output = string.Empty;
        if (_outputInput != null)
            _outputInput.text = "(no output)";
    }

    // --- UI helpers ---

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void CreateSeparator(Transform parent)
    {
        var go = CreateUIObject("Separator", parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 0.5f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1;
        le.flexibleWidth = 1;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static GameObject CreateLabel(string text, Transform parent, int fontSize, FontStyles style,
        float flexWidth = 0, float width = 0, float height = 0, Color? color = null)
    {
        var go = CreateUIObject("Label", parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color ?? new Color(0.85f, 0.85f, 0.9f, 1f);
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        if (width > 0 || height > 0 || flexWidth > 0)
        {
            var le = go.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            if (height > 0) le.preferredHeight = height;
            if (flexWidth > 0) le.flexibleWidth = flexWidth;
        }

        return go;
    }

    private static TMP_Text CreateTMPText(string name, Transform parent, int fontSize, FontStyles style)
    {
        var go = CreateUIObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        return tmp;
    }

    private static GameObject CreateButton(string label, Transform parent, float width, float height)
    {
        var go = CreateUIObject("Btn_" + label, parent);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.22f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.28f, 0.32f, 0.4f, 1f);
        colors.pressedColor = new Color(0.14f, 0.16f, 0.2f, 1f);
        btn.colors = colors;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.32f, 0.38f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        var textGo = CreateUIObject("Text", go.transform);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        Stretch(textGo.GetComponent<RectTransform>());

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        return go;
    }
}
