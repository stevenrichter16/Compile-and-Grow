# Building a production code editor inside Unity

**The most robust path to a VS Code-like editor in Unity depends on your platform targets**: embed Monaco or CodeMirror via a WebView plugin (Vuplex) for desktop builds where you need full IDE features fast, or build a custom TMP-based editor with virtual scrolling and incremental tokenization for maximum control and cross-platform reach. No off-the-shelf Unity solution delivers Monaco-level features today — the best existing asset, InGame Code Editor (IGCE), caps out around 200–500 lines before performance degrades. Every shipped game with a serious code editor either embedded a web-based editor (Bitburner uses Monaco in Electron) or built something deliberately constrained for a custom DSL (Zachtronics games). Your architecture choice will cascade through every subsequent decision, so this report covers all viable approaches, their trade-offs, and the technical details needed to choose well.

## The landscape of existing solutions is thin but informative

The Unity ecosystem has surprisingly few production-ready code editor components. The most mature option is **InGame Code Editor (IGCE)** by Trivial Interactive ($7.50, last updated June 2024). It uses TextMeshPro for rendering, ships with a custom lexer supporting C#, Lua, MiniScript, and JSON, includes optional line numbers and three built-in themes, and is extensible for custom languages. Its core architecture uses the proven overlay pattern: an invisible `TMP_InputField` captures input while a separate `TMP_Text` component displays the syntax-highlighted output. The critical limitation is performance — community reports consistently note degradation beyond **200–500 lines** due to TMP mesh regeneration costs.

On GitHub, the pickings are slimmer. The `my-basic/code_editor_unity` repo (MIT, 14 stars) demonstrates a UGUI-based editor for the MY-BASIC scripting language with configurable keyword colors and line counts. `joshcamas/UnityCodeEditor` (23 stars, 3 commits) is a proof-of-concept syntax highlighting API. The formerly impressive `uCodeEditor` embedded Monaco via CEF in the Unity Editor, but it's **completely broken on Unity 2020+** since Unity removed the internal WebView component.

Unity's UI Toolkit (`TextField`) cannot be used for syntax highlighting — it does not support per-character styling. USS applies to the entire field. This was confirmed across multiple Unity forum threads and remains true as of Unity 6.3 (November 2025). **UGUI + TextMeshPro remains the only viable native Unity approach** for a syntax-highlighted code editor, using the dual-layer overlay pattern.

## Embedding Monaco or CodeMirror via WebView is the fastest path to full features

The web embedding approach offers the richest feature set with the least custom development. The key decision is choosing the right WebView plugin and the right web editor.

**Vuplex 3D WebView** is the strongest candidate for desktop builds. It renders web content to a `Texture2D` (placeable on 3D objects or Canvas), supports Windows, macOS, Android, iOS, WebGL, and VR platforms, and provides a clean JavaScript↔C# bridge via `ExecuteJavaScript()`, `PostMessage()`, and `MessageEmitted` events. Pricing runs $180 for Windows+Mac, $360 for Android+iOS, or ~$150 for WebGL alone. On desktop, it uses Chromium under the hood.

**ZFBrowser** (Embedded Browser, ~$75) is another Chromium-based option for Windows/macOS/Linux that renders to texture via a separate process with IPC. It adds ~1.3GB to your project and has no mobile support, but it's notably used by HoYoverse games (Genshin Impact, Star Rail). **UniWebView** ($30) covers iOS, Android, and macOS but critically **has no Windows standalone support** — it overlays native platform WebViews rather than rendering to texture by default.

The integration pattern is straightforward: bundle Monaco or CodeMirror files in `StreamingAssets`, create an `index.html` that initializes the editor, load it in the WebView, and bridge events (content changes, cursor position, diagnostics) through the JavaScript↔C# messaging layer. Every keystroke flows through: input event → WebView process → render → texture copy → Unity display, introducing **1–3 frames of latency** minimum.

**CodeMirror 6 is dramatically lighter than Monaco** and often the better choice for embedding. Monaco's bundle is 5–10MB uncompressed (~2–5MB gzipped); CodeMirror 6's full bundle with extensions is ~1.26MB gzipped — a **4× reduction**. CodeMirror was designed ground-up for mobile browsers, has a modular architecture where you only include needed features, and doesn't use web workers that might conflict with Unity's WASM threading. Monaco's advantage is built-in TypeScript/JavaScript language services and IntelliSense, a minimap, and a diff viewer. For a custom game DSL, CodeMirror's lighter footprint wins.

