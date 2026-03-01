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
}
