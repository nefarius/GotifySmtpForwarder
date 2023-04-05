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

            SequencePosition position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;

            //string content = await new StreamReader(stream).ReadToEndAsync(cancellationToken);

            MimeMessage? message = await MimeMessage.LoadAsync(stream, cancellationToken);

            if (message is null)
            {
                await _gotifyApi.CreateMessage(new GotifyMessage
                {
                    Message = "Failed to parse message body!", Title = transaction.From.AsAddress()
                });

                return SmtpResponse.SyntaxError;
            }

            Config config = new()
            {
                UnknownTags = Config.UnknownTagsOption.Drop,
                GithubFlavored = false,
                RemoveComments = true,
                SmartHrefHandling = true
            };

            Converter converter = new(config);

            string? html = message.TextBody;

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogError("Message text body is empty.");

                return SmtpResponse.SyntaxError;
            }
            
            HtmlSanitizer sanitizer = new();

            string sanitized = sanitizer.Sanitize(html);

            string? markdown = converter.Convert(sanitized);

            if (string.IsNullOrEmpty(markdown))
            {
                _logger.LogError("HTML to Markdown conversion returned empty result.");

                return SmtpResponse.SyntaxError;
            }

            await _gotifyApi.CreateMessage(new GotifyMessage
            {
                Title = transaction.From.AsAddress(),
                Message = markdown,
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
            _logger.LogError(ex, "Failed to process mail message.");

            return SmtpResponse.MailboxUnavailable;
        }
    }
}