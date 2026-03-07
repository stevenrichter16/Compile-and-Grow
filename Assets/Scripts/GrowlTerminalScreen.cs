using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeEditor.Completion;
using CodeEditor.View;
using GrowlLanguage.Runtime;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    private TMP_Text _outputTmp;
    private string _output = string.Empty;

    // Tick mode
    private bool _ticking;
    private long _tickCount;
    private OrganismEntity _sandboxOrganism;
    private BiologicalContext _bioContext;
    private GrowthTickManager _tickManager;
    private TMP_Text _tickBtnLabel;

    // Detail overlay
    private GameObject _detailOverlay;
    private UIPlantGraphic _detailGraphic;
    private TMP_Text _detailNameLabel;
    private OrganismEntity _detailOrganism;
    private Transform _partsListContent;
    private ResourceGrid _resourceGrid;
    private TMP_Text _detailIOText;
    private TMP_Text _detailEnvText;
    private TMP_InputField _skipTickInput;
    private bool _fastForwarding;

    public bool IsDetailOverlayOpen => _detailOverlay != null && _detailOverlay.activeSelf;

    // Tabs
    private GameObject _codePanel;
    private GameObject _plantsPanel;
    private Image _codeTabImg;
    private Image _plantsTabImg;
    private TMP_Text _codeTabLabel;
    private TMP_Text _plantsTabLabel;
    private bool _showingPlantsTab;
    private Transform _gridContent;
    private readonly List<PlantCellData> _plantCells = new List<PlantCellData>();

    private struct PlantCellData
    {
        public OrganismEntity organism;
        public UIPlantGraphic graphic;
        public TMP_Text nameLabel;
        public GameObject cellRoot;
    }

    private static readonly Color TabActive = new Color(0.22f, 0.24f, 0.30f, 1f);
    private static readonly Color TabInactive = new Color(0.14f, 0.14f, 0.17f, 1f);

    private const string DefaultProgram =
@"# Sprout — a simple photosynthesizer
# Absorbs light and CO2, produces oxygen

@role(""intake"")
fn intake():
    phase ""seedling""(0, 0.1):
        root.grow_down(1)
    root.absorb(""water"")
    leaf.absorb_chemical(""co2"")

@role(""structure"")
fn structure():
    phase ""seedling""(0, 0.1):
        stem.grow_up(1)

    phase ""more leaves""(0.1, 1.0):
        leaf.grow(4)

@role(""energy"")
fn energy():
    photo.absorb_light()
    leaf.open_stomata(1.0)

#@role(""output"")
#fn output():
#    o2 = synthesize(""chemical"", type: ""oxygen"")
#    if o2 != none:
#        emit(o2, 0.1)

fn main():
    intake()
    structure()
    energy()
    #output()

    a = org_get(""age"")
    e = org_get(""energy"")
    w = org_get(""water"")
    m = org_get(""maturity"")
    print(""tick "" + str(a) + ""  energy="" + str(e) + ""  water="" + str(w) + ""  maturity="" + str(m))
