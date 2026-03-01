using System;
using UnityEngine;
using CodeEditor.View;
using CodeEditor.Core;
using CodeEditor.Editor;

public class SpaceKeyTest : MonoBehaviour
{
    [SerializeField] private CodeEditorView _editor;

    private int _frame;
    private EditorController _controller;
    private DocumentModel _doc;
    private int _blankLine = -1;
    private bool _done;

    private void Update()
    {
        if (_done || _editor == null) return;
        _frame++;

        // Wait 5 frames for initialization
        if (_frame < 5) return;

        if (_frame == 5)
        {
            _controller = _editor.Controller;
            if (_controller == null) { Debug.LogError("[SpaceKeyTest] controller null"); _done = true; return; }
            _doc = _controller.Document;
            if (_doc == null) { Debug.LogError("[SpaceKeyTest] doc null"); _done = true; return; }

            // Find blank line
            for (int i = 0; i < _doc.LineCount; i++)
                if (_doc.GetLine(i).Length == 0) { _blankLine = i; break; }

            if (_blankLine < 0)
            {
                Debug.Log("[SpaceKeyTest] No blank line found, inserting one");
                _doc.SetCursor(new TextPosition(0, 0));
                _controller.Enter();
                _doc.SetCursor(new TextPosition(0, 0));
                _blankLine = 0;
            }

            Debug.Log($"[SpaceKeyTest] Setup done. Blank line={_blankLine}, lineCount={_doc.LineCount}");
            _doc.SetCursor(new TextPosition(_blankLine, 0));
        }

        if (_frame == 7)
        {
            string before = _doc.GetLine(_blankLine);
            Debug.Log($"[SpaceKeyTest] Before: line=\"{before}\" cursor={_doc.Cursor}");
            _controller.TypeCharacter(' ');
        }

        if (_frame == 9)
        {
            string line1 = _doc.GetLine(_blankLine);
            bool pass1 = line1 == " ";
            Debug.Log($"[SpaceKeyTest] 1st SPACE: line=\"{line1}\" cursor={_doc.Cursor} {(pass1 ? "PASS" : "FAIL")}");
            _controller.TypeCharacter(' ');
        }

        if (_frame == 11)
        {
            string line2 = _doc.GetLine(_blankLine);
            bool pass2 = line2 == "  ";
            Debug.Log($"[SpaceKeyTest] 2nd SPACE: line=\"{line2}\" cursor={_doc.Cursor} {(pass2 ? "PASS" : "FAIL")}");
            _controller.TypeCharacter(' ');
        }

        if (_frame == 13)
        {
            string line3 = _doc.GetLine(_blankLine);
            bool pass3 = line3 == "   ";
            Debug.Log($"[SpaceKeyTest] 3rd SPACE: line=\"{line3}\" cursor={_doc.Cursor} {(pass3 ? "PASS" : "FAIL")}");

            bool allPass = _doc.GetLine(_blankLine).Length == 3;
            Debug.Log(allPass ? "[SpaceKeyTest] === ALL PASSED ===" : "[SpaceKeyTest] === SOME FAILED ===");
            _done = true;
        }
    }
}
