using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

class Program
{
    static void Main()
    {
        string folderToSearch = GetInput("Enter the folder to search: ", Directory.GetCurrentDirectory());

        while (!Directory.Exists(folderToSearch))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            folderToSearch = GetInput("Enter the folder to search: ", Directory.GetCurrentDirectory());
        }
        
        string searchTerm = GetInput("Enter the string to search: ", "Hello World!");

        string caseSensitiveInput = GetInput("Case-sensitive search? (yes/no): ", "yes").ToLower();
        bool isCaseSensitive = caseSensitiveInput.StartsWith('y');

        string countFilesInput = GetInput("Count files before scanning? (yes/no): ", "yes").ToLower();
        bool countFiles = countFilesInput.StartsWith('y');

        string openOnFoundInput = GetInput("Open location on found files? (yes/no): ", "no").ToLower();
        bool openOnFound = openOnFoundInput.StartsWith('y');

        if (!Directory.Exists(folderToSearch))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            return;
        }

        SearchStringInFiles(folderToSearch, searchTerm, countFiles, openOnFound, isCaseSensitive);
    }

    static string GetInput(string prompt, string defaultValue)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            input = defaultValue;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine(prompt + input);
        }
        return input;
    }

    static void SearchStringInFiles(string folder, string searchString, bool countFiles, bool openOnFound, bool isCaseSensitive)
    {
        ConcurrentBag<string> foundFiles = new();
        int scannedFiles = 0;
        int totalFiles = 0;

        if (countFiles)
        {
            try
            {
                totalFiles = EnumerateFilesSafe(folder).Count();
                Console.WriteLine($"Total files to scan: {totalFiles}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during file count: {e.Message}");
            }
        }
        else
        {
            Console.WriteLine("Skipping file count. Scanning directly...");
        }
        Stopwatch stopwatch = Stopwatch.StartNew();
        Parallel.ForEach(
            EnumerateFilesSafe(folder),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            file =>
            {
                try
                {
                    if (IsUtf8File(file) && FileContainsString(file, searchString, isCaseSensitive))
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

        stopwatch.Stop();
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"\nTotal files scanned: {scannedFiles}");
        Console.WriteLine($"Total files found: {foundFiles.Count}");

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

    static bool FileContainsString(string filePath, string searchString, bool isCaseSensitive)
    {
        try
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8, true, bufferSize: 4096);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (isCaseSensitive)
                {
                    if (line.Contains(searchString, StringComparison.Ordinal))
                        return true;
                }
                else
                {
                    if (line.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
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
