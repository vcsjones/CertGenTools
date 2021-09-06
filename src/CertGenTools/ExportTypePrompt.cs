using System.ComponentModel;
using Spectre.Console;

public enum ExportType
{
    [Description("PFX / PKCS12")]
    Pkcs12,
    [Description("PEM / Text")]
    Pem
}

internal static partial class Prompts
{
    public static ExportType ExportTypePrompt(GenerateType generateType)
    {
        // CSRs can't export to PKCS12.
        if (generateType == GenerateType.CertificateRequest)
        {
            return ExportType.Pem;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<ExportType>()
                .Title("What format would you like to export it?")
                .UseConverter(EnumDescriptionConverter)
                .AddChoices(ExportType.Pem, ExportType.Pkcs12));
    }
}