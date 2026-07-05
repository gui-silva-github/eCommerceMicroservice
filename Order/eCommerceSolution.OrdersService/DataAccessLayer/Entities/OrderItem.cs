using MongoDB.Bson.Serialization.Attributes;

namespace eCommerce.OrdersMicroservice.DataAccessLayer.Entities
{
    public class OrderItem
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public Guid _id { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public Guid ProductID { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.Double)]
        public decimal UnitPrice { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.Int32)]
        public int Quantity { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.Double)]
        public decimal TotalPrice { get; set; }
    }
}