﻿using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

class Program
{
    static void Main()
    {
        Console.Write("Enter the folder to search: ");
        string? folderToSearch = Console.ReadLine()?.Trim();

        Console.Write("Enter the string to search for: ");
        string? searchTerm = Console.ReadLine()?.Trim();

        Console.Write("Count files before scanning? (yes/no): ");
        string? countFilesInput = Console.ReadLine()?.Trim().ToLower();
        bool countFiles = string.IsNullOrEmpty(countFilesInput) || countFilesInput.StartsWith('y');

        Console.Write("Open location on found files? (yes/no): ");
        string? openOnFoundInput = Console.ReadLine()?.Trim().ToLower();
        bool openOnFound = string.IsNullOrEmpty(openOnFoundInput) || openOnFoundInput.StartsWith('y');

        if (string.IsNullOrEmpty(folderToSearch) || !Directory.Exists(folderToSearch))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            return;
        }

        if (string.IsNullOrEmpty(searchTerm))
        {
            Console.WriteLine("Search term cannot be empty. Please try again.");
            return;
        }

        SearchStringInFiles(folderToSearch, searchTerm, countFiles, openOnFound);
    }

    static void SearchStringInFiles(string folder, string searchString, bool countFiles, bool openOnFound)
    {
        ConcurrentBag<string> foundFiles = new();
        int scannedFiles = 0;
        int totalFiles = 0;

        if (countFiles)
        {
            try
            {
                totalFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories).Count();
                Console.WriteLine($"Total files to scan: {totalFiles}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Skipping file count due to insufficient permissions.");
            }
        }
        else
        {
            Console.WriteLine("Skipping file count. Scanning directly...");
        }

        Parallel.ForEach(
            EnumerateFilesSafe(folder),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            file =>
            {
                try
                {
                    if (IsUtf8File(file) && FileContainsString(file, searchString))
                    {
                        foundFiles.Add(file);
                        if (openOnFound)
                        {
                            Process.Start("explorer.exe", $"/select,\"{file}\"");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error processing file {file}: {e.Message}");
                }

                int currentScanned = Interlocked.Increment(ref scannedFiles);
                if (countFiles)
                    Console.WriteLine($"Scanned {currentScanned}/{totalFiles}: {file}");
                else
                    Console.WriteLine($"Scanned {currentScanned}: {file}");
            });

        Console.WriteLine($"\nTotal files scanned: {scannedFiles}");
        Console.WriteLine($"Total files found: {foundFiles.Count}");

        if (foundFiles.Count > 0)
        {
            Console.WriteLine("\nFound files:");
            foreach (string foundFile in foundFiles)
            {
                Console.WriteLine(foundFile);
            }
        }
        else
        {
            Console.WriteLine("\nNo files found containing the search term.");
        }

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    static IEnumerable<string> EnumerateFilesSafe(string path)
    {
        Stack<string> dirs = new();
        List<string> files = new();
        dirs.Push(path);
    
        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            try
            {
                files.AddRange(Directory.EnumerateFiles(currentDir));
    
                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    dirs.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Access denied to: {currentDir}");
            }
        }
    
        return files;
    }


    static bool FileContainsString(string filePath, string searchString)
    {
        try
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8, true, bufferSize: 4096);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
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

            if (b <= 0x7F)
            {
                i++;
                continue;
            }
            else if ((b & 0xE0) == 0xC0)
            {
                if (i + 1 >= length || (bytes[i + 1] & 0xC0) != 0x80)
                    return false;
                i += 2;
            }
            else if ((b & 0xF0) == 0xE0)
            {
                if (i + 2 >= length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80)
                    return false;
                i += 3;
            }
            else if ((b & 0xF8) == 0xF0)
            {
                if (i + 3 >= length || (bytes[i + 1] & 0xC0) != 0x80 || (bytes[i + 2] & 0xC0) != 0x80 || (bytes[i + 3] & 0xC0) != 0x80)
                    return false;
                i += 4;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
