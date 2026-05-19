namespace Gobo.Printer;

internal class DocPrinterOptions
{
    public bool UseTabs { get; init; } = false;
    public int TabWidth { get; init; } = 4;
    public bool TrimInitialLines { get; init; } = true;
}
