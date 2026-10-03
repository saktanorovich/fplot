using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

namespace FPlot.Utils;

public static class FileUtils
{
    public static async Task<IStorageFile?> OpenFileAsync(this Window window)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
        {
            return null;
        }

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

    public static async Task<List<string>> ReadLinesAsync(this Window window, IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var streamReader = new StreamReader(stream);
        var result = await ReadAsync(streamReader);
        return result;
    }

    public static async Task<bool> SaveFileAsync(this Window window, IEnumerable<string> lines, string? fileName)
    {
        if (fileName is null || !File.Exists(fileName))
        {
            return false;
        }

        await using var streamWriter = new StreamWriter(fileName);
        await WriteAsync(streamWriter, lines);
        await streamWriter.FlushAsync();
        return true;
    }

    public static async Task<string?> SaveFileAsync(this Window window, IEnumerable<string> lines)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is null)
        {
            return null;
        }

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
            await WriteAsync(streamWriter, lines);
            await streamWriter.FlushAsync();
            return file.TryGetLocalPath() ?? file.Path.LocalPath;
        }
        return null;
    }

    public static async Task SetClipboardAsync(this Window window, IEnumerable<string> lines)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var text = new StringBuilder();
        foreach (var line in lines)
        {
            text.AppendLine(line);
        }
        await clipboard.SetTextAsync(text.ToString());
    }

    public static Task<IClipboard?> GetClipboardAsync(this Window window)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        return Task.FromResult(clipboard);
    }

    public static async Task<List<string>> ReadLinesAsync(this Window window, IClipboard file)
    {
        var text = await file.TryGetTextAsync();
        if (text is null)
        {
            return new List<string>();
        }

        return text.Split(["\n", "\r"], StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static async Task<List<string>> ReadAsync(StreamReader streamReader)
    {
        var result = new List<string>();
        while (await streamReader.ReadLineAsync() is { } line)
        {
            result.Add(line);
        }
        return result;
    }

    private static async Task WriteAsync(StreamWriter writer, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line);
        }
    }
}
