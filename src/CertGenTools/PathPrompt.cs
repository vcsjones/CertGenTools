using System.IO;
using Spectre.Console;

internal static partial class Prompts
{
    public static string PathPrompt()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>("Where do you want to save your file?")
                .Validate(result => {
                    if (File.Exists(result))
                    {
                        return ValidationResult.Error("File already exists.");
                    }
                    if (Directory.Exists(result))
                    {
                        return ValidationResult.Error("Destination is a directory.");
                    }

                    return ValidationResult.Success();
                })
        );
    }
}