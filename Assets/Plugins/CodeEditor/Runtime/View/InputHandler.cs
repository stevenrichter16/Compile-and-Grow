using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeEditor.Completion;
using CodeEditor.Editor;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CodeEditor.View
{
    public sealed class InputHandler : MonoBehaviour
    {
        private EditorController _controller;
        private CodeEditorView _editorView;
        private TMP_InputField _inputField;
        private string _prevText = string.Empty;
        private bool _ignoreNextChange;
        private int _suppressInputFrame = -1;
        private int _deletionHandledFrame = -1;

        public event Action CtrlEnterPressed;

        // Key repeat
        private KeyCode _repeatKey = KeyCode.None;
        private float _repeatTimer;
        private const float RepeatDelay = 0.4f;
        private const float RepeatRate = 0.035f;

        public void Initialize(EditorController controller, TMP_InputField inputField, CodeEditorView editorView = null)
        {
            _controller = controller;
            _inputField = inputField;
            _editorView = editorView;

            _inputField.navigation = new Navigation { mode = Navigation.Mode.None };
            _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            _inputField.onValueChanged.AddListener(OnInputChanged);

            SyncInputFieldFromDocument();
            Debug.Log($"[Input] Initialize: docLines={_controller.Document.LineCount} prevTextLen={_prevText.Length} inputFieldLen={_inputField.text.Length}");
        }

        private int _lastCheckedVersion = -1;

        private void OnDestroy()
        {
            if (_inputField != null)
                _inputField.onValueChanged.RemoveListener(OnInputChanged);
        }

        private void LateUpdate()
        {
            if (_controller == null) return;
            var doc = _controller.Document;
            if (doc.Version != _lastCheckedVersion)
            {
                _lastCheckedVersion = doc.Version;
                string docText = _controller.GetText();
                string inputText = _inputField.text;
                if (docText != inputText)
                {
                    Debug.LogWarning($"[Input] LateUpdate DESYNC: docLen={docText.Length} inputLen={inputText.Length} ver={doc.Version}");
                }
                // Log full document state on every version change
                string firstLine = doc.GetLine(0);
                int wsCount = 0;
                foreach (char c in firstLine) { if (c == ' ' || c == '\t') wsCount++; else break; }
                if (wsCount > 0 || firstLine.Length > 0)
                    Debug.Log($"[Input] DocState ver={doc.Version} lines={doc.LineCount} cursor=({doc.Cursor.Line}:{doc.Cursor.Column}) line0=\"{firstLine}\" leadingWs={wsCount}");
            }
        }

        private string LineStats()
        {
            var doc = _controller.Document;
            var cursor = doc.Cursor;
            string line = doc.GetLine(cursor.Line);
            int ws = line.Count(c => c == ' ' || c == '\t');
            int nonWs = line.Length - ws;
            return $"cursor=({cursor.Line}:{cursor.Column}) lineLen={line.Length} ws={ws} nonWs={nonWs} ver={doc.Version}";
        }

        private string SelectionStats()
        {
            var doc = _controller.Document;
            var range = doc.SelectionRange;
            if (range.IsEmpty) return "sel=none";
            return $"sel=({range.Start.Line}:{range.Start.Column})->({range.End.Line}:{range.End.Column})";
        }

        private void Update()
        {
            if (_controller == null || _inputField == null) return;
            if (!_inputField.isFocused) return;

            bool ctrl = IsCtrlHeld();
            bool shift = IsShiftHeld();

            if (ctrl)
            {
                if (GetKeyDown(KeyCode.Z))
                {
                    if (shift)
                    {
                        _controller.Redo();
                        Debug.Log($"[Input] Ctrl+Shift+Z (Redo) | {LineStats()} | {SelectionStats()}");
                    }
                    else
                    {
                        _controller.Undo();
                        Debug.Log($"[Input] Ctrl+Z (Undo) | {LineStats()} | {SelectionStats()}");
                    }
                    SyncAndSuppressFrame();
                    return;
                }
                if (GetKeyDown(KeyCode.Y))
                {
                    _controller.Redo();
                    Debug.Log($"[Input] Ctrl+Y (Redo) | {LineStats()} | {SelectionStats()}");
                    SyncAndSuppressFrame();
                    return;
                }
                if (GetKeyDown(KeyCode.C))
                {
                    string text = _controller.GetSelectedText();
                    if (!string.IsNullOrEmpty(text))
                        GUIUtility.systemCopyBuffer = text;
                    Debug.Log($"[Input] Ctrl+C (Copy) | copied={text?.Length ?? 0} chars | {SelectionStats()}");
                    return;
                }
                if (GetKeyDown(KeyCode.X))
                {
                    string text = _controller.GetSelectedText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        GUIUtility.systemCopyBuffer = text;
                        _controller.Cut();
                        SyncAndSuppressFrame();
                    }
                    Debug.Log($"[Input] Ctrl+X (Cut) | cut={text?.Length ?? 0} chars | {LineStats()} | {SelectionStats()}");
                    return;
                }
                if (GetKeyDown(KeyCode.V))
                {
                    string clip = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(clip))
                    {
                        _controller.Paste(clip);
                        SyncAndSuppressFrame();
                    }
                    Debug.Log($"[Input] Ctrl+V (Paste) | pasted={clip?.Length ?? 0} chars | {LineStats()}");
                    return;
                }
                if (GetKeyDown(KeyCode.A))
                {
                    _controller.SelectAll();
                    SyncSelectionToInputField();
                    Debug.Log($"[Input] Ctrl+A (SelectAll) | {SelectionStats()} | totalLines={_controller.Document.LineCount}");
                    return;
                }
                if (GetKeyDown(KeyCode.Space))
                {
                    _editorView?.TriggerCompletion(force: true);
                    Debug.Log("[Input] Ctrl+Space (TriggerCompletion)");
                    return;
                }
                // Ctrl+= / Ctrl+- to adjust font size
                if (_editorView != null)
                {
                    if (GetKeyDown(KeyCode.Equals) || GetKeyDown(KeyCode.KeypadPlus))
                    {
                        _editorView.SetFontSize(_editorView.FontSize + 2f);
                        Debug.Log($"[Input] Ctrl+= (FontSize+) | fontSize={_editorView.FontSize}");
                        return;
                    }
                    if (GetKeyDown(KeyCode.Minus) || GetKeyDown(KeyCode.KeypadMinus))
                    {
                        _editorView.SetFontSize(_editorView.FontSize - 2f);
                        Debug.Log($"[Input] Ctrl+- (FontSize-) | fontSize={_editorView.FontSize}");
                        return;
                    }
                }
            }

            // ── Completion popup interception ──────────────────────────────
            bool popupVisible = _editorView != null
                && _editorView.CompletionPopup != null
                && _editorView.CompletionPopup.IsVisible;

            bool hintVisible = _editorView != null
                && _editorView.SignatureHintPopup != null
                && _editorView.SignatureHintPopup.IsVisible;

            if ((popupVisible || hintVisible) && GetKeyDown(KeyCode.Escape))
            {
                if (popupVisible) _editorView.DismissCompletion();
                if (hintVisible) _editorView.DismissSignatureHint();
                Debug.Log("[Input] Escape (DismissPopups)");
                return;
            }

            if (HandleRepeatable(KeyCode.Tab))
            {
                if (popupVisible)
                {
                    _editorView.CompletionPopup.AcceptSelected();
                    SyncAndSuppressFrame();
                    Debug.Log("[Input] Tab (AcceptCompletion)");
                    return;
                }
                _controller.Tab(shift);
                SyncAndSuppressFrame();
                return;
            }

            if (HandleRepeatable(KeyCode.LeftArrow))
            {
                _controller.MoveCursor(MoveDirection.Left, shift, word: ctrl);
                SyncSelectionToInputField();
                if (popupVisible) _editorView.DismissCompletion();
                return;
            }
            if (HandleRepeatable(KeyCode.RightArrow))
            {
                _controller.MoveCursor(MoveDirection.Right, shift, word: ctrl);
                SyncSelectionToInputField();
                if (popupVisible) _editorView.DismissCompletion();
                return;
            }
            if (HandleRepeatable(KeyCode.UpArrow))
            {
                if (popupVisible)
                {
                    _editorView.CompletionPopup.MoveSelection(-1);
                    return;
                }
                _controller.MoveCursor(MoveDirection.Up, shift);
                SyncSelectionToInputField();
                return;
            }
            if (HandleRepeatable(KeyCode.DownArrow))
            {
                if (popupVisible)
                {
                    _editorView.CompletionPopup.MoveSelection(1);
                    return;
                }
                _controller.MoveCursor(MoveDirection.Down, shift);
                SyncSelectionToInputField();
                return;
            }

            if (HandleRepeatable(KeyCode.Home))
            {
                _controller.MoveCursor(ctrl ? MoveDirection.DocumentStart : MoveDirection.Home, shift);
                SyncSelectionToInputField();
                return;
            }
            if (HandleRepeatable(KeyCode.End))
            {
                _controller.MoveCursor(ctrl ? MoveDirection.DocumentEnd : MoveDirection.End, shift);
                SyncSelectionToInputField();
                return;
            }

            if (HandleRepeatable(KeyCode.PageUp))
            {
                _controller.MoveCursor(MoveDirection.PageUp, shift);
                SyncSelectionToInputField();
                return;
            }
            if (HandleRepeatable(KeyCode.PageDown))
            {
                _controller.MoveCursor(MoveDirection.PageDown, shift);
                SyncSelectionToInputField();
                return;
            }

            if (HandleRepeatable(KeyCode.Backspace))
            {
                if (Time.frameCount == _deletionHandledFrame)
                {
                    SyncAndSuppressFrame();
                    return;
                }
                _controller.Backspace();
                SyncAndSuppressFrame();
                // Re-trigger completion/hint with updated prefix, or dismiss if prefix gone
                if (popupVisible) _editorView.TriggerCompletion(force: false);
                if (hintVisible) _editorView.TriggerSignatureHint();
                return;
            }
            if (HandleRepeatable(KeyCode.Delete))
            {
                if (Time.frameCount == _deletionHandledFrame)
                {
                    SyncAndSuppressFrame();
                    return;
                }
                _controller.Delete();
                SyncAndSuppressFrame();
                return;
            }

            if (GetKeyDown(KeyCode.Return) || GetKeyDown(KeyCode.KeypadEnter))
            {
                if (popupVisible)
                {
                    _editorView.CompletionPopup.AcceptSelected();
                    SyncAndSuppressFrame();
                    Debug.Log("[Input] Enter (AcceptCompletion)");
                    return;
                }

                if (ctrl)
                {
                    CtrlEnterPressed?.Invoke();
                    SyncAndSuppressFrame();
                    return;
                }

                _controller.Enter();
                SyncAndSuppressFrame();
                return;
            }
        }

        private void NotifyCompletionAfterTyping(char lastChar)
        {
            if (_editorView == null) return;

            bool isWordChar = char.IsLetterOrDigit(lastChar) || lastChar == '_';
            bool isDot = lastChar == '.';

            if (isDot)
            {
                // Dot-access: trigger immediately
                _editorView.TriggerCompletion(force: false);
                _editorView.DismissSignatureHint();
            }
            else if (lastChar == '(')
            {
                // Open paren: dismiss completion, show signature hint
                _editorView.DismissCompletion();
                _editorView.TriggerSignatureHint();
            }
            else if (lastChar == ')')
            {
                // Close paren: dismiss signature hint, re-check if we're still inside an outer call
                _editorView.DismissSignatureHint();
                _editorView.TriggerSignatureHint();
            }
            else if (lastChar == ',')
            {
                // Comma: update active parameter in signature hint
                _editorView.TriggerSignatureHint();
            }
            else if (isWordChar)
            {
                // Identifier character: trigger completion if prefix is >= 2 chars,
                // or update filter if popup is already visible
                var (prefix, _) = _controller.GetWordPrefixAtCursor();
                if (prefix.Length >= 2 || (_editorView.CompletionPopup != null && _editorView.CompletionPopup.IsVisible))
                {
                    _editorView.TriggerCompletion(force: false);
                }
                // Also keep signature hint updated while typing inside a call
                if (_editorView.SignatureHintPopup != null && _editorView.SignatureHintPopup.IsVisible)
                {
                    _editorView.TriggerSignatureHint();
                }
            }
            else
            {
                // Non-identifier, non-dot: dismiss completion
                _editorView.DismissCompletion();
            }
        }

        private void OnInputChanged(string newValue)
        {
            // Suppress the synchronous callback from our own _inputField.text = ... calls
            if (_ignoreNextChange)
            {
                _ignoreNextChange = false;
                Debug.Log($"[Input] OnInputChanged IGNORED (our own sync) newLen={newValue.Length} prevLen={_prevText.Length}");
                return;
            }

            // Suppress late callbacks from TMP_InputField's own processing of keys
            // we already handled in Update() this frame (Tab, Enter, Backspace, etc.).
            // Revert TMP's text to keep it in sync with the document.
            if (Time.frameCount == _suppressInputFrame)
            {
                Debug.Log($"[Input] OnInputChanged SUPPRESSED frame={Time.frameCount} newLen={newValue.Length} prevLen={_prevText.Length}");
                if (_inputField.text != _prevText)
                {
                    _ignoreNextChange = true;
                    _inputField.text = _prevText;
                }
                return;
            }

            if (newValue.Length > _prevText.Length)
            {
                // If Ctrl/Cmd is held, this is a paste or other shortcut —
                // revert TMP's change and let Update() handle it to avoid double-processing
                if (IsCtrlHeld())
                {
                    Debug.Log($"[Input] OnInputChanged: Ctrl held, reverting TMP change (likely paste). newLen={newValue.Length} prevLen={_prevText.Length}");
                    _ignoreNextChange = true;
                    _inputField.text = _prevText;
                    return;
                }

                int added = newValue.Length - _prevText.Length;
                int pos = _inputField.caretPosition - added;
                if (pos >= 0 && pos + added <= newValue.Length)
                {
                    string typed = newValue.Substring(pos, added);

                    // Filter out characters that are handled as special keys in Update()
                    // (Tab, Enter, etc.) to prevent double-processing, since
                    // EventSystem.Update runs before InputHandler.Update in the same frame.
                    string filtered = "";
                    foreach (char ch in typed)
                    {
                        if (ch != '\t' && ch != '\n' && ch != '\r')
                            filtered += ch;
                    }

                    _ignoreNextChange = true;
                    _inputField.text = _prevText;

                    if (filtered.Length > 0)
                    {
                        foreach (char c in filtered)
                            _controller.TypeCharacter(c);

                        SyncInputFieldFromDocument();

                        // Auto-trigger completion after typing
                        char lastChar = filtered[filtered.Length - 1];
                        NotifyCompletionAfterTyping(lastChar);

                        var doc = _controller.Document;
                        string curLine = doc.GetLine(doc.Cursor.Line);
                        int ws = curLine.Count(c => c == ' ' || c == '\t');
                        int nonWs = curLine.Length - ws;
                        string charCodes = string.Join(",", filtered.Select(c => $"0x{(int)c:X2}"));
                        Debug.Log($"[Input] Typed \"{filtered}\" [{charCodes}] | {LineStats()} | lineText=\"{curLine}\" ws={ws} nonWs={nonWs} | prevTextLen={_prevText.Length} inputFieldLen={_inputField.text.Length}");
                    }
                    else
                    {
                        // All typed chars were special keys — just revert TMP and let Update() handle them
                        SyncInputFieldFromDocument();
                        Debug.Log($"[Input] OnInputChanged: filtered out special key chars from \"{typed.Replace("\t","\\t").Replace("\n","\\n").Replace("\r","\\r")}\"");
                    }
                    return;
                }
                else
                {
                    Debug.LogWarning($"[Input] OnInputChanged: char detection out of bounds — added={added} pos={pos} caretPos={_inputField.caretPosition} newLen={newValue.Length} prevLen={_prevText.Length}");
                    // Revert TMP to prevent untracked text changes
                    _ignoreNextChange = true;
                    _inputField.text = _prevText;
                    return;
                }
            }
            else if (newValue.Length < _prevText.Length)
            {
                // If Ctrl/Cmd is held, this is a cut or other shortcut —
                // revert TMP's change and let Update() handle it
                if (IsCtrlHeld())
                {
                    Debug.Log($"[Input] OnInputChanged: Ctrl held, reverting TMP deletion (likely cut). newLen={newValue.Length} prevLen={_prevText.Length}");
                    _ignoreNextChange = true;
                    _inputField.text = _prevText;
                    return;
                }

                // TMP processed Backspace/Delete before our Update().
                // Handle the deletion through our controller now, and flag
                // this frame so Update() doesn't double-process it.
                bool isBackspace = IsKeyPressed(KeyCode.Backspace);
                bool isDelete = IsKeyPressed(KeyCode.Delete);

                _ignoreNextChange = true;
                _inputField.text = _prevText;

                if (isBackspace)
                    _controller.Backspace();
                else if (isDelete)
                    _controller.Delete();
                else
                    _controller.Backspace(); // fallback — most common case

                SyncInputFieldFromDocument();
                _deletionHandledFrame = Time.frameCount;

                var doc = _controller.Document;
                string curLine = doc.GetLine(doc.Cursor.Line);
                Debug.Log($"[Input] {(isBackspace ? "Backspace" : "Delete")} (via TMP) | {LineStats()} | lineText=\"{curLine}\"");
                return;
            }
            else
            {
                Debug.Log($"[Input] OnInputChanged: same length — newLen={newValue.Length} prevLen={_prevText.Length} (possible replacement)");
            }

            _prevText = newValue;
        }

        /// <summary>
        /// Syncs the hidden input field text and caret from the document model.
        /// Called after character input processing (from OnInputChanged) and
        /// after special key handling (from Update via SyncAndSuppressFrame).
        /// </summary>
        public void SyncInputFieldFromDocument()
        {
            string text = _controller.GetText();
            if (_inputField.text != text)
            {
                Debug.Log($"[Input] SyncFromDoc: inputField changing len={_inputField.text.Length}->{text.Length} text=\"{text.Replace("\n","\\n")}\"");
                _ignoreNextChange = true;
                _inputField.text = text;
            }
            _prevText = text;
            SyncSelectionToInputField();
        }

        /// <summary>
        /// Syncs the input field AND suppresses any TMP_InputField callbacks for the
        /// rest of this frame. Only called from Update() after handling special keys
        /// (Tab, Enter, Backspace, etc.) to prevent TMP from double-processing them.
        /// </summary>
        private void SyncAndSuppressFrame()
        {
            SyncInputFieldFromDocument();
            _suppressInputFrame = Time.frameCount;
            Debug.Log($"[Input] SyncAndSuppress frame={Time.frameCount}");
        }

        private void SyncSelectionToInputField()
        {
            var doc = _controller.Document;
            int caretOffset = doc.GetCharOffset(doc.Cursor);
            int anchorOffset = doc.GetCharOffset(doc.SelectionAnchor);

            _inputField.caretPosition = Mathf.Clamp(caretOffset, 0, _inputField.text.Length);
            _inputField.selectionAnchorPosition = Mathf.Clamp(anchorOffset, 0, _inputField.text.Length);
            _inputField.selectionFocusPosition = Mathf.Clamp(caretOffset, 0, _inputField.text.Length);
        }

        private static bool IsCtrlHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed;
