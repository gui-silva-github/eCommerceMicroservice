namespace eCommerce.BusinessLogicLayer.RabbitMQ
{
    public interface IRabbitMQPublisher
    {
        void Publish(string routingKey, ProductEventMessage message);
    }
}
