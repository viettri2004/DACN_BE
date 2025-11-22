using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Entities;
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
        private readonly IVnPayService _vnPayService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            IMomoService momoService,
            ILogger<PaymentController> logger,
            ISepayService sepayService,
            IVnPayService vnPayService,
            IPaymentRepository paymentRepository,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _momoService = momoService;
            _logger = logger;
            _sepayService = sepayService;
            _vnPayService = vnPayService;
            _paymentRepository = paymentRepository;
            _configuration = configuration;
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
        
        [Authorize]
        [HttpPost("vnpay/checkout")]
        public async Task<ActionResult<ApiResponse>> CreateVnPayPayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", "User not authenticated", null, false));
            }

            var response = await _paymentService.CreateVnPayPaymentAsync(checkoutRequest, studentId);
            return response.ToActionResult();
        }

        // VNPAY Return URL - Xử lý payment và redirect FE
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturnHandler()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);

                // 1. Check Checksum
                if (!response.Success)
                {
                    var failUrl = _configuration["FrontendUrls:PaymentFail"];
                    return Redirect($"{failUrl}?errorCode=InvalidSignature");
                }

                // 2. Check Order tồn tại
                var order = await _paymentRepository.GetOrderByIdAsync(response.OrderId);
                if (order == null)
                {
                    var failUrl = _configuration["FrontendUrls:PaymentFail"];
                    return Redirect($"{failUrl}?errorCode=OrderNotFound");
                }

                // 3. Check Amount
                if (order.TotalAmount != response.Amount)
                {
                    var failUrl = _configuration["FrontendUrls:PaymentFail"];
                    return Redirect($"{failUrl}?orderId={response.OrderId}&errorCode=InvalidAmount");
                }

                // 4. Check trạng thái đơn hàng (Idempotency)
                if (order.Status == "Paid")
                {
                    var successUrl = _configuration["FrontendUrls:PaymentSuccess"];
                    return Redirect($"{successUrl}?orderId={response.OrderId}");
                }

                // Process the payment in the service
                await _vnPayService.ProcessVnPayIpnAsync(response);

                // Redirect based on result
                var successUrlFinal = _configuration["FrontendUrls:PaymentSuccess"];
                var failUrlFinal = _configuration["FrontendUrls:PaymentFail"];

                if (response.VnPayResponseCode == "00")
                {
                    return Redirect($"{successUrlFinal}?orderId={response.OrderId}");
                }
                else
                {
                    return Redirect($"{failUrlFinal}?orderId={response.OrderId}&errorCode={response.VnPayResponseCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPAY Return Error");
                var failUrl = _configuration["FrontendUrls:PaymentFail"];
                return Redirect($"{failUrl}?errorCode=UnknownError");
            }
        }
    }
}