
using System;
using System.ComponentModel;
using System.Security.Cryptography;
using Spectre.Console;

abstract record KeyAlgorithm();
sealed record RsaKey(int KeySize, HashAlgorithmName HashAlgorithm) : KeyAlgorithm();
sealed record EcdsaKey(ECCurve Curve, HashAlgorithmName HashAlgorithm) : KeyAlgorithm(); 

internal static partial class Prompts
{
    private enum KeyType
    {
        [Description("RSA (2048 bit) + SHA256")]
        Rsa2048,
        [Description("ECDSA (P-256) + SHA256")]
        EcdsaP256,
        [Description("RSA Custom")]
        RsaCustom,
        [Description("ECDSA Custom")]
        EcdsaCustom,
    }

    public static KeyAlgorithm KeyTypePrompt()
    {
        var keyType = AnsiConsole.Prompt(
            new SelectionPrompt<KeyType>()
                .Title("What kind of key?")
                .UseConverter(EnumDescriptionConverter)
                .AddChoices(KeyType.Rsa2048, KeyType.EcdsaP256, KeyType.RsaCustom, KeyType.EcdsaCustom));

        switch (keyType)
        {
            case KeyType.Rsa2048:
                return new RsaKey(2048, HashAlgorithmName.SHA256);
            case KeyType.EcdsaP256:
                return new EcdsaKey(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256);
            case KeyType.RsaCustom:
                int size = AnsiConsole.Prompt(
                    new TextPrompt<int>("How many bits (Greater than or equal to 2048)?")
                        .Validate(size => size >= 512 && size % 64 == 0 && size <= 8192));
                
                if (size < 2048 &&
                    !AnsiConsole.Confirm("[red]You chose an RSA key size less than 2048, which is too small. Are you certain?[/]", defaultValue: false))
                {
                    goto case KeyType.RsaCustom;
                }
                else if (size % 1024 != 0 || size > 4096 &&
                    !AnsiConsole.Confirm("[yellow]You chose an unusual RSA key size which may not be supported by all software. Are you certain?[/]", defaultValue: false))
                {
                    goto case KeyType.RsaCustom;
                }

                var hash = HashAlgorithmPrompt();
                return new RsaKey(size, hash);
            case KeyType.EcdsaCustom:
                var curve = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose a curve")
                        .AddChoices(nameof(ECCurve.NamedCurves.nistP256), nameof(ECCurve.NamedCurves.nistP384), nameof(ECCurve.NamedCurves.nistP521))
                );

                return curve switch
                {
                    nameof(ECCurve.NamedCurves.nistP256) => new EcdsaKey(ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256),
                    nameof(ECCurve.NamedCurves.nistP384) => new EcdsaKey(ECCurve.NamedCurves.nistP384, HashAlgorithmName.SHA384),
                    nameof(ECCurve.NamedCurves.nistP521) => new EcdsaKey(ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512),
                    _ => throw new InvalidOperationException("Not all EC curves were handled."),
                };
            default:
                throw new InvalidOperationException("Unknown key type.");
        }

        static HashAlgorithmName HashAlgorithmPrompt()
        {
            var hash = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a hash algorithm")
                    .AddChoices(nameof(HashAlgorithmName.SHA256), nameof(HashAlgorithmName.SHA384), nameof(HashAlgorithmName.SHA512))
            );

            return new HashAlgorithmName(hash);
        }
    }
}