For **WebGL builds**, the optimal approach bypasses WebView plugins entirely. Since Unity WebGL runs in a browser, you can create DOM elements directly via `.jslib` plugins, position a CodeMirror instance over the Unity canvas with CSS absolute positioning, and communicate bidirectionally via `SendMessage()` (JS→C#) and `[DllImport("__Internal")]` functions (C#→JS). This has **zero rendering overhead** for the editor — the browser composites it natively.

| Scenario | Recommended approach |
|----------|---------------------|
| Desktop (Win/Mac) | Vuplex + Monaco or CodeMirror |
| Mobile (iOS/Android) | Vuplex or UniWebView + CodeMirror |
| WebGL | Direct DOM overlay via .jslib + CodeMirror 6 |
| VR | Vuplex 3D WebView + CodeMirror (renders to texture) |
| Cross-platform unified | Custom TMP-based (see next section) |

## Building from scratch with TMP gives maximum control at higher cost

A custom TMP-based editor is the approach with the most architectural control, best testability, and zero external dependencies — but it requires significant engineering. Here is the technical blueprint.

**The overlay pattern** is foundational: an invisible or transparent `TMP_InputField` captures keyboard input, while a separate `TMP_Text` component renders the syntax-highlighted display text with rich text tags like `<color=#569CD6>function</color>`. The document model is maintained independently as plain text; on each edit, the tokenizer runs and the display string is rebuilt with injected color tags. IGCE and every successful community implementation uses this dual-layer approach.

**Virtual scrolling is non-negotiable** for documents over ~50 lines. TMP generates mesh vertices for every character — a 1,000-line file with 80 characters per line produces **320,000+ vertices**, causing 100ms+ mesh generation spikes. The solution is line-level virtualization: maintain a pool of ~30–50 `TMP_Text` objects (one per visible line plus a buffer), calculate the visible line range from scroll position (`firstLine = scrollOffset / lineHeight`), and recycle off-screen objects by updating their text content. The `ScrollRect` scrolls a virtual content area sized to `totalLines × lineHeight`, but only the pooled objects exist in the scene. Each line's TMP object should live under a separate `Canvas` sub-hierarchy to prevent rebuild cascading.

**Cursor management** with monospaced fonts is exact: `x = cursorColumn × characterWidth + leftPadding`, `y = cursorLine × lineHeight + topPadding`. Render the caret as a thin `Image` UI element, toggling visibility on a ~530ms timer for blinking. For text selection, track `selectionAnchor` and `selectionFocus` positions and render highlights using TMP's `<mark>` tag (semi-transparent overlay) or separate stretched `Image` elements. Line numbering uses a synchronized `TMP_Text` component parented under the same `ScrollRect` content container.

**The Tab key problem** is a universal Unity pain point — the EventSystem consumes Tab for UI navigation. Fix it by setting `Navigation.mode = Navigation.Mode.None` on the InputField and intercepting Tab in an overridden `OnUpdateSelected` to insert spaces or a tab character. Clipboard access uses `GUIUtility.systemCopyBuffer` on desktop, but **requires platform-specific native plugins on Android** (Java `ClipboardManager` via JNI) and **JavaScript interop on WebGL** (`navigator.clipboard.writeText()`).

## Document model and architecture should prioritize testability

The architecture must cleanly separate document model, editor logic, and Unity rendering — both for testability and maintainability.

**For the document data structure**, three options scale differently. A simple `List<string>` (array of lines) is sufficient for documents under ~10,000 lines and is trivial to implement and test. A **piece table** (VS Code's approach) uses a red-black tree of pieces pointing into an original buffer and an append-only add buffer, delivering O(log n) insert/delete and natural undo support — VS Code's piece tree outperforms line arrays at 100k+ lines. A **rope** (used by Zed, xi-editor) is a B-tree of text chunks with excellent worst-case performance. For most in-game editors where documents stay under a few thousand lines, `List<string>` is the pragmatic starting point; graduate to a piece table if you need multi-cursor or very large files.

**The recommended architecture** follows the Humble Object Pattern:

```
DocumentModel (POCO) ←→ EditorController (POCO) ←→ EditorView (MonoBehaviour)
```

`DocumentModel` holds the text buffer, cursor positions, and selection state with zero Unity dependencies. `EditorController` handles input commands, manages the undo/redo stack (command pattern with `ITextCommand` interface), invokes the tokenizer, and determines visible lines. `EditorView` is the thin MonoBehaviour that manages TMP objects, ScrollRect, caret images, and line number display. Only the View knows about GameObjects. This separation means **the Document, Controller, Tokenizer, and Command system are all testable with pure NUnit Edit Mode tests** — no Play Mode required.

**Undo/redo** uses the command pattern with batching: accumulate consecutive character insertions at adjacent positions into a single `InsertTextCommand`, starting a new undo group on whitespace after non-whitespace, cursor movement, or a >1 second typing pause. The piece table naturally supports efficient undo since its buffers are append-only — restoring previous piece descriptors is cheap.

**Incremental tokenization** is the critical performance optimization for syntax highlighting. VS Code's approach: tokenize line-by-line, caching the tokenizer state at each line boundary. When the user edits line N, re-tokenize from N forward, stopping when the end-of-line state matches the cached value. "Most of the time, typing on a line results in only that line being retokenized." For token storage, use compact structs (`struct Token { int Start; int Length; TokenType Type; }`) in flat arrays rather than object lists — this keeps tokens on the stack and avoids GC pressure.

## LSP integration works on desktop but lightweight alternatives serve custom DSLs better

The **Language Server Protocol** can be integrated into a Unity editor for desktop platforms using the **OmniSharp/csharp-language-server-protocol** library (NuGet, MIT licensed, .NET Standard 2.0 compatible). This library provides both LSP client and server implementations. Communication works via `System.Diagnostics.Process` to spawn an LSP server and read/write its stdin/stdout, or via TCP sockets. LSP delivers autocomplete, diagnostics, hover information, go-to-definition, rename, and formatting through a standardized JSON-RPC protocol.

**Platform constraints are the dealbreaker for universal LSP**: `Process.Start()` is unavailable on WebGL (browser sandbox), unsupported on iOS, and limited on Android. Console platforms similarly restrict process spawning. For these platforms, all language intelligence must run **in-process**.

For a **custom game DSL** — the most common case for programming games — **full LSP is usually overkill**. A simpler approach provides 80% of the value:

- **Autocomplete**: Maintain a dictionary of known keywords, function signatures, and player-defined identifiers. Filter by prefix with fuzzy subsequence matching (Sublime Text-style scoring). This runs in microseconds for lists under 1,000 items.
- **Diagnostics**: Run your lexer/parser on a background thread or coroutine, emit diagnostic objects (line, column, message, severity), and display them directly — no JSON-RPC needed.
- **Hover info**: Map token positions to documentation strings in a lookup table.

If you do build a full LSP server for your DSL (using `OmniSharp.Extensions.LanguageServer`), the server can run **in-process** using pipe streams rather than actual stdio, making it viable on all platforms. The key requirement is building an **error-tolerant parser** — the code being edited is usually incomplete or syntactically invalid, so standard parsers that expect valid input will fail.

## Shipped games reveal a spectrum of intentional constraints

Analyzing real games with in-game code editors reveals that **most successful implementations deliberately constrain their editors** to match their DSL's complexity.

**Bitburner** represents the maximum-feature end of the spectrum. Built with React and Electron, it embeds the full Monaco Editor with TypeScript autocomplete powered by ~4,000 lines of custom `.d.ts` type definitions covering ~300 Netscript API functions. Multi-tab editing, configurable settings, and an external VS Code extension for remote editing. This is only possible because the game runs in a web runtime — Monaco is a native citizen, not an embedded guest.

**Zachtronics games** (TIS-100, Shenzhen I/O, EXAPUNKS) sit at the opposite extreme. Built with custom C++ engines, their "editors" are minimal fixed-size text boxes per computational node — TIS-100 limits each node to **15 lines of 18 characters**. No syntax highlighting, no autocomplete. The austerity is a deliberate design choice matching the vintage terminal aesthetic and the simplicity of their assembly-like DSLs. The free "ZACH-LIKE" book on Steam contains design documents about these decisions.

**Screeps** uses a web-based IDE in its HTML5 client (PixiJS for rendering, real JavaScript as the player language) and eventually released an official Atom editor package, acknowledging that players preferred external editors. **Else Heart.Break()** built a custom text editor with basic syntax highlighting for its BASIC-like "Sprak" language. **Human Resource Machine** and **while True: learn()** (the latter built in Unity) chose visual/drag-and-drop programming instead of text editing entirely.

The lesson: **match editor complexity to language complexity**. A simple DSL with 20 keywords and no nested structures needs only a basic highlighted text input. A full programming language (JavaScript, Lua, C#) demands Monaco-level tooling or you'll frustrate experienced programmers.

## Performance requires fighting Unity's text rendering and C#'s GC on two fronts

The two dominant performance bottlenecks are **TMP mesh regeneration** and **GC pressure from string manipulation**.

TMP's `Rebuild()` regenerates the entire mesh (vertices, UVs, colors) whenever `.text` changes. Community profiling shows **~1ms per 5 simple text elements** on a Samsung Galaxy S6, with spikes up to **278ms** for complex rebuilds. Rich text tags multiply the parsing cost — a heavily highlighted line with many `<color>` tags is significantly more expensive than plain text. The fix is virtual scrolling (only 30–50 TMP objects exist) combined with **only updating objects whose content actually changed** — TMP's `.text` setter skips rebuild if the string hasn't changed (equality check).

For GC pressure, every string concatenation to build rich text output allocates heap memory. Mitigations, ranked by impact:

- **Reuse a pooled `StringBuilder`** with pre-set capacity for building display strings
- **Use `Span<T>` / `ReadOnlySpan<char>`** for zero-allocation text slicing during tokenization — **38% faster** than `string.Split` with zero heap allocation
- **Cysharp's ZString library** provides zero-allocation string building with TMP-specific extensions (`SetCharArray` bypasses string allocation entirely)
- **Use `ArrayPool<char>.Shared`** for temporary character buffers during tokenization
- **Store tokens as structs** (`struct Token`) in flat arrays — stack-allocated, no GC pressure
- **Avoid LINQ and lambdas in hot paths** — both create heap-allocated enumerator/closure objects

For syntax highlighting specifically, **async/threaded tokenization** prevents frame drops: run the tokenizer on `Task.Run()`, merge results back on the main thread via `ConcurrentQueue<TokenResult>`. Alternatively, time-slice tokenization by processing ~50 lines per frame, prioritizing visible lines first. A hand-written state-machine lexer is the fastest approach — IGCE's "highly optimized lexer" follows this pattern. Regex-based highlighting works for simple cases but degrades with many patterns. Tree-sitter (incremental parsing, used by Neovim and Helix) is the gold standard but requires C native interop since no maintained C# binding exists.

## The hardest technical challenges are input handling and squiggly lines

**IME support** is historically buggy in Unity. The Input System provides `Keyboard.SetIMEEnabled()`, `Keyboard.onIMECompositionChange`, and cursor positioning APIs, but rich text tags displayed raw during IME composition is a known incompatibility. If your game targets CJK markets, budget significant testing time here.

**Error squiggles** are surprisingly hard in TMP. The `<u>` tag produces straight underlines, not wavy ones. Options include: modifying TMP's mesh data via `textInfo.meshInfo` to add sine-wave vertices below character baselines (most integrated), overlaying a separate UI element with a tiling squiggle sprite positioned using `characterInfo` coordinates (easiest), or a custom shader rendering a wave pattern on a positioned quad. Gutter icons for errors/warnings are simpler — position sprites in a vertical strip to the left of the text, updating Y positions on scroll.

**Multi-cursor support** requires sorting cursors by document position and applying edits from bottom to top (highest offset first) to avoid cascading position adjustments. The piece table excels here because multi-cursor edits are efficient operations on the tree structure.

**A minimap** is best implemented as a dynamically generated `Texture2D` where each pixel row represents a line of code, with colored rectangles proportional to line length using syntax highlighting colors — this is essentially what VS Code does at the pixel level, not actual zoomed-out text rendering.

## Conclusion: recommended strategy by project type

For a **desktop-only game with a real programming language** (JavaScript, Lua, Python), embed CodeMirror 6 via Vuplex. You get a battle-tested editor with ~1.26MB gzipped overhead, full mobile support, and a clean JS↔C# bridge. Add your game's API documentation as autocomplete snippets through CodeMirror's completion extensions.

For a **cross-platform game with a custom DSL**, build a custom TMP-based editor using the overlay pattern, virtual scrolling, a hand-written incremental lexer, and the Humble Object Pattern for testability. Start with `List<string>` as the document model. Implement fuzzy-matched autocomplete from a keyword dictionary. This approach has zero external dependencies, works everywhere including WebGL and consoles, and keeps everything testable with Edit Mode NUnit tests.

For a **WebGL game specifically**, use direct DOM overlay with CodeMirror 6 via `.jslib` plugins — zero rendering overhead, native browser performance, and the smallest possible bundle.

The piece of wisdom from shipped games: **constrain your editor to match your language**. Zachtronics proved that 15 lines of assembly in a tiny text box creates compelling gameplay. Bitburner proved that full Monaco with IntelliSense enables deep JavaScript programming. Neither approach is wrong — the editor's complexity should mirror the language's complexity, not exceed it.