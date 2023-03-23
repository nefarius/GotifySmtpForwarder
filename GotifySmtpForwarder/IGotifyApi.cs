using GotifySmtpForwarder.Schema;

using Refit;

namespace GotifySmtpForwarder;

/// <summary>
///     Gotify REST API implementation.
/// </summary>
internal interface IGotifyApi
{
    /// <summary>
    ///     https://gotify.net/api-docs#/message/createMessage
    /// </summary>
    [Post("/message")]
    Task CreateMessage([Body] GotifyMessage message);
}