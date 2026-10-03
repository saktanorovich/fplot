# Spec: Plot View

**Status:** Draft — Claude-authored from existing code, pending user review
**Slug:** `plot-view`

## Summary
The plot draws the points as a smooth curve `Y = f(X)`. The user can click and drag points on the plot to
change them, and toggle display of the inverse function f⁻¹.

## Tech Stack
- Language/runtime: C# on .NET 10 (`net10.0`)
- UI: Avalonia 11.3.11, **ScottPlot.Avalonia 5.1.57** for rendering and hit-testing
- MVVM: CommunityToolkit.Mvvm 8.4.0 (`PlotViewModel` exposes intents; `PlotView` renders)
- Tests: **xUnit** (proposed; confirm) for `PlotViewModel`; **Avalonia.Headless.XUnit** for `PlotView`.

## Acceptance Criteria
1. **Curve.** Points are drawn in grid order as a smooth scatter line with markers (size 10) in the palette's
   first color; axes labelled `X` and `Y = f(X)`.
2. **Autoscale on data change.** Opening, pasting, adding, deleting or editing points in the grid redraws
   the curve and auto-scales the axes.
3. **Hover cursor.** The cursor is a hand over a point and an arrow elsewhere.
4. **Click selects.** Pressing on a point makes it the clicked point: drawn enlarged (×1.4) and enabling Delete.
5. **Drag edits.** Dragging a point moves it with the mouse, updates its X/Y in the grid live, and does not
   rescale the axes; plot panning/zooming is disabled during the drag and re-enabled on release.
6. **Changed markers.** Points with unsaved changes are drawn orange `#E67E22`.
7. **Inverse toggle.** The `f⁻¹ Inverse` toggle shows/hides the curve with X and Y swapped (mirror across
   `y = x`) in red `#C0392B`, drawn beneath the main curve. Dragging a main point moves its inverse point too.
   The inverse curve is never draggable.

## Test Approach
- **Unit:** `PlotViewModel` raises `DataChanged`/`MarkersChanged`, `BeginDrag`/`DragTo`/`EndDrag` update the
  owner's point and clicked state (criteria 2, 4, 5); `ShowInverse` raises a redraw (7).
- **UI (headless):** `PlotView` builds the main and inverse scatters with the expected colors and visibility (1, 6, 7).
- Pointer-driven drag (3, 5) verified manually in the smoke test.

## Smoke Test
```bash
cd src && dotnet build FPlot.sln && dotnet run --project FPlot
```
Drag a point → grid row updates, point turns orange; toggle f⁻¹ → red mirrored curve appears/disappears.

## Known gaps / questions for review
- f⁻¹ is drawn by swapping axes even when f is not monotonic (not a true function) — acceptable?
- Smooth (spline) interpolation can overshoot between points — intended visual?

## Out of Scope
Multiple curves, exporting the plot as an image, custom axis ranges.
