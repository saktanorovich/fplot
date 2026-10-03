# ADR: Plot View

## Status
Proposed

## Context
`plot-view` documents existing behavior: an interactive ScottPlot chart of `Y = f(X)` that the user edits by
dragging points, with an optional mirrored f⁻¹ overlay. The code is split between
`src/FPlot/ViewModels/Widgets/PlotViewModel.cs` (state + intents, raises `DataChanged` / `MarkersChanged`)
and `src/FPlot/Views/Widgets/PlotView.axaml(.cs)` (rendering, hit-testing, pointer handling).
`MainViewModel` (`src/FPlot/ViewModels/MainViewModel.cs`) mediates between the plot, the grid
(`GridViewModel`) and the file/clipboard commands. Forces at play:
- The view owns mutable `double[] xs/ys` snapshots that are mutated in place during a drag and shared
  (swapped) with the inverse scatter. This avoids a full rebuild on every pointer move.
- Plot points and grid rows are identified **by index** into `MainViewModel.Points`.
- Rendering, hit-testing and pan/zoom come entirely from ScottPlot.Avalonia 5.1.57.
- There is no test project in `src/FPlot.sln` yet (only FPlot, FPlot.Math, FPlot.Utils). The spec's test
  approach depends on Avalonia.Headless rendering a Skia-backed `AvaPlot`.

## Architectural Risks
- **R1. Drag can get stuck if pointer capture is lost. Severity: medium.** `OnMouseDown` calls
  `UserInputProcessor.Disable()` and `BeginDrag`. Only `PointerReleased` reverses them. There is no
  `PointerCaptureLost` handler and no explicit `e.Pointer.Capture(...)`. If the window loses focus, a modal
  opens (e.g. a keyboard-triggered Open/Paste) or the release is missed, `DragIndex` stays set and pan/zoom
  stays disabled. Any later mouse move without a button pressed keeps moving the point.
