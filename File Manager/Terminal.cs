using System;
using System.IO;
using System.Collections.Generic;

namespace File_Manager
{
    /// <summary>
    /// Симулирует поведение командной строки в консоли.
    /// Предназначен для работы с файлами.
    /// </summary>
    public static partial class Terminal
    {
        // Текущий путь, используется как база в неполных путях, постоянно выводится в командной строке
        private static string s_currentPath;

        // Путь КУДА записывать, используется в некоторых командах
        private static string s_destinationPath;

        // Подробные описания всех возможных команд
        // Советую свернуть
        private static readonly Dictionary<string, string> s_cmdsDescriptions = new()
        {
            {
                "cd",
                "cd [disc:][path]\n" +
                "Переходит в указанную директорию. Можно указать полный путь или его часть, тогда она будет добавлена к текущему пути\n" +
                "cd . для перехода в папку где лежит exe файл.\n" +
                "cd .. для перехода в родительскую директорию.\n"
            },
            {
                "dir",
                "dir\n" +
                "Выводит все файлы и поддиректории текущей директории\n"
            },
            {
                "ls",
                "ls\n" +
                "Выводит все файлы и поддиректории текущей директории\n"
            },
            {
                "match",
                "match [mask] [-r]\n" +
                "Выводит все ФАЙЛЫ подходящие под данную маску в текущей директории, можно включить в вывод файлы из поддиректорий [-r]\n" +
                "[mask]   Маска по которой будут отбираться файлы. Если не указанa - то включены все файлы.\n" +
                "[-recursive или -rec или -r]   Рекурсивно выводит все файлы и папки текущего каталога и всех подкаталогов\n"
            },
            {
                "rrcopy",
                "rrcopy [destination_path] [mask] [-overwrite]\n" +
                "Regular Recursive Copy. Копирует все файлы из директории и всех её поддиректорий по маске в другую директорию," +
                " причём, если директория, в которую происходит копирование, не существует – она создаётся.\n" +
                "[mask] Маска по которой будут отбираться файлы. Если не указанa - то включены все файлы.\n" +
                "[-overwrite или -o] Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.\n"
            },
            {
                "copy",
                "copy [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                "Копирует файл по пути [file\\path\\name] в директорию [destination\\directory\\path]\n" +
                "[-overwrite или -o] Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.\n"
            },
            {
                "cp",
                "cp [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                "Копирует файл по пути [file\\path\\name] в директорию [destination\\directory\\path]\n" +
                "[-overwrite или -o] Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.\n"
            },
            {
                "move",
                "move [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                "Перемещает файл по пути [file\\path\\name] в директорию [destination\\directory\\path]\n" +
                "[-overwrite или -o] Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.\n"
            },
            {
                "mv",
                "mv [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                "Перемещает файл по пути [file\\path\\name] в директорию [destination\\directory\\path]\n" +
                "[-overwrite или -o] Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.\n"
            },
            {
                "del",
                "del [file\\path\\name]\n" +
                "Удаляет файл по пути [file\\path\\name]\n"
            },
            {
                "rm",
                "rm [file\\path\\name]\n" +
                "Удаляет файл по пути [file\\path\\name]\n"
            },
            {
                "rmdir",
                "rmdir [directory\\path\\name]\n" +
                "Удаляет директорию по пути [file\\path\\name]\n"
            },
            {
                "md",
                "md [directory\\path\\name]\n" +
                "Создает все директории и поддиректории по пути [file\\path\\name]. Не создает уже существующие\n"
            },
            {
                "mkdir",
                "mkdir [directory\\path\\name]\n" +
                "Создает все директории и поддиректории по пути [file\\path\\name]. Не создает уже существующие\n"
            },
            {
                "type",
                "type [file\\path\\name] [-encoding_name]\n" +
                "Выводит файл в консоль в указанной кодировке. Кодировка по умолчанию: UTF-8\n" +
                "[-encoding_name]   Название кодировки. Например: -utf-8, -ascii, -utf-16\n"
            },
            {
                "cat",
                "cat [file\\path\\name] [-encoding_name]\n" +
                "Выводит файл в консоль в указанной кодировке. Кодировка по умолчанию: UTF-8\n" +
                "[-encoding_name]   Название кодировки. Например: -utf-8, -ascii, -utf-16\n"
            },
            {
                "mktxtfile",
                "mktxtfile [filename] [-encoding_name]\n" +
                "Создает текстовый файл в указанной кодировке. Кодировка по умолчанию: UTF-8\n" +
                "Сразу после этой команды нужно ввести в консоль текст, который будет записан в файл.\n" +
                "Чтобы закончить ввод текста нужно ввести на отдельной строке q!\n" +
                "Например:\n" +
                "some\\path\\>mtf text.txt -utf-16\n" +
                "123\n" +
                "q!\n" +
                "В файл будет записано: 123\n" +
                "[-encoding_name]   Название кодировки. Например: -utf-8, -ascii, -utf-16\n"
            },
            {
                "concat",
                "concat [filename_A] [filename_B] etc\n" +
                "Соединяет содержимое N файлов, записывает результат в outputConcatenated.dat и выводит результат в кодировке UTF-8\n" +
                "Складывать можно любые файлы, любого формата, хоть .docx с .png. Просто получится непонятно что.\n"
            },
            {
                "exit",
                "exit\n" +
                "Выходит из программы. Если что для людей крестик придумали...\n"
            }
        };

        private static char s_separator = '\\';

        /// <summary>
        /// Инициализирует s_currentPath, записывая в нее первый попавшийся рабочий логический диск.
        /// </summary>
        public static void Init()
        {
            List<string> drives = new();
            try
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.IsReady)
                        drives.Add(di.Name);
                }
                s_currentPath = drives[0];
            }
            catch (IOException e)
            {
                s_currentPath = "/";
                Console.WriteLine(e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                s_currentPath = "/";
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                s_currentPath = "/";
                Console.WriteLine(e.Message);
            }
            if (!OperatingSystem.IsWindows())
            {
                s_separator = '/';
            }
        }

        /// <summary>
        /// Запускает командную строку в консоли. Содержит основную логику, общий алгоритм командной строки.
        /// </summary>
        public static void Run()
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
                            PrintDirectoryContent();
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
                            Console.WriteLine(s_currentPath);
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
