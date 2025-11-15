using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace src.Services.PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IMomoService _momoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ISepayService _sepayService;

        public PaymentController(
            IPaymentService paymentService,
            IMomoService momoService,
            ILogger<PaymentController> logger,
            ISepayService sepayService)
        {
            _paymentService = paymentService;
            _momoService = momoService;
            _logger = logger;
            _sepayService = sepayService;
        }

        [Authorize]
        [HttpPost("momo/checkout")]
        public async Task<ActionResult<ApiResponse>> CreateMoMoPayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", "User not authenticated", null, false));
            }

            var response = await _paymentService.CreateMoMoPaymentAsync(checkoutRequest, studentId);
            return response.ToActionResult();
        }

        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MoMoIpnHandler([FromBody] MomoIpnRequest request)
        {
            _logger.LogInformation("Nhận được IPN từ MoMo, OrderId: {OrderId}, ResultCode: {ResultCode}",
                request.orderId,
                request.resultCode);

            _logger.LogDebug(JsonSerializer.Serialize(request));

            try
            {
                await _momoService.ProcessMoMoIpnAsync(request);

                var response = new MomoIpnResponse
                {
                    partnerCode = "MOMO",
                    requestId = request.requestId,
                    orderId = request.orderId,
                    resultCode = 0,
                    message = "Success",
                    responseTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý IPN cho OrderId: {OrderId}", request.orderId);
                return StatusCode(500, new { resultCode = 99, message = ex.Message });
            }
        }
        
        [HttpPost("sepay/checkout-bank")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CreateBankPayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", "User not authenticated", null, false));
            }

            var response = await _paymentService.CreateBankPaymentAsync(checkoutRequest, studentId);
            return response.ToActionResult();
        }

        [HttpPost("sepay/webhook")]
        public async Task<IActionResult> SepayWebhookHandler([FromBody] SepayWebhookRequest request)
        {
            try
            {
                _logger.LogInformation("Nhận được Webhook từ Sepay, ID: {Id}, Code: {Code}, Amount: {Amount}",
                    request.Id, request.Code, request.TransferAmount);

                await _sepayService.ProcessSepayWebhookAsync(request);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý Sepay Webhook cho Code: {Code}", request.Code);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}