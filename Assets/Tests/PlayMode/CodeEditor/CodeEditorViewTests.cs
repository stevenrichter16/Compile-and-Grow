using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using CodeEditor.Core;
using CodeEditor.Editor;
using CodeEditor.View;

namespace CodeEditor.PlayMode.Tests
{
    [TestFixture]
    public class CodeEditorViewTests
    {
        private GameObject _canvas;
        private CodeEditorView _view;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create a Canvas + EventSystem
            _canvas = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Load and instantiate the prefab
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Plugins/CodeEditor/Prefabs/CodeEditor.prefab");
            Assert.IsNotNull(prefab, "CodeEditor prefab not found");

            var instance = Object.Instantiate(prefab, _canvas.transform);
            var rt = instance.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            _view = instance.GetComponent<CodeEditorView>();
            Assert.IsNotNull(_view, "CodeEditorView component not found on prefab");

            // Wait two frames for layout and initialization
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_canvas != null)
                Object.Destroy(_canvas);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AC_View_CaretMovesOnArrowDown()
        {
            _view.Text = "aaa\nbbb\nccc";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var caret = _view.Caret;
            var linePool = _view.Lines;

            // Position cursor at line 0
            controller.Document.SetCursor(new TextPosition(0, 1));
            yield return null;

            Vector2 posLine0 = caret.CurrentPosition;
            Vector2 expectedLine0 = linePool.GetPixelPosition(new TextPosition(0, 1));
            Assert.AreEqual(expectedLine0.x, posLine0.x, 0.1f, "Caret X should match line 0, col 1");
            Assert.AreEqual(expectedLine0.y, posLine0.y, 0.1f, "Caret Y should match line 0");

            // Move down to line 1
            controller.MoveCursor(MoveDirection.Down, shift: false);
            yield return null;

            Vector2 posLine1 = caret.CurrentPosition;
            Vector2 expectedLine1 = linePool.GetPixelPosition(new TextPosition(1, 1));
            Assert.AreEqual(expectedLine1.x, posLine1.x, 0.1f, "Caret X should match line 1, col 1");
            Assert.AreEqual(expectedLine1.y, posLine1.y, 0.1f, "Caret Y should match line 1");

            // Caret should have actually moved
            Assert.AreNotEqual(posLine0.y, posLine1.y, "Caret Y must differ between line 0 and line 1");
        }

        [UnityTest]
        public IEnumerator AC_View_CaretMovesOnArrowLeftRight()
        {
            _view.Text = "hello";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var caret = _view.Caret;
            var linePool = _view.Lines;

            controller.Document.SetCursor(new TextPosition(0, 2));
            yield return null;

            Vector2 posCol2 = caret.CurrentPosition;

            controller.MoveCursor(MoveDirection.Right, shift: false);
            yield return null;

            Vector2 posCol3 = caret.CurrentPosition;
            Vector2 expectedCol3 = linePool.GetPixelPosition(new TextPosition(0, 3));
            Assert.AreEqual(expectedCol3.x, posCol3.x, 0.1f, "Caret X should match col 3");

            Assert.Greater(posCol3.x, posCol2.x, "Caret should move right");
        }

        [UnityTest]
        public IEnumerator AC_View_CaretMovesOnTyping()
        {
            _view.Text = "ab";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var caret = _view.Caret;
            var linePool = _view.Lines;

            controller.Document.SetCursor(new TextPosition(0, 2));
            yield return null;

            Vector2 posBefore = caret.CurrentPosition;

            controller.TypeCharacter('c');
            yield return null;

            Vector2 posAfter = caret.CurrentPosition;
            Vector2 expected = linePool.GetPixelPosition(controller.Document.Cursor);
            Assert.AreEqual(expected.x, posAfter.x, 0.1f, "Caret X should match cursor after typing");
            Assert.Greater(posAfter.x, posBefore.x, "Caret should advance after typing");
        }

