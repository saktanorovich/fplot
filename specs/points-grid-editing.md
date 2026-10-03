# Spec: Points Grid Editing

**Status:** Draft — Claude-authored from existing code, pending user review
**Slug:** `points-grid-editing`

## Summary
The "Control points" grid lists every point as editable X and Y cells. The user can edit values,
add a point, and delete a point; unsaved edits are visually highlighted.

## Tech Stack
- Language/runtime: C# on .NET 10 (`net10.0`)
- UI: Avalonia 11.3.11 with `Avalonia.Controls.DataGrid`
- MVVM: CommunityToolkit.Mvvm 8.4.0
- Shared library: `FPlot.Math` (`Point2d`, `MathUtils.Sign` with `EPS = 1e-9`)
- Tests: **xUnit** (proposed; confirm). **Avalonia.Headless.XUnit** for the converter/grid binding tests.

## Acceptance Criteria
1. **Initial data.** On launch the grid shows 10 points `x = 0..9`, `y = round(e^x / 100, 2)`, none marked changed.
2. **Display precision.** Values display in fixed-point `F6` in the current culture; `NaN` displays `N/A`.
3. **Editing a cell** to a valid number updates the point and redraws the plot.
4. **Invalid input is ignored.** Typing a non-number keeps the previous value.
5. **Rounding is not an edit.** Writing back the displayed `F6` text of an unedited value does not change the
   stored full-precision value and does not mark the point changed.
6. **Change detection.** A coordinate is "changed" when it differs from its last saved/loaded value by more
   than `1e-9`. Changed cells render **LightCoral**; unchanged cells render **Black**.
7. **Add Point** appends a point at `(lastX + 1, lastY)`, or `(0, 0)` if the list is empty. The new point
   counts as an unsaved change even before editing.
8. **Delete** is enabled only when a point has been clicked on the plot; it removes that point from grid and
   plot, clears the grid selection if it was that point, counts as an unsaved change, and disables itself.
9. **Saving clears change marks.** After a successful Save/Save As, all cells render unchanged and new points
   are no longer "added".

## Test Approach
- **Unit:** `PointViewModel` (criteria 4–7, 9), `MainViewModel` add/remove (7, 8), `MainViewModelInit` (1),
  `DoubleToStringConverter` and `ChangedToBrushConverter` (2, 6).
- **UI (headless):** edit a DataGrid cell and assert the bound value (3).

## Smoke Test
```bash
cd src && dotnet build FPlot.sln && dotnet run --project FPlot
```
App opens with 10 points; edit a Y cell → text turns coral and plot updates; Add Point → new row appears.

## Known gaps / questions for review
- Delete acts on the point **clicked on the plot**, not the row selected in the grid — intended?
- Edits are culture-sensitive (grid) while files are invariant (CSV). Confirm this split.

## Out of Scope
Sorting, multi-row selection, undo/redo, reordering rows.
