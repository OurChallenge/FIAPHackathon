namespace Hackathon.Application.Interfaces.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message);
}