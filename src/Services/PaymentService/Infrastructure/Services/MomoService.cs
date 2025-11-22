using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Services
{
    public class MoMoService : IMomoService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MoMoConfig _moMoConfig;

        public MoMoService(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _moMoConfig = configuration.GetSection("MoMo").Get<MoMoConfig>() ?? new MoMoConfig();
        }

        public async Task<MomoCreateResponse> CreatePaymentRequestAsync(Order order)
        {
            var request = new MomoCreateRequest
            {
                partnerCode = _moMoConfig.PartnerCode,
                requestId = order.MoMoRequestId,
                amount = (long)order.TotalAmount,
                orderId = order.Id,
                orderInfo = $"Thanh toán đơn hàng {order.Id}",
                redirectUrl = _moMoConfig.RedirectUrl,
                ipnUrl = _moMoConfig.IpnUrl,
                requestType = "captureWallet",
                extraData = ""
            };

            var rawSignature = new StringBuilder();
            rawSignature.Append("accessKey=").Append(_moMoConfig.AccessKey);
            rawSignature.Append("&amount=").Append(request.amount);
            rawSignature.Append("&extraData=").Append(request.extraData);
            rawSignature.Append("&ipnUrl=").Append(request.ipnUrl);
            rawSignature.Append("&orderId=").Append(request.orderId);
            rawSignature.Append("&orderInfo=").Append(request.orderInfo);
            rawSignature.Append("&partnerCode=").Append(request.partnerCode);
            rawSignature.Append("&redirectUrl=").Append(request.redirectUrl);
            rawSignature.Append("&requestId=").Append(request.requestId);
            rawSignature.Append("&requestType=").Append(request.requestType);

            request.signature = MoMoSignatureHelper.GenerateSignature(rawSignature.ToString(), _moMoConfig.SecretKey);

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_moMoConfig.BaseUrl}/v2/gateway/api/create", jsonContent);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"MoMo API request failed: {jsonResponse}");
            }

            return JsonSerializer.Deserialize<MomoCreateResponse>(jsonResponse);
        }

        public async Task ProcessMoMoIpnAsync(MomoIpnRequest request)
        {
            var rawSignature = new StringBuilder();
            rawSignature.Append("accessKey=").Append(_moMoConfig.AccessKey);
            rawSignature.Append("&amount=").Append(request.amount);
            rawSignature.Append("&extraData=").Append(request.extraData);
            rawSignature.Append("&message=").Append(request.message);
            rawSignature.Append("&orderId=").Append(request.orderId);
            rawSignature.Append("&orderInfo=").Append(request.orderInfo);
            rawSignature.Append("&orderType=").Append(request.orderType);
            rawSignature.Append("&partnerCode=").Append(request.partnerCode);
            rawSignature.Append("&payType=").Append(request.payType);
            rawSignature.Append("&requestId=").Append(request.requestId);
            rawSignature.Append("&responseTime=").Append(request.responseTime);
            rawSignature.Append("&resultCode=").Append(request.resultCode);
            rawSignature.Append("&transId=").Append(request.transId);

            var calculatedSignature = MoMoSignatureHelper.GenerateSignature(rawSignature.ToString(), _moMoConfig.SecretKey);

            if (calculatedSignature != request.signature)
            {
                throw new Exception("Invalid MoMo IPN signature.");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == request.orderId);

            if (order == null) throw new Exception("Order not found.");
            if (order.Status == "Paid") return;

            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                GatewayTransactionId = request.transId.ToString(),
                GatewayToken = request.requestId,
                Amount = request.amount,
                PaymentStatus = request.resultCode == 0 ? "Success" : "Failed",
                TransactionDate = DateTime.UtcNow,
                GatewayResponse = "MoMo",
                ErrorCode = request.resultCode.ToString()
            };
            await _context.PaymentTransactions.AddAsync(transaction);

            if (request.resultCode == 0)
            {
                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;

                foreach (var item in order.OrderItems)
                {
                    var enrollment = new Enrollment
                    {
                        Id = Guid.NewGuid().ToString(),
                        CourseId = item.CourseId,
                        StudentId = order.StudentId,
                        OrderId = order.Id,
                        Status = true,
                        EnrolledAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddYears(100)
                    };
                    await _context.Enrollments.AddAsync(enrollment);
                }
            }
            else
            {
                order.Status = "Failed";
            }

            await _context.SaveChangesAsync();
        }
    }
}