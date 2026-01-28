using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FPlot.Model;

namespace FPlot.Utils;

public static class FileUtils
{
    public static async Task<IStorageFile?> OpenFileAsync(this Window window)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
            return null;
        var filePickerFileType = new FilePickerFileType("Csv file")
        {
            Patterns = ["*.csv"]
        };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open Data",
                SuggestedFileType = filePickerFileType,
                FileTypeFilter = [filePickerFileType],
                AllowMultiple = false
            });
        if (files.Count >= 1)
        {
            return files[0];
        }
        return null;
    }

    public static async Task<List<Point2d>> ReadPointsAsync(this Window window, IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var streamReader = new StreamReader(stream);
        var result = await ReadAsync(streamReader);
        return result;
    }

    public static async Task<bool> SaveFileAsync(this Window window, List<Point2d> points, string? fileName)
    {
        if (fileName is null || !File.Exists(fileName))
            return false;
        await using var streamWriter = new StreamWriter(fileName);
        await WriteAsync(streamWriter, points);
        await streamWriter.FlushAsync();
        return true;
    }

    public static async Task<string?> SaveFileAsync(this Window window, List<Point2d> points)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
            return null;
        var filePickerFileType = new FilePickerFileType("Csv file")
        {
            Patterns = ["*.csv"]
        };
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Data",
                SuggestedFileType = filePickerFileType,
                FileTypeChoices = [filePickerFileType],
                ShowOverwritePrompt = true
            });
        if (file is not null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var streamWriter = new StreamWriter(stream);
            await WriteAsync(streamWriter, points);
            await streamWriter.FlushAsync();
            return file.Path.AbsolutePath;
        }
        return null;
    }

    public static async Task SetClipboardAsync(this Window window, List<Point2d> points)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is null)
            return;
        var text = new StringBuilder();
        await WriteAsync(text, points);
        await clipboard.SetTextAsync(text.ToString());
    }

    public static Task<IClipboard?> GetClipboardAsync(this Window window)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        return Task.FromResult(clipboard);
    }

    public static async Task<bool> HasClipboardAsync(this Window window)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard == null)
            return false;
        var text = await clipboard.TryGetTextAsync();
        return !string.IsNullOrWhiteSpace(text);
    }

    public static async Task<List<Point2d>> ReadPointsAsync(this Window window, IClipboard file)
    {
        var text = await file.TryGetTextAsync();
        if (text is null)
            return new List<Point2d>();
        var points = await ReadAsync(text);
        return points;
    }

    private static Task<List<Point2d>> ReadAsync(String reader)
    {
        var result = new List<Point2d>();
        var text = reader.Split(System.Environment.NewLine);
        foreach (var line in text)
        {
            if (Point2d.TryParse(line, out var point))
            {
                result.Add(point);
            }
        }
        return Task.FromResult(result);
    }

    private static async Task<List<Point2d>> ReadAsync(StreamReader streamReader)
    {
        var result = new List<Point2d>();
        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync();
            if (Point2d.TryParse(line, out var point))
            {
                result.Add(point);
            }
        }
        return result;
    }

    private static Task WriteAsync(StringBuilder writer, List<Point2d> points)
    {
        foreach (var point in points)
        {
            writer.AppendLine(point.ToString());
        }
        return Task.CompletedTask;
    }

    private static async Task WriteAsync(StreamWriter writer, List<Point2d> points)
    {
        foreach (var point in points)
        {
            await writer.WriteLineAsync(point.ToString());
        }
    }
}