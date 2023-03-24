using System.Buffers;

using GotifySmtpForwarder.Schema;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace GotifySmtpForwarder;

internal class GotifyMessageStore : MessageStore
{
    private readonly IGotifyApi _gotifyApi;

    public GotifyMessageStore(IGotifyApi gotifyApi)
    {
        _gotifyApi = gotifyApi;
    }

    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        await using MemoryStream stream = new();

        SequencePosition position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        string content = await new StreamReader(stream).ReadToEndAsync(cancellationToken);

        await _gotifyApi.CreateMessage(new GotifyMessage { Message = content, Title = transaction.From.AsAddress() });

        return SmtpResponse.Ok;
    }
}