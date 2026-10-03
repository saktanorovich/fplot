# ADR: CSV File I/O

## Status
Proposed

## Context
FPlot (C# / .NET 10, Avalonia 11.3.11, CommunityToolkit.Mvvm 8.4.0) lets the user load control points from a CSV file,
edit them, and write them back via Save (same file) or Save As (new file). The spec (`specs/csv-file-io.md`) is a draft
reverse-engineered from existing code, so this ADR is mostly about pinning down current behavior as a contract, flagging
where that behavior is unsafe, and making the feature testable (the repo has no test project yet).

Current shape of the code:
- `src/FPlot/ViewModels/MainViewModel.cs` owns the commands (`OpenFile`, `SaveFile`, `SaveAsFile`), the dirty state
  (`pointsRemoved` plus `PointViewModel.HasChanges()`) and `SelectedPath`. Save's `CanExecute` calls `File.Exists(SelectedPath)`.
- `src/FPlot/ViewModels/Services/IPointsIoService.cs` is the seam between view model and UI. Its implementation
  `src/FPlot/Services/PointsIoService.cs` uses `Window`-extension helpers in `src/FPlot.Utils/FileUtils.cs`.
- `src/FPlot/ViewModels/Widgets/PointViewModel.cs` owns the line format (`ToCsv`, `TryParse`) and per-point change tracking
  (`MathUtils.EPS = 1e-9`, plus a `DisplayFormat = "F6"` rule that ignores grid write-backs).
- Open and Save As go through Avalonia `IStorageProvider`/`IStorageFile`. Save goes straight to `System.IO`
  (`new StreamWriter(fileName)`) on the stored local path.

## Architectural Risks
- **R1. Save is not atomic. Severity: high.** `FileUtils.SaveFileAsync(lines, path)` truncates the target with
  `new StreamWriter(fileName)` and then writes line by line. A crash, power loss, full disk or exception partway through
  leaves the user's only copy truncated. Save As (`file.OpenWriteAsync()`) has the same problem.
- **R2. I/O exceptions are not handled. Severity: high.** No `try/catch` anywhere in Open, Save or Save As
  (`IOException`, `UnauthorizedAccessException` on a read-only or locked file, a file removed between `File.Exists`
  and the write, a decoding failure). With `[RelayCommand]` async commands these exceptions are re-thrown on the UI
  synchronization context and can crash the app, losing unsaved edits. The spec defines no error behavior.
- **R3. Silent data loss on Open (and Paste). Severity: medium-high.** `OpenFileAsync` → `Invalidate(points)` replaces all points
  with no "unsaved changes" prompt. Window close does not prompt either. The spec lists this as an open question; it needs a decision.
- **R4. Two file-access paths. Severity: medium, hard to reverse.** Open/Save As use `IStorageFile`, but Save turns the file back
  into a string path (`TryGetLocalPath() ?? file.Path.LocalPath`) and uses `System.IO`. This works for unsandboxed desktop
  apps. It breaks in sandboxed or non-local storage (macOS App Sandbox security-scoped URLs, Flatpak portals, browser/mobile),
  where `File.Exists` is false or the write is denied. Save would then stay disabled or fail. Also, `SelectedPath` mixes a real
  path, the label `Default: F(x) = e^x / 100`, and `"{MachineName}/Clipboard"` in one string field.
- **R5. Synchronous disk I/O in `CanExecute`, and a stale result. Severity: medium.** `CanSaveFileAsync` calls `File.Exists` on the
  UI thread every time it is re-evaluated (after every edit notification). On slow or network volumes this blocks the UI. The result
  only refreshes on edit notifications, so if the file is deleted or created outside the app the button state goes stale
  (time-of-check/time-of-use gap; overlaps R2).
- **R6. Parsing is not strictly CSV or invariant. Severity: medium.** `PointViewModel.TryParse` has a tab-separated branch that parses
  in `CultureInfo.CurrentCulture` first. A file line containing a tab is therefore parsed in the user's locale (on a `de-DE`
  machine, `1,5\t2` becomes (1.5, 2)). This conflicts with AC 4 ("regardless of OS locale"). `NumberStyles.Float` also accepts
  `NaN`, `Infinity` and `-Infinity`, and exponents. Non-finite points then reach the plot and math code. Quoted fields (`"1","2"`)
  and `;`-delimited European Excel exports are silently dropped as "non-point lines", so a whole file can load as zero points
  with no feedback.
