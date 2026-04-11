using System.Text.Json.Serialization;

namespace BatistaFloramar.Models
{
    public class DoacaoPaymentRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = "";

        [JsonPropertyName("issuer_id")]
        public string? IssuerId { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; } = "";

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("installments")]
        public int Installments { get; set; } = 1;

        [JsonPropertyName("payer")]
        public DoacaoPayer Payer { get; set; } = new();

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "Doação";
    }

    public class DoacaoPayer
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("identification")]
        public DoacaoIdentification Identification { get; set; } = new();
    }

    public class DoacaoIdentification
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "CPF";

        [JsonPropertyName("number")]
        public string Number { get; set; } = "";
    }
}
