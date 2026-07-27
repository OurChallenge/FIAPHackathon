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
            queue: _settings.DonationDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] =
                _settings.DonationDeadLetterQueueName
        };

        await channel.QueueDeclareAsync(
            queue: _settings.DonationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments);

        var json = JsonSerializer.Serialize(message);

        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _settings.DonationQueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}