using GotifySmtpForwarder.Schema;

using Refit;

namespace GotifySmtpForwarder;

/// <summary>
///     Gotify REST API implementation.
/// </summary>
internal interface IGotifyApi
{
    /// <summary>
    ///     Create a message.
    /// </summary>
    /// <remarks>https://gotify.net/api-docs#/message/createMessage</remarks>
    [Post("/message")]
    Task CreateMessage([Body] GotifyMessage message);
}