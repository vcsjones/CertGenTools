using Spectre.Console;

abstract record PasswordResult();
sealed record NoPasswordResult() : PasswordResult;
sealed record SecretPasswordResult(string Password) : PasswordResult;

internal static partial class Prompts
{
    public static PasswordResult PasswordPrompt(string prompt)
    {
        if (AnsiConsole.Confirm(prompt))
        {
            return new SecretPasswordResult(AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret()));
        }
        else
        {
            return new NoPasswordResult();
        }
    }
}