# ADR: Clipboard Copy & Paste

## Status
Proposed

## Context
FPlot (C# / .NET 10 / Avalonia 11.3.11 desktop app) lets the user move the whole point set to and from the OS
clipboard so it interoperates with spreadsheets (Excel, Numbers) and plain CSV text. The spec documents existing
behavior rather than new work, so this ADR mainly records the decisions already made in code, the risks they
carry, and what tests/implementation must settle.

Current shape of the feature:
- `MainViewModel.CopyAsync` / `PasteAsync` (`src/FPlot/ViewModels/MainViewModel.cs` ~L184-205) call through the
  `IPointsIoService` seam (`src/FPlot/ViewModels/Services/IPointsIoService.cs`).
- `PointsIoService` (`src/FPlot/Services/PointsIoService.cs`) formats with `PointViewModel.ToTabular(point,
  CultureInfo.CurrentCulture)` and parses with `PointViewModel.TryParse`
  (`src/FPlot/ViewModels/Widgets/PointViewModel.cs`).
- `FileUtils.SetClipboardAsync` / `GetClipboardAsync` / `ReadLinesAsync(IClipboard)`
  (`src/FPlot.Utils/FileUtils.cs`) wrap `TopLevel.Clipboard` (text only).
- Entry points: toolbar buttons and icons in `src/FPlot/Views/Widgets/MenuView.axaml` (L185-230), and a macOS
  native menu with `Meta+C` / `Meta+V` gestures in `src/FPlot/Windows/MainWindow.axaml` (L31-32).

Forces: spreadsheet interop wants culture-formatted, tab-separated text; file I/O (`ToCsv`) uses invariant
culture; the app must behave the same on macOS and Windows clipboards; Paste is destructive (it replaces all data).

## Architectural Risks
- **R1 — Global Cmd+C / Cmd+V gestures can override text editing in the grid (high).** `NativeMenuItem
  Gesture="Meta+V"` in `MainWindow.axaml` is application-wide on macOS. While a DataGrid cell is in edit mode,
  Cmd+V may trigger `PasteCommand` (replacing ALL points, without confirmation) instead of pasting into the cell,
  and Cmd+C may copy the whole data set instead of the selected text. Combined with R2 this is a silent data-loss
  path. Must be verified manually on macOS.
- **R2 — Paste destroys unsaved edits with no confirmation or undo (high).** `Invalidate(points)` clears
  `Points`, `pointsRemoved` and `clickedPoint`. There is no undo stack in the app. (Spec flags this as an open
  question.)
- **R3 — Unhandled clipboard exceptions can crash the app (medium-high).** `IClipboard.SetTextAsync` /
  `TryGetTextAsync` can throw (on Windows `OpenClipboard` fails with `CLIPBRD_E_CANT_OPEN` when another process
  holds the clipboard; on Linux/X11 the owner may time out). Neither `FileUtils` nor `PointsIoService` nor the
  `[RelayCommand]` handlers catch. CommunityToolkit `AsyncRelayCommand` rethrows on the UI context by default,
  which reaches the Avalonia dispatcher as an unhandled exception.
- **R4 — Copy `CanExecute` is never re-evaluated (medium; AC2 is not actually met).** `CopyCommand.
  NotifyCanExecuteChanged()` is not called anywhere (`NotifyPlot`/`NotifyGrid` only notify `SaveFileCommand` and
  `RemovePointCommand`). Removing the last point leaves Copy enabled and copying then writes an empty string to
  the clipboard (overwriting the user's clipboard). Adding a point to an empty list leaves Copy disabled.
- **R5 — "Not a file" is inferred from `File.Exists("<MachineName>/Clipboard")` (medium).** `CanSaveFileAsync`
  relies on that relative path not existing. It resolves against the process CWD; if a `<machine>/Clipboard`
  file happens to exist there, Save would be enabled and `SaveFileAsync` would overwrite it. It also displays the
  machine name in the UI (minor information exposure in screenshots). Data-source kind is encoded in a display
  string rather than state.
- **R6 — Culture-dependent copy format (medium).** Copy uses `CurrentCulture`, Save uses invariant. A comma-decimal
  culture produces `1,5<TAB>2,5`, which cannot be re-read as CSV if tabs are lost (e.g. through chat apps or
  editors converting tabs to spaces). On macOS, .NET derives `CurrentCulture` from ICU/`LANG`, which can differ
  from the region set in System Settings (and from what Numbers/Excel use), especially when launched from Finder
  where `LANG` may be unset. Interop correctness therefore depends on the launch environment.
- **R7 — Parsing ambiguity and silent data loss (medium).** Lines that fail to parse are skipped silently; the
  user gets no count of dropped rows. Spreadsheet display formats (`1,234.5` thousands separators, `12%`,
  currency, `1.23E+05` is fine) are rejected since `NumberStyles.Float` excludes thousands. Rows with more than
  two columns silently take the first two. `NaN`, `Infinity`, `-Infinity` (and culture symbols like `∞`) parse
  successfully and are injected into the plot (`ScottPlot` axis auto-scaling may misbehave).
- **R8 — Unbounded clipboard input processed on the UI thread (low-medium).** `ReadLinesAsync(IClipboard)` reads
  the full text, splits it, parses on the UI thread, then `Invalidate` adds rows to an `ObservableCollection` one
  at a time (one `CollectionChanged` per row) and rebuilds the plot. A large paste (100k+ rows) freezes the UI.
- **R9 — Testability of the clipboard layer (low-medium).** `FileUtils` methods are extension methods on `Window`
  and resolve `TopLevel.GetTopLevel(window)`; the integration test in the spec needs a real Avalonia headless
  platform. No test project or `tests/` directory exists yet.
- **R10 — Copy ships `Environment.NewLine` line endings with a trailing newline (low).** `StringBuilder.AppendLine`
  yields `\n` on macOS and `\r\n` on Windows plus a trailing terminator; some spreadsheet targets paste an extra
  empty row. Tests asserting exact clipboard text must account for platform newline.

## Non-Functional Requirements
- **Cross-platform parity:** Copy/Paste must behave identically on macOS and Windows (Linux best-effort), including
  line endings on input (`\n`, `\r`, `\r\n`) and keyboard access (Cmd on macOS, Ctrl on Windows; see Gap G3).
- **Resilience:** a clipboard failure (unavailable, locked, non-text content, owner timeout) must never crash the
  app; it is a no-op (optionally with user feedback) and leaves points and path unchanged (extends AC6).
- **Non-destructive by default:** a paste that would discard unsaved changes should be confirmable, or at
  minimum the risk accepted explicitly in the spec (R2).
- **Responsiveness:** paste of a "reasonable" data set (suggest up to 10,000 rows) completes without visible UI
  freeze (< ~200 ms); an upper bound or off-thread parse for larger input.
- **Precision:** Copy must round-trip doubles without loss within the app (`double.ToString(culture)` in .NET Core
  3.0+ is shortest round-trippable - keep it; do not switch to `DisplayFormat` "F6").
- **Culture determinism in tests:** tests must set `CultureInfo.CurrentCulture` explicitly (en-US and de-DE) and
  restore it; must not depend on the CI machine locale.
- **Privacy:** clipboard content is user data; it must not be logged. Writing to the clipboard should only happen
  on explicit user action (never on an empty list - see R4).
- **Commands' enabled state must be live:** any command whose CanExecute depends on `Points` must be notified on
  every mutation of `Points` (add, remove, open, paste).

## Decisions
- **D1 — Keep the `IPointsIoService` seam as the only clipboard boundary.** View models never touch Avalonia
  clipboard types; this keeps `MainViewModel` unit-testable with a fake (spec Test Approach) and isolates the
  platform dependency to `PointsIoService`/`FileUtils`.
- **D2 — Plain text (`text/plain` / `CF_UNICODETEXT` / `NSPasteboardTypeString`) is the only clipboard format.**
  Matches Out of Scope (no rich formats). Avoids per-platform `DataFormats` handling. Hard to reverse only in the
  sense that external users will start relying on the text shape.
- **D3 — Copy wire format is `x<TAB>y` per line in `CurrentCulture`; Save remains invariant CSV.** Rationale:
  spreadsheets split on tab and parse numbers in the user's locale. This is the most user-visible and hardest to
  reverse decision (users build workflows around it); the spec owner must explicitly accept the R6 trade-off
  (spec "Known gaps" item 1). Recommended: accept, since in-app round-trip always succeeds via the tab branch.
- **D4 — Parsing is tolerant: tab row (current then invariant culture) else invariant CSV; unparseable lines
  skipped.** Rationale: transparently skips CSV headers and spreadsheet header rows. Recommend tightening to
  reject non-finite values (`NaN`/`Infinity`) - pending spec decision (G5).
- **D5 — Paste replaces, not appends, and resets the unsaved-change baseline.** Consistent with Open. Appending is
  out of scope.
- **D6 — Failure-handling policy belongs in `PointsIoService`** (catch clipboard exceptions, return `null` from
  `PasteAsync`, swallow/report on `CopyAsync`), so `MainViewModel` keeps its simple null/empty no-op contract
  (AC6). This preserves the existing interface contract "null when the clipboard is unavailable".
- **D7 — Recommend modelling data source explicitly** (e.g. a flag/enum "file vs. clipboard vs. default") instead
  of inferring "not a file" from `File.Exists` on a display label (R5). The label text `<MachineName>/Clipboard`
  may remain as presentation. Application Architect to decide shape; this is cheap now and harder later once
  more sources exist.
- **D8 — Tests:** xUnit for unit tests on `PointViewModel.ToTabular/TryParse` and `MainViewModel` with a fake
  `IPointsIoService`; integration round-trip via `Avalonia.Headless.XUnit` (version aligned to Avalonia 11.3.x),
  which provides an in-memory clipboard. This adds new test-only package dependencies (xUnit, test SDK,
  Avalonia.Headless.XUnit) - acceptable, no runtime impact.

## Gaps in Spec
- **G1 — Confirmation on destructive paste (R2):** spec asks "intended?" but gives no answer. Decide: (a) accept
  silent replace, (b) confirm when `HasChanges()`/`pointsRemoved`, or (c) undo. Blocks the AC3 test wording.
- **G2 — Comma-decimal copy re-parse (R6):** spec asks "acceptable?" without an answer. Need an explicit accept or
  a format change (e.g. always invariant, or invariant when culture decimal separator is `,`).
- **G3 — Keyboard shortcuts:** spec does not mention them. Code binds `Meta+C`/`Meta+V` only in the macOS
  `NativeMenu`; Windows has no `Ctrl+C`/`Ctrl+V` binding for these commands. Also unspecified: should these
  gestures be suppressed while a grid cell is being edited (R1)?
- **G4 — AC2 enablement must be live:** spec should state Copy re-enables/disables as points are added/removed
  (currently broken, R4), so tests cover state transitions, not just initial state.
- **G5 — Non-finite and edge numbers:** are `NaN`, `Infinity`, values with thousands separators, percent, or
  leading/trailing whitespace in CSV cells valid? CSV branch does not trim; tab branch does. Lines with >2
  columns: take first two, or reject?
- **G6 — Clipboard error behavior:** AC6 covers "no text / no parseable lines" but not exceptions or clipboard
  unavailable (`TopLevel` null). Should the user get feedback (status message) on empty/failed paste or on
  skipped rows?
- **G7 — Exact copy text:** trailing newline and line separator (`Environment.NewLine` vs fixed `\n` or `\r\n`)
  are unspecified; tests need a defined expectation.
- **G8 — Paste and selection/grid state:** after paste, should grid selection/scroll and plot view reset? (Code
  clears `clickedPoint` but not `Grid.SelectedPoint` explicitly.) Also whether `ShowInverse` persists.
- **G9 — Size limits:** no maximum row count or performance target (R8).
- **G10 — Test framework:** spec says xUnit "(proposed; confirm)". No test project exists; the test project
  layout (`tests/clipboard-copy-paste/`, referencing a `WinExe` project) and the headless package must be
  confirmed. Spec's "Status: Draft" also means acceptance criteria are not yet user-approved.
- **G11 — Status label semantics:** `<MachineName>/Clipboard` exposes the host name and is coupled to Save
  enablement (R5); confirm whether the label is a requirement or an implementation detail.

## Constraints Confirmed
- .NET 10 / Avalonia 11.3.11 / CommunityToolkit.Mvvm 8.4.0 (`src/FPlot/FPlot.csproj`) - `TopLevel.Clipboard`
  with `SetTextAsync`/`TryGetTextAsync` is available and cross-platform in this version.
- Whole-dataset copy, replace-on-paste, and text-only clipboard (Out of Scope list) keep the surface small and
  avoid platform-specific clipboard formats - architecturally sound.
- Tolerant dual-format parsing (tab with culture fallback, else invariant CSV) is sound and shared with file Open
  (`PointsIoService.Parse`), giving one parsing path for both features.
- `IPointsIoService` abstraction enables view-model tests without a UI (spec Test Approach is feasible).
- Accepting `\n`, `\r`, `\r\n` via `Split(["\n","\r"], RemoveEmptyEntries)` correctly covers macOS, Windows and
  legacy line endings (AC4).
- Save disabled after paste until Save As (AC5) is consistent with the file-I/O model, subject to R5 hardening.
