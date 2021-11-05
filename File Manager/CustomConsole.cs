﻿using Spectre.Console;
using System.Threading;

/// <summary>
/// Wrapper for Spectre.Console class.
/// </summary>
public static class CustomConsole
{
    private static Canvas s_canvas = new Canvas(32, 32);
    private static Style s_menuHighlightStyle = new Style(HexToColor("#0D9688"));
    private const string MENU_COLOR = "#daa368";

    /// <summary>
    /// Clears the console buffer and erases displayed information.
    /// </summary>
    public static void ClearScreen()
    {
        System.Console.Clear();
    }

    /// <summary>
    /// Sleep current thread for given time (in milliseconds).
    /// </summary>
    /// <param name="ms">Number of milliseconds to sleep. 1000 = 1 second.</param>
    public static void Sleep(int ms)
    {
        Thread.Sleep(ms);
    }

    /// <summary>
    /// Prints colored <c>text</c>, with <c>delay</c> between each character.
    /// </summary>
    /// <param name="text">text</param>
    /// <param name="color"><c>color</c> of the text, has to be string in HEX format. Example: #ffffff</param>
    /// <param name="delay"><c>delay</c> between character outputs (bigger delay -> slower output)</param>
    public static void PrintAnimatedText(string text, string color = "#FFFFFF", int delay = 20)
    {
        for (int i = 0; i < text.Length; i++)
        {
            AnsiConsole.Write(new Markup($"[{color}]{text[i]}[/]"));
            Thread.Sleep(delay);
        }
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders given string and prints it.
    /// Examples of string markup can be found on https://spectreconsole.net/markup.
    /// </summary>
    /// <param name="text">hmmm, may it be text?</param>
    public static void PrintText(string text)
    {
        AnsiConsole.Write(new Markup($"{text}"));
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders given table.
    /// </summary>
    /// <param name="table">instance of Spectre.Console.Table</param>
    public static void PrintTable(Table table)
    {
        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Asks user a question, returns if answer is yes or no. No is default value.
    /// </summary>
    /// <param name="question">Text of the question. User must be able to answer YES or NO.</param>
    /// <returns></returns>
    public static bool Confirm(string question)
    {
        return AnsiConsole.Confirm(question, false);
    }

    /// <summary>
    /// Prints calculation menu with selectable options.
    /// </summary>
    /// <returns>Chosen option</returns>
    public static string PrintCalcMenuOptions()
    {
        return AnsiConsole.Prompt<string>(new SelectionPrompt<string>()
        .Title($"[{MENU_COLOR}]Choose an option that will be performed on the first matrix[/]")
        .PageSize(10)
        .AddChoices(new[] {
            $"[{MENU_COLOR}]Add number(+)[/]", $"[{MENU_COLOR}]Subtract number(-)[/]",
            $"[{MENU_COLOR}]Multiply by number(*)[/]", $"[{MENU_COLOR}]Sum (F + S)[/]",
            $"[{MENU_COLOR}]Subtract (F - S)[/]", $"[{MENU_COLOR}]Dot product (F * S)[/]",
            $"[{MENU_COLOR}]Transpose (T)[/]", $"[{MENU_COLOR}]Trace[/]",
            $"[{MENU_COLOR}]Determinant[/]", $"[{MENU_COLOR}]Back to Menu[/]"
        }).HighlightStyle(s_menuHighlightStyle));
    }

    /// <summary>
    /// Prints calculation menu with selectable options.
    /// </summary>
    /// <returns>Chosen option</returns>
    public static string PrintOptions(string[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = $"[{MENU_COLOR}]{options[i]}[/]";
            //options[i] = $"[{MENU_COLOR}]{options[i]}[/]";
        }
        string chosenOption = AnsiConsole.Prompt<string>(new SelectionPrompt<string>()
        .Title($"[{MENU_COLOR}]Choose the drive[/]")
        .PageSize(10).AddChoices(options).HighlightStyle(s_menuHighlightStyle));
        PrintText(chosenOption);
        PrintText(chosenOption.Length.ToString());
        return chosenOption.Substring(9, chosenOption.Length - 12);
    }

    /// <summary>
    /// Prints main menu with selectable options.
    /// </summary>
    /// <returns>Chosen option</returns>
    public static string PrintMenuOptions()
    {
        return AnsiConsole.Prompt<string>(new SelectionPrompt<string>()
        .Title($"[#daa368]Menu:[/]")
        .PageSize(10)
        .AddChoices(new[] {
            $"[{MENU_COLOR}]Set first matrix[/]", $"[{MENU_COLOR}]Set second matrix[/]",
            $"[{MENU_COLOR}]Swap matrices[/]", $"[{MENU_COLOR}]Load matrices from file[/]",
            $"[{MENU_COLOR}]Show matrices[/]", $"[{MENU_COLOR}]Calculation mode[/]",
            $"[{MENU_COLOR}]Guide[/]", $"[{MENU_COLOR}]Solve system with Gauss[/]",
            $"[{MENU_COLOR}]Exit[/]"
        }).HighlightStyle(s_menuHighlightStyle));
    }

    /// <summary>
    /// Converts hex string to Color.
    /// </summary>
    /// <param name="hexRepr">Hex representation of color.
    /// Format: #FFFFFF or FFFFFF. Case insensitive.
    /// </param>
    /// <returns>instance of Spectre.Console.Color</returns>
    public static Color HexToColor(string hexRepr)
    {
        try
        {
            byte r, g, b;
            if (hexRepr[0] == '#')
            {
                r = byte.Parse(hexRepr.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hexRepr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hexRepr.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color(r, g, b);
            }
            r = byte.Parse(hexRepr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hexRepr.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hexRepr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r, g, b);
        }
        catch
        {
            return new Color(255, 255, 250);
        }
    }

}