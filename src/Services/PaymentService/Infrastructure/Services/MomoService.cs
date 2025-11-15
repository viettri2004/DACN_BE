using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Services
{
    public class MoMoService : IPaymentService
    {
        private static class MoMoTestConfig
        {
            public const string PartnerCode = "MOMO";
            public const string AccessKey = "F8BBA842ECF85";
            public const string SecretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
            public const string BaseUrl = "https://test-payment.momo.vn";

            public const string IpnUrl = "http://dacn.runasp.net/api/Payment/momo/ipn";
            public const string RedirectUrl = "https://webhook.site/b3088a6a-2d17-4f8d-a383-71389a6c600b";
        }


        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public MoMoService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<MomoCreateResponse> CreatePaymentRequestAsync(Order order)
        {
            var request = new MomoCreateRequest
            {
                partnerCode = MoMoTestConfig.PartnerCode,
                requestId = order.MoMoRequestId,
                amount = (long)order.TotalAmount,
                orderId = order.Id,
                orderInfo = $"Thanh toán đơn hàng {order.Id}",
                redirectUrl = MoMoTestConfig.RedirectUrl,
                ipnUrl = MoMoTestConfig.IpnUrl,
                requestType = "captureWallet",
                extraData = ""
            };

            var rawSignature = new StringBuilder();
            rawSignature.Append("accessKey=").Append(MoMoTestConfig.AccessKey);
            rawSignature.Append("&amount=").Append(request.amount);
            rawSignature.Append("&extraData=").Append(request.extraData);
            rawSignature.Append("&ipnUrl=").Append(request.ipnUrl);
            rawSignature.Append("&orderId=").Append(request.orderId);
            rawSignature.Append("&orderInfo=").Append(request.orderInfo);
            rawSignature.Append("&partnerCode=").Append(request.partnerCode);
            rawSignature.Append("&redirectUrl=").Append(request.redirectUrl);
            rawSignature.Append("&requestId=").Append(request.requestId);
            rawSignature.Append("&requestType=").Append(request.requestType);

            request.signature = MoMoSignatureHelper.GenerateSignature(rawSignature.ToString(), MoMoTestConfig.SecretKey);

            var client = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{MoMoTestConfig.BaseUrl}/v2/gateway/api/create", jsonContent);

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
            rawSignature.Append("accessKey=").Append(MoMoTestConfig.AccessKey);
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

            var calculatedSignature = MoMoSignatureHelper.GenerateSignature(rawSignature.ToString(), MoMoTestConfig.SecretKey);

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
                MoMoTransId = request.transId.ToString(),
                MoMoRequestId = request.requestId,
                Amount = request.amount,
                PaymentStatus = request.resultCode == 0 ? "Success" : "Failed",
                TransactionDate = DateTime.UtcNow,
                GatewayResponse = JsonSerializer.Serialize(request),
                ErrorCode = request.resultCode.ToString()
            };
            await _context.PaymentTransactions.AddAsync(transaction);

            // 4. Xử lý logic nghiệp vụ (Thành công/Thất bại)
            if (request.resultCode == 0)
            {
                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;

                // Kích hoạt Enrollment
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