        [UnityTest]
        public IEnumerator AC_View_CurrentLineHighlightTracksCaretLine()
        {
            _view.Text = "aaa\nbbb\nccc";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var linePool = _view.Lines;

            // Move to line 0
            controller.Document.SetCursor(new TextPosition(0, 0));
            yield return null;

            // Move to line 2
            controller.MoveCursor(MoveDirection.Down, shift: false);
            controller.MoveCursor(MoveDirection.Down, shift: false);
            yield return null;

            // Verify cursor is on line 2
            Assert.AreEqual(2, controller.Document.Cursor.Line);

            // The caret should be at line 2's Y position
            var caret = _view.Caret;
            Vector2 expectedPos = linePool.GetPixelPosition(new TextPosition(2, 0));
            Assert.AreEqual(expectedPos.y, caret.CurrentPosition.y, 0.1f, "Caret Y should be at line 2");
        }

        [UnityTest]
        public IEnumerator AC_View_ShiftTab_TextUpdatesVisually()
        {
            _view.Text = "hello";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var linePool = _view.Lines;
            var caret = _view.Caret;

            // Place cursor at start and indent with Tab
            controller.Document.SetCursor(new TextPosition(0, 0));
            controller.Tab(shift: false);
            yield return null; // let LateUpdate → SyncView run

            // Verify indent happened in model AND visually
            Assert.AreEqual("    hello", controller.Document.GetLine(0), "Model: line should be indented");
            string displayedAfterTab = linePool.GetDisplayedText(0);
            Assert.AreEqual("    hello", displayedAfterTab, "Visual: displayed text should be indented");

            Vector2 caretAfterTab = caret.CurrentPosition;
            Vector2 expectedAfterTab = linePool.GetPixelPosition(new TextPosition(0, 4));
            Assert.AreEqual(expectedAfterTab.x, caretAfterTab.x, 0.1f, "Caret X should be at col 4 after Tab");

            // Now Shift+Tab to outdent
            controller.Tab(shift: true);
            yield return null; // let LateUpdate → SyncView run

            // Verify outdent in model
            Assert.AreEqual("hello", controller.Document.GetLine(0), "Model: line should be outdented");
            Assert.AreEqual(new TextPosition(0, 0), controller.Document.Cursor, "Model: cursor should be at col 0");

            // Verify outdent visually — this is the bug scenario
            string displayedAfterShiftTab = linePool.GetDisplayedText(0);
            Assert.AreEqual("hello", displayedAfterShiftTab,
                "Visual: displayed text must update after Shift+Tab (bug: text stays indented while caret moves back)");

            Vector2 caretAfterShiftTab = caret.CurrentPosition;
            Vector2 expectedAfterShiftTab = linePool.GetPixelPosition(new TextPosition(0, 0));
            Assert.AreEqual(expectedAfterShiftTab.x, caretAfterShiftTab.x, 0.1f, "Caret X should be at col 0 after Shift+Tab");
        }

        [UnityTest]
        public IEnumerator AC_View_ShiftTab_MultiLine_AllLinesUpdateVisually()
        {
            _view.Text = "    aaa\n    bbb\n    ccc";
            yield return null;
            yield return null;

            var controller = _view.Controller;
            var linePool = _view.Lines;

            // Select all three lines
            controller.Document.SetSelection(new TextPosition(0, 0), new TextPosition(2, 7));
            controller.Tab(shift: true);
            yield return null;

            // Model check
            Assert.AreEqual("aaa", controller.Document.GetLine(0));
            Assert.AreEqual("bbb", controller.Document.GetLine(1));
            Assert.AreEqual("ccc", controller.Document.GetLine(2));

            // Visual check — all lines should show outdented text
            Assert.AreEqual("aaa", linePool.GetDisplayedText(0), "Visual: line 0 should be outdented");
            Assert.AreEqual("bbb", linePool.GetDisplayedText(1), "Visual: line 1 should be outdented");
            Assert.AreEqual("ccc", linePool.GetDisplayedText(2), "Visual: line 2 should be outdented");
        }
    }
}
