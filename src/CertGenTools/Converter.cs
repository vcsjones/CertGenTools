using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

internal static partial class Prompts
{
    static string EnumDescriptionConverter<T>(T value) where T:struct, Enum
    {
        return typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .First(f => ((T)f.GetRawConstantValue()!).Equals(value))
            .GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
    }
}