- **R2. Any mouse button starts a drag. Severity: medium.** `OnMouseDown` does not check
  `e.GetCurrentPoint(...).Properties.IsLeftButtonPressed`. A right-click (ScottPlot's zoom/menu) or
  middle-click on a point starts an edit, which marks the point as changed and makes the document dirty.
- **R3. Index-based identity between scatter and model. Severity: medium.** `DragIndex`, `xs[index]` and
  `NotifyGrid(point, index)` assume `Points` keeps the same order and count from `Redraw` until `EndDrag`.
  If the collection changes during a drag (keyboard Delete, Ctrl+V, an async Open finishing), the index can
  point at the wrong row or go out of range (`Points[index]` in `GridViewModel.Update`, `xs[i]` in
  `AddMarkers`). This is hard to reverse once tests and other specs depend on it.
- **R4. View and model can diverge during a drag. Severity: low.** The view writes `xs/ys[index]` before the
  model accepts the value. `PointViewModel.X/Y` setters reject changes that are equal at `F6` display
  precision (`IsDisplayedValue`). The plotted point can then sit up to about 5e-7 away from the model value
  until the next full `Redraw`. Data values are not affected; only the rendering is.
- **R5. Non-finite values reach ScottPlot. Severity: medium.** `PointViewModel.TryParse` uses
  `NumberStyles.Float`, which accepts `NaN`, `Infinity` and `∞`. ScottPlot's `AutoScale` and the
  smooth-spline path can then produce invalid axis limits or a blank plot. Neither the spec nor the code
  defines a guard.
- **R6. Full rebuild + autoscale on every data change. Severity: medium (performance).** Every grid cell
  commit goes through `GridViewModel.PointViewModelOnPropertyChanged → NotifyPlot → Redraw`, which calls
  `plot.Clear()`, rebuilds both scatters, recomputes smoothing and autoscales. During a drag, each
  `PointerMoved` removes and re-adds every changed/clicked `Marker` plottable and calls `Refresh()` with no
  throttling. `GetNearest` is O(n) on every move, including hover. Cost grows with point count and with the
  number of changed points (one plottable per changed point).
- **R7. Toggling f⁻¹ rescales the axes. Severity: low.** `MainViewModel.ShowInverse` calls `Plot.Update()`
  (`DataChanged`), not `UpdateMarkers()`. Each toggle triggers a full redraw and autoscale, which resets the
  user's pan/zoom. With the default data (x 0..9, y 0..81) showing the inverse widens X to about 81 and
  squashes the main curve. The `PlotViewModel` comment says `MarkersChanged` covers inverse visibility,
  which contradicts this.
- **R8. Coordinate mapping on HiDPI. Severity: medium (verify).** `GetNearest` passes
  `e.GetPosition(PlotControl)` (DIPs) directly as a ScottPlot `Pixel`. If `AvaPlot` renders with a
  `DisplayScale` other than 1 (Retina on macOS, 125–200% on Windows), hit-testing and drag position will be
  offset. This must be checked on the target platforms.
- **R9. Tight coupling to the ScottPlot 5 API. Severity: medium (hard to reverse).** The code depends on
  `Scatter.Smooth`, `Data.GetNearest(..., LastRender)`, `UserInputProcessor.Disable/Enable`,
  `Add.Palette.GetColor(0)` and plottable `IsVisible`. These APIs have changed between ScottPlot 5.0.x and
  5.1.x. The default palette color is a library choice, not an app constant.
- **R10. Headless testability of `AvaPlot`. Severity: medium.** Avalonia.Headless needs Skia
  (`UseSkia()` + `UseHeadlessDrawing = false`) for ScottPlot to render and fill `LastRender`. The scatters
  are private fields, so asserting colors and visibility (AC1, 6, 7) needs a test seam
  (`InternalsVisibleTo` or inspecting `PlotControl.Plot.GetPlottables()`).
- **R11. Smooth spline misrepresents data. Severity: low.** Catmull-Rom-style smoothing overshoots, and it
  draws loops when X is not monotonic. A drag can freely move a point past its neighbors. The plot can then
  show a "function" that is not one, and the f⁻¹ overlay of a non-monotonic f is not a function either.

## Non-Functional Requirements
- **Performance:** dragging must stay interactive (target at most 16 ms per pointer-move frame) for up to N
  points (proposed N = 10,000). A grid edit or file open must redraw in under 200 ms for the same N.
  Consider throttling `Refresh()` to the render loop and reusing one marker plottable (or a small fixed set)
  instead of one per changed point.
- **Robustness:** the plot must render with 0, 1 and 2 points, duplicate X values and non-finite values
  without throwing. No exception may escape a pointer handler, because that would crash the UI thread.
- **Interaction integrity:** a drag must always end: on release, capture loss, window deactivation, or
  `DataChanged` arriving mid-drag. Pan/zoom must always be re-enabled when it ends.
- **Threading:** all `DataChanged`/`MarkersChanged` handlers must run on the UI thread. This currently holds
  because async continuations return through the Avalonia SynchronizationContext. It should be stated so a
  future background loader does not break it.
- **DPI correctness:** hit-testing must be accurate at scale factors of 1.0, 1.25, 1.5 and 2.0.
- **Accessibility:** "changed" is shown only by color (orange). Users with color-vision deficiency need
  another cue, e.g. marker shape or the grid's changed indicator. There is no keyboard alternative to
  dragging; the grid is the accessible editing path, and the spec should say so.
- **Memory/leaks:** `PlotView` must unsubscribe from view-model events on DataContext change. It does this
  today. Marker plottables must not build up over time; today they are cleared on each pass.

## Decisions
- **D1. Keep rendering and hit-testing in the view and intents in `PlotViewModel`.** `PlotViewModel` stays
  free of ScottPlot types, so AC 2, 4 and 5 can be unit-tested without UI. This is sound; keep it.
- **D2. Keep two change channels (`DataChanged` with rescale, `MarkersChanged` without).** This is what
  satisfies AC5 ("does not rescale"). Inverse visibility should be classified explicitly; see Gap G3.
- **D3. Keep the shared, in-place-mutated arrays for main and inverse scatters.** This is the mechanism that
  makes the inverse follow a drag (AC7) with no rebuild. The constraint is that only `PlotView` mutates
  them, and only between `BeginDrag` and `EndDrag`.
- **D4. Index-based identity is accepted for now.** Any mutation of `Points` must raise `DataChanged`, and
  a `DataChanged` during a drag must cancel the drag. Revisit (identify by `PointViewModel` reference) if
  multi-curve or sorting features arrive.
- **D5. Pin ScottPlot.Avalonia to exactly 5.1.57.** Treat upgrades as deliberate changes that re-run the
  headless tests and the manual smoke test.
- **D6. Add a test project (e.g. `src/FPlot.Tests`, or `tests/plot-view/` per CLAUDE.md) to `FPlot.sln`,
  using xUnit + Avalonia.Headless.XUnit with Skia enabled.** Where the project lives is for the application
  architect to decide.

## Gaps in Spec
- **G1. Test framework not confirmed.** The spec says xUnit is "proposed; confirm", and no test project
  exists. The user must confirm before tests are written.
- **G2. Which mouse button drags** (left only?), and what pressing on empty space does. Today it does not
  clear `ClickedPoint`, so Delete stays enabled for a point that may be far from the cursor.
- **G3. Should toggling f⁻¹ rescale the axes?** The code rescales (`DataChanged`). The spec's test approach
  says only "raises a redraw", and the `PlotViewModel` comment implies `MarkersChanged`. It must be pinned
  down so the unit test asserts the right event.
- **G4. Should the inverse be part of autoscaling when visible**, or should axes fit only the main curve?
- **G5. Marker precedence when a point is both clicked and changed.** The code draws it orange at ×1.4.
  The spec lists the two independently.
- **G6. Does a press without movement count as an edit?** Today `BeginDrag` sets only the clicked point
  (no change), which is fine. But a press-release with sub-pixel jitter can mark the point changed. Is a
  drag threshold required?
- **G7. Drag constraints.** May a point be dragged past its X-neighbors, making the data non-monotonic or
  non-functional? Should the drag be clamped or snapped? This also relates to the spec's two open questions
  (non-monotonic f⁻¹ and spline overshoot).
- **G8. Degenerate data.** Expected rendering for 0 or 1 points, duplicate X values, and NaN/±Infinity.
- **G9. Behavior when data changes mid-drag** (Delete, Paste, Open): cancel the drag, or block those
  commands?
- **G10. Inverse points and the hover cursor.** AC7 says the inverse is never draggable, but should the
  hand cursor appear over inverse points? Today hit-testing uses only the main scatter, so no.
- **G11. Performance targets and DPI support are not stated** (see NFRs).
- **G12. Smoke test is manual-only for AC3/AC5.** It has no pass/fail evidence format, and the headless
  tests could simulate pointer events (`MouseDown/MouseMove/MouseUp` on the headless window) to automate
  them.

## Constraints Confirmed
- C# / .NET 10, Avalonia 11.3.11, CommunityToolkit.Mvvm 8.4.0 and ScottPlot.Avalonia 5.1.57 match
  `src/FPlot/FPlot.csproj`.
- MVVM split (view model exposes intents and events; view renders) is sound and keeps ScottPlot out of the
  view model.
- Drag updates go through `MainViewModel.NotifyGrid(point, index)`, which temporarily unsubscribes the
  grid's `PropertyChanged` handler. This correctly avoids a rescale loop during a drag (AC5).
- The inverse is drawn first with the main scatter pinned to `Palette.GetColor(0)`, so the main color stays
  stable and the main curve is drawn on top (AC1, AC7).
- Colors `#E67E22` and `#C0392B` are shared with `MenuView`, which keeps the visual language consistent.
- Out of scope (multiple curves, image export, custom axis ranges) is consistent with the single-scatter,
  index-based design.
