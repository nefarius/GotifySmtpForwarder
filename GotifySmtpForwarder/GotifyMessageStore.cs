using System.Buffers;

using Ganss.Xss;

using GotifySmtpForwarder.Schema;

using MimeKit;

using Refit;

using ReverseMarkdown;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace GotifySmtpForwarder;

internal class GotifyMessageStore : MessageStore
{
    private readonly IGotifyApi _gotifyApi;
    private readonly ILogger<GotifyMessageStore> _logger;

    public GotifyMessageStore(IGotifyApi gotifyApi, ILogger<GotifyMessageStore> logger)
    {
        _gotifyApi = gotifyApi;
        _logger = logger;
    }

    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            await using MemoryStream stream = new();

            // get message headers and body
            SequencePosition position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;

            // parses the MIME message
            MimeMessage? message = await MimeMessage.LoadAsync(stream, cancellationToken);

            if (message is null)
            {
                _logger.LogError("Failed to parse message body");
                
                return SmtpResponse.SyntaxError;
            }

            // fallback message if parsing fails
            string content = "<failed to parse message body>";

            switch (message.Body)
            {
                // body is HTML content
                case TextPart { IsHtml: true }:
                    {
                        string? html = message.HtmlBody;

                        if (string.IsNullOrEmpty(html))
                        {
                            _logger.LogError("Message text body is empty");

                            return SmtpResponse.SyntaxError;
                        }

                        HtmlSanitizer sanitizer = new();

                        // doesn't hurt, a lot of awful HTML out there :D
                        string sanitized = sanitizer.Sanitize(html).Trim(' ', '\t', '\n');

                        // converter options
                        Config config = new()
                        {
                            UnknownTags = Config.UnknownTagsOption.Drop,
                            GithubFlavored = true, // Gotify supports this, yay!
                            RemoveComments = true,
                            SmartHrefHandling = true
                        };

                        Converter converter = new(config);

                        string? markdown = converter.Convert(sanitized);

                        // an empty result isn't really useful, treat as error
                        if (string.IsNullOrEmpty(markdown))
                        {
                            _logger.LogError("HTML to Markdown conversion returned empty result");

                            return SmtpResponse.SyntaxError;
                        }

                        content = markdown;
                        break;
                    }
                // body is plain text
                case TextPart { IsPlain: true }:
                    content = message.TextBody;
                    break;
            }

            await _gotifyApi.CreateMessage(new GotifyMessage
            {
                Title = transaction.From.AsAddress(),
                Message = content,
                Extras = new GotifyMessageExtras
                {
                    ClientDisplay =
                        new ExtraClientDisplay { ContentType = ExtraClientDisplay.ContentTypeMarkdown }
                }
            });

            return SmtpResponse.Ok;
        }
        catch (ApiException apiException)
        {
            _logger.LogError(apiException, "Failed to submit Gotify message. Content: {Content}",
                apiException.Content);

            return SmtpResponse.MailboxUnavailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process mail message");

            return SmtpResponse.MailboxUnavailable;
        }
    }
}