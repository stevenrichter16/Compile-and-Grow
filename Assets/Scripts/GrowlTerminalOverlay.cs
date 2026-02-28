using System.Collections.Generic;
using UnityEngine;
using GrowlLanguage.Runtime;

public sealed class GrowlTerminalOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

    [Header("Runtime")]
    [SerializeField] private bool autoInvokeEntryFunction = true;
    [SerializeField] private string entryFunctionName = "main";
    [SerializeField] private int maxLoopIterations = 100000;
    [SerializeField] private MonoBehaviour runtimeHostComponent;

    private Rect _windowRect = new Rect(20f, 20f, 760f, 560f);
    private string _source =
        "fn main():\n" +
        "    print(\"Growl runtime online\")\n" +
        "    return 1\n";
    private string _output = string.Empty;
    private Vector2 _sourceScroll;
    private Vector2 _outputScroll;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Growl Terminal");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Source");
        _sourceScroll = GUILayout.BeginScrollView(_sourceScroll, GUILayout.Height(260f));
        _source = GUILayout.TextArea(_source, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Run", GUILayout.Width(120f)))
            Run();
        if (GUILayout.Button("Clear Output", GUILayout.Width(120f)))
            _output = string.Empty;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("Output");
        _outputScroll = GUILayout.BeginScrollView(_outputScroll, GUILayout.Height(200f));
        GUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.Label("Toggle: " + toggleKey);

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
    }

    private void Run()
    {
        IGrowlRuntimeHost runtimeHost = runtimeHostComponent as IGrowlRuntimeHost;
        if (runtimeHostComponent != null && runtimeHost == null)
        {
            _output = "Runtime Host Component must implement IGrowlRuntimeHost.";
            return;
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
}
