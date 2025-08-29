using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.ActivitySources;


public static class BKSActivitySources
{
    public const string SourceName = "bks.sdk";

    public static readonly ActivitySource Framework = new(SourceName);
    public static readonly ActivitySource Pipeline = new($"{SourceName}.pipeline");
    public static readonly ActivitySource Processing = new($"{SourceName}.processing");
    public static readonly ActivitySource Events = new($"{SourceName}.events");
    public static readonly ActivitySource Security = new($"{SourceName}.security");
    public static readonly ActivitySource Validation = new($"{SourceName}.validation");

    public static void Dispose()
    {
        Framework?.Dispose();
        Pipeline?.Dispose();
        Processing?.Dispose();
        Events?.Dispose();
        Security?.Dispose();
        Validation?.Dispose();
    }
}

