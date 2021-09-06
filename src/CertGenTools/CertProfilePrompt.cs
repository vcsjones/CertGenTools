using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spectre.Console;

record CertificateProfile(string CommonName, string[] DnsNames, OidCollection Ekus);

internal static partial class Prompts
{
    private enum CertProfile
    {
        [Description("HTTPS Web Server Certificate")]
        HttpsCertificate,
        [Description("Code Signing")]
        CodeSigning,
        [Description("Custom")]
        Custom
    }

    public static CertificateProfile CertProfilePrompt()
    {
        var profilePrompt = AnsiConsole.Prompt(
            new SelectionPrompt<CertProfile>()
                .Title("What kind of certificate is this?")
                .UseConverter(EnumDescriptionConverter)
                .AddChoices(CertProfile.HttpsCertificate, CertProfile.CodeSigning, CertProfile.Custom));

        switch (profilePrompt)
        {
            case CertProfile.HttpsCertificate:
                string[] dnsNames = StringListPrompt("What domains is this HTTPS certificate valid for? (use a blank entry to indicate you're done)");
                OidCollection ekus = new() {
                    new Oid("1.3.6.1.5.5.7.3.1"),
                    new Oid("1.3.6.1.5.5.7.3.2")
                };

                return new CertificateProfile(dnsNames[0], dnsNames, ekus);
        }
        
        throw new InvalidOperationException();
    }

    public static string[] StringListPrompt(string prompt)
    {
        AnsiConsole.MarkupLine(prompt);
        HashSet<string> inputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (AnsiConsole.Prompt(new TextPrompt<string>("> ").AllowEmpty()) is {} item)
        {
            if (item is { Length: 0 })
            {
                if (inputs.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]At least one is required.[/]");
                }
                else
                {
                    break;
                }
            }
            else
            {
                inputs.Add(item);
            }
        }

        return inputs.ToArray();
    }
}