using UnityEngine;
using CodeEditor.View;

public class CodeEditorTestHarness : MonoBehaviour
{
    [SerializeField] private CodeEditorView _editor;

    private void Start()
    {
        if (_editor == null) return;

        _editor.Text = "";
        _editor.Focus();
    }
}
