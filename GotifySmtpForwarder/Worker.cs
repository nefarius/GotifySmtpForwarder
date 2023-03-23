namespace GotifySmtpForwarder;

public class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly SmtpServer.SmtpServer _smtpServer;

	public Worker(ILogger<Worker> logger, SmtpServer.SmtpServer smtpServer)
	{
		_logger = logger;
		_smtpServer = smtpServer;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Starting SMTP server");

		return _smtpServer.StartAsync(stoppingToken);
	}
}