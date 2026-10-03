# ADR: Points Grid Editing

## Status
Proposed

## Context
The "Control points" grid (`src/FPlot/Views/Widgets/GridView.axaml`) shows each point as two always-editable
`TextBox` cells inside `DataGridTemplateColumn`s. The spec (`specs/points-grid-editing.md`) is a draft written
after the fact from existing code. Its job is to fix the current behavior under test before anything is
refactored. The behavior lives in:

- `PointViewModel` (`src/FPlot/ViewModels/Widgets/PointViewModel.cs`) keeps a saved `mainPoint` and a working
  `workPoint`, both `Point2d` structs (`src/FPlot.Math/Point2d.cs`). It also has the `added` flag, the
  `XChanged`/`YChanged` flags, the `IsDisplayedValue` rounding guard and `Invalidate()`.
- `MainViewModel` (`src/FPlot/ViewModels/MainViewModel.cs`) mediates between grid, plot and file IO, and owns
  the AddPoint, RemovePoint, Save and SaveAs commands and the `pointsRemoved` and `clickedPoint` state.
- `GridViewModel` (`src/FPlot/ViewModels/Widgets/GridViewModel.cs`) subscribes to each point's
  `PropertyChanged` and calls `NotifyPlot()`. When the plot drags a point, it unsubscribes while writing
  (`Update(Point2d, int)`) so the write does not loop back.
- `DoubleToStringConverter` and `ChangedToBrushConverter` (`src/FPlot/Converters/`) format values and pick
  the change color.
- `MathUtils.Sign` (`src/FPlot.Math/MathUtils.cs`) does comparisons with an absolute `EPS = 1e-9`.

Forces in play:
- The grid displays values at `F6` precision, but files (CSV) keep full precision.
- Grid input follows the current culture, but CSV is always invariant.
- Change detection uses an absolute 1e-9 tolerance.
- The same `PointViewModel` setters serve both grid edits and plot drags.
- The repo has no test projects yet (`src/FPlot.sln` holds only FPlot, FPlot.Math and FPlot.Utils).

## Architectural Risks
- **R1: Per-keystroke write-back and redraw. Severity: medium.** The `TextBox.Text` bindings in
  `GridView.axaml` set no `UpdateSourceTrigger`, and Avalonia's default for `TextBox.Text` is `PropertyChanged`.
  So every keystroke that parses runs `ConvertBack`, then the setter, then `PropertyChanged`, then
  `GridViewModel.PointViewModelOnPropertyChanged`, then `NotifyPlot()`, which redraws the whole plot and
  re-evaluates CanExecute. Two consequences:
  - Partly typed values become real edits. Typing "12" stores 1 and then 12, and a cell can turn coral
    mid-typing even if the final value equals the saved one.
  - The source raises `PropertyChanged(X)` while the text is still being typed. If the binding pushes the
    `F6`-formatted value back, the caret jumps or the text gets reformatted.

  Criterion 3 ("updates the point and redraws") depends on this timing but does not say when the commit
  happens.
- **R2: Display precision and change tolerance disagree. Severity: medium.** `IsDisplayedValue` drops any
  write whose `F6` text matches the current value. An edit smaller than about 5e-7 can therefore never be
  made from the grid, even though criterion 6 says a change is anything above 1e-9. The guard also lives in
  the setters, so it applies to plot drags too (`GridViewModel.Update(Point2d, int)`), and drags under 5e-7
  are dropped without notice. The guard formats with `InvariantCulture` while the converter formats with the
  binding culture. The numbers are the same, but the coupling is implicit: `PointViewModel.DisplayFormat` is
  shared with `DoubleToStringConverter.Format`, and changing one without the other breaks criterion 5.
