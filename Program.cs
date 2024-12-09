using System.Text;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.Write("Enter the folder to search: ");
        string folderToSearch = Console.ReadLine()?.Trim();

        Console.Write("Enter the string to search for: ");
        string searchTerm = Console.ReadLine()?.Trim();

        Console.Write("Count files before scanning? (yes/no): ");
        string countFilesInput = Console.ReadLine()?.Trim().ToLower();
        bool countFiles = countFilesInput?.StartsWith('y') ?? false;

        if (!Directory.Exists(folderToSearch))
        {
            Console.WriteLine("Invalid folder path. Please try again.");
            return;
        }

        SearchStringInFiles(folderToSearch, searchTerm, countFiles);
    }

    static void SearchStringInFiles(string folder, string searchString, bool countFiles)
    {
        int scannedFiles = 0;
        int foundFilesCount = 0;
        List<string> foundFiles = new();

        int? totalFiles = countFiles ? Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories).Count() : null;

        if (totalFiles.HasValue)
            Console.WriteLine($"Total files to scan: {totalFiles}");
        else
            Console.WriteLine("Skipping file count. Scanning directly...");

        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
        {
            scannedFiles++;

            string status = "Not Found";

            try
            {
                if (IsUtf8File(file) && FileContainsString(file, searchString))
                {
                    foundFiles.Add(file);
                    foundFilesCount++;
                    status = "Found";
                }
            }
            catch (Exception e)
            {
                status = $"Error ({e.Message})";
            }

            if (totalFiles.HasValue)
                Console.WriteLine($"Scanned {scannedFiles}/{totalFiles}: {file} | Status: {status}");
            else
                Console.WriteLine($"Scanned {scannedFiles}: {file} | Status: {status}");
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
        catch{}
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
