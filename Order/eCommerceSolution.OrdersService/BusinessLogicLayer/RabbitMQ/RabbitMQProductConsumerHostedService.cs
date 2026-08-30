using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductConsumerHostedService : IHostedService, IDisposable
    {
        private const string QueueName = "orders.product-cache";
        private const string BindingRoutingKey = "product.*";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly RabbitMQOptions _options;
        private readonly IDistributedCache _distributedCache;
        private readonly ResilienceOptions _resilienceOptions;
        private readonly ILogger<RabbitMQProductConsumerHostedService> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private bool _disposed;

        public RabbitMQProductConsumerHostedService(
            RabbitMQOptions options,
            IDistributedCache distributedCache,
            ResilienceOptions resilienceOptions,
            ILogger<RabbitMQProductConsumerHostedService> logger)
        {
            _options = options;
            _distributedCache = distributedCache;
            _resilienceOptions = resilienceOptions;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
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

                _channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueBind(
                    queue: QueueName,
                    exchange: _options.ExchangeName,
                    routingKey: BindingRoutingKey);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                EventingBasicConsumer consumer = new(_channel);
                consumer.Received += OnReceived;

                _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation(
                    "Consumer RabbitMQ iniciado na fila {QueueName} (autoAck: false, binding {Binding}).",
                    QueueName,
                    BindingRoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ indisponível ao iniciar o consumer. O cache do Orders não será sincronizado até o próximo restart.");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            _logger.LogInformation("Consumer RabbitMQ encerrado.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao fechar o channel do consumer RabbitMQ.");
            }

            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao fechar a connection do consumer RabbitMQ.");
            }

            _channel = null;
            _connection = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void OnReceived(object? sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                string json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                ProductEventMessage? message = JsonSerializer.Deserialize<ProductEventMessage>(json, JsonOptions);

                if (message == null || message.ProductID == Guid.Empty)
                {
                    _logger.LogError(
                        "Mensagem RabbitMQ inválida. DeliveryTag {DeliveryTag}.",
                        eventArgs.DeliveryTag);
                    _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Mensagem recebida: {EventType} produto {ProductID} (routing key {RoutingKey}).",
                    message.EventType,
                    message.ProductID,
                    eventArgs.RoutingKey);

                HandleMessageAsync(message).GetAwaiter().GetResult();

                _channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao processar mensagem RabbitMQ. DeliveryTag {DeliveryTag}.",
                    eventArgs.DeliveryTag);

                try
                {
                    _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception nackEx)
                {
                    _logger.LogWarning(nackEx, "Falha ao enviar Nack da mensagem {DeliveryTag}.", eventArgs.DeliveryTag);
                }
            }
        }

        private Task HandleMessageAsync(ProductEventMessage message)
        {
            return message.EventType.ToLowerInvariant() switch
            {
                "created" => HandleProductCreation(message),
                "updated" => HandleProductUpdation(message),
                "deleted" => HandleProductDeletion(message),
                _ => HandleUnknownEvent(message)
            };
        }

        private async Task HandleProductCreation(ProductEventMessage message)
        {
            await RefreshProductCacheAsync(message);
            _logger.LogInformation("Cache do produto {ProductID} atualizado após criação.", message.ProductID);
        }

        private async Task HandleProductUpdation(ProductEventMessage message)
        {
            await RefreshProductCacheAsync(message);
            _logger.LogInformation("Cache do produto {ProductID} atualizado após alteração.", message.ProductID);
        }

        private async Task HandleProductDeletion(ProductEventMessage message)
        {
            await TryRemoveCacheAsync($"{message.ProductID}");
            _logger.LogInformation("Cache do produto {ProductID} invalidado após exclusão.", message.ProductID);
        }

        private Task HandleUnknownEvent(ProductEventMessage message)
        {
            _logger.LogWarning(
                "EventType desconhecido {EventType} para o produto {ProductID}. Mensagem ignorada.",
                message.EventType,
                message.ProductID);
            return Task.CompletedTask;
        }

        private async Task RefreshProductCacheAsync(ProductEventMessage message)
        {
            ProductDTO product = new(
                message.ProductID,
                message.ProductName,
                message.Category,
                message.UnitPrice,
                message.QuantityInStock);

            await TrySetCacheAsync($"{message.ProductID}", product);
        }

        private async Task TrySetCacheAsync<T>(string cacheKey, T value)
        {
            try
            {
                DistributedCacheEntryOptions options = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_resilienceOptions.CacheSeconds)
                };

                await _distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(value, JsonOptions),
                    options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao gravar a chave {CacheKey}.", cacheKey);
            }
        }

        private async Task TryRemoveCacheAsync(string cacheKey)
        {
            try
            {
                await _distributedCache.RemoveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao invalidar a chave {CacheKey}.", cacheKey);
            }
        }
    }
}