- **R3: Non-finite input is accepted. Severity: medium.** `double.TryParse(..., NumberStyles.Float, culture)`
  accepts "NaN", "Infinity" and "-Infinity" (and the culture's symbols, such as "∞").
  - Infinity passes `MathUtils.Sign` (the difference is ±∞), so it gets stored. `F6` then shows "∞", the plot
    gets an infinite coordinate, and Add Point produces `lastX + 1 = ∞`.
  - With NaN, `Sign(NaN - v)` returns 0, so a NaN value can never be set, compared or flagged as changed.
    Change detection is silently wrong for any NaN loaded from a file.
- **R4: Absolute epsilon. Severity: low.** `EPS = 1e-9` is absolute. For |values| above about 1e7 it is
  smaller than one ULP, so detection becomes exact comparison. For very small values (around 1e-10), real
  differences count as "unchanged". This is acceptable for the function-plotting use case, but it is not
  stated anywhere.
- **R5: Culture-sensitive parsing gives surprising results. Severity: medium.** `NumberStyles.Float` excludes
  `AllowThousands`.
  - In en-US, "1,5" is rejected, which is fine.
  - In de-DE, "1.5" is rejected and the old value is kept without notice (criterion 4). Users who paste
    invariant values into the grid will see edits silently ignored.
  - Clipboard paste falls back to invariant (`PointViewModel.TryParseCell`), but grid entry does not. That
    split is inconsistent.
- **R6: Delete is tied to plot-click state, not grid selection. Severity: medium.** `RemovePoint` acts on
  `clickedPoint`, which `PlotViewModel.BeginDrag` then `NotifyClick(index)` sets. Selecting a row in the
  grid does not enable Delete. `GridViewModel.SelectedPoint` only triggers `NotifySelection()`, which
  re-queries a CanExecute that ignores selection. This is a UX contract that will be hard to reverse once
  it is tested. `NotifyClick(int index)` also indexes `Points[index]` without a bounds check.
- **R7: Stale selection after a full reload. Severity: low.** `MainViewModel.Invalidate(List<Point2d>)` (on
  open or paste) clears `Points` and `clickedPoint` but not `Grid.SelectedPoint`. It also never unsubscribes
  the old points from `GridViewModel`. The old points are collectable, because the subscription runs from
  the point to the grid, but `SelectedPoint` can briefly refer to a removed item.
- **R8: Change color is the only signal and is hard-coded. Severity: low to medium.**
  `ChangedToBrushConverter` returns `Brushes.LightCoral` or `Brushes.Black`.
  - LightCoral on the hard-coded white background has a contrast of about 2.3:1, which fails WCAG 1.4.3.
  - Color is the only indicator, which fails WCAG 1.4.1.
  - Black breaks under a dark theme.
- **R9: Testability of the WinExe assembly. Severity: medium.** The tests have to reference `FPlot`, an
  `OutputType=WinExe` project. That works in .NET, but:
  - `MainViewModelInit` is `internal`, so it needs `InternalsVisibleTo` or must be tested through
    `MainViewModel.Points`.
  - The `MainViewModel` constructor needs an `IPointsIoService`, so a fake is required.
  - `SaveFileCommand` CanExecute calls `File.Exists(SelectedPath)` directly. Save tests (criterion 9) need a
    real temp file, or they must go through Save As.
  - The converters derive from `MarkupExtension` and use `Avalonia.Media.Brushes`. They can be unit tested
    without a running platform, but the headless DataGrid test (criterion 3) needs Avalonia.Headless.XUnit,
    and that package's version has to match Avalonia 11.3.11.

## Non-Functional Requirements
- **Responsiveness:** a committed grid edit must redraw within one frame for typical point counts (up to a
  few hundred points) without blocking typing. If the trigger stays per keystroke, a full `Plot.Update()` on
  each key needs a stated budget, or the plot needs debouncing.
- **Threading:** every `PointViewModel` and `Points` mutation happens on the UI thread. `ObservableCollection`
  is not thread-safe, and async IO continuations must resume on the UI dispatcher.
- **Precision preservation:** values that are loaded and not edited must round-trip to file bit for bit.
  This is criterion 5, and it should also be checked end to end through Save.
- **Locale determinism in tests:** tests must set `CultureInfo.CurrentCulture` and `CurrentUICulture`
  explicitly, at least for en-US and one comma-decimal culture such as de-DE. Otherwise results depend on
  the machine.
- **Accessibility:** a changed cell needs a non-color cue, or at least a color with ≥ 3:1 contrast for UI
  state. The editable cells need to be keyboard reachable (Tab and arrow keys).
- **Robustness:** non-finite values must not reach the plot or the files (see R3).
- **Scalability:** the DataGrid virtualizes rows. Each row's TextBox bindings must not leak `PropertyChanged`
  handlers as the user adds and removes points.

## Decisions
- **D1: Treat the spec as a characterization of current behavior.** Tests pin today's semantics, including
  the asymmetries in R2, R3 and R6. Any behavior change needs a spec update first, as CLAUDE.md requires.
  No refactor goes in under this spec.
- **D2: Keep two-snapshot change tracking in `PointViewModel`** (`mainPoint`/`workPoint` plus `added`) as
  the single source of truth for "unsaved". `MainViewModel.pointsRemoved` stays the record of deletion state.
  Both reset only through `MainViewModel.Invalidate()` after a successful save, and are reset by a reload.
- **D3: Keep one source for the display format.** `DoubleToStringConverter.Format` must keep referring to
  `PointViewModel.DisplayFormat`. A test should assert they are equal, so that criterion 5 cannot regress.
- **D4: Use an absolute epsilon of 1e-9 (`MathUtils.EPS`) for change detection**, as the spec states. It is
  accepted for now (R4). Moving to a relative tolerance later is a behavior change and needs a spec update.
- **D5: Culture split.** The grid uses `CurrentCulture` and CSV uses `InvariantCulture`. This is accepted as
  the spec states it, pending the user's answer to the spec's open question.
- **D6: Test infrastructure.**
  - Add an xUnit test project under `tests/points-grid-editing/` that references `src/FPlot`.
  - Use a hand-written fake `IPointsIoService` rather than a mocking library, so no new dependency is added.
  - Avalonia.Headless.XUnit, pinned to 11.3.x, is used only for the criterion 3 test.
  - Whether the test project is added to `FPlot.sln` is left to the application architect. Note that the
    smoke test builds `FPlot.sln`.
- **D7: Do not change production code to support tests** beyond, at most, `InternalsVisibleTo` for
  `MainViewModelInit`. Criterion 1 can be tested through `new MainViewModel(fake).Points` instead.

## Gaps in Spec
- **G1: Commit timing for edits.** Does an edit commit per keystroke (the current default) or on focus loss
  or Enter? This changes what criteria 3, 4 and 6 observe and how the headless test is written (R1).
- **G2: Non-finite values.** Criterion 2 defines how `NaN` is displayed, but there is no criterion for
  typed "NaN" or "Infinity", or for NaN loaded from a file. These are accepted today (Infinity) or silently
  unsettable (NaN) (R3).
- **G3: Sub-display-precision edits.** Criterion 5 conflicts with criterion 6 for user edits between 1e-9
  and about 5e-7. Should the grid be able to make such edits? The same guard also swallows small plot drags.
  Is that intended (R2)?
- **G4: The culture for display and parse is never pinned.** "Current culture" in criterion 2 means
  `CurrentCulture` or `CurrentUICulture`? It also means whatever Avalonia passes as the converter
  `culture` argument. The thousands-separator and invariant-fallback behavior is undefined (R5).
- **G5: Delete semantics** (already raised in the spec): plot click or grid selection? It is also unspecified
  whether a plot drag counts as a "click" that enables Delete (today it does, via `BeginDrag`), and whether
  Delete becomes disabled after Open or Paste (today it does).
- **G6: Criterion 9 preconditions.** Save is enabled only when `File.Exists(SelectedPath)` and there are
  changes. With the default path ("Default: F(x) = e^x / 100") or a clipboard path, only Save As works. The
  spec should state this, and what happens when a save fails (marks are kept).
- **G7: Add Point edge cases.** Adding after a point whose Y or X is NaN or Infinity is undefined. "lastX"
  means the last row, not the maximum X, so after edits the new point can land before others in X. Confirm.
- **G8: Removal followed by re-adding.** `pointsRemoved` stays true until a save even if the user adds an
  identical point back. Save stays enabled, but no cell is coral. Is that acceptable?
- **G9: Change-color contract.** The colors are hard-coded LightCoral and Black, with no dark-theme and no
  non-color cue (R8). Confirm the exact brushes are part of the contract, because criterion 6 pins them.
- **G10: Test framework** is marked "proposed; confirm". xUnit v2 or v3? It must be compatible with
  Avalonia.Headless.XUnit 11.3.x. Confirm before Phase B.
- **G11: Smoke test is manual** (it requires observing coral text and a plot update). No automated pass/fail
  signal is defined.

## Constraints Confirmed
- The tech stack (.NET 10, Avalonia 11.3.11 with DataGrid 11.3.11, CommunityToolkit.Mvvm 8.4.0, FPlot.Math)
  matches `src/FPlot/FPlot.csproj` and needs no new runtime dependencies.
- The mediator pattern (the `MainViewModel` `Notify*` methods) and the unsubscribe-while-writing in
  `GridViewModel.Update(Point2d, int)` correctly prevent plot↔grid feedback loops.
- `Point2d` is a mutable struct, so `workPoint = point.Copy()` and `mainPoint = workPoint` in `Invalidate()`
  copy by value. The saved and working snapshots do not alias.
- `ConvertBack` returns `BindingOperations.DoNothing` on parse failure. That is the correct Avalonia
  mechanism for criterion 4.
- `CanUserSortColumns="False"` and `SelectionMode="Single"` in `GridView.axaml` enforce the stated out-of-scope
  items (sorting and multi-select).
- All state is in-process and single-user. There are no network or external service dependencies beyond
  the file system and clipboard, both reached through `IPointsIoService`.
