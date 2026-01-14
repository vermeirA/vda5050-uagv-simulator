using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using VehicleSim.Infrastructure.Mqtt;

public interface IMqttAdapter
{
    Task ConnectAsync();
    Task SubscribeAsync(string serialNumber);
    Task UnsubscribeAsync(string serialNumber);
    Task PublishStateAsync(string serialNumber, string payload);
    Task PublishVisualizationAsync(string serialNumber, string payload);
    Task AnnounceConnectionAsync(string serialNumber, string payload);
    Task AnnounceDisconnectionAsync(string serialNumber, string payload);

    event Action<string, string> MessageReceived;
}

public class MqttAdapter(IMqttClient client, IOptions<MqttSettings> settings, ILogger<MqttAdapter> logger) : IMqttAdapter
{
    public event Action<string, string>? MessageReceived;

    public async Task ConnectAsync()
    {
        if (client.IsConnected) return;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.Value.BrokerIp, settings.Value.Port)
            .WithClientId($"Vehicle-Simulator-{Guid.NewGuid()}")
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options);

        client.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        logger.LogInformation("Connected to MQTT broker at {BrokerIp}:{Port}", settings.Value.BrokerIp, settings.Value.Port);
    }

    public async Task SubscribeAsync(string serialNumber)
    {
        var formattedTopic = FormatTopic(settings.Value.Topics.OrderTopic, serialNumber);

        var subOpts = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(formattedTopic, MqttQualityOfServiceLevel.ExactlyOnce)
            .Build();

        await client.SubscribeAsync(subOpts);
        logger.LogInformation("Vehicle {SerialNumber} subscribed to topic: {Topic}", serialNumber, formattedTopic);
    }

    public async Task UnsubscribeAsync(string serialNumber)
    {
        var formattedTopic = FormatTopic(settings.Value.Topics.OrderTopic, serialNumber);

        var unsubOpts = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(formattedTopic)
            .Build();

        await client.UnsubscribeAsync(unsubOpts);
        logger.LogInformation("Vehicle {SerialNumber} unsubscribed from topic: {Topic}", serialNumber, formattedTopic);
    }

    public Task PublishStateAsync(string serialNumber, string payload)
        => PublishAsync(settings.Value.Topics.StateTopic, serialNumber, payload, MqttQualityOfServiceLevel.ExactlyOnce);

    public Task PublishVisualizationAsync(string serialNumber, string payload)
        => PublishAsync(settings.Value.Topics.VisualizationTopic, serialNumber, payload, MqttQualityOfServiceLevel.AtMostOnce);

    public Task AnnounceConnectionAsync(string serialNumber, string payload)
        => PublishAsync(settings.Value.Topics.ConnectionTopic, serialNumber, payload, MqttQualityOfServiceLevel.ExactlyOnce);

    public Task AnnounceDisconnectionAsync(string serialNumber, string payload)
        => PublishAsync(settings.Value.Topics.ConnectionTopic, serialNumber, payload, MqttQualityOfServiceLevel.ExactlyOnce);

    private async Task PublishAsync(string topicTemplate, string serialNumber, string payload, MqttQualityOfServiceLevel qos)
    {
        var formattedTopic = FormatTopic(topicTemplate, serialNumber);

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(formattedTopic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .Build();

        await client.PublishAsync(msg);
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        var topic = e.ApplicationMessage.Topic;

        logger.LogDebug("MQTT message received on topic: {Topic}", topic);
        MessageReceived?.Invoke(topic, payload);

        return Task.CompletedTask;
    }

    private string FormatTopic(string template, string serialNumber) =>
        template
            .Replace("{prefix}", settings.Value.TopicPrefix)
            .Replace("{serialNumber}", serialNumber);
}