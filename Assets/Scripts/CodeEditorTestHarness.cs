using UnityEngine;
using UnityEngine.UI;
using CodeEditor.View;
using GrowlLanguage.Runtime;

public class CodeEditorTestHarness : MonoBehaviour
{
    [Header("Editor")]
    [SerializeField] private CodeEditorView _editor;

    [Header("Output")]
    [SerializeField] private GrowlOutputPanel _outputPanel;

    [Header("Buttons")]
    [SerializeField] private Button _runButton;
    [SerializeField] private Button _clearButton;
    [SerializeField] private Button _template1Button;
    [SerializeField] private Button _template2Button;
    [SerializeField] private Button _template3Button;

    [Header("Runtime")]
    [SerializeField] private bool _autoInvokeEntryFunction = true;
    [SerializeField] private string _entryFunctionName = "main";
    [SerializeField] private int _maxLoopIterations = 100000;
    [SerializeField] private MonoBehaviour _runtimeHostComponent;

    private void Start()
    {
        if (_editor == null) return;

        _editor.Text = "";
        _editor.Focus();

        if (_runButton != null)
            _runButton.onClick.AddListener(RunGrowl);

        if (_clearButton != null && _outputPanel != null)
            _clearButton.onClick.AddListener(_outputPanel.Clear);

        _editor.CtrlEnterPressed += RunGrowl;

        if (_template1Button != null)
            _template1Button.onClick.AddListener(() => LoadTemplate(Template1));
        if (_template2Button != null)
            _template2Button.onClick.AddListener(() => LoadTemplate(Template2));
        if (_template3Button != null)
            _template3Button.onClick.AddListener(() => LoadTemplate(Template3));
    }

    private void OnDestroy()
    {
        if (_editor != null)
            _editor.CtrlEnterPressed -= RunGrowl;
    }

    private void RunGrowl()
    {
        if (_editor == null || _outputPanel == null) return;

        string source = _editor.Text;

        IGrowlRuntimeHost host = _runtimeHostComponent as IGrowlRuntimeHost;
        if (host == null)
        {
            _runtimeHostComponent = GrowlRuntimeHostResolver.GetOrCreateHostBridge();
            host = _runtimeHostComponent as IGrowlRuntimeHost;
        }

        RuntimeResult result = GrowlRuntime.Execute(source, new RuntimeOptions
        {
            AutoInvokeEntryFunction = _autoInvokeEntryFunction,
            EntryFunctionName = _entryFunctionName,
            MaxLoopIterations = _maxLoopIterations,
            Host = host,
        });

        _outputPanel.ShowResult(result);
        _editor.Focus();
    }

    private void LoadTemplate(string source)
    {
        if (_editor == null) return;
        _editor.Text = source;
        _editor.Focus();
    }

    private const string Template1 =
        "fn greet(name):\n" +
        "    return \"Hello, \" + name + \"!\"\n" +
        "\n" +
        "fn main():\n" +
        "    print(greet(\"Growl\"))\n" +
        "    print(\"2 + 2 =\", 2 + 2)\n" +
        "    print(\"ready to grow\")\n" +
        "    return 0\n";

    private const string Template2 =
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
        "    i = 0\n" +
        "    while i < 5:\n" +
        "        plant = tick(plant)\n" +
        "        print(\"tick\", plant.age, \"energy\", plant.energy, \"health\", plant.health)\n" +
        "        i = i + 1\n" +
        "    return plant\n";

    private const string Template3 =
        "fn pulse():\n" +
        "    world_add(\"tick\", 1)\n" +
        "    org_add(\"age\", 1)\n" +
        "    org_add(\"energy\", -2.5)\n" +
        "    if org_get(\"energy\", 0) < 8:\n" +
        "        emit_signal(\"low_energy\", intensity: 0.9, radius: 3)\n" +
        "        org_memory_set(\"state\", \"seeking_light\")\n" +
        "    if org_get(\"energy\", 0) < 3:\n" +
        "        org_damage(0.15)\n" +
        "    print(\"tick\", world_get(\"tick\"), \"energy\", org_get(\"energy\"), \"health\", org_get(\"health\"))\n" +
        "    return org_get(\"health\")\n" +
        "\n" +
        "fn main():\n" +
        "    pulse()\n" +
        "    pulse()\n" +
        "    pulse()\n" +
        "    return org_get(\"memory\")\n";
}
