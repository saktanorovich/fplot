# Spec: CSV File I/O

**Status:** Draft — Claude-authored from existing code, pending user review
**Slug:** `csv-file-io`

## Summary
The user loads control points from a CSV file and saves them back, either to the same file
(Save) or to a new file (Save As). One point per line, `x,y`, invariant culture.

## Tech Stack
- Language/runtime: C# on .NET 10 (`net10.0`)
- UI: Avalonia 11.3.11 (`StorageProvider` file pickers)
- MVVM: CommunityToolkit.Mvvm 8.4.0 (`[RelayCommand]`, `CanExecute`)
- Tests: **xUnit** (proposed — repo has no test project yet; confirm). `IPointsIoService` is mocked/faked
  for view-model tests; `PointViewModel.TryParse`/`ToCsv` tested directly.

## Acceptance Criteria
1. **Open loads points.** Choosing a `.csv` file in the Open dialog replaces all current points with the
   file's points, in file order, and sets the path label to the file's local path.
2. **Open skips non-point lines.** Lines that do not parse as `x,y` (e.g. a header `x,y`, blank lines,
   non-numeric text) are ignored; remaining lines still load.
3. **Open cancelled is a no-op.** Cancelling the Open dialog leaves points and path unchanged.
4. **CSV format.** Each line is `x,y` with both numbers formatted/parsed in the invariant culture
   (`.` decimal separator) regardless of OS locale. Extra columns after the second are ignored on read.
5. **Save enabled only when meaningful.** Save is enabled iff the current path is an existing file
   **and** there are unsaved changes: an edited X or Y (difference > 1e-9), an added point, or a removed point.
6. **Save writes the file.** Save overwrites the current file with all points in grid order, one `x,y`
   line each; afterwards there are no unsaved changes and Save becomes disabled.
7. **Save As.** Save As is always enabled, opens a save dialog filtered to `*.csv` with overwrite
   prompt, writes all points, sets the path label to the new file, and clears unsaved changes.
8. **Save As cancelled is a no-op.** Cancelling leaves the path and unsaved-change state unchanged.
9. **Startup state.** On launch the path label is `Default: F(x) = e^x / 100` (not a file), so Save is
   disabled until a file is opened or Save As is used.

## Test Approach
- **Unit:** `PointViewModel.TryParse` / `ToCsv` (criteria 2, 4); `MainViewModel` with a fake
  `IPointsIoService` (criteria 1, 3, 5–9).
- **Integration:** `FileUtils.SaveFileAsync(lines, path)` round-trip on a temp file (criterion 6).
- File pickers themselves are not automated.

## Smoke Test
```bash
cd src && dotnet build FPlot.sln && dotnet run --project FPlot
```
Open `data/line.csv` → grid shows `(0,0)` and `(10,10)`; edit a value → Save enables; Save → Save disables;
reopen the file and the edit persisted.

## Known gaps / questions for review
- The "● Unsaved changes" label in `MenuView.axaml` is always visible (its `IsVisible` binding is commented
  out). Should it reflect the same unsaved-change state as criterion 5?
- No prompt to save unsaved changes before Open/Paste/quit replaces the data — intended?

## Out of Scope
Formats other than CSV; multiple series; file encodings other than the .NET default (UTF-8).