- **R7. Opening an empty or unparseable file wipes the data. Severity: medium.** If `Parse` returns an empty list, Open still replaces
  the points with nothing and sets the path. Paste guards against this (`Count: > 0`); Open does not.
- **R8. Unbounded memory and UI cost. Severity: low-medium.** `ReadLinesAsync` reads the whole file into a `List<string>`, then
  `Invalidate` adds points one at a time to an `ObservableCollection`. Each add raises `CollectionChanged` for the grid and plot.
  There is no size limit, so a large or mistakenly chosen file (for example a multi-GB log) can freeze or exhaust the app.
- **R9. Testability of the seam. Severity: medium (blocks the planned integration test).** `FileUtils.SaveFileAsync(this Window, ...)`
  needs a `Window` it never uses. Creating a `Window` in xUnit needs Avalonia.Headless. `FPlot.Utils` also references Avalonia,
  so the pure file-write logic cannot be tested without UI dependencies. A unit test of `CanSaveFileAsync` touches the real
  file system through `File.Exists`.
- **R10. Output depends on the platform. Severity: low.** `WriteLineAsync` uses `Environment.NewLine` (CRLF on Windows, LF elsewhere).
  Round-trips work, but the bytes on disk differ by OS, so byte-level test assertions and diffs of `data/*.csv` will differ.
- **R11. Concurrent command execution. Severity: low.** The async relay commands do not set `AllowConcurrentExecutions`, so the
  toolkit's default disables a command only while it is running. Open can still be invoked while a Save is in flight (or the other
  way round), so the in-memory state and the file can disagree.

## Non-Functional Requirements
- **Durability:** a failed or interrupted Save or Save As must leave the original file intact. Use write-to-temp in the same directory,
  then `File.Replace`/`File.Move(overwrite: true)`, or the equivalent for `IStorageFile`.
- **Error handling:** every I/O failure (open, read, decode, write, permission, file missing) must surface as a user-visible message
  and leave in-memory points, `SelectedPath` and the dirty state unchanged. The app must not crash.
- **Responsiveness:** file I/O stays async and off the UI thread. `CanExecute` must not perform blocking I/O.
- **Scale:** state a supported size. Suggested: at least 100k points open in under 1 s without UI freeze, and a hard limit or
  warning above a configurable size. Populate the collection in one batch rather than one item at a time.
- **Locale independence:** reading and writing a `.csv` file must give identical results under any `CurrentCulture`
  (test under `de-DE`, `fr-FR` and `en-US`).
- **Round-trip fidelity:** written values use the shortest round-trippable form (the current .NET `double.ToString` default),
  so Save then Open reproduces every bit-identical `double`.
- **Encoding:** read UTF-8 with or without a BOM. Write UTF-8 without a BOM, with a single defined line terminator.
- **Platform support:** state the target OSes (Windows, macOS, Linux desktop). If a sandboxed distribution is ever planned,
  this is a hard requirement on R4.
- **Concurrency:** at most one file operation runs at a time, and file commands are disabled while one is in flight.

## Decisions
- **D1. Keep `IPointsIoService` as the only I/O seam for view models.** `MainViewModel` must not call `System.IO` directly. Move the
  file-existence check behind the service (for example `bool CanSave(path)`, or a cached `HasBackingFile` flag set when Open or
  Save As succeeds), so view-model tests are deterministic with a fake. This addresses R5 and R9.
- **D2. The format is "two numeric columns, comma-delimited, invariant culture" for files.** CSV file parsing must not use the
  tab/current-culture branch. Split `TryParse` into a strict CSV parser (file) and a lenient clipboard parser (paste), or pass
  the source and culture explicitly. Non-finite values (`NaN`, `±Infinity`) are rejected as non-point lines. This addresses R6
  and makes AC 4 true.
- **D3. Save and Save As write atomically** (temp file plus replace). If that is out of scope for this pass, record it as accepted
  risk R1 explicitly. Do not let it go unstated.
- **D4. Model the document identity explicitly, not as a display string.** Keep a nullable backing-file handle or path, separate from
  the label text (default function, clipboard, file path). This is hard to reverse once tests and bindings rely on `SelectedPath`
  semantics, so decide it before tests are written. At minimum, tests should assert on behavior (Save enabled or disabled), not on
  the clipboard sentinel string.
