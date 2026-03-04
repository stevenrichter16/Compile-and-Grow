# Auto-Complete / Auto-Suggest for Growl in the TPM Editor

## Executive Summary

The TPM code editor already has a clean architecture (DocumentModel → EditorController → CodeEditorView) with an `ILanguageService` extension point. Auto-complete can be added **without any WebView, LSP server, or external dependency** by:

1. Adding an `ICompletionProvider` interface to the CodeEditor plugin
2. Implementing a `GrowlCompletionProvider` that knows Growl's keywords, builtins, and user-defined symbols
3. Building a `CompletionPopup` UI component (a small `ScrollRect` with selectable items) that appears near the caret
4. Wiring trigger logic into `InputHandler` (on character typed / Ctrl+Space) and `EditorController` (to extract the current word prefix)

---

## Architecture Overview

### Current System (relevant components)

```
InputHandler          — captures keyboard input (Update loop + TMP_InputField)
  ↓
EditorController      — document editing commands, cursor movement, word-boundary logic
  ↓
DocumentModel         — line-based text buffer, cursor/selection state, version tracking
  ↓
CodeEditorView        — coordinates all visual sub-components, owns the LinePool
  ↓
LinePool              — virtual-scrolled TMP_Text pool, provides pixel positions
```

**Key integration points:**

| Component | File | What it provides for auto-complete |
|-----------|------|------------------------------------|
| `EditorController.GetWordBoundsAt()` | `EditorController.cs:406` | Extracts the word under/before the cursor — perfect for getting the current prefix |
| `EditorController.TypeCharacter()` | `EditorController.cs:40` | Hook point: after a character is typed, check if completion should trigger |
| `InputHandler.Update()` | `InputHandler.cs:93` | Where Ctrl+Space can be intercepted to force-open completions |
| `LinePool.GetPixelPosition()` | `LinePool.cs:86` | Converts a `TextPosition` → pixel coords for positioning the popup |
| `CodeEditorView.SyncView()` | `CodeEditorView.cs:327` | Natural place to update/reposition the completion popup after edits |
| `ILanguageService` | `ILanguageService.cs:34` | Extension interface — completion can be added here or as a parallel interface |
| Growl `Lexer.s_keywords` | `Lexer.cs:612` | The definitive keyword list (55 keywords) |
| `GrowlMathBuiltins` | `GrowlMathBuiltins.cs` | 15 global math functions + `math.*` namespace |
| `GrowlRandomBuiltins` | `GrowlRandomBuiltins.cs` | 5 global random functions |
| `GrowlBioBuiltins` | `GrowlBioBuiltins.cs` | 5 biological timing functions |
| `GrowlConstants` | `GrowlConstants.cs` | 11 global constants (UP, DOWN, TICK, etc.) |
| `Interpreter.RegisterBuiltins()` | `Interpreter.cs:517` | `print`, `log`, `len`, `type`, `warn`, `error` |

---

## Approach: In-Process Prefix-Matched Completion

Since Growl is a custom DSL with a known, bounded vocabulary, full LSP is overkill. The right approach (confirmed by `research_editor.md:71-76`) is:

> Maintain a dictionary of known keywords, function signatures, and player-defined identifiers.
> Filter by prefix with fuzzy subsequence matching. This runs in microseconds for lists under 1,000 items.

### Completion Sources (what appears in the popup)

#### 1. Static completions (known at compile time)

| Category | Items | Source |
|----------|-------|--------|
| **Keywords** | `fn`, `class`, `struct`, `enum`, `trait`, `if`, `elif`, `else`, `for`, `in`, `while`, `loop`, `break`, `continue`, `return`, `yield`, `match`, `case`, `try`, `recover`, `always`, `and`, `or`, `not`, `is`, `const`, `type`, `module`, `import`, `from`, `as`, `self`, `super`, `cls`, `phase`, `when`, `then`, `respond`, `to`, `adapt`, `toward`, `rate`, `otherwise`, `cycle`, `at`, `period`, `ticker`, `every`, `wait`, `defer`, `until`, `mutate`, `by`, `abstract`, `static`, `mixin`, `true`, `false`, `none` | `Lexer.s_keywords` |
| **Global builtins** | `print`, `log`, `len`, `type`, `warn`, `error`, `str` | `Interpreter.cs` |
| **Math builtins** | `min`, `max`, `abs`, `round`, `sqrt`, `sin`, `cos`, `tan`, `clamp`, `lerp`, `remap`, `floor`, `ceil`, `pow` | `GrowlMathBuiltins.cs` |
| **Random builtins** | `random`, `random_int`, `random_choice`, `noise`, `chance` | `GrowlRandomBuiltins.cs` |
| **Bio builtins** | `every`, `after`, `between`, `season`, `time_of_day` | `GrowlBioBuiltins.cs` |
| **Constants** | `UP`, `DOWN`, `LEFT`, `RIGHT`, `NORTH`, `SOUTH`, `EAST`, `WEST`, `NONE`, `TICK`, `SELF` | `GrowlConstants.cs` |
| **Namespaces** | `math` (with sub-entries: `math.PI`, `math.E`, `math.TAU`, `math.INF`, `math.sin`, `math.cos`, etc.) | `GrowlMathBuiltins.cs` |
| **Literals** | `true`, `false`, `none` | keywords |

