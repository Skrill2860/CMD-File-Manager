using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace File_Manager
{
    public sealed class Terminal
    {
        private static string currentPath;
        private static string destinationPath;
        private static readonly Dictionary<string, string> cmdsDescriptions = new()
        {
            { "cd", "cd [disc:][path]\n" },
            { "dir", "dir\n" },
            { "ls", "ls\n" },
            {
                "match",
                "match [--regular_expr] [-r]\n" +
                        "--regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.\n" +
                        "-recursive или -rec или -r Рекурсивно выводит все файлы и папки текущего каталога и всех подкаталогов"
            },
            {
                "rrcopy",
                "rrcopy [destination_path] [--regular_expr] [-overwrite]\n" +
                        "Regular Recursive Copy. Скопировать все файлы из директории и всех её поддиректорий по маске в другую директорию," +
                        " причём, если директория, в которую происходит копирование, не существует – она создаётся.\n" +
                        "--regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.\n" +
                        "-overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя."
            },
            { "copy", "copy [file\\path\\name] [destination\\directory\\path]" },
            { "cp", "cp [file\\path\\name] [destination\\directory\\path]" },
            { "move", "move [file\\path\\name] [destination\\directory\\path]" },
            { "mv", "mv [file\\path\\name] [destination\\directory\\path]" },
            { "del", "del [file\\path\\name]" },
            { "rm", "rm [file\\path\\name]" },
            { "rmdir", "rmdir [directory\\path\\name]" },
            { "md", "md [directory\\path\\name]" },
            { "mkdir", "mkdir [directory\\path\\name]" },
            { "type", "type [file\\path\\name]" },
            { "cat", "cat [file\\path\\name]" },
            { "mktxtfile", "mktxtfile [filename] [-enc encoding_name]" },
            { "concat", "concat [filename_A] [filename_B]" },
            //{ "", "" },
        };

        private static readonly string[] allowedCommands = { "cd", "dir", "ls",
                                                    "copy", "cp", "move",
                                                    "mv", "del", "rm",
                                                    "rmdir", "md", "mkdir",
                                                    "type", "cat", "concat" };

        public Terminal()
        {
            List<string> drives = new();
            try
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.IsReady)
                        drives.Add(di.Name);
                }
                currentPath = drives[0];
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private bool TryGetDirectoryPathIfExists(string path, out string outPath)
        {
            if (Directory.Exists(path))
            {
                outPath = path;
                return true;
            }
            else if (Directory.Exists($"{currentPath}\\{path}"))
            {
                outPath = $"{currentPath}\\{path}";
                return true;
            }
            outPath = currentPath;
            return false;
        }

        private bool TryGetFilePathIfExists(string path, out string outPath)
        {
            if (File.Exists(path))
            {
                outPath = path;
                return true;
            }
            else if (File.Exists($"{currentPath}\\{path}"))
            {
                outPath = $"{currentPath}\\{path}";
                return true;
            }
            outPath = currentPath;
            return false;
        }

        private string[] ParseInput(string input)
        {
            if (input == null) return new string[0];
            bool isInQuotes = false;
            int wordBeg = 0, wordEnd = 0;
            List<string> args = new();
            for (int i = 1; i < input.Length; i++)
            {
                if (!isInQuotes)
                {
                    if (input[i] == ' ' && input[i - 1] != ' ')
                    {
                        args.Add(input[wordBeg..i]);
                    }
                    else if (input[i] == '"' && input[i - 1] == ' ')
                    {
                        isInQuotes = true;
                        wordBeg = i + 1;
                    }
                    else if (input[i] != ' ' && input[i - 1] == ' ')
                    {
                        wordBeg = i;
                    }
                }
                else
                {
                    if (input[i] == '"')
                    {
                        isInQuotes = false;
                        args.Add(input[wordBeg..i]);
                    }
                }
            }
            args.Add(input[wordBeg..input.Length]);
            return args.ToArray();
        }

        private bool TryAutoCompleteCommand(string input, out string output)
        {
            // TODO: Try to parse path in the last argument, get dirs/files in the directory,
            // write possible variants, rewrite previous user input

            string[] args = ParseInput(input);

            if (args.Length < 2)
            {
                output = "";
                return false;
            }

            string basePath, startOfFileName;
            args[^1].Trim('\"');
            int lastSepIdx = args[^1].LastIndexOfAny(new char[] { '\\', '/' });
            if (lastSepIdx != -1)
            {
                basePath = args[^1][0..(lastSepIdx + 1)];
                startOfFileName = args[^1][(lastSepIdx + 1)..];
            }
            else
            {
                startOfFileName = args[^1];
                basePath = currentPath;
            }
            if (!TryGetDirectoryPathIfExists(basePath, out basePath))
            {
                output = input;
                return false;
            }

            List<string> possiblePaths = new();

            string[] fileUsingCommands = new string[] { "copy", "cp", "move", "mv", "del", "rm", "type", "cat", "concat" };

            foreach (var dir in new DirectoryInfo(basePath).GetDirectories())
            {
                if (dir.Name.ToLower().StartsWith(startOfFileName.ToLower()))
                {
                    possiblePaths.Add(dir.Name);
                }
            }
            if (fileUsingCommands.Contains(args[0]))
            {
                foreach (var file in new DirectoryInfo(basePath).GetFiles())
                {
                    if (file.Name.ToLower().StartsWith(startOfFileName.ToLower()))
                    {
                        possiblePaths.Add(file.Name);
                    }
                }
            }
            // If prediction is unique - substitute
            if (possiblePaths.Count() == 1)
            {
                StringBuilder guessBuilder = new();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    guessBuilder.Append(args[i] + " ");
                }
                if (possiblePaths[0].Contains(' '))
                    possiblePaths[0] = $"\"{possiblePaths[0]}\"";
                if (basePath != currentPath)
                    guessBuilder.Append(basePath);
                guessBuilder.Append(possiblePaths[0]);
                output = guessBuilder.ToString();
                return true;
            }
            // More than 1 guess - print them all
            StringBuilder outBuilder = new("", possiblePaths.Count * 12);
            foreach (var guess in possiblePaths)
            {
                outBuilder.Append(guess + " ");
            }
            output = outBuilder.ToString();
            return false;
        }

        private string ReadCommandLine()
        {
            StringBuilder commandBuilder = new(currentPath + ">");
            int minLineLength = commandBuilder.Length;
            Console.Write(currentPath + ">");
            while (true)
            {
                ConsoleKeyInfo keyPressed = Console.ReadKey();
                if (keyPressed.Key == ConsoleKey.Tab)
                {
                    if (TryAutoCompleteCommand(commandBuilder.ToString().Substring(minLineLength), out string guess))
                    {
                        commandBuilder = new($"{currentPath}>{guess}");
                        Console.Write($"\r{commandBuilder}");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine(guess);
                        Console.Write(commandBuilder);
                    }
                }
                else if (keyPressed.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return commandBuilder.ToString().Substring(minLineLength);
                }
                else if (keyPressed.Key == ConsoleKey.Backspace)
                {
                    if (commandBuilder.Length > minLineLength)
                    {
                        commandBuilder.Remove(commandBuilder.Length - 1, 1);
                        Console.Write(" \b");
                    }
                    else
                    {
                        Console.Write('>');
                    }
                }
                else
                {
                    try
                    {
                        commandBuilder.Append(keyPressed.KeyChar);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Input string is too long");
                        return "";
                    }
                }
            }
        }

        private void PrintCommandInfo(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("cd [disc:][path]\n" +
                    "dir || ls [-recursive]\n" +
                    "match [--regular_expr] [-r]\n" +
                    "rrcopy [destination_path] [--regular_expr] [-overwrite]\n" +
                    "copy || cp [file\\path\\name] [destination\\directory\\path]\n" +
                    "move || mv [file\\path\\name] [destination\\directory\\path]\n" +
                    "del || rm [file\\path\\name]\n" +
                    "rmdir [directory\\path\\name]\n" +
                    "md || mkdir [directory\\path\\name]\n" +
                    "type || cat [file\\path\\name]\n" +
                    "mktxtfile [filename] [-enc encoding_name]\n" +
                    "concat [filename_A] [filename_B] etc\n");
                return;
            }
            if (cmdsDescriptions.TryGetValue(args[1].ToLower(), out string desc))
                Console.WriteLine(desc);
            else
                Console.WriteLine("Напишите help чтобы получить список доступных команд");
            // COMMANDS:
            // cd [disc:][path]

            // dir || ls [-recursive]

            // match [--regular_expr] [-r]
            // --regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.
            // -recursive или -rec или -r Рекурсивно выводит все файлы и папки текущего каталога и всех подкаталогов

            // rrcopy [destination_path] [--regular_expr] [-overwrite]
            // Regular Recursive Copy. Скопировать все файлы из директории и всех её поддиректорий по маске в другую директорию, причём, если директория, в которую происходит копирование, не существует – она создаётся.
            // --regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.
            // -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

            // copy || cp [file\path\name] [destination\directory\path]
            // move || mv [file\path\name] [destination\directory\path]
            // del || rm [file_path_name]
            // rmdir [directory_path_name]
            // md || mkdir [directory_path_name]
            // type || cat [file_path_name]
            // mktxtfile [filename] [-enc encoding_name]
            // concat [filename_A] [filename_B] etc
        }

        private void ChooseDrive(string drive)
        {
            List<string> drives = new();
            try
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.IsReady)
                        drives.Add(di.Name);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }

            currentPath = drives.Contains(drive.ToUpper() + "\\") ? $"{drive.ToUpper()}\\" : throw new ArgumentException($"This drive is unavailable or does not exist: {drive.ToUpper()}\\");
        }

        private void ChangeDirectory(string[] args)
        {
            if (args.Length < 2) throw new ArgumentException("Not enough parameters. Destination path is not specified.");
            if (args[1] == "\\" || args[1] == "/")
            {
                if (OperatingSystem.IsWindows())
                {
                    currentPath = Path.GetPathRoot(currentPath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    currentPath = "/";
                }
                return;
            }
            // Move into exe directory
            if (args[1] == ".")
            {
                currentPath = Directory.GetCurrentDirectory() + "\\";
                return;
            }
            // Move into parent directory
            if (args[1] == "..")
            {
                if (currentPath == null || new DirectoryInfo(currentPath).Parent == null)
                    return;
                currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                return;
            }
            // Try move into directory if full path given
            if (Directory.Exists(args[1]))
            {
                currentPath = args[1] + (args[1].EndsWith('\\') ? "" : "\\");
                return;
            }
            // Try move into subdirectory if only name given 
            if (currentPath != null)
            {
                string temp = $"{currentPath}{(currentPath.EndsWith('\\') || args[1].StartsWith('\\') ? "" : '\\')}{args[1]}";
                if (Directory.Exists(temp))
                {
                    currentPath = temp;
                    return;
                }
            }
            // If nothing worked tell that we cannot 'cd'
            Console.WriteLine("This directory does not exist.");
        }

        private void PrintDirectoryContent(string[] args)
        {
            SortedDictionary<string, string> content = new();

            foreach (var file in new DirectoryInfo(currentPath).GetFiles())
                content[file.Name] = $"{file.CreationTime:dd.MM.yyyy hh:mm}           {file.Name}";
            foreach (var dir in new DirectoryInfo(currentPath).GetDirectories())
                content[dir.Name] = $"{dir.CreationTime:dd.MM.yyyy hh:mm}   <DIR>   {dir.Name}";
            foreach (var s in content.Values)
                Console.WriteLine(s);

        }

        private void PrintFilesRecursively(string regex, DirectoryInfo baseDir, string prevDirs = "")
        {
            try
            {
                foreach (FileInfo file in baseDir.GetFiles(regex))
                    Console.WriteLine($"{file.CreationTime:dd.MM.yyyy hh:mm}           {prevDirs}{file.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            foreach (DirectoryInfo dir in baseDir.GetDirectories())
                PrintFilesRecursively(regex, dir, prevDirs + baseDir.Name + '\\');
        }

        private void PrintFilesInDirectory(string regex, DirectoryInfo baseDir)
        {
            try
            {
                foreach (FileInfo file in baseDir.GetFiles(regex))
                    Console.WriteLine($"{file.CreationTime:dd.MM.yyyy hh:mm}           {file.Name}");
            }
            catch (Exception)
            {
                Console.WriteLine("Wrong search pattern");
            }
        }

        private void Match(string[] args)
        {
            bool recursive = false;
            string regex = "*";
            if (args.Contains("-r") || args.Contains("-rec") || args.Contains("-recursive"))
                recursive = true;
            if (args.Length > 3)
                throw new ArgumentException("Too much arguments");
            if (args.Length > 1 && !args[1].StartsWith("-r"))
            {
                regex = args[1];
            }
            else if (args.Length > 2)
            {
                regex = args[2];
            }
            Console.WriteLine(regex);
            if (recursive)
                PrintFilesRecursively(regex, new DirectoryInfo(currentPath));
            else
                PrintFilesInDirectory(regex, new DirectoryInfo(currentPath));
        }

        private static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs = false, bool useOverwrite = false, string mask = "*")
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        
        // If the destination directory doesn't exist, create it.       
        Directory.CreateDirectory(destDirName);        

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles(mask);
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, useOverwrite);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

        private void RRCopy(string[] args)
        {
            bool useOverwrite = false;
            string regex = "*";
            DirectoryInfo newDirectory = new DirectoryInfo(currentPath);
            if (args.Length > 4)
                throw new ArgumentException("Too much arguments");
            if (args.Length < 2)
                throw new ArgumentException("Destination path is not specified");
            if (args.Length > 1)
            {
                if (!TryGetDirectoryPathIfExists(args[1], out destinationPath))
                {
                    if (Path.IsPathRooted(args[1]))
                        destinationPath = args[1];
                    else
                        destinationPath = $"{currentPath}\\{args[1]}";
                    try
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return;
                    }
                }
                if (args.Length > 2)
                {
                    regex = args[2];
                }
                if (args.Length > 3 && (args[3] == "-overwrite" || args[3] == "-o"))
                {
                    useOverwrite = true;
                }
            }
            CopyDirectory(currentPath, destinationPath, true, useOverwrite, regex);
            destinationPath = "";
        }

        private void Copy(string[] args)
        {
            bool useOverwrite = false;
            if (args.Contains("-overwrite"))
                useOverwrite = true;
            if (args.Length > 4 || (args.Length > 3 && !useOverwrite))
                throw new ArgumentException("Too much arguments");
            if (args.Length < 3)
                throw new ArgumentException("Destination directory path is not specified");
            if (TryGetFilePathIfExists(args[1], out string filePath) && TryGetDirectoryPathIfExists(args[2], out string directoryPath))
            {
                FileInfo file = new FileInfo(filePath);
                File.Copy(filePath, $"{directoryPath}\\{file.Name}", useOverwrite);
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        private void Move(string[] args)
        {
            bool useOverwrite = false;
            if (args.Contains("-overwrite"))
                useOverwrite = true;
            if (args.Length > 4 || (args.Length > 3 && !useOverwrite))
                throw new ArgumentException("Too much arguments");
            if (args.Length < 3)
                throw new ArgumentException("Destination directory path is not specified");
            if (TryGetFilePathIfExists(args[1], out string filePath) && TryGetDirectoryPathIfExists(args[2], out string directoryPath))
            {
                FileInfo file = new FileInfo(filePath);
                File.Move(filePath, $"{directoryPath}\\{file.Name}", useOverwrite);
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        private void DeleteFile(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("File path is not specified");
            if (TryGetFilePathIfExists(args[1], out string filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        private void DeleteDirectory(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("Directory path is not specified");
            if (TryGetDirectoryPathIfExists(args[1], out string dirPath))
            {
                try
                {
                    Directory.Delete(dirPath);
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                throw new DirectoryNotFoundException($"Unable to find the directory: {args[1]}");
            }
        }

        private void CreateDirectory(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("New directory path is not specified");
            if (!TryGetDirectoryPathIfExists(args[1], out string dirPath))
            {
                try
                {
                    if (Path.IsPathRooted(args[1]))
                        Directory.CreateDirectory(args[1]);
                    else
                        Directory.CreateDirectory($"{currentPath}\\{args[1]}");
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                throw new IOException($"Directory already exists: {args[1]}");
            }
        }

        private void PrintTextFile(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("File path is not specified");
            if (TryGetFilePathIfExists(args[1], out string filePath))
            {
                try
                {
                    using StreamReader sr = new StreamReader(filePath);
                    while (!sr.EndOfStream)
                    {
                        Console.WriteLine(sr.ReadLine());
                    }
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        private void MakeTextFile(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("File path is not specified");
            string encoding = "";
            if (args.Length > 2)
            {
                if (args[2].StartsWith('-'))
                {
                    encoding = args[2][1..];
                }
            }
            else
            {
                encoding = "utf-8";
            }
            if (!TryGetFilePathIfExists(args[1], out string filePath))
            {
                try
                {
                    if (Path.IsPathRooted(args[1]))
                    {
                        filePath = args[1];
                    }
                    else
                    {
                        filePath = $"{currentPath}\\{args[1]}";
                    }
                    using FileStream newFile = new FileStream(filePath, FileMode.Create);
                    using StreamWriter sr = new StreamWriter(newFile, Encoding.GetEncoding(encoding));
                    string line = Console.ReadLine();
                    while (line != "q!")
                    {
                        sr.WriteLine(line);
                        line = Console.ReadLine();
                    }
                    sr.Flush();
                    sr.Close();
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Cannot make file {args[1]}. Access denied. {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Cannot make the file.\n{e.Message}");
                }
            }
            else
            {
                throw new ArgumentException($"File already exists: {args[1]}");
            }
        }

        private void ConcatenateFiles(string[] args)
        {
            const int chunkSize = 2 * 1024; // 2KB
            var inputFiles = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (TryGetFilePathIfExists(args[i], out args[i]))
                {
                    inputFiles.Add(args[i]);
                }
                else
                {
                    Console.WriteLine($"Could not find file {args[i]}. It will be ignored");
                }
            }
            using (var output = File.Create($"{currentPath}\\outputConcatenated.dat"))
            {
                foreach (var file in inputFiles)
                {
                    using (var input = File.OpenRead(file))
                    {
                        var buffer = new byte[chunkSize];
                        int bytesRead;
                        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            output.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
            using (var input = File.OpenRead($"{currentPath}\\output.dat"))
            {
                var buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Console.Write(Encoding.UTF8.GetString(buffer));
                }
            }
        }

        public void Run()
        {
            while (true)
            {
                string[] args = ParseInput(ReadCommandLine());
                if (args.Length == 0) continue;
                args[0] = args[0].ToLower();
                if (args[0].Contains(':') && args[0].Length == 2)
                {
                    try
                    {
                        ChooseDrive(args[0]);
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    switch (args[0])
                    {
                        case "h":
                        case "help":
                        case "-h":
                        case "-help":
                        case "--h":
                        case "--help":
                            PrintCommandInfo(args);
                            break;
                        case "cd":
                            try
                            {
                                ChangeDirectory(args);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "dir":
                        case "ls":
                            PrintDirectoryContent(args);
                            break;
                        case "match":
                            Match(args);
                            break;
                        case "copy":
                        case "cp":
                            try
                            {
                                Copy(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "rrcopy":
                            try
                            {
                                RRCopy(args);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "move":
                        case "mv":
                            try
                            {
                                Move(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "del":
                        case "rm":
                            try
                            {
                                DeleteFile(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "rmdir":
                            try
                            {
                                DeleteDirectory(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "mkdir":
                        case "md":
                            try
                            {
                                CreateDirectory(args);
                            }
                            catch (IOException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "type":
                        case "cat":
                            try
                            {
                                PrintTextFile(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "mtf":
                        case "mktxtfile":
                            try
                            {
                                MakeTextFile(args);
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "concat":
                            try
                            {
                                ConcatenateFiles(args);
                            }
                            catch (UnauthorizedAccessException e)
                            {
                                Console.WriteLine($"Permission denied. {e.Message}");
                            }
                            catch (FileNotFoundException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            break;
                        case "exit":
                            return;
                        case "curpath":
                            Console.WriteLine(currentPath);
                            break;
                        default:
                            Console.WriteLine($"Command @{args[0]}@ does not exist");
                            break;
                    }
                }
            }
        }
    }
}
