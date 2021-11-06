using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace File_Manager
{
    public static partial class Terminal
    {
        /// <summary>
        /// Выбирает диск из готовых для работы логических дисков.
        /// </summary>
        /// <param name="drive">Диск который нужно выбрать</param>
        private static void ChooseDrive(string drive)
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

            s_currentPath = drives.Contains(drive.ToUpper() + separator) ? $"{drive.ToUpper()}{separator}" : throw new ArgumentException($"This drive is unavailable or does not exist: {drive.ToUpper()}\\");
        }

        /// <summary>
        /// Меняет <code>currentPath</code> в соответствии с аргументами, результат может зависеть от ОС.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void ChangeDirectory(string[] args)
        {
            if (args.Length < 2) throw new ArgumentException("Not enough parameters. Destination path is not specified.");
            if (args[1] == "\\" || args[1] == "/")
            {
                if (OperatingSystem.IsWindows())
                {
                    s_currentPath = Path.GetPathRoot(s_currentPath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    s_currentPath = "/";
                }
                return;
            }
            // Move into exe directory
            if (args[1] == ".")
            {
                s_currentPath = Directory.GetCurrentDirectory() + separator;
                return;
            }
            // Move into parent directory
            if (args[1] == "..")
            {
                if (s_currentPath == null || new DirectoryInfo(s_currentPath).Parent == null)
                    return;
                s_currentPath = new DirectoryInfo(s_currentPath).Parent.FullName;
                return;
            }
            // Try move into directory if full path given
            if (Directory.Exists(args[1]))
            {
                s_currentPath = args[1] + (args[1].EndsWith(separator) ? "" : separator);
                return;
            }
            // Try move into subdirectory if only name given 
            if (s_currentPath != null)
            {
                string temp = $"{s_currentPath}{(s_currentPath.EndsWith(separator) || args[1].StartsWith(separator) ? "" : separator)}{args[1]}";
                if (Directory.Exists(temp))
                {
                    s_currentPath = temp;
                    return;
                }
            }
            // If nothing worked tell that we cannot 'cd'
            Console.WriteLine("This directory does not exist.");
        }

        /// <summary>
        /// Выводит все файлы и поддиректории в текущей директории. Вывод формата cd в Windows.
        /// </summary>
        /// <param name="args"></param>
        private static void PrintDirectoryContent()
        {
            SortedDictionary<string, string> content = new();

            foreach (var file in new DirectoryInfo(s_currentPath).GetFiles())
                content[file.Name] = $"{file.CreationTime:dd.MM.yyyy hh:mm}           {file.Name}";
            foreach (var dir in new DirectoryInfo(s_currentPath).GetDirectories())
                content[dir.Name] = $"{dir.CreationTime:dd.MM.yyyy hh:mm}   <DIR>   {dir.Name}";
            foreach (var s in content.Values)
                Console.WriteLine(s);

        }

        /// <summary>
        /// Выводит файлы подходящие под данную маску в данной директории и всех ее поддиректориях.
        /// </summary>
        /// <param name="regex">Маска</param>
        /// <param name="baseDir">Директория в которой искать файлы</param>
        /// <param name="prevDirs">Пройденный путь, относительно <code>baseDir</code>, указывать не нужно</param>
        private static void PrintFilesRecursively(string regex, DirectoryInfo baseDir, string prevDirs = "")
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
                PrintFilesRecursively(regex, dir, prevDirs + baseDir.Name + separator);
        }

        /// <summary>
        /// Выводит файлы подходящие под данную маску в данной директории.
        /// </summary>
        /// <param name="regex">Маска</param>
        /// <param name="baseDir">Директория в которой искать файлы</param>
        private static void PrintFilesInDirectory(string regex, DirectoryInfo baseDir)
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

        /// <summary>
        /// Выводит все ФАЙЛЫ подходящие под данную маску в текущей директории.
        /// Можно включить в вывод файлы из поддиректорий (если указано в аргумента).
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void Match(string[] args)
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
                PrintFilesRecursively(regex, new DirectoryInfo(s_currentPath));
            else
                PrintFilesInDirectory(regex, new DirectoryInfo(s_currentPath));
        }

        /// <summary>
        /// Копирует файлы из папки <code>sourceDirName</code> в папку <code>destDirName</code>.
        /// </summary>
        /// <param name="sourceDirName">Путь до папки ИЗ которой копировать</param>
        /// <param name="destDirName">Путь до папки В которую копировать</param>
        /// <param name="copySubDirs">Копировать ли файлы из поддиректорий. По умолчанию: false</param>
        /// <param name="useOverwrite">Перезаписывать ли файлы при коллизиях. По умолчанию: false</param>
        /// <param name="mask">Маска по которой отбираются файлы. По умолчанию: * (то есть все файлы)</param>
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

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    if (tempPath.Length > 250)
                        throw new IOException("Path is too long");
                    CopyDirectory(subdir.FullName, tempPath, copySubDirs, useOverwrite, mask);
                }
            }

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles(mask);
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                try
                {
                    file.CopyTo(tempPath, useOverwrite);
                }
                catch { }
            }
        }

        /// <summary>
        ///  Копирует все файлы из директории и всех её поддиректорий по маске в другую директорию, 
        ///  причём, если директория, в которую происходит копирование, не существует – она создаётся.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void RRCopy(string[] args)
        {
            bool useOverwrite = false;
            string regex = "*";
            DirectoryInfo newDirectory = new DirectoryInfo(s_currentPath);
            if (args.Length > 4)
                throw new ArgumentException("Too much arguments");
            if (args.Length < 2)
                throw new ArgumentException("Destination path is not specified");
            if (args.Length > 1)
            {
                if (!TryGetDirectoryPathIfExists(args[1], out s_destinationPath))
                {
                    if (Path.IsPathRooted(args[1]))
                        s_destinationPath = args[1];
                    else
                        s_destinationPath = $"{s_currentPath}{separator}{args[1]}";
                    try
                    {
                        Directory.CreateDirectory(s_destinationPath);
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
            try
            {
                CopyDirectory(s_currentPath, s_destinationPath, true, useOverwrite, regex);
            }
            catch
            {
                throw;
            }
            s_destinationPath = "";
        }

        /// <summary>
        /// Копирует файл по данному в args[1] пути в директорию args[2]. Если указано в аргументах, перезаписывает уже существующий.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void Copy(string[] args)
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
                File.Copy(filePath, $"{directoryPath}{separator}{file.Name}", useOverwrite);
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        /// <summary>
        /// Перемещает файл по данному в args[1] пути в директорию args[2]. Если указано в аргументах, перезаписывает уже существующий.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void Move(string[] args)
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
                File.Move(filePath, $"{directoryPath}{separator}{file.Name}", useOverwrite);
            }
            else
            {
                throw new FileNotFoundException($"Unable to find the file: {args[1]}");
            }
        }

        /// <summary>
        /// Удаляет файл по данному в args[1] пути.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void DeleteFile(string[] args)
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

        /// <summary>
        /// Удаляет директорию по данному в args[1] пути.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void DeleteDirectory(string[] args)
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

        /// <summary>
        /// Создает все директории и поддиректории по данному в args[1] пути. Не создает уже существующие папки.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void CreateDirectory(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("New directory path is not specified");
            try
            {
                if (Path.IsPathRooted(args[1]))
                    Directory.CreateDirectory(args[1]);
                else
                    Directory.CreateDirectory($"{s_currentPath}{separator}{args[1]}");
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Выводит файл в консоль в указанной кодировке. Кодировка по умолчанию: UTF-8.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void PrintTextFile(string[] args)
        {
            string encoding = "utf-8";
            if (args.Length < 2)
                throw new ArgumentException("File path is not specified");
            if (args.Length > 2)
            {
                if (args[2].StartsWith('-'))
                {
                    encoding = args[2][1..];
                }
            }
            if (TryGetFilePathIfExists(args[1], out string filePath))
            {
                try
                {
                    using StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding(encoding));
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

        /// <summary>
        /// Создает текстовый файл в указанной кодировке, затем считывает текст файла до строки "q!".
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void MakeTextFile(string[] args)
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
                        filePath = $"{s_currentPath}{separator}{args[1]}";
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

        /// <summary>
        /// Соединяет содержимое N файлов, записывает результат в outputConcatenated.dat и выводит результат в кодировке UTF-8.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void ConcatenateFiles(string[] args)
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
            using (var output = File.Create($"{s_currentPath}{separator}outputConcatenated.dat"))
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
            using (var input = File.OpenRead($"{s_currentPath}{separator}output.dat"))
            {
                var buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Console.Write(Encoding.UTF8.GetString(buffer));
                }
            }
        }
    }
}
