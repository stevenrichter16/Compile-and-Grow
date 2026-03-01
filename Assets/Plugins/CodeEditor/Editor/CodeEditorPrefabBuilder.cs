#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using CodeEditor.View;

namespace CodeEditor.EditorTools
{
    public static class CodeEditorPrefabBuilder
    {
        [MenuItem("Tools/CodeEditor/Create Prefab")]
        public static void CreatePrefab()
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("No default TMP font asset found. Import TextMeshPro essentials first.");
                return;
            }

            // Root
            var root = new GameObject("CodeEditor", typeof(RectTransform), typeof(Image));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(800, 500);
            var rootImg = root.GetComponent<Image>();
            rootImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            // ScrollArea
            var scrollArea = CreateChild(root, "ScrollArea", typeof(ScrollRect), typeof(Image), typeof(Mask));
            var scrollAreaRt = scrollArea.GetComponent<RectTransform>();
            SetStretch(scrollAreaRt);
            scrollAreaRt.offsetMin = new Vector2(0, 0);
            scrollAreaRt.offsetMax = new Vector2(-14, 0); // room for scrollbar
            var scrollAreaImg = scrollArea.GetComponent<Image>();
            scrollAreaImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            var mask = scrollArea.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            // Content
            var content = CreateChild(scrollArea, "Content", typeof(RectTransform));
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.sizeDelta = new Vector2(0, 5000); // will be resized by ScrollManager

            // Gutter
            var gutter = CreateChild(content, "Gutter", typeof(RectTransform), typeof(Image));
            var gutterRt = gutter.GetComponent<RectTransform>();
            gutterRt.anchorMin = new Vector2(0, 0);
            gutterRt.anchorMax = new Vector2(0, 1);
            gutterRt.pivot = new Vector2(0, 1);
            gutterRt.sizeDelta = new Vector2(48, 0);
            gutterRt.anchoredPosition = Vector2.zero;
            var gutterImg = gutter.GetComponent<Image>();
            gutterImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            gutterImg.raycastTarget = false;

            // TextArea
            var textArea = CreateChild(content, "TextArea", typeof(RectTransform));
            var textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = new Vector2(0, 0);
            textAreaRt.anchorMax = new Vector2(1, 1);
            textAreaRt.pivot = new Vector2(0, 1);
            textAreaRt.offsetMin = new Vector2(54, 0); // past gutter
            textAreaRt.offsetMax = Vector2.zero;

            // ScrollRect setup
            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = scrollAreaRt;

            // Scrollbar
            var scrollbar = CreateChild(root, "Scrollbar", typeof(Scrollbar), typeof(Image));
            var scrollbarRt = scrollbar.GetComponent<RectTransform>();
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.sizeDelta = new Vector2(14, 0);
            scrollbarRt.anchoredPosition = Vector2.zero;
            var scrollbarComp = scrollbar.GetComponent<Scrollbar>();
            scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
            var scrollbarImg = scrollbar.GetComponent<Image>();
            scrollbarImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Scrollbar handle
            var handle = CreateChild(scrollbar, "Handle", typeof(Image));
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = Vector2.zero;
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);

            // Scrollbar sliding area
            var slidingArea = CreateChild(scrollbar, "SlidingArea", typeof(RectTransform));
            var slidingRt = slidingArea.GetComponent<RectTransform>();
            SetStretch(slidingRt);
            handle.transform.SetParent(slidingArea.transform, false);
            SetStretch(handleRt);

            scrollbarComp.handleRect = handleRt;
            scrollbarComp.targetGraphic = handleImg;
            scrollRect.verticalScrollbar = scrollbarComp;

            // HiddenInput (invisible TMP_InputField for keyboard capture)
            var hiddenInput = CreateChild(root, "HiddenInput", typeof(RectTransform), typeof(CanvasRenderer));
            var hiddenRt = hiddenInput.GetComponent<RectTransform>();
            SetStretch(hiddenRt);

            // TMP_InputField needs a text area child
            var inputTextArea = CreateChild(hiddenInput, "Text Area", typeof(RectTransform), typeof(RectMask2D));
            var inputTextAreaRt = inputTextArea.GetComponent<RectTransform>();
            SetStretch(inputTextAreaRt);

            var placeholder = CreateChild(inputTextArea, "Placeholder", typeof(TextMeshProUGUI));
            var placeholderTmp = placeholder.GetComponent<TextMeshProUGUI>();
            placeholderTmp.font = font;
            placeholderTmp.fontSize = 1;
            placeholderTmp.color = new Color(0, 0, 0, 0);
            placeholderTmp.enableWordWrapping = false;
            var placeholderRt = placeholder.GetComponent<RectTransform>();
            SetStretch(placeholderRt);

            var inputText = CreateChild(inputTextArea, "Text", typeof(TextMeshProUGUI));
            var inputTextTmp = inputText.GetComponent<TextMeshProUGUI>();
            inputTextTmp.font = font;
            inputTextTmp.fontSize = 1;
            inputTextTmp.color = new Color(0, 0, 0, 0); // invisible
            inputTextTmp.enableWordWrapping = false;
            var inputTextRt = inputText.GetComponent<RectTransform>();
            SetStretch(inputTextRt);

            var inputField = hiddenInput.AddComponent<TMP_InputField>();
            inputField.textViewport = inputTextAreaRt;
            inputField.textComponent = inputTextTmp;
            inputField.placeholder = placeholderTmp;
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.richText = false;

            // Make input field fully transparent
            var hiddenInputImg = hiddenInput.AddComponent<Image>();
            hiddenInputImg.color = new Color(0, 0, 0, 0);
            inputField.targetGraphic = hiddenInputImg;

            // Add and wire CodeEditorView
            var view = root.AddComponent<CodeEditorView>();
            SetSerializedField(view, "_hiddenInput", inputField);
            SetSerializedField(view, "_scrollRect", scrollRect);
            SetSerializedField(view, "_contentArea", contentRt);
            SetSerializedField(view, "_gutterArea", gutterRt);
            SetSerializedField(view, "_textArea", textAreaRt);
            SetSerializedField(view, "_monoFont", font);

            // Save as prefab
            string prefabPath = "Assets/Plugins/CodeEditor/Prefabs/CodeEditor.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"CodeEditor prefab created at {prefabPath}");
            AssetDatabase.Refresh();
        }

        private static GameObject CreateChild(GameObject parent, string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetSerializedField(Component component, string fieldName, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"Could not find serialized field '{fieldName}' on {component.GetType().Name}");
            }
        }
    }
}
#endif