#else
            return kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
#endif
#elif ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
#else
            return false;
#endif
        }

        private static bool IsShiftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private bool HandleRepeatable(KeyCode key)
        {
            if (GetKeyDown(key))
            {
                _repeatKey = key;
                _repeatTimer = RepeatDelay;
                return true;
            }

            if (_repeatKey == key && IsKeyPressed(key))
            {
                _repeatTimer -= Time.unscaledDeltaTime;
                if (_repeatTimer <= 0f)
                {
                    _repeatTimer = RepeatRate;
                    return true;
                }
            }
            else if (_repeatKey == key)
            {
                _repeatKey = KeyCode.None;
            }

            return false;
        }

        private static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            var control = KeyCodeToInputSystem(key);
            return control != null && control.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        private static bool IsKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            var control = KeyCodeToInputSystem(key);
            return control != null && control.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static UnityEngine.InputSystem.Controls.KeyControl KeyCodeToInputSystem(KeyCode key)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            switch (key)
            {
                case KeyCode.Z: return kb.zKey;
                case KeyCode.Y: return kb.yKey;
                case KeyCode.C: return kb.cKey;
                case KeyCode.X: return kb.xKey;
                case KeyCode.V: return kb.vKey;
                case KeyCode.A: return kb.aKey;
                case KeyCode.Tab: return kb.tabKey;
                case KeyCode.LeftArrow: return kb.leftArrowKey;
                case KeyCode.RightArrow: return kb.rightArrowKey;
                case KeyCode.UpArrow: return kb.upArrowKey;
                case KeyCode.DownArrow: return kb.downArrowKey;
                case KeyCode.Home: return kb.homeKey;
                case KeyCode.End: return kb.endKey;
                case KeyCode.PageUp: return kb.pageUpKey;
                case KeyCode.PageDown: return kb.pageDownKey;
                case KeyCode.Backspace: return kb.backspaceKey;
                case KeyCode.Delete: return kb.deleteKey;
                case KeyCode.Return: return kb.enterKey;
                case KeyCode.KeypadEnter: return kb.numpadEnterKey;
                case KeyCode.Escape: return kb.escapeKey;
                case KeyCode.Space: return kb.spaceKey;
                default: return null;
            }
        }
#endif
    }
}
