using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace eCommerce.BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly RabbitMQOptions _options;
        private readonly ILogger<RabbitMQPublisher> _logger;
        private readonly object _sync = new();
        private IConnection? _connection;
        private IModel? _channel;
        private bool _disposed;

        public RabbitMQPublisher(RabbitMQOptions options, ILogger<RabbitMQPublisher> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Publish(string routingKey, ProductEventMessage message)
        {
            try
            {
                byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                lock (_sync)
                {
                    EnsureChannel();

                    IBasicProperties properties = _channel!.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";

                    _channel.BasicPublish(
                        exchange: _options.ExchangeName,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body);
                }

                _logger.LogInformation(
                    "Evento {EventType} publicado com routing key {RoutingKey} para o produto {ProductID}.",
                    message.EventType,
                    routingKey,
                    message.ProductID);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ indisponível ao publicar {RoutingKey} do produto {ProductID}.",
                    routingKey,
                    message.ProductID);
            }
        }

        private void EnsureChannel()
        {
            if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
            {
                return;
            }

            DisposeConnection();

            ConnectionFactory factory = new()
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_sync)
            {
                DisposeConnection();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void DisposeConnection()
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao fechar o channel do RabbitMQ.");
            }

            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao fechar a connection do RabbitMQ.");
            }

            _channel = null;
            _connection = null;
        }
    }
}
