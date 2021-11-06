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
        /// Разбивает строку на команды, с учетом "", всё что внутри "" - один аргумент командной строки.
        /// </summary>
        /// <param name="input">Введенная строка</param>
        /// <returns>Массив строк - аргументы командной строки</returns>
        private static string[] ParseInput(string input)
        {
            if (input == null) return new string[0];
            bool isInQuotes = false;
            int wordBeg = 0;
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

        /// <summary>
        /// Если директория существует - возвращает True и записывает в <paramref name="outPath"/> полный путь до директории.
        /// Иначе возвращает False и записывает путь до текущей директории в <paramref name="outPath"/>.
        /// </summary>
        /// <param name="path">Путь до директории (полный или отностительный)</param>
        /// <param name="outPath">Путь до директории, если он существует</param>
        /// <returns>True если директория существует, иначе False</returns>
        private static bool TryGetDirectoryPathIfExists(string path, out string outPath)
        {
            if (Directory.Exists(path))
            {
                outPath = path;
                return true;
            }
            else if (Directory.Exists($"{s_currentPath}{s_separator}{path}"))
            {
                outPath = $"{s_currentPath}{s_separator}{path}";
                return true;
            }
            outPath = s_currentPath;
            return false;
        }

        /// <summary>
        /// Если файл существует - возвращает True и записывает в <paramref name="outPath"/> полный путь до файла.
        /// Иначе возвращает False и записывает путь до текущей директории в <paramref name="outPath"/>.
        /// </summary>
        /// <param name="path">Путь до файла (полный или отностительный)</param>
        /// <param name="outPath">Путь до файла, если он существует</param>
        /// <returns>True если файл существует, иначе False</returns>
        private static bool TryGetFilePathIfExists(string path, out string outPath)
        {
            if (File.Exists(path))
            {
                outPath = path;
                return true;
            }
            else if (File.Exists($"{s_currentPath}{s_separator}{path}"))
            {
                outPath = $"{s_currentPath}{s_separator}{path}";
                return true;
            }
            outPath = s_currentPath;
            return false;
        }

        /// <summary>
        /// Дополняет путь последнего вводимого аргумента в командной строке.
        /// Если вариантов дополения несколько, то выводит их все, не автозаполняя.
        /// </summary>
        /// <param name="input">Вводимая строка</param>
        /// <param name="output">Строка с вариантом(ами) дополнения</param>
        /// <returns>
        /// <code>True</code> если можно дозаполнить текущий аргумент.
        /// <code>False</code> если вариантов автозаполнения нет, или их несколько.
        /// </returns>
        private static bool TryAutoCompleteCommand(string input, out string output)
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
            int lastSepIdx = args[^1].LastIndexOfAny(new [] { s_separator, '/' });
            if (lastSepIdx != -1)
            {
                basePath = args[^1][0..(lastSepIdx + 1)];
                startOfFileName = args[^1][(lastSepIdx + 1)..];
            }
            else
            {
                startOfFileName = args[^1];
                basePath = s_currentPath;
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
                if (basePath != s_currentPath)
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

        /// <summary>
        /// Посимвольно считывает строку до переноса строки.
        /// Нужно, чтобы обрабатывать нажатия на отдельные клавиши.
        /// </summary>
        /// <returns>Возвращает строку, введенную пользователем.</returns>
        private static string ReadCommandLine()
        {
            StringBuilder commandBuilder = new(s_currentPath + ">");
            int minLineLength = commandBuilder.Length;
            Console.Write(s_currentPath + ">");
            int cursorPosX = Console.CursorLeft;
            while (true)
            {
                ConsoleKeyInfo keyPressed = Console.ReadKey();
                if (keyPressed.Key == ConsoleKey.Tab)
                {
                    if (TryAutoCompleteCommand(commandBuilder.ToString().Substring(minLineLength), out string guess))
                    {
                        commandBuilder = new($"{s_currentPath}>{guess}");
                        Console.Write($"\r{commandBuilder}");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine(guess);
                        Console.Write(commandBuilder);
                        cursorPosX = Console.CursorLeft;
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
                        if (Console.CursorLeft < cursorPosX)
                        {
                            Console.Write(" \b");
                        }
                        else
                        {
                            Console.Write("\b \b");
                        }
                    }
                    else
                    {
                        Console.Write('>');
                    }
                }
                else if (char.IsLetterOrDigit(keyPressed.KeyChar) ||
                    char.IsWhiteSpace(keyPressed.KeyChar) ||
                    char.IsPunctuation(keyPressed.KeyChar) ||
                    char.IsSeparator(keyPressed.KeyChar) ||
                    char.IsSymbol(keyPressed.KeyChar))
                {
                    try
                    {
                        cursorPosX = Console.CursorLeft;
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

        /// <summary>
        /// Выводит справку по пользованию программой.
        /// Если в аргумены передана команда, то выводит справку по ней.
        /// </summary>
        /// <param name="args">Параметры командной строки, включая саму команду.</param>
        private static void PrintCommandInfo(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("Справка:\n" +
                    "БОльшая часть команд - существующие в командной строке Windows, MacOS и Linux. Если знаете - пользоваться будет просто.\n" +
                    "Есть автодополнение, если вы начнете вводить путь. Если вариант дополнения один - оно вставит вариант за вас, иначе напишет все возможные.\n" +
                    "Оно +- умное, где нужно только папки - файлы не предложит. Наоборот не обязательно, потому +-.\n" +
                    "Для доп параметров есть сокращения, пишите help [название_команды] для подробной справки.\n" +
                    "cd . переходит в папку с exe'шником, чуть выше есть папки для поиграться с проверкой.\n" +
                    "Если папка(путь до неё) имеет пробелы в названии, то нужно обернуть его в двойные кавычки: \"path\\to\\file\"." +
                    "\nСписок команд:\n" +
                    "cd [disc:][path]\n" +
                    "dir || ls\n" +
                    "match [mask] [-r]\n" +
                    "rrcopy [destination_path] [mask] [-overwrite]\n" +
                    "copy || cp [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                    "move || mv [file\\path\\name] [destination\\directory\\path] [-overwrite]\n" +
                    "del || rm [file\\path\\name]\n" +
                    "rmdir [directory\\path\\name]\n" +
                    "md || mkdir [directory\\path\\name]\n" +
                    "type || cat [file\\path\\name] [-encoding_name]\n" +
                    "mktxtfile [filename] [-encoding_name]\n" +
                    "concat [filename_A] [filename_B] etc\n" +
                    "exit\n");
                return;
            }
            if (s_cmdsDescriptions.TryGetValue(args[1].ToLower(), out string desc))
                Console.WriteLine(desc);
            else
                Console.WriteLine("Напишите help чтобы получить список доступных команд");
        }
    }
}
