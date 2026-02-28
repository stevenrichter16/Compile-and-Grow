using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GrowlLanguage.Runtime;

public sealed class GrowlTerminalUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InputField sourceInput;
    [SerializeField] private Text outputText;
    [SerializeField] private Button runButton;
    [SerializeField] private Button clearButton;

    [Header("Runtime")]
    [SerializeField] private bool autoInvokeEntryFunction = true;
    [SerializeField] private string entryFunctionName = "main";
    [SerializeField] private int maxLoopIterations = 100000;
    [SerializeField] private bool runOnCtrlEnter = true;
    [SerializeField] private MonoBehaviour runtimeHostComponent;

    private void Awake()
    {
        if (runButton != null)
            runButton.onClick.AddListener(RunCurrentSource);
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearOutput);
    }

    private void OnDestroy()
    {
        if (runButton != null)
            runButton.onClick.RemoveListener(RunCurrentSource);
        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearOutput);
    }

    private void Update()
    {
        if (!runOnCtrlEnter || sourceInput == null || !sourceInput.isFocused)
            return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.Return))
            RunCurrentSource();
    }

    public void RunCurrentSource()
    {
        if (sourceInput == null)
        {
            Debug.LogError("GrowlTerminalUI requires Source InputField.", this);
            return;
        }

        IGrowlRuntimeHost runtimeHost = runtimeHostComponent as IGrowlRuntimeHost;
        if (runtimeHostComponent != null && runtimeHost == null)
        {
            Debug.LogError("Runtime Host Component must implement IGrowlRuntimeHost.", this);
            return;
        }

        RuntimeResult result = GrowlRuntime.Execute(sourceInput.text, new RuntimeOptions
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

        string final = lines.Count == 0 ? "(no output)" : string.Join("\n", lines);

        if (outputText != null)
            outputText.text = final;
        else
            Debug.Log(final, this);
    }

    public void ClearOutput()
    {
        if (outputText != null)
            outputText.text = string.Empty;
    }
}
