using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Spectre.Console;
using static CustomConsole;

namespace File_Manager
{
    class Program
    {
        // Colors used for text highlighting
        const string WARNING_COLOR = "#eb3447";
        const string TEXT_COLOR = "#daa368";
        const string ADDITIONAL_TEXT_COLOR = "#896a70";
        const string CODE_COLOR = "#948cbb";

        // COMMANDS:
        // cd [/D] [диск:][путь]
        // .. - Перейти на папку выше
        // . - Перейти в папку с exeшником

        // dir || ls [-recursive]

        // match [--regular_expr] [-recursive]
        // --regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.
        // -recursive или -rec или -r Рекурсивно выводит все файлы и папки текущего каталога и всех подкаталогов. Если не указано - то поиск только по текущему каталогу.

        // rrcopy [destination_path] [--regular_expr] [-overwrite]
        // Regular Recursive Copy. Скопировать все файлы из директории и всех её поддиректорий по маске в другую директорию, причём, если директория, в которую происходит копирование, не существует – она создаётся.
        // --regular_expression Само регулярное выражение, -- в начале обязательно. Если не указано - то включены все файлы.
        // -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

        // copy || cp [file\path\name] [destination\directory\path] [-overwrite]
        // -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

        // move || mv [file\\path\\name] [destination\\directory\\path] [-overwrite]
        // -overwrite Если указано, то при копировании все файлы будут записаны поверх существующих, если они имеют одинаковое имя.

        // del || rm [file\\path\\name]
        // rmdir [directory\\path\\name]
        // md || mkdir [directory\\path\\name]
        // type || cat [file\\path\\name]
        // mktxtfile [filename] [-enc encoding_name]
        // concat [filename_A] [filename_B] etc

        static void Main(string[] args)
        {
            Terminal terminal = new();
            terminal.Run();
        }
    }
}
