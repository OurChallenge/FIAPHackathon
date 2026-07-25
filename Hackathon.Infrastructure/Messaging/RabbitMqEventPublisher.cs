using System.Text;
using System.Text.Json;
using Hackathon.Application.Interfaces.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hackathon.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqSettings _settings;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    public async Task PublishAsync<T>(T message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        await using var connection =
            await factory.CreateConnectionAsync();

        await using var channel =
            await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: _settings.DonationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _settings.DonationQueueName,
            body: body);
    }
}