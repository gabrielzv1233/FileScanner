using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Console.Write("Enter the folder to search: ");
        string folderToSearch = Console.ReadLine()?.Trim();

        Console.Write("Enter the string to search for: ");
        string searchTerm = Console.ReadLine()?.Trim();

        Console.Write("Skip file count? (yes/no): ");
        string skipCountInput = Console.ReadLine()?.Trim().ToLower();
        bool skipCount = skipCountInput?.StartsWith('y') ?? false;

        if (!Directory.Exists(folderToSearch))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            return;
        }

        SearchStringInFiles(folderToSearch, searchTerm, skipCount);
    }

    static void SearchStringInFiles(string folder, string searchString, bool skipCount)
    {
        int scannedFiles = 0;
        int foundFilesCount = 0;
        List<string> foundFiles = new();

        int? totalFiles = skipCount ? null : CountFiles(folder);

        if (totalFiles.HasValue)
            Console.WriteLine($"Total files to scan: {totalFiles}");
        else
            Console.WriteLine("Skipping file count. Scanning directly...");

        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
        {
            scannedFiles++;

            try
            {
                if (IsUtf8File(file) && FileContainsString(file, searchString))
                {
                    foundFiles.Add(file);
                    foundFilesCount++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing file {file}: {e.Message}");
            }

            // Progress display
            if (totalFiles.HasValue)
                Console.WriteLine($"Scanning {scannedFiles}/{totalFiles}: {file}");
            else
                Console.WriteLine($"Scanning {scannedFiles}: {file}");
        }

        Console.WriteLine($"\nTotal files scanned: {scannedFiles}");
        Console.WriteLine($"Total files found: {foundFilesCount}");
        
        if (foundFilesCount > 0)
        {
            Console.WriteLine("\nFound files:");
            foreach (string foundFile in foundFiles)
            {
                Console.WriteLine(foundFile);
                Process.Start("explorer.exe", $"/select,\"{foundFile}\"");
            }
        }
        else
        {
            Console.WriteLine("\nNo files found containing the search term.");
        }
        
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    static int CountFiles(string folder)
    {
        return Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories).Count();
    }

    static bool FileContainsString(string filePath, string searchString)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8, true);
        string content = reader.ReadToEnd();
        return content.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsUtf8File(string filePath)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (!IsValidUtf8(buffer, bytesRead))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    static bool IsValidUtf8(byte[] bytes, int length)
    {
        int i = 0;
        while (i < length)
        {
            byte b = bytes[i];

            if (b <= 0x7F) // ASCII byte
            {
                i++;
                continue;
            }
            else if ((b & 0xE0) == 0xC0) // 2-byte sequence
            {
                if (i + 1 >= length || (bytes[i + 1] & 0xC0) != 0x80)
                    return false;
                i += 2;
            }
            else if ((b & 0xF0) == 0xE0) // 3-byte sequence
            {
                if (i + 2 >= length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80)
                    return false;
                i += 3;
            }
            else if ((b & 0xF8) == 0xF0) // 4-byte sequence
            {
                if (i + 3 >= length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80 || (bytes[i + 3] & 0xC0) != 0x80)
                    return false;
                i += 4;
            }
            else
            {
                return false; // Invalid UTF-8 byte
            }
        }

        return true;
    }
}
