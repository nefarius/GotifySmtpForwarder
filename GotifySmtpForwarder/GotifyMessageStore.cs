using System.Buffers;

using Ganss.Xss;

using GotifySmtpForwarder.Schema;

using MimeKit;

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

            HtmlSanitizer sanitizer = new HtmlSanitizer();

            string sanitized = sanitizer.Sanitize(html);

            string? markdown = converter.Convert(sanitized);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message.");
            
            return SmtpResponse.MailboxUnavailable;
        }
    }
}