using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Models;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Config;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Application.Services
{
    public class DoacaoService
    {
        private readonly string _accessToken;
        private readonly BatistaFloramarDbContext _db;

        public DoacaoService(IConfiguration config, BatistaFloramarDbContext db)
        {
            _accessToken = config["MercadoPago:AccessToken"] ?? "";
            _db = db;
        }

        public async Task<(bool sucesso, string mensagem, long? pagamentoId)> CriarPagamentoAsync(DoacaoPaymentRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Token))
                return (false, "Token de pagamento inválido.", null);

            if (req.TransactionAmount < 1)
                return (false, "Valor mínimo é R$1,00.", null);

            var descricao = req.Tipo.ToLower() switch
            {
                "dízimo" or "dizimo" => "Dízimo - Comunidade Batista Floramar",
                "oferta" => "Oferta - Comunidade Batista Floramar",
                _ => "Doação - Comunidade Batista Floramar"
            };

            var cpfLimpo = req.Payer.Identification.Number
                .Replace(".", "").Replace("-", "").Replace(" ", "");

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = req.TransactionAmount,
                Token = req.Token,
                Description = descricao,
                Installments = req.Installments,
                PaymentMethodId = req.PaymentMethodId,
                Payer = new PaymentPayerRequest
                {
                    Email = req.Payer.Email,
                    Identification = new IdentificationRequest
                    {
                        Type = req.Payer.Identification.Type,
                        Number = cpfLimpo
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(req.IssuerId))
                paymentRequest.IssuerId = req.IssuerId;

            try
            {
                var requestOptions = new RequestOptions { AccessToken = _accessToken };
                var client = new PaymentClient();
                var payment = await client.CreateAsync(paymentRequest, requestOptions);

                if (payment.Status?.ToString() == "approved")
                {
                    await SalvarEntradaAsync(payment.Id!.Value, req.Tipo, req.TransactionAmount);
                    return (true, "Pagamento aprovado! Que Deus abençoe sua contribuição.", payment.Id);
                }
                else if (payment.Status?.ToString() is "in_process" or "pending")
                {
                    return (true, "Pagamento em análise. Você receberá confirmação em breve.", payment.Id);
                }
                else
                {
                    var detail = payment.StatusDetail ?? payment.Status?.ToString() ?? "rejected";
                    return (false, TraducirErro(detail), payment.Id);
                }
            }
            catch (Exception)
            {
                return (false, "Erro ao conectar com o serviço de pagamento. Tente novamente.", null);
            }
        }

        public async Task ProcessarWebhookAsync(long pagamentoId)
        {
            var jaExiste = await _db.EntradasFinanceiras
                .AnyAsync(e => e.Origem == $"MP-{pagamentoId}");
            if (jaExiste) return;

            try
            {
                var requestOptions = new RequestOptions { AccessToken = _accessToken };
                var client = new PaymentClient();
                var payment = await client.GetAsync(pagamentoId, requestOptions);

                if (payment?.Status?.ToString() != "approved") return;

                var valor = payment.TransactionAmount ?? 0;
                var descricao = payment.Description ?? "";
                var tipo = descricao.Contains("Dízimo") ? "Dízimo" :
                           descricao.Contains("Oferta") ? "Oferta" : "Doação";

                await SalvarEntradaAsync(pagamentoId, tipo, valor);
            }
            catch
            {
                // Silently fail — MP will retry the webhook
            }
        }

        private static string TraducirErro(string statusDetail) => statusDetail switch
        {
            "cc_rejected_call_for_authorize"    => "Seu banco bloqueou a transação. Ligue para o número no verso do cartão e autorize a compra, depois tente novamente.",
            "cc_rejected_insufficient_amount"   => "Saldo insuficiente no cartão. Verifique seu limite disponível e tente novamente.",
            "cc_rejected_bad_filled_card_number"=> "Número do cartão incorreto. Verifique e tente novamente.",
            "cc_rejected_bad_filled_date"       => "Data de vencimento incorreta. Verifique e tente novamente.",
            "cc_rejected_bad_filled_security_code" => "Código de segurança (CVV) incorreto. Verifique e tente novamente.",
            "cc_rejected_bad_filled_other"      => "Dados do cartão inválidos. Verifique todas as informações e tente novamente.",
            "cc_rejected_blacklist"             => "Não foi possível processar o pagamento com este cartão. Tente com outro cartão.",
            "cc_rejected_card_disabled"         => "Cartão desativado. Entre em contato com seu banco para reativá-lo.",
            "cc_rejected_card_error"            => "Não foi possível processar o cartão. Verifique os dados ou tente com outro cartão.",
            "cc_rejected_duplicated_payment"    => "Pagamento duplicado detectado. Aguarde alguns minutos antes de tentar novamente.",
            "cc_rejected_high_risk"             => "Pagamento recusado por análise de segurança. Tente com outro cartão ou use o PIX.",
            "cc_rejected_max_attempts"          => "Número máximo de tentativas atingido. Aguarde alguns minutos e tente novamente.",
            "cc_rejected_other_reason"          => "Pagamento recusado pelo banco. Tente com outro cartão ou entre em contato com seu banco.",
            "rejected_by_bank"                  => "Pagamento recusado pelo banco. Entre em contato com seu banco para mais informações.",
            "rejected_insufficient_data"        => "Dados insuficientes para processar o pagamento. Verifique todas as informações.",
            _                                   => "Pagamento não aprovado. Verifique os dados do cartão ou tente com outro cartão. Se o problema persistir, use o PIX."
        };

        private async Task SalvarEntradaAsync(long pagamentoId, string tipo, decimal valor)
        {
            var tipoEnum = tipo.ToLower() switch
            {
                "dízimo" or "dizimo" => TipoEntrada.Dizimo,
                "oferta" => TipoEntrada.Oferta,
                _ => TipoEntrada.Doacao
            };

            _db.EntradasFinanceiras.Add(new EntradaFinanceira
            {
                Tipo = tipoEnum,
                Valor = valor,
                Data = DateTime.Today,
                Descricao = $"{tipo} via cartão - Mercado Pago",
                Origem = $"MP-{pagamentoId}",
                RegistradoPor = "Site"
            });
            await _db.SaveChangesAsync();
        }
    }
}