#### 2. Context-sensitive completions (dot-access methods)

When the user types `someVar.`, the system should offer type-appropriate methods:

| Receiver type | Methods | Source |
|---------------|---------|--------|
| **String** | `split`, `join`, `upper`, `lower`, `trim`, `contains`, `startswith`, `endswith`, `replace`, `format`, `indexOf` | `GrowlStringMethods.cs` |
| **List** | `push`, `pop`, `insert`, `remove`, `contains`, `sort`, `reverse`, `map`, `filter`, `reduce`, `each`, `any`, `all`, `find`, `flatten`, `zip`, `unique`, `count`, `min`, `max`, `sum`, `avg`, `sample`, `shuffle`, `enumerate`, `indexOf` | `GrowlListMethods.cs` |
| **Dict** | `keys`, `values`, `entries`, `has`, `remove`, `merge`, `get` | `GrowlDictMethods.cs` |
| **Set** | `add`, `remove`, `contains` | `GrowlSetMethods.cs` |
| **math namespace** | `PI`, `E`, `TAU`, `INF`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan2`, `sqrt`, `abs`, `floor`, `ceil`, `round`, `log`, `log2`, `log10`, `pow`, `radians`, `degrees`, `sigmoid`, `smoothstep`, `map_range` | `GrowlMathBuiltins.cs` |

#### 3. Dynamic completions (user-defined symbols)

Scan the current document for identifiers the user has defined:
- **Function names**: lines matching `fn <name>(`
- **Variable names**: lines matching `<name> =` (simple assignment at start of line or after indent)
- **Class/struct names**: lines matching `class <name>`, `struct <name>`, etc.
- **Parameters**: within a `fn` block, the parameter names

This can be done with a lightweight scan of the document text (regex or manual parse), not a full AST walk — fast enough to run on every keystroke.

---

## Proposed New Files & Changes

### New files

```
Assets/Plugins/CodeEditor/Runtime/Completion/
├── CompletionItem.cs          — data struct: label, detail, kind, insertText
├── CompletionResult.cs        — list of items + the prefix range being replaced
├── ICompletionProvider.cs     — interface: GetCompletions(doc, cursor) → CompletionResult
└── CompletionPopup.cs         — MonoBehaviour: ScrollRect popup with selectable items

Assets/GrowlLanguage/Editor/
└── GrowlCompletionProvider.cs — implements ICompletionProvider for Growl
```

### Modified files

| File | Change |
|------|--------|
| `EditorController.cs` | Add `GetWordPrefixAtCursor()` method (reuse existing `GetWordBoundsAt` logic) |
| `InputHandler.cs` | Add Ctrl+Space handler; after `TypeCharacter`, notify completion system |
| `CodeEditorView.cs` | Hold a reference to `CompletionPopup`, create it in `InitializeSubComponents()`, wire events |
| `ILanguageService.cs` | (Optional) Add `ICompletionProvider GetCompletionProvider()` or keep it as a separate interface set on `CodeEditorView` |

---

## Detailed Design

### 1. CompletionItem

```csharp
namespace CodeEditor.Completion
{
    public enum CompletionKind
    {
        Keyword,
        Function,
        Variable,
        Constant,
        Method,      // dot-access method
        Property,    // dot-access property (e.g. math.PI)
        Snippet,     // multi-character template
    }

    public readonly struct CompletionItem
    {
        public readonly string Label;       // displayed in popup
        public readonly string InsertText;  // text to insert (defaults to Label)
        public readonly string Detail;      // short description shown beside label
        public readonly CompletionKind Kind;

        public CompletionItem(string label, CompletionKind kind,
                              string detail = null, string insertText = null)
        {
            Label = label;
            Kind = kind;
            Detail = detail;
            InsertText = insertText ?? label;
        }
    }
}
```

### 2. ICompletionProvider

```csharp
namespace CodeEditor.Completion
{
    public interface ICompletionProvider
    {
        CompletionResult GetCompletions(DocumentModel doc, TextPosition cursor);
    }
}
```

### 3. Trigger Logic (in InputHandler)

Completions should trigger:
- **Automatically** after typing an identifier character (`a-z`, `A-Z`, `_`, `0-9`) when a word prefix of 2+ characters exists
- **Automatically** after typing `.` (dot-access completions)
- **Manually** on `Ctrl+Space` (force open, even with empty prefix)
- **Dismiss** on `Escape`, clicking outside, or typing a non-identifier character that isn't `.`

```
On character typed:
  1. Get word prefix at cursor via EditorController.GetWordPrefixAtCursor()
  2. If prefix.Length >= 2 OR char == '.':
       results = completionProvider.GetCompletions(doc, cursor)
       if results.Items.Count > 0:
           completionPopup.Show(results, caretPixelPosition)
       else:
           completionPopup.Hide()
  3. If popup is visible and user typed a non-identifier char (except '.'):
       completionPopup.Hide()

On Ctrl+Space:
  results = completionProvider.GetCompletions(doc, cursor)
  completionPopup.Show(results, caretPixelPosition)  // even if empty prefix

On Tab or Enter (while popup visible):
  Accept selected completion → replace prefix range with InsertText
  completionPopup.Hide()

On Escape:
  completionPopup.Hide()

On Up/Down arrow (while popup visible):
  Navigate popup selection instead of moving cursor
```

### 4. CompletionPopup (Unity UI)

Structure:
```
CompletionPopup (GameObject)
├── Background (Image, dark semi-transparent)
├── ScrollRect
│   └── Content (VerticalLayoutGroup)
│       ├── CompletionRow_0 (Button + TMP_Text for label + TMP_Text for detail)
│       ├── CompletionRow_1
│       └── ... (pool of ~8-10 rows, recycled)
└── (optional) DetailPanel for extended docs
```

Key behaviors:
- **Positioning**: Anchored to the caret position via `LinePool.GetPixelPosition()`, offset below/above the current line depending on available space
- **Sizing**: Width auto-sized to longest item (capped at ~300px), height capped at ~8 items with scroll
- **Selection**: Highlighted row follows keyboard (Up/Down), mouse hover also selects
- **Filtering**: As the user continues typing, the list filters in real-time by the growing prefix
- **Insertion**: On accept (Tab/Enter), replace the prefix range in the document with `CompletionItem.InsertText`

### 5. GrowlCompletionProvider

```csharp
public class GrowlCompletionProvider : ICompletionProvider
{
    // Pre-built static completion lists
    private static readonly List<CompletionItem> Keywords = BuildKeywordList();
    private static readonly List<CompletionItem> Builtins = BuildBuiltinList();
    private static readonly List<CompletionItem> Constants = BuildConstantList();
    private static readonly Dictionary<string, List<CompletionItem>> DotCompletions = BuildDotCompletions();

    public CompletionResult GetCompletions(DocumentModel doc, TextPosition cursor)
    {
        string line = doc.GetLine(cursor.Line);
        // Determine if this is a dot-access or a free-standing identifier

        if (IsDotAccess(line, cursor.Column, out string receiver, out string memberPrefix))
        {
            // Look up receiver to determine type, or offer all dot methods
            return FilterDotCompletions(receiver, memberPrefix, cursor);
        }

        // Free-standing identifier completion
        string prefix = GetWordPrefix(line, cursor.Column);
        var items = new List<CompletionItem>();

        // 1. Keywords
        AddMatching(items, Keywords, prefix);

        // 2. Builtins
        AddMatching(items, Builtins, prefix);

        // 3. Constants
        AddMatching(items, Constants, prefix);

        // 4. User-defined symbols (scan document)
        AddMatching(items, ScanUserSymbols(doc), prefix);

        // Sort: exact prefix match first, then alphabetical
        items.Sort((a, b) => ...);

        return new CompletionResult(items, prefixRange);
    }
}
```

### 6. Prefix Matching Strategy

**Simple prefix match** is sufficient and fast for the Growl vocabulary size (~150 static items + user symbols):

```csharp
bool Matches(string candidate, string prefix)
{
    return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
```

Optional enhancement: **fuzzy subsequence matching** (typing `rci` matches `random_choice`) using a scoring algorithm similar to Sublime Text's. This is nice-to-have but not essential for a first pass.

### 7. User-Defined Symbol Scanning

A lightweight scan of the document text to find user-defined names:

```csharp
List<CompletionItem> ScanUserSymbols(DocumentModel doc)
{
    var symbols = new HashSet<string>();
    for (int i = 0; i < doc.LineCount; i++)
    {
        string line = doc.GetLine(i).TrimStart();

        // fn name(...)
        if (line.StartsWith("fn "))
            ExtractIdentifier(line, 3, symbols, CompletionKind.Function);

        // class/struct/enum/trait Name
        foreach (var kw in new[] { "class ", "struct ", "enum ", "trait " })
            if (line.StartsWith(kw))
                ExtractIdentifier(line, kw.Length, symbols, CompletionKind.Variable);

        // name = ...  (assignment)
        int eqIdx = line.IndexOf(" = ");
        if (eqIdx > 0)
        {
            string name = line.Substring(0, eqIdx).Trim();
            if (IsValidIdentifier(name))
                symbols.Add(name);
        }
    }
    return symbols.Select(s => new CompletionItem(s, CompletionKind.Variable)).ToList();
}
```

---

## Integration with Existing Tab Key

Currently, `Tab` in `InputHandler.cs:181` always calls `_controller.Tab(shift)`. When the completion popup is visible, `Tab` should instead accept the selected completion. The flow becomes:

```csharp
if (HandleRepeatable(KeyCode.Tab))
{
    if (_completionPopup != null && _completionPopup.IsVisible)
    {
        _completionPopup.AcceptSelected();
        SyncAndSuppressFrame();
        return;
    }
    _controller.Tab(shift);
    SyncAndSuppressFrame();
    return;
}
```

Similarly for `Enter` and `Up`/`Down` arrows.

---

## Performance Considerations

| Concern | Mitigation |
|---------|------------|
| Completion computed on every keystroke | The candidate list is <200 items; prefix filtering is O(n) string comparison, completes in <0.1ms |
| Document scanning for user symbols | Scan only on first trigger + cache, invalidate on document version change |
| Popup UI updates | Pool 8-10 row GameObjects, only update `.text` on visible rows |
| GC pressure from string building | Use `StringComparison.OrdinalIgnoreCase` for matching; avoid LINQ in hot path |
| Popup positioning on scroll | Reposition in `CodeEditorView.SyncView()` or listen to `CursorMoved` event |

---

## Implementation Order (suggested phases)

### Phase 1: Core infrastructure
- `CompletionItem`, `CompletionResult`, `ICompletionProvider`
- `EditorController.GetWordPrefixAtCursor()` method
- Basic `CompletionPopup` UI (show/hide, render list, accept selection)
- Wire into `InputHandler` (Ctrl+Space trigger only)

### Phase 2: Growl static completions
- `GrowlCompletionProvider` with keywords + builtins + constants
- Automatic trigger on 2+ character prefix
- Tab/Enter to accept, Escape to dismiss
- Up/Down to navigate while popup is visible

### Phase 3: Dot-access completions
- Detect `.` trigger, determine receiver context
- Offer appropriate method list (string/list/dict/set/math)
- For `math.`, offer namespace members

### Phase 4: User-defined symbols
- Document scanning for `fn`, `class`, variable assignments
- Cache with version-based invalidation

### Phase 5: Polish
- Fuzzy matching
- Completion detail/documentation panel
- Visual icons per CompletionKind (colored squares or letters: K, F, V, C, M)
- Scroll to keep selected item visible in popup
- Handle edge cases (popup at bottom of viewport flips above caret)

---

## Alternative Approaches Considered

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Full LSP server in-process** | Standard protocol, reusable | Massive overkill for ~150 static completions; complex | Rejected |
| **Tree-sitter incremental parsing** | Best-in-class accuracy | Requires C native interop, no maintained C# binding | Rejected |
| **Semantic Analyzer integration** | Accurate types for dot-completions | Analyzer requires valid syntax; user is mid-typing | Optional enhancement for Phase 3 |
| **Simple prefix dictionary** (chosen) | Fast, simple, sufficient for Growl's scope | No type inference for dot-access | Best fit |
| **WebView-embedded CodeMirror** | Full IDE features free | Adds dependency, latency, complexity; not cross-platform | Rejected (per research_editor.md) |

---

## Key Insight

The existing codebase already provides almost everything needed:

- **Word boundary detection** → `EditorController.GetWordBoundsAt()` / `IsWordChar()`
- **Pixel positioning** → `LinePool.GetPixelPosition()`
- **Caret tracking** → `DocumentModel.CursorMoved` event
- **Document change notification** → `DocumentModel.Changed` event
- **Complete keyword list** → `Lexer.s_keywords` dictionary
- **Complete builtin list** → all `globals.Define()` calls across runtime files
- **Method lists per type** → `GrowlStringMethods`, `GrowlListMethods`, `GrowlDictMethods`, `GrowlSetMethods`

The work is primarily UI (the popup component) and wiring (connecting triggers to the provider and the popup to the editor).
