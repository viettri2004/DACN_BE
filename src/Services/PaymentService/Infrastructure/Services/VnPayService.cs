using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Services.Helpers;

namespace PaymentService.Infrastructure.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(IConfiguration config, AppDbContext context, ILogger<VnPayService> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public string CreatePaymentUrl(HttpContext context, VnPayPaymentRequestModel model)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", ((long)model.Amount * 100).ToString());

            TimeZoneInfo timeZoneId;

            try
            {
                timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }

            DateTime timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneId);

            vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));

            // Thêm ExpireDate (thường là +15 phút) để tránh lỗi timeout
            vnpay.AddRequestData("vnp_ExpireDate", timeNow.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            vnpay.AddRequestData("vnp_CurrCode", "VND");

            // 3. Xử lý IP Address (Fix cứng nếu là localhost để tránh lỗi ::1)
            // Bạn nên check lại hàm Utils.GetIpAddress, hoặc dùng logic dưới đây cho an toàn
            string clientIpAddress = Utils.GetIpAddress(context);
            if (clientIpAddress == "::1" || string.IsNullOrEmpty(clientIpAddress))
            {
                clientIpAddress = "127.0.0.1";
            }
            vnpay.AddRequestData("vnp_IpAddr", clientIpAddress);

            vnpay.AddRequestData("vnp_Locale", "vn");

            // 4. Thông tin đơn hàng (Tiếng Việt KHÔNG DẤU, không ký tự đặc biệt)
            // "Thanh toan don hang:" + model.OrderId có thể chứa ký tự lạ nếu OrderId là chuỗi phức tạp
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");

            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:ReturnUrl"]);

            // LƯU Ý: Thường không gửi vnp_IpnUrl qua API, bạn cấu hình trên trang quản trị VNPAY.
            // Nếu gửi mà bên kia không nhận => Sai checksum => Lỗi 99.
            // vnpay.AddRequestData("vnp_IpnUrl", _config["VnPay:IpnUrl"]); // Comment dòng này lại thử xem

            vnpay.AddRequestData("vnp_TxnRef", model.OrderId);

            return vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], Environment.GetEnvironmentVariable("VnPay__HashSecret"));
        }

        public VnPayPaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();

            // Lấy toàn bộ dữ liệu trả về từ VNPAY
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Lấy các tham số quan trọng
            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

            // Kiểm tra chữ ký bảo mật
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, Environment.GetEnvironmentVariable("VnPay__HashSecret"));

            if (!checkSignature)
            {
                return new VnPayPaymentResponseModel
                {
                    Success = false,
                    VnPayResponseCode = "InvalidSignature"
                };
            }

            return new VnPayPaymentResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderId = vnp_orderId,
                TransactionId = vnp_TransactionId,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode,
                Amount = !string.IsNullOrEmpty(vnp_Amount) ? decimal.Parse(vnp_Amount) / 100 : 0
            };
        }

        public async Task ProcessVnPayIpnAsync(VnPayPaymentResponseModel response)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == response.OrderId);

            if (order == null)
            {
                _logger.LogError("Order not found for VnPay IPN: {OrderId}", response.OrderId);
                throw new Exception("Order not found.");
            }

            if (order.Status == "Paid")
            {
                _logger.LogInformation("Order {OrderId} already paid, skipping VnPay IPN processing", response.OrderId);
                return;
            }

            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = order.Id,
                GatewayTransactionId = response.TransactionId,
                GatewayToken = response.Token,
                Amount = response.Amount,
                PaymentStatus = response.VnPayResponseCode == "00" ? "Success" : "Failed",
                TransactionDate = DateTime.UtcNow,
                GatewayResponse = "VnPay",
                ErrorCode = response.VnPayResponseCode
            };
            await _context.PaymentTransactions.AddAsync(transaction);

            if (response.VnPayResponseCode == "00")
            {
                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;
                order.PaymentMethod = "VnPay";

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

                // Remove purchased courses from cart
                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.StudentId == order.StudentId);

                if (cart != null)
                {
                    var courseIds = order.OrderItems.Select(oi => oi.CourseId).ToList();
                    var cartItemsToRemove = await _context.CartItems
                        .Where(ci => ci.CartId == cart.Id && courseIds.Contains(ci.CourseId))
                        .ToListAsync();

                    if (cartItemsToRemove.Any())
                    {
                        _context.CartItems.RemoveRange(cartItemsToRemove);
                        _logger.LogInformation("Removed {Count} items from cart for student {StudentId}", cartItemsToRemove.Count, order.StudentId);
                    }
                }
            }
            else
            {
                order.Status = "Failed";
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed VnPay IPN for Order {OrderId} with status {Status}", response.OrderId, order.Status);
        }
    }
}