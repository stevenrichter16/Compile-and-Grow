using UnityEditor;
using UnityEngine;

public static class SetupPlayer
{
    [MenuItem("Tools/Setup Player Components")]
    public static void Run()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[SetupPlayer] No Player GameObject found.");
            return;
        }

        if (player.GetComponent<PlayerController>() == null)
            player.AddComponent<PlayerController>();

        // Add TerminalInteractable to terminal GOs
        string[] terminalNames = { "Console_Main", "Console_Alt", "Terminal_1", "Terminal_2" };
        foreach (string name in terminalNames)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                var lab = GameObject.Find("ReactorAccessLab");
                if (lab != null)
                    go = lab.transform.Find(name)?.gameObject;
            }
            if (go != null && go.GetComponent<TerminalInteractable>() == null)
                go.AddComponent<TerminalInteractable>();
        }

        // Create interaction prompt child
        var existing = player.transform.Find("InteractionPrompt");
        if (existing == null)
        {
            var prompt = new GameObject("InteractionPrompt");
            prompt.transform.SetParent(player.transform);
            prompt.transform.localPosition = Vector3.zero;
            var sr = prompt.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/prompt_key_t.png");
            sr.sortingOrder = 10;
            sr.enabled = false;

            var pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                var so = new SerializedObject(pc);
                so.FindProperty("promptRenderer").objectReferenceValue = sr;
                so.ApplyModifiedProperties();
            }
        }

        // Wire codeEditorRoot to TerminalCanvas (may be inactive, so search all root GOs)
        var pc2 = player.GetComponent<PlayerController>();
        GameObject canvas = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "TerminalCanvas") { canvas = root; break; }
        }
        if (pc2 != null && canvas != null)
        {
            var so2 = new SerializedObject(pc2);
            so2.FindProperty("codeEditorRoot").objectReferenceValue = canvas;
            so2.ApplyModifiedProperties();
            Debug.Log("[SetupPlayer] Wired codeEditorRoot -> TerminalCanvas");
        }

        // Remove old GrowlTerminalOverlay from Player if present
        var oldOverlay = player.GetComponent<GrowlTerminalOverlay>();
        if (oldOverlay != null)
        {
            Object.DestroyImmediate(oldOverlay);
            Debug.Log("[SetupPlayer] Removed old GrowlTerminalOverlay from Player.");
        }

        // Add GrowlTerminalScreen to TerminalCanvas if not present
        if (canvas != null && canvas.GetComponent<GrowlTerminalScreen>() == null)
        {
            canvas.AddComponent<GrowlTerminalScreen>();
            EditorUtility.SetDirty(canvas);
            Debug.Log("[SetupPlayer] Added GrowlTerminalScreen to TerminalCanvas.");
        }

        EditorUtility.SetDirty(player);
        Debug.Log("[SetupPlayer] Done.");
    }
}
