using System.Diagnostics;

namespace FixtureHub.Application.Diagnostics;

public static class ApplicationTracing
{
    public const string SourceName = "FixtureHub.Application";

    public const string OperationTag = "fixturehub.operation";
    public const string ResultTag = "fixturehub.result";
    public const string ErrorCodeTag = "fixturehub.error_code";
    public const string EventIdTag = "fixturehub.event_id";

    public const string Command = "command";
    public const string Query = "query";
    public const string Success = "success";
    public const string Rejected = "rejected";

    internal static readonly ActivitySource Source = new(SourceName);
}
