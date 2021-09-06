using Spectre.Console;

internal static partial class Prompts
{
    public static int ValidityDays()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>("How many days should the certificate be valid for?")
                .DefaultValue(365)
                .Validate(i => i > 0));
    }
}