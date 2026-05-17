using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gobo;

public enum BraceStyle
{
    SameLine,
    NewLine,
}

public record FormatOptions
{
    public bool UseTabs { get; set; } = false;
    public int TabWidth { get; set; } = 4;
    public int MaxLineWidth { get; set; } = 90;
    public bool FlatExpressions { get; set; } = false;
    public bool MultilineStructs { get; set; } = true;
    public bool MultilineArrays { get; set; } = true;
    public bool MultilineTernary { get; set; } = false;
    public bool LimitWidth { get; set; } = false;
    public bool BlankLineAfterBlocks { get; set; } = false;
    public bool ExplicitUndefined { get; set; } = false;
    public bool MultilineArguments { get; set; } = false;
    public bool MultilineAccessors { get; set; } = false;
    public bool MultilineConstructors { get; set; } = false;

    [JsonIgnore]
    public BraceStyle BraceStyle { get; set; } = BraceStyle.SameLine;

    [JsonIgnore]
    public bool ValidateOutput { get; set; } = true;

    [JsonIgnore]
    public bool RemoveSyntaxExtensions { get; set; } = false;

    [JsonIgnore]
    public bool GetDebugInfo { get; set; } = false;

    public static FormatOptions DefaultTestOptions { get; } = new() { GetDebugInfo = true };

    public static FormatOptions Default { get; } = new();
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(FormatOptions))]
public partial class FormatOptionsSerializer : JsonSerializerContext { }
