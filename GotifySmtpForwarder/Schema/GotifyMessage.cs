using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GotifySmtpForwarder.Schema;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class GotifyMessage
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = null!;

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;
}