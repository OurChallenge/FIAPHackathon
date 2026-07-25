using System.Text;
using System.Text.Json;
using Hackathon.Application.Events;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        await _channel.QueueDeclareAsync(
            queue: _settings.DonationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
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
                _logger.LogError(
                    ex,
                    "Error processing donation message.");

                await _channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
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

        var donation =
            await donationRepository.GetByIdAsync(
                donationEvent.DonationId);

        if (donation is null)
            throw new KeyNotFoundException(
                $"Donation {donationEvent.DonationId} not found.");

        // Evita processar a mesma doação duas vezes.
        if (donation.Status ==
            Domain.Enums.DonationStatus.Processada)
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

        await campaignRepository.UpdateAsync(campaign);

        await donationRepository.UpdateAsync(donation);

        _logger.LogInformation(
            "Donation {DonationId} processed. Amount: {Amount}",
            donation.Id,
            donation.Amount);
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