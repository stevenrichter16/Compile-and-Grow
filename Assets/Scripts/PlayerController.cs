using UnityEngine;
using CodeEditor.View;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 0.5f;
    [SerializeField] private GameObject codeEditorRoot;
    [SerializeField] private SpriteRenderer promptRenderer;

    private CodeEditorView _editorView;
    private bool _inputLocked;
    private TerminalInteractable _nearestTerminal;

    private void Start()
    {
        if (codeEditorRoot != null)
        {
            _editorView = codeEditorRoot.GetComponentInChildren<CodeEditorView>(true);
            codeEditorRoot.SetActive(false);
        }

        if (promptRenderer != null)
            promptRenderer.enabled = false;
    }

    private void Update()
    {
        if (_inputLocked)
        {
            if (IsEscapePressed())
                CloseTerminal();
            return;
        }

        HandleMovement();
        HandleInteraction();
    }

    private void HandleMovement()
    {
        Vector2 input = ReadMoveInput();
        if (input.sqrMagnitude < 0.001f)
            return;

        Vector3 delta = new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
        transform.position += delta;
    }

    private void HandleInteraction()
    {
        _nearestTerminal = FindNearestTerminal();

        if (promptRenderer != null)
        {
            if (_nearestTerminal != null)
            {
                promptRenderer.enabled = true;
                Vector3 termPos = _nearestTerminal.transform.position;
                promptRenderer.transform.position = new Vector3(termPos.x, termPos.y + 0.3f, termPos.z - 0.01f);
            }
            else
            {
                promptRenderer.enabled = false;
            }
        }

        if (_nearestTerminal != null && IsTKeyPressed())
            OpenTerminal();
    }

    private void OpenTerminal()
    {
        if (codeEditorRoot == null)
            return;

        _inputLocked = true;
        codeEditorRoot.SetActive(true);

        if (_editorView != null)
            _editorView.Focus();

        if (promptRenderer != null)
            promptRenderer.enabled = false;
    }

    private void CloseTerminal()
    {
        if (codeEditorRoot != null)
            codeEditorRoot.SetActive(false);

        _inputLocked = false;
    }

    private TerminalInteractable FindNearestTerminal()
    {
        TerminalInteractable[] terminals = FindObjectsOfType<TerminalInteractable>();
        TerminalInteractable closest = null;
        float closestDist = interactRadius;

        for (int i = 0; i < terminals.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, terminals[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = terminals[i];
            }
        }

        return closest;
    }

    private static Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return Vector2.zero;

        float x = 0f, y = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.wKey.isPressed) y += 1f;
        if (kb.sKey.isPressed) y -= 1f;

        if (x != 0f && y != 0f)
        {
            float inv = 1f / Mathf.Sqrt(2f);
            x *= inv;
            y *= inv;
        }

        return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    private static bool IsTKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return false;

        if (kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed ||
            kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed)
            return false;

        return kb.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            return false;

        return Input.GetKeyDown(KeyCode.T);
#else
        return false;
#endif
    }

    private static bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
