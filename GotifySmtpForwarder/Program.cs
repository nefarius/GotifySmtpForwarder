using GotifySmtpForwarder;

using Polly;
using Polly.Contrib.WaitAndRetry;

using Refit;

using SmtpServer;
using SmtpServer.Storage;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        IConfigurationSection config = context.Configuration.GetSection("Gotify");

        services.Configure<Gotify>(config);

        services.AddTransient<IMessageStore, GotifyMessageStore>();

        services
            .AddRefitClient<IGotifyApi>()
            .ConfigureHttpClient(c =>
            {
                Gotify? cfg = config.Get<Gotify>();

                c.BaseAddress = new Uri(cfg!.ServerUrl);
                c.DefaultRequestHeaders.Add("X-Gotify-Key", cfg!.Key);
            }).AddTransientHttpErrorPolicy(pb =>
                pb.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(5), 10)));

        services.AddSingleton(
            provider =>
            {
                ISmtpServerOptions? options = new SmtpServerOptionsBuilder()
                    .ServerName("SMTP Server")
                    .Port(9025)
                    .Build();

                return new SmtpServer.SmtpServer(options, provider.GetRequiredService<IServiceProvider>());
            });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();