- **D5. Testing strategy.** Add an xUnit test project (`tests/csv-file-io/`, referencing `FPlot` and `FPlot.Utils`).
  - View-model tests use a fake `IPointsIoService`. No Avalonia runtime is needed, because `MainViewModel` builds `GridViewModel`
    and `PlotViewModel`; the application architect must confirm these construct without a UI thread.
  - Integration tests either call the pure write helper directly, or call `FileUtils.SaveFileAsync(null!, lines, path)` (the `window`
    argument is unused) against a temp file. Prefer extracting a `Window`-free helper over adding Avalonia.Headless just for this.
- **D6. One defined line terminator** (recommend `\n`) for written files, so output is identical across platforms (R10).
- **D7. Open should not discard state on an empty parse result.** Either refuse with a message ("no points found") or require
  confirmation, consistent with Paste's `Count > 0` guard (R7).
- **D8. Keep the existing Avalonia `StorageProvider` dialogs for Open and Save As.** They are cross-platform and already in place.
  Leaving file pickers out of automated tests is acceptable.

## Gaps in Spec
- **G1. Error behavior is undefined.** What does the user see when Open, Save or Save As fails (missing, locked, read-only file, no
  permission, bad encoding)? Required so tests can be written for it (R2).
- **G2. Unsaved-changes prompt.** The spec already asks this: prompt before Open, Paste and quit when dirty? Yes or no is needed
  (R3). Also, should the "● Unsaved changes" label in `MenuView.axaml` bind to the same dirty state as AC 5? Today it is always
  visible, and `MainViewModel` exposes no `IsDirty` property to bind to.
- **G3. AC 4 versus tab-separated lines.** Must a tab-separated line in a `.csv` file be ignored, or parsed in the invariant
  culture? Current code parses it in the current culture (R6).
- **G4. Non-finite and extreme values.** Are `NaN`, `Infinity` or `1e308` valid points?
- **G5. Opening a file with zero valid points.** Should it replace the data with an empty set (current behavior), or be rejected
  with a message (R7)?
- **G6. AC 5 "edited X or Y" is underspecified.** Code also treats a value equal at `F6` display precision as unchanged
  (`IsDisplayedValue`), and an edit that returns to the original value as not dirty. Removing and re-adding the same point stays
  dirty (`pointsRemoved` is sticky). Confirm these as intended and state them in the spec.
- **G7. AC 6 "grid order".** The grid has `CanUserSortColumns="False"`, so grid order equals `MainViewModel.Points` order today.
  Confirm the contract is "collection order" so tests do not depend on the view.
- **G8. AC 9 and the clipboard sentinel.** After Paste, `SelectedPath` is `"{MachineName}/Clipboard"` and Save is disabled. The spec
  does not mention this. State it, or cover it in `clipboard-copy-paste.md`.
- **G9. Save As extension.** Is `.csv` enforced if the user types a name without an extension? Platform pickers differ.
- **G10. Supported data size and line-ending/encoding contract** (see NFRs). Today these exist only as code behavior, not as spec.
- **G11. The test framework is "proposed".** The spec says "xUnit (proposed … confirm)". Per CLAUDE.md the stack must come from the
  spec, so the user must confirm xUnit before the unit-test-writer starts. Also confirm the test project location and the run
  command (for example `dotnet test tests/csv-file-io`).
- **G12. The spec status is Draft, "pending user review".** The open questions above should be resolved, or explicitly deferred,
  before implementation is treated as conforming.

## Constraints Confirmed
- C# on .NET 10 with Avalonia 11.3.11 and CommunityToolkit.Mvvm 8.4.0 match `src/FPlot/FPlot.csproj` and `FPlot.Utils.csproj`.
- MVVM with an `IPointsIoService` seam is sound and makes AC 1, 3 and 5-9 testable with a fake, no UI needed.
- Writing invariant-culture CSV via `PointViewModel.ToCsv` gives locale-independent, round-trippable output for finite doubles.
- Skipping non-point lines (header, blanks) on read is a reasonable, forgiving contract (AC 2).
- `ShowOverwritePrompt = true` and the `*.csv` filter on Save As are appropriate (AC 7).
- Cancelling either dialog returns `null` and leaves state unchanged (AC 3, 8). This is implemented correctly today.
- Dirty tracking compares against a value-type snapshot (`Point2d` is a `struct`), so there is no aliasing after `Invalidate()`.
- Leaving out other formats, multiple series and other encodings is reasonable for a single-user desktop tool.
