using System.ComponentModel;
using Spectre.Console;

public enum GenerateType
{
    [Description("Certificate Signing Request (CSR)")]
    CertificateRequest,
    [Description("Self Signed Certificate")]
    SelfSignedCertificate,
}

internal static partial class Prompts
{
    public static GenerateType GenerateTypePrompt()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<GenerateType>()
                .Title("What would you like to create?")
                .UseConverter(EnumDescriptionConverter)
                .AddChoices(GenerateType.CertificateRequest, GenerateType.SelfSignedCertificate));
    }
}