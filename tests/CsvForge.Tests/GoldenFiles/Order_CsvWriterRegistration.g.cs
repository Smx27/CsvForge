using System.Runtime.CompilerServices;

namespace Demo;

file static class Order_CsvWriterRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        global::CsvForge.CsvTypeWriterCache<global::Demo.Order>.RegisterGenerated(Order_CsvUtf16Writer.Instance);
        global::CsvForge.CsvUtf8TypeWriterCache<global::Demo.Order>.RegisterGenerated(Order_CsvUtf8Writer.Instance);
    }
}
