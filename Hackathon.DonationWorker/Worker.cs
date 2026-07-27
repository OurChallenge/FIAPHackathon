using Hackathon.Application.Events;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Infrastructure.Messaging;
using Hackathon.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Hackathon.DonationWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<Worker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(
            cancellationToken: stoppingToken);

        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        // Dead Letter Queue: destino final das mensagens
        // que não puderam ser processadas após as tentativas.
        await _channel.QueueDeclareAsync(
            queue: _settings.DonationDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Retry Queue:
        // a mensagem permanece aqui por alguns segundos e,
        // após o TTL, volta automaticamente para a fila principal.
        var retryQueueArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = _settings.DonationRetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = _settings.DonationQueueName
        };

        await _channel.QueueDeclareAsync(
            queue: _settings.DonationRetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArguments,
            cancellationToken: stoppingToken);

        // Fila principal.
        // Mensagens rejeitadas definitivamente vão para a DLQ.
        var mainQueueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] =
                _settings.DonationDeadLetterQueueName
        };

        await _channel.QueueDeclareAsync(
            queue: _settings.DonationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainQueueArguments,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());

                var donationEvent =
                    JsonSerializer.Deserialize<DonationReceivedEvent>(json);

                if (donationEvent is null)
                    throw new InvalidOperationException(
                        "Invalid donation event.");

                await ProcessDonationAsync(
                    donationEvent,
                    stoppingToken);

                await _channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(args.BasicProperties.Headers);

                _logger.LogError(
                    ex,
                    "Error processing donation message. Retry {RetryCount}/{MaxRetries}.",
                    retryCount,
                    _settings.DonationMaxRetries);

                if (retryCount < _settings.DonationMaxRetries)
                {
                    var properties = new BasicProperties(args.BasicProperties)
                    {
                        Persistent = true
                    };

                    properties.Headers ??=
                        new Dictionary<string, object?>();

                    properties.Headers["x-retry-count"] =
                        retryCount + 1;

                    await _channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _settings.DonationRetryQueueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: args.Body,
                        cancellationToken: stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogWarning(
                        "Donation message sent to retry queue. Attempt {RetryCount}/{MaxRetries}.",
                        retryCount + 1,
                        _settings.DonationMaxRetries);
                }
                else
                {

                    await _channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);

                    _logger.LogError(
                        "Donation message exceeded retry limit and was sent to DLQ.");
                }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _settings.DonationQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Donation Worker listening to queue {QueueName}",
            _settings.DonationQueueName);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    private async Task ProcessDonationAsync(
        DonationReceivedEvent donationEvent,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var donationRepository =
            scope.ServiceProvider
                .GetRequiredService<IDonationRepository>();

        var campaignRepository =
            scope.ServiceProvider
                .GetRequiredService<ICampaignRepository>();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<HackathonDbContext>();

        var donation =
            await donationRepository.GetByIdAsync(
                donationEvent.DonationId);

        if (donation is null)
            throw new KeyNotFoundException(
                $"Donation {donationEvent.DonationId} not found.");

        // Idempotência: evita processar a mesma doação duas vezes.
        if (donation.Status == Domain.Enums.DonationStatus.Processada)
        {
            _logger.LogWarning(
                "Donation {DonationId} was already processed.",
                donation.Id);

            return;
        }

        var campaign =
            await campaignRepository.GetByIdAsync(
                donationEvent.CampaignId);

        if (campaign is null)
            throw new KeyNotFoundException(
                $"Campaign {donationEvent.CampaignId} not found.");

        campaign.AddDonation(donationEvent.Amount);

        donation.MarkAsProcessed();

        // Campaign e Donation são rastreadas pelo mesmo DbContext.
        // Um único SaveChanges persiste as duas alterações.
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Donation {DonationId} processed. Amount: {Amount}",
            donation.Id,
            donation.Amount);
    }

    private static int GetRetryCount(
    IDictionary<string, object?>? headers)
    {
        if (headers is null ||
            !headers.TryGetValue("x-retry-count", out var value) ||
            value is null)
        {
            return 0;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            byte byteValue => byteValue,
            _ => 0
        };
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }
}