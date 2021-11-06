using System;
namespace File_Manager
{
    class Program
    {
        static void Main()
        {
            try
            {
                Terminal.Init();
                Terminal.Run();
            }
            catch
            {
                Console.WriteLine("Произошла ошибка. Перезапуск терминала:");
                Terminal.Run();
            }
        }
    }
}

// COMMANDS:
// cd [/D] [диск:][путь] (если Windows)
// .. - Перейти на папку выше
// . - Перейти в папку с exeшником

// dir || ls

// match [regular_expr] [-recursive]
// regular_expression Само регулярное выражение. Если не указано - то включены все файлы.
// -recursive или -rec или -r Рекурсивно выводит все файлы и папки текущего каталога и всех подкаталогов. Если не указано - то поиск только по текущему каталогу.

// rrcopy [destination_path] [regular_expr] [-overwrite]
// Regular Recursive Copy. Скопировать все файлы из директории и всех её поддиректорий по маске в другую директорию, причём, если директория, в которую происходит копирование, не существует – она создаётся.
// regular_expression Само регулярное выражение. Если не указано - то включены все файлы.
// -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

// copy || cp [file\path\name] [destination\directory\path] [-overwrite]
// -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

// move || mv [file\\path\\name] [destination\\directory\\path] [-overwrite]
// -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

// del || rm [file\\path\\name]
// rmdir [directory\\path\\name]
// md || mkdir [directory\\path\\name]
// type || cat [file\\path\\name]
// mktxtfile [filename] [-encoding_name]
// -encoding_name Например: -utf-8, -ascii, -utf-16
// concat [filename_A] [filename_B] etc

