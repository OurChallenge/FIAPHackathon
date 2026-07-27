namespace Hackathon.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string DonationQueueName { get; set; }
        = "donation-received";

    public string DonationRetryQueueName { get; set; }
        = "donation-received-retry";

    public int DonationRetryDelayMilliseconds { get; set; }
        = 5000;

    public int DonationMaxRetries { get; set; }
        = 2;

    public string DonationDeadLetterQueueName { get; set; }
        = "donation-received-dlq";
}