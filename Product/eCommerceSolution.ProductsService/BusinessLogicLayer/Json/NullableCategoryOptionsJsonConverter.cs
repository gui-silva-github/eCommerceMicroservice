using System.Text.Json;
using System.Text.Json.Serialization;
using eCommerce.BusinessLogicLayer.DTO;

namespace eCommerce.BusinessLogicLayer.Json
{
    public class NullableCategoryOptionsJsonConverter : JsonConverter<CategoryOptions?>
    {
        public override CategoryOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                if (Enum.TryParse(value, ignoreCase: true, out CategoryOptions category))
                {
                    return category;
                }

                throw new JsonException($"Categoria inválida: '{value}'.");
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intValue))
            {
                if (Enum.IsDefined(typeof(CategoryOptions), intValue))
                {
                    return (CategoryOptions)intValue;
                }

                throw new JsonException("Categoria inválida.");
            }

            throw new JsonException("Categoria inválida.");
        }

        public override void Write(Utf8JsonWriter writer, CategoryOptions? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.Value.ToString());
        }
    }
}