";

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
        _editor.CtrlClickWord += OnCtrlClickWord;
    }

    private void Start()
    {
        if (_editor != null)
            _editor.Text = DefaultProgram;
    }

    private void OnDestroy()
    {
        if (_ticking)
            StopTicking();

        if (_editor != null)
        {
            _editor.CtrlEnterPressed -= RunCode;
            _editor.CtrlClickWord -= OnCtrlClickWord;
        }
    }

    private void BuildLayout()
    {
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

        var fontLabelTmp = fontLabel.GetComponent<TMP_Text>();
        fontDownBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            _editor.SetFontSize(_editor.FontSize - 2f);
            fontLabelTmp.text = $"{_editor.FontSize:0}pt";
            if (_outputTmp != null) _outputTmp.fontSize = _editor.FontSize - 3f;
            _editor.Focus();
        });
        fontUpBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            _editor.SetFontSize(_editor.FontSize + 2f);
            fontLabelTmp.text = $"{_editor.FontSize:0}pt";
            if (_outputTmp != null) _outputTmp.fontSize = _editor.FontSize - 3f;
            _editor.Focus();
        });

        // --- Tab bar ---
        BuildTabBar(panel.transform);

        // --- Code panel (wraps editor + output side by side, buttons below) ---
        _codePanel = CreateUIObject("CodePanel", panel.transform);
        var codePanelVlg = _codePanel.AddComponent<VerticalLayoutGroup>();
        codePanelVlg.spacing = 4;
        codePanelVlg.childForceExpandWidth = true;
        codePanelVlg.childForceExpandHeight = false;
        codePanelVlg.childControlWidth = true;
        codePanelVlg.childControlHeight = true;
        var codePanelLE = _codePanel.AddComponent<LayoutElement>();
        codePanelLE.flexibleHeight = 1;

        // --- Editor + Output side by side ---
        var editorOutputRow = CreateUIObject("EditorOutputRow", _codePanel.transform);
        var rowHlg = editorOutputRow.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 4;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        var rowLE = editorOutputRow.AddComponent<LayoutElement>();
        rowLE.flexibleHeight = 1;

        // --- Code editor (reparent existing, left side) ---
        _editor.transform.SetParent(editorOutputRow.transform, false);
        var editorLE = _editor.gameObject.AddComponent<LayoutElement>();
        editorLE.flexibleWidth = 3;
        editorLE.flexibleHeight = 1;
        editorLE.minHeight = 120;

        // --- Output panel (right side) ---
        var outputPanel = CreateUIObject("OutputPanel", editorOutputRow.transform);
        var outputPanelVlg = outputPanel.AddComponent<VerticalLayoutGroup>();
        outputPanelVlg.spacing = 4;
        outputPanelVlg.padding = new RectOffset(0, 0, 0, 0);
        outputPanelVlg.childForceExpandWidth = true;
        outputPanelVlg.childForceExpandHeight = false;
        outputPanelVlg.childControlWidth = true;
        outputPanelVlg.childControlHeight = true;
        var outputPanelLE = outputPanel.AddComponent<LayoutElement>();
        outputPanelLE.flexibleWidth = 2;

        // Output label
        CreateLabel("Output", outputPanel.transform, 14, FontStyles.Bold, height: 20);

        // Output area (read-only, selectable TMP_InputField)
        var outputArea = CreateUIObject("OutputArea", outputPanel.transform);

        var outputLE = outputArea.AddComponent<LayoutElement>();
        outputLE.flexibleHeight = 1;
        outputLE.minHeight = 60;

        var outputImg = outputArea.AddComponent<Image>();
        outputImg.color = new Color(0.09f, 0.09f, 0.11f, 1f);
        outputImg.raycastTarget = true;

        var textArea = CreateUIObject("Text Area", outputArea.transform);
        var textAreaRt = textArea.GetComponent<RectTransform>();
        Stretch(textAreaRt);
        textAreaRt.offsetMin = new Vector2(6, 4);
        textAreaRt.offsetMax = new Vector2(-6, -4);
        textArea.AddComponent<RectMask2D>();

        var outputTextGo = CreateUIObject("Text", textArea.transform);
        _outputTmp = outputTextGo.AddComponent<TextMeshProUGUI>();
        var outputTmp = _outputTmp;
        outputTmp.fontSize = 13;
        outputTmp.fontStyle = FontStyles.Normal;
        outputTmp.color = new Color(0.75f, 0.85f, 0.75f, 1f);
        outputTmp.alignment = TextAlignmentOptions.TopLeft;
        outputTmp.enableWordWrapping = true;
        outputTmp.richText = true;
        var outputTmpRt = outputTextGo.GetComponent<RectTransform>();
        Stretch(outputTmpRt);

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

        // Force TMP_InputField initialization
        outputArea.SetActive(false);
        outputArea.SetActive(true);

        // --- Separator ---
        CreateSeparator(_codePanel.transform);

        // --- Button bar ---
        var buttonBar = CreateUIObject("ButtonBar", _codePanel.transform);
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

        var tickBtn = CreateButton("Start Tick", buttonBar.transform, 120, 30);
        _tickBtnLabel = tickBtn.GetComponentInChildren<TMP_Text>();
        tickBtn.GetComponent<Button>().onClick.AddListener(ToggleTick);

        // --- Plants panel (hidden by default) ---
        BuildPlantsPanel(panel.transform);

        // --- Footer ---
        CreateLabel("T to open  |  Esc to close  |  Ctrl+Enter to run", panel.transform, 11, FontStyles.Italic, height: 18, color: new Color(1, 1, 1, 0.35f));

        // --- Detail overlay (sibling of Panel, not inside PlantsPanel) ---
        BuildDetailOverlay(transform);

        // Start on Code tab
        SwitchTab(false);
    }

    private void BuildTabBar(Transform parent)
    {
        var tabBar = CreateUIObject("TabBar", parent);
        var tabBarLE = tabBar.AddComponent<LayoutElement>();
        tabBarLE.preferredHeight = 30;
        tabBarLE.flexibleWidth = 1;

        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 2;
        tabHlg.childAlignment = TextAnchor.MiddleLeft;
        tabHlg.childForceExpandWidth = false;
        tabHlg.childForceExpandHeight = false;
        tabHlg.childControlWidth = true;
        tabHlg.childControlHeight = true;

        // Code tab
        var codeTab = CreateTabButton("Code", tabBar.transform);
        _codeTabImg = codeTab.GetComponent<Image>();
        _codeTabLabel = codeTab.GetComponentInChildren<TMP_Text>();
        codeTab.GetComponent<Button>().onClick.AddListener(() => SwitchTab(false));

        // Plants tab
        var plantsTab = CreateTabButton("Plants", tabBar.transform);
        _plantsTabImg = plantsTab.GetComponent<Image>();
        _plantsTabLabel = plantsTab.GetComponentInChildren<TMP_Text>();
        plantsTab.GetComponent<Button>().onClick.AddListener(() => SwitchTab(true));

        // Spacer
        var spacer = CreateUIObject("Spacer", tabBar.transform);
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.preferredWidth = 16;

        // Skip-to-tick label
        var skipLabel = CreateUIObject("SkipLabel", tabBar.transform);
        var skipLabelTmp = skipLabel.AddComponent<TextMeshProUGUI>();
        skipLabelTmp.text = "Skip to tick:";
        skipLabelTmp.fontSize = 11;
        skipLabelTmp.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        skipLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        skipLabelTmp.enableWordWrapping = false;
        var skipLabelLE = skipLabel.AddComponent<LayoutElement>();
        skipLabelLE.preferredWidth = 80;
        skipLabelLE.preferredHeight = 24;

        // Skip tick input
        var skipInputGo = CreateUIObject("SkipTickInput", tabBar.transform);
        var skipInputImg = skipInputGo.AddComponent<Image>();
        skipInputImg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
        var skipInputLE = skipInputGo.AddComponent<LayoutElement>();
        skipInputLE.preferredWidth = 60;
        skipInputLE.preferredHeight = 24;

        var skipTextArea = CreateUIObject("Text Area", skipInputGo.transform);
        var skipTextAreaRt = skipTextArea.GetComponent<RectTransform>();
        Stretch(skipTextAreaRt);
        skipTextAreaRt.offsetMin = new Vector2(4, 2);
        skipTextAreaRt.offsetMax = new Vector2(-4, -2);
        skipTextArea.AddComponent<RectMask2D>();

        var skipTextGo = CreateUIObject("Text", skipTextArea.transform);
        var skipTmp = skipTextGo.AddComponent<TextMeshProUGUI>();
        skipTmp.fontSize = 12;
        skipTmp.color = new Color(0.85f, 0.9f, 0.85f, 1f);
        skipTmp.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(skipTextGo.GetComponent<RectTransform>());

        _skipTickInput = skipInputGo.AddComponent<TMP_InputField>();
        _skipTickInput.textComponent = skipTmp;
        _skipTickInput.textViewport = skipTextAreaRt;
        _skipTickInput.targetGraphic = skipInputImg;
        _skipTickInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        _skipTickInput.text = "";
        _skipTickInput.placeholder = null;

        // Skip button
        var skipBtn = CreateButton("Skip", tabBar.transform, 50, 24);
        skipBtn.GetComponent<Button>().onClick.AddListener(SkipToTick);
    }

    private static GameObject CreateTabButton(string label, Transform parent)
    {
        var go = CreateUIObject("Tab_" + label, parent);

        var img = go.AddComponent<Image>();
        img.color = TabInactive;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        var textGo = CreateUIObject("Text", go.transform);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.7f, 0.7f, 0.75f, 1f);
        Stretch(textGo.GetComponent<RectTransform>());

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 80;
        le.preferredHeight = 7;

        return go;
    }

    private void BuildPlantsPanel(Transform parent)
    {
        _plantsPanel = CreateUIObject("PlantsPanel", parent);
        var plantsPanelLE = _plantsPanel.AddComponent<LayoutElement>();
        plantsPanelLE.flexibleHeight = 1;

        // ScrollRect
        var scrollRect = _plantsPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewport = CreateUIObject("Viewport", _plantsPanel.transform);
        var viewportRt = viewport.GetComponent<RectTransform>();
        Stretch(viewportRt);
        viewport.AddComponent<RectMask2D>();
        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0.09f, 0.09f, 0.11f, 1f);

        // Content
        var content = CreateUIObject("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 180);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        _gridContent = content.transform;
    }

    private void BuildDetailOverlay(Transform parent)
    {
        _detailOverlay = CreateUIObject("DetailOverlay", parent);
        var overlayRt = _detailOverlay.GetComponent<RectTransform>();
        Stretch(overlayRt);

        // Dim background (click to close)
        var dimGo = CreateUIObject("DimBackground", _detailOverlay.transform);
        var dimRt = dimGo.GetComponent<RectTransform>();
        Stretch(dimRt);
        var dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.7f);
        dimImg.raycastTarget = true;
        var dimBtn = dimGo.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(HidePlantDetail);

        // Detail panel (~70% centered)
        var detailPanel = CreateUIObject("DetailPanel", _detailOverlay.transform);
        var detailRt = detailPanel.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.15f, 0.1f);
        detailRt.anchorMax = new Vector2(0.85f, 0.9f);
        detailRt.sizeDelta = Vector2.zero;
        detailRt.anchoredPosition = Vector2.zero;

        var detailBg = detailPanel.AddComponent<Image>();
        detailBg.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        detailBg.raycastTarget = true;

        var detailVlg = detailPanel.AddComponent<VerticalLayoutGroup>();
        detailVlg.spacing = 4;
        detailVlg.padding = new RectOffset(12, 12, 8, 12);
        detailVlg.childForceExpandWidth = true;
        detailVlg.childForceExpandHeight = false;
        detailVlg.childControlWidth = true;
        detailVlg.childControlHeight = true;

        // Header bar
        var headerBar = CreateUIObject("HeaderBar", detailPanel.transform);
        var headerLE = headerBar.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 32;
        headerLE.flexibleWidth = 1;

        var headerHlg = headerBar.AddComponent<HorizontalLayoutGroup>();
        headerHlg.spacing = 8;
        headerHlg.childAlignment = TextAnchor.MiddleLeft;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = false;
        headerHlg.childControlWidth = true;
        headerHlg.childControlHeight = true;

        var nameLabelGo = CreateLabel("Unnamed", headerBar.transform, 16, FontStyles.Bold, flexWidth: 1f);
        _detailNameLabel = nameLabelGo.GetComponent<TMP_Text>();

        var closeBtn = CreateButton("X", headerBar.transform, 32, 28);
        closeBtn.GetComponent<Button>().onClick.AddListener(HidePlantDetail);

        // Content area (two-column: graphic left, parts list right)
        var contentArea = CreateUIObject("ContentArea", detailPanel.transform);
        var contentAreaHlg = contentArea.AddComponent<HorizontalLayoutGroup>();
        contentAreaHlg.spacing = 8;
        contentAreaHlg.childForceExpandWidth = false;
        contentAreaHlg.childForceExpandHeight = true;
        contentAreaHlg.childControlWidth = true;
        contentAreaHlg.childControlHeight = true;
        var contentAreaLE = contentArea.AddComponent<LayoutElement>();
        contentAreaLE.flexibleHeight = 1;

        // Left: Plant graphic
        var graphicGo = CreateUIObject("DetailPlantGraphic", contentArea.transform);
        graphicGo.AddComponent<CanvasRenderer>();
        _detailGraphic = graphicGo.AddComponent<UIPlantGraphic>();
        _detailGraphic.raycastTarget = true;
        var graphicLE = graphicGo.AddComponent<LayoutElement>();
        graphicLE.flexibleWidth = 3;
        graphicLE.flexibleHeight = 1;
        graphicLE.minHeight = 200;

        // Right: Parts list panel
        var partsPanel = CreateUIObject("PartsListPanel", contentArea.transform);
        var partsPanelImg = partsPanel.AddComponent<Image>();
        partsPanelImg.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        partsPanelImg.raycastTarget = true;
        var partsPanelVlg = partsPanel.AddComponent<VerticalLayoutGroup>();
        partsPanelVlg.spacing = 4;
        partsPanelVlg.padding = new RectOffset(8, 8, 8, 8);
        partsPanelVlg.childForceExpandWidth = true;
        partsPanelVlg.childForceExpandHeight = false;
        partsPanelVlg.childControlWidth = true;
        partsPanelVlg.childControlHeight = true;
        var partsPanelLE = partsPanel.AddComponent<LayoutElement>();
        partsPanelLE.flexibleWidth = 2;

        // "Parts" header
        CreateLabel("Parts", partsPanel.transform, 12, FontStyles.Bold, height: 18,
            color: new Color(0.5f, 0.55f, 0.5f, 1f));

        // Separator
        CreateSeparator(partsPanel.transform);

        // ScrollRect for parts list
        var partsScrollGo = CreateUIObject("PartsScroll", partsPanel.transform);
        var partsScrollRect = partsScrollGo.AddComponent<ScrollRect>();
        partsScrollRect.horizontal = false;
        partsScrollRect.vertical = true;
        partsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        var partsScrollLE = partsScrollGo.AddComponent<LayoutElement>();
        partsScrollLE.flexibleHeight = 1;

        // Viewport
        var partsViewport = CreateUIObject("Viewport", partsScrollGo.transform);
        var partsViewportRt = partsViewport.GetComponent<RectTransform>();
        Stretch(partsViewportRt);
        partsViewport.AddComponent<RectMask2D>();

        // Content
        var partsContent = CreateUIObject("Content", partsViewport.transform);
        var partsContentRt = partsContent.GetComponent<RectTransform>();
        partsContentRt.anchorMin = new Vector2(0, 1);
        partsContentRt.anchorMax = new Vector2(1, 1);
        partsContentRt.pivot = new Vector2(0.5f, 1);
        partsContentRt.sizeDelta = Vector2.zero;

        var partsContentVlg = partsContent.AddComponent<VerticalLayoutGroup>();
        partsContentVlg.spacing = 2;
        partsContentVlg.padding = new RectOffset(4, 4, 4, 4);
        partsContentVlg.childForceExpandWidth = true;
        partsContentVlg.childForceExpandHeight = false;
        partsContentVlg.childControlWidth = true;
        partsContentVlg.childControlHeight = true;

        var partsContentCsf = partsContent.AddComponent<ContentSizeFitter>();
        partsContentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        partsScrollRect.viewport = partsViewportRt;
        partsScrollRect.content = partsContentRt;

        _partsListContent = partsContent.transform;

        // --- Organism I/O section ---
        CreateSeparator(partsPanel.transform);
        CreateLabel("Organism I/O", partsPanel.transform, 12, FontStyles.Bold, height: 18,
            color: new Color(0.5f, 0.55f, 0.5f, 1f));

        var ioGo = CreateUIObject("DetailIOText", partsPanel.transform);
        _detailIOText = ioGo.AddComponent<TextMeshProUGUI>();
        _detailIOText.fontSize = 10;
        _detailIOText.fontStyle = FontStyles.Normal;
        _detailIOText.color = new Color(0.75f, 0.8f, 0.75f, 1f);
        _detailIOText.enableWordWrapping = true;
        _detailIOText.richText = true;
        _detailIOText.raycastTarget = false;
        var ioLE = ioGo.AddComponent<LayoutElement>();
        ioLE.flexibleWidth = 1;
        ioLE.preferredHeight = 80;

        // --- Environment section ---
        CreateSeparator(partsPanel.transform);
        CreateLabel("Environment", partsPanel.transform, 12, FontStyles.Bold, height: 18,
            color: new Color(0.5f, 0.55f, 0.5f, 1f));

        var envGo = CreateUIObject("DetailEnvText", partsPanel.transform);
        _detailEnvText = envGo.AddComponent<TextMeshProUGUI>();
        _detailEnvText.fontSize = 10;
        _detailEnvText.fontStyle = FontStyles.Normal;
        _detailEnvText.color = new Color(0.75f, 0.8f, 0.75f, 1f);
        _detailEnvText.enableWordWrapping = true;
        _detailEnvText.richText = true;
        _detailEnvText.raycastTarget = false;
        var envLE = envGo.AddComponent<LayoutElement>();
        envLE.flexibleWidth = 1;
        envLE.preferredHeight = 80;

        _detailOverlay.SetActive(false);
    }

    private void ShowPlantDetail(OrganismEntity organism)
    {
        _detailOrganism = organism;
        _detailGraphic.SetBody(organism.Body);
        _detailGraphic.Refresh();
        _detailNameLabel.text = organism.OrganismName ?? "Unnamed";
        RefreshPartsList();
        RefreshDetailInfo();
        _detailOverlay.SetActive(true);
    }

    private void HidePlantDetail()
    {
        _detailOverlay.SetActive(false);
        _detailOrganism = null;
    }

    private void Update()
    {
        if (IsDetailOverlayOpen && IsEscapePressed())
            HidePlantDetail();
    }

    private void SwitchTab(bool showPlants)
    {
        _showingPlantsTab = showPlants;
        _codePanel.SetActive(!showPlants);
        _plantsPanel.SetActive(showPlants);

        _codeTabImg.color = showPlants ? TabInactive : TabActive;
        _plantsTabImg.color = showPlants ? TabActive : TabInactive;

        _codeTabLabel.fontStyle = showPlants ? FontStyles.Normal : FontStyles.Bold;
        _plantsTabLabel.fontStyle = showPlants ? FontStyles.Bold : FontStyles.Normal;

        _codeTabLabel.color = showPlants
            ? new Color(0.7f, 0.7f, 0.75f, 1f)
            : new Color(0.9f, 0.9f, 0.95f, 1f);
        _plantsTabLabel.color = showPlants
            ? new Color(0.9f, 0.9f, 0.95f, 1f)
            : new Color(0.7f, 0.7f, 0.75f, 1f);

        if (showPlants)
            RefreshPlantsGrid();
    }

    private void RefreshPlantsGrid()
    {
        var organisms = FindObjectsOfType<OrganismEntity>();

        // Build set of current organism instance IDs
        var currentIds = new HashSet<int>();
        for (int i = 0; i < organisms.Length; i++)
            currentIds.Add(organisms[i].GetInstanceID());

        // Remove stale cells (destroyed organisms)
        for (int i = _plantCells.Count - 1; i >= 0; i--)
        {
            if (_plantCells[i].organism == null || !currentIds.Contains(_plantCells[i].organism.GetInstanceID()))
            {
                if (_plantCells[i].cellRoot != null)
                    Destroy(_plantCells[i].cellRoot);
                _plantCells.RemoveAt(i);
            }
        }

        // Build set of existing cell organism IDs
        var existingIds = new HashSet<int>();
        for (int i = 0; i < _plantCells.Count; i++)
            existingIds.Add(_plantCells[i].organism.GetInstanceID());

        // Add new cells
        for (int i = 0; i < organisms.Length; i++)
        {
            if (!existingIds.Contains(organisms[i].GetInstanceID()))
            {
                var cell = CreatePlantCell(organisms[i]);
                _plantCells.Add(cell);
            }
        }

        // Refresh all graphics
        for (int i = 0; i < _plantCells.Count; i++)
        {
            var cell = _plantCells[i];
            if (cell.organism != null)
            {
                cell.graphic.SetBody(cell.organism.Body);
                cell.graphic.Refresh();
                cell.nameLabel.text = cell.organism.OrganismName ?? "Unnamed";
            }
        }
    }

    private PlantCellData CreatePlantCell(OrganismEntity organism)
    {
        var cellGo = CreateUIObject("PlantCell", _gridContent);

        var bgImg = cellGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        bgImg.raycastTarget = true;

        var cellBtn = cellGo.AddComponent<Button>();
        var cellBtnColors = cellBtn.colors;
        cellBtnColors.normalColor = Color.white;
        cellBtnColors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cellBtnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cellBtn.colors = cellBtnColors;
        var capturedOrganism = organism;
        cellBtn.onClick.AddListener(() => ShowPlantDetail(capturedOrganism));

        var cellVlg = cellGo.AddComponent<VerticalLayoutGroup>();
        cellVlg.spacing = 2;
        cellVlg.padding = new RectOffset(4, 4, 4, 4);
        cellVlg.childForceExpandWidth = true;
        cellVlg.childForceExpandHeight = false;
        cellVlg.childControlWidth = true;
        cellVlg.childControlHeight = true;

        // Plant graphic
        var graphicGo = CreateUIObject("PlantGraphic", cellGo.transform);
        graphicGo.AddComponent<CanvasRenderer>();
        var graphic = graphicGo.AddComponent<UIPlantGraphic>();
        graphic.raycastTarget = false;
        var graphicLE = graphicGo.AddComponent<LayoutElement>();
        graphicLE.flexibleHeight = 1;
        graphicLE.minHeight = 120;

        // Name label
        var labelGo = CreateUIObject("NameLabel", cellGo.transform);
        var nameLabel = labelGo.AddComponent<TextMeshProUGUI>();
        nameLabel.text = organism.OrganismName ?? "Unnamed";
        nameLabel.fontSize = 11;
        nameLabel.fontStyle = FontStyles.Normal;
        nameLabel.color = new Color(0.7f, 0.8f, 0.7f, 1f);
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.enableWordWrapping = false;
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;
        var nameLabelLE = labelGo.AddComponent<LayoutElement>();
        nameLabelLE.preferredHeight = 20;

        graphic.SetBody(organism.Body);

        return new PlantCellData
        {
            organism = organism,
            graphic = graphic,
            nameLabel = nameLabel,
            cellRoot = cellGo
        };
    }

    private void RunCode()
    {
        if (_editor == null) return;

        // If already ticking, stop and restart with fresh state
        if (_ticking)
            StopTicking();

        StartTicking();
    }

    private void ClearOutput()
    {
        _output = string.Empty;
        if (_outputInput != null)
            _outputInput.text = "(no output)";
    }

    private void SkipToTick()
    {
        if (_skipTickInput == null || string.IsNullOrWhiteSpace(_skipTickInput.text)) return;
        if (!int.TryParse(_skipTickInput.text, out int targetTick) || targetTick <= 0) return;

        // Start ticking if not already
        if (!_ticking)
            StartTicking();
        if (_tickManager == null) return;

        int ticksToRun = targetTick - (int)_tickCount;
        if (ticksToRun <= 0) return;

        _fastForwarding = true;
        for (int i = 0; i < ticksToRun; i++)
            _tickManager.AdvanceTick(1);
        _fastForwarding = false;

        // Update output text once
        if (_outputInput != null) _outputInput.text = _output;

        // Do a single UI refresh at the end
        if (_showingPlantsTab && _plantsPanel.activeSelf)
            RefreshPlantsGrid();
        if (IsDetailOverlayOpen && _detailOrganism != null)
        {
            _detailGraphic.SetBody(_detailOrganism.Body);
            _detailGraphic.Refresh();
            RefreshPartsList();
            RefreshDetailInfo();
        }

        _skipTickInput.text = "";
    }

    // --- Tick mode ---

    private void ToggleTick()
    {
        if (_ticking)
            StopTicking();
        else
            StartTicking();
    }

    private void StartTicking()
    {
        _tickManager = FindObjectOfType<GrowthTickManager>();
        if (_tickManager == null)
        {
            _output = "<color=#FF6666>No GrowthTickManager found in scene.</color>";
            if (_outputInput != null) _outputInput.text = _output;
            return;
        }

        // Create organism for the terminal code to operate on
        string orgName = ExtractOrganismName(_editor.Text);
        var sandboxGo = new GameObject("[Terminal:" + orgName + "]");
        sandboxGo.hideFlags = HideFlags.HideInHierarchy;
        _sandboxOrganism = sandboxGo.AddComponent<OrganismEntity>();
        _sandboxOrganism.TrySetState("name", orgName, out _);

        _bioContext = new BiologicalContext();
        _tickCount = 0;
        _output = "<color=#AAAAAA>Ticking started...</color>\n";
        if (_outputInput != null) _outputInput.text = _output;

        _tickManager.OnTickAdvanced += OnTickExecute;
        _ticking = true;
        if (_tickBtnLabel != null) _tickBtnLabel.text = "Stop Tick";
    }

    private void StopTicking()
    {
        if (_tickManager != null)
            _tickManager.OnTickAdvanced -= OnTickExecute;

        if (_sandboxOrganism != null)
            Destroy(_sandboxOrganism.gameObject);

        _sandboxOrganism = null;
        _bioContext = null;
        _ticking = false;
        if (_tickBtnLabel != null) _tickBtnLabel.text = "Start Tick";
    }

    private void OnTickExecute(long tick)
    {
        if (_editor == null || _sandboxOrganism == null) return;

        string source = _editor.Text;
        if (string.IsNullOrWhiteSpace(source)) return;

        // Age the sandbox organism consistently with GeneExecutionManager
        _sandboxOrganism.TryAddState("age", 1, out _, out _);
        _sandboxOrganism.TryAddState("maturity", 0.05, out _, out _);

        var bridge = GrowlRuntimeHostResolver.GetOrCreateHostBridge();
        bridge.ClearActionLog();
        bridge.SetOrganism(_sandboxOrganism);
        _bioContext.CurrentTick = tick;
        bridge.SetBioContext(_bioContext);

        RuntimeResult result = GrowlRuntime.Execute(source, new RuntimeOptions
        {
            AutoInvokeEntryFunction = autoInvokeEntryFunction,
            EntryFunctionName = entryFunctionName,
            MaxLoopIterations = maxLoopIterations,
            Host = bridge,
            BioContext = _bioContext,
        });

        _tickCount++;

        // Build per-tick output
        var lines = new List<string>();

        if (result.Messages.Count > 0)
        {
            for (int i = 0; i < result.Messages.Count; i++)
                lines.Add("<color=#FF6666>" + result.Messages[i] + "</color>");
        }

        for (int i = 0; i < result.OutputLines.Count; i++)
            lines.Add(result.OutputLines[i]);

        // Append action log from bridge
        var actionLog = bridge.ActionLog;
        for (int i = 0; i < actionLog.Count; i++)
            lines.Add("<color=#AABB99>" + actionLog[i] + "</color>");

        // Always show tick header with actions
        _output += "<color=#888888>--- Tick " + _tickCount + " ---</color>\n";
        if (lines.Count > 0)
            _output += string.Join("\n", lines) + "\n";

        // Skip heavy UI updates during fast-forward; update text at end
        if (_fastForwarding) return;

        if (_outputInput != null) _outputInput.text = _output;

        // Refresh plants grid if visible
        if (_showingPlantsTab && _plantsPanel.activeSelf)
            RefreshPlantsGrid();

        // Refresh detail overlay if open
        if (IsDetailOverlayOpen && _detailOrganism != null)
        {
            _detailGraphic.SetBody(_detailOrganism.Body);
            _detailGraphic.Refresh();
            RefreshPartsList();
            RefreshDetailInfo();
        }
    }

    // --- Parts list ---

    private void RefreshPartsList()
    {
        if (_partsListContent == null || _detailOrganism == null) return;

        // Clear existing rows
        for (int i = _partsListContent.childCount - 1; i >= 0; i--)
            Destroy(_partsListContent.GetChild(i).gameObject);

        var parts = _detailOrganism.Body.Parts;
        for (int i = 0; i < parts.Count; i++)
            CreatePartRow(parts[i]);
    }

    private void RefreshDetailInfo()
    {
        if (_detailOrganism == null) return;

        // --- Organism I/O ---
        if (_detailIOText != null)
        {
            var snap = _detailOrganism.CreateStateSnapshot();
            var sb = new System.Text.StringBuilder();

            // Core stats (always present)
            float energy = snap.ContainsKey("energy") ? System.Convert.ToSingle(snap["energy"]) : 0f;
            float water = snap.ContainsKey("water") ? System.Convert.ToSingle(snap["water"]) : 0f;
            float health = snap.ContainsKey("health") ? System.Convert.ToSingle(snap["health"]) : 0f;
            float stress = snap.ContainsKey("stress") ? System.Convert.ToSingle(snap["stress"]) : 0f;
            float maturity = snap.ContainsKey("maturity") ? System.Convert.ToSingle(snap["maturity"]) : 0f;
            long age = snap.ContainsKey("age") ? System.Convert.ToInt64(snap["age"]) : 0;

            sb.AppendLine($"energy: {energy:F1}    water: {water:F2}");
            sb.AppendLine($"health: {health:F0}%    stress: {stress:F2}");
            sb.Append($"maturity: {maturity:F1}    age: {age}");

            // Custom/absorbed resources (skip internal complex objects)
            var skipKeys = new HashSet<string>(System.StringComparer.Ordinal)
                { "name", "alive", "age", "maturity", "energy", "water", "health", "stress",
                  "memory", "parts", "morphology", "nutrients" };
            var customEntries = new List<string>();
            foreach (var kv in snap)
            {
                if (skipKeys.Contains(kv.Key)) continue;
                if (kv.Value is System.Collections.IDictionary) continue;
                if (kv.Value is System.Collections.IList) continue;
                customEntries.Add($"{kv.Key}: {FormatValue(kv.Value)}");
            }

            if (customEntries.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
                sb.Append(string.Join("   ", customEntries));
            }

            _detailIOText.text = sb.ToString();
        }

        // --- Environment ---
        if (_detailEnvText != null)
        {
            if (_resourceGrid == null)
                _resourceGrid = FindObjectOfType<ResourceGrid>();

            if (_resourceGrid != null)
            {
                var world = _resourceGrid.CreateWorldSnapshot();
                var sb = new System.Text.StringBuilder();

                // Base values
                var baseEntries = new List<string>();
                var groups = new Dictionary<string, List<string>>(System.StringComparer.Ordinal);

                foreach (var kv in world)
                {
                    string key = kv.Key;
                    int uscore = key.IndexOf('_');
                    if (uscore > 0)
                    {
                        string prefix = key.Substring(0, uscore);
                        if (!groups.ContainsKey(prefix))
                            groups[prefix] = new List<string>();
                        groups[prefix].Add($"{key}: {FormatValue(kv.Value)}");
                    }
                    else
                    {
                        string suffix = key == "temperature" ? "\u00b0" : "";
                        baseEntries.Add($"{key}: {FormatValue(kv.Value)}{suffix}");
                    }
                }

                sb.Append(string.Join("   ", baseEntries));

                foreach (var g in groups)
                {
                    sb.AppendLine();
                    sb.AppendLine($"\u2500\u2500\u2500\u2500 {g.Key} \u2500\u2500\u2500\u2500");
                    sb.Append(string.Join("   ", g.Value));
                }

                _detailEnvText.text = sb.ToString();
            }
            else
            {
                _detailEnvText.text = "<color=#808080>No ResourceGrid in scene</color>";
            }
        }
    }

    private static string FormatValue(object value)
    {
        if (value is float f) return f.ToString(f == (int)f ? "F0" : "F2");
        if (value is double d) return d.ToString(d == (long)d ? "F0" : "F2");
        return value?.ToString() ?? "null";
    }

    private void CreatePartRow(PlantPart part)
    {
        var row = CreateUIObject("PartRow", _partsListContent);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = new Color(0.13f, 0.13f, 0.16f, 1f);
        rowImg.raycastTarget = false;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 32;

        // Type dot — positioned manually at left-center
        var dotGo = CreateUIObject("TypeDot", row.transform);
        var dotImg = dotGo.AddComponent<Image>();
        dotImg.color = PartTypeColor(part.PartType);
        dotImg.raycastTarget = false;
        var dotRt = dotGo.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0, 0.5f);
        dotRt.anchorMax = new Vector2(0, 0.5f);
        dotRt.pivot = new Vector2(0, 0.5f);
        dotRt.anchoredPosition = new Vector2(6, 0);
        dotRt.sizeDelta = new Vector2(10, 10);

        // Info label — fills remaining space with left offset for dot
        string partName = part.Name ?? "unnamed";
        string typeName = string.IsNullOrEmpty(part.PartType) ? "unknown" : part.PartType.ToLowerInvariant();
        string stats = $"{typeName} \u00b7 age {part.Age} \u00b7 hp {part.Health:P0}";
        string infoText = $"<b>{partName}</b>\n<size=10><color=#808D80>{stats}</color></size>";

        var infoGo = CreateUIObject("InfoLabel", row.transform);
        var infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
        infoTmp.text = infoText;
        infoTmp.fontSize = 12;
        infoTmp.color = new Color(0.85f, 0.9f, 0.85f, 1f);
        infoTmp.enableWordWrapping = false;
        infoTmp.overflowMode = TextOverflowModes.Ellipsis;
        infoTmp.richText = true;
        infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var infoRt = infoGo.GetComponent<RectTransform>();
        Stretch(infoRt);
        infoRt.offsetMin = new Vector2(22, 2);
        infoRt.offsetMax = new Vector2(-4, -2);
    }

    private static Color PartTypeColor(string partType)
    {
        switch ((partType ?? "").ToLowerInvariant())
        {
            case "root":     return new Color(0.55f, 0.35f, 0.15f);
            case "stem":     return new Color(0.2f, 0.5f, 0.2f);
            case "branch":
            case "segment":  return new Color(0.3f, 0.45f, 0.2f);
            case "leaf":     return new Color(0.3f, 0.75f, 0.3f);
            case "product":  return new Color(0.85f, 0.7f, 0.2f);
            default:         return new Color(0.5f, 0.5f, 0.5f);
        }
    }

    // --- Go-to-definition ---

    private void OnCtrlClickWord(string word, int clickedLine)
    {
        if (_editor == null || string.IsNullOrEmpty(word)) return;

        int defLine = FindDefinitionLine(word);
        if (defLine >= 0)
            _editor.FlashLine(defLine);
    }

    private int FindDefinitionLine(string word)
    {
        string text = _editor.Text;
        if (string.IsNullOrEmpty(text)) return -1;

        string[] lines = text.Split('\n');
        int firstCandidate = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();

            // fn word(...) — function declaration
            if (trimmed.StartsWith("fn "))
            {
                string afterFn = trimmed.Substring(3);
                string name = ExtractIdentifier(afterFn, 0);
                if (name == word) return i;

                // Check parameters
                int parenOpen = afterFn.IndexOf('(');
                if (parenOpen >= 0 && firstCandidate < 0)
                {
                    if (ContainsIdentifier(afterFn.Substring(parenOpen + 1), word))
                        firstCandidate = i;
                }
            }

            // class/struct/enum/trait word
            string[] declKeywords = { "class ", "struct ", "enum ", "trait " };
            for (int k = 0; k < declKeywords.Length; k++)
            {
                if (trimmed.StartsWith(declKeywords[k]))
                {
                    string name = ExtractIdentifier(trimmed, declKeywords[k].Length);
                    if (name == word) return i;
                }
            }

            // for word in ...
            if (trimmed.StartsWith("for ") && firstCandidate < 0)
            {
                string name = ExtractIdentifier(trimmed, 4);
                if (name == word)
                    firstCandidate = i;
            }

            // word = ... (first assignment, not ==)
            if (firstCandidate < 0 && IsAssignmentTo(trimmed, word))
                firstCandidate = i;
        }

        return firstCandidate;
    }

    private static string ExtractIdentifier(string text, int start)
    {
        int i = start;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
            i++;
        return i > start ? text.Substring(start, i - start) : null;
    }

    private static bool ContainsIdentifier(string text, string word)
    {
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsLetter(text[i]) || text[i] == '_')
            {
                int start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    i++;
                if (text.Substring(start, i - start) == word)
                    return true;
            }
            else
            {
                i++;
            }
        }
        return false;
    }

    private static bool IsAssignmentTo(string trimmed, string word)
    {
        if (!trimmed.StartsWith(word)) return false;
        int afterWord = word.Length;
        if (afterWord >= trimmed.Length) return false;
        if (char.IsLetterOrDigit(trimmed[afterWord]) || trimmed[afterWord] == '_')
            return false;

        int j = afterWord;
        while (j < trimmed.Length && trimmed[j] == ' ') j++;

        if (j < trimmed.Length && trimmed[j] == '=')
        {
            if (j + 1 < trimmed.Length && trimmed[j + 1] == '=')
                return false;
            return true;
        }
        return false;
    }

    private static string ExtractOrganismName(string source)
    {
        if (string.IsNullOrEmpty(source)) return "Terminal";
        string[] lines = source.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("class "))
            {
                string name = ExtractIdentifier(trimmed, 6);
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        return "Terminal";
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

    private static bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
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
