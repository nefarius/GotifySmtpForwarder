using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GotifySmtpForwarder.Schema;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal sealed class ExtraClientDisplay
{
    public const string ContentTypeText = "text/plain";
    public const string ContentTypeMarkdown = "text/markdown";

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
internal sealed class GotifyMessageExtras
{
    [JsonPropertyName("client::display")]
    public ExtraClientDisplay? ClientDisplay { get; set; }
}

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal sealed class GotifyMessage
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = null!;

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("extras")]
    public GotifyMessageExtras? Extras { get; set; }
}