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
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace src.Services.PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ISepayService _sepayService;
        private readonly IVnPayService _vnPayService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger,
            ISepayService sepayService,
            IVnPayService vnPayService,
            IPaymentRepository paymentRepository,
            IConfiguration configuration,
            IStringLocalizer<SharedResources> localizer)
        {
            _paymentService = paymentService;
            _logger = logger;
            _sepayService = sepayService;
            _vnPayService = vnPayService;
            _paymentRepository = paymentRepository;
            _configuration = configuration;
            _localizer = localizer;
        }


        [HttpPost("sepay/checkout-bank")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> CreateBankPayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
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
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _paymentService.CreateVnPayPaymentAsync(checkoutRequest, studentId);
            return response.ToActionResult();
        }

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

                // Call ProcessVnPayIpnAsync to update database (idempotent)
                if (response.VnPayResponseCode == "00")
                {
                    await _vnPayService.ProcessVnPayIpnAsync(response);
                    
                    var successUrlFinal = _configuration["FrontendUrls:PaymentSuccess"];
                    return Redirect($"{successUrlFinal}?orderId={response.OrderId}");
                }
                else
                {
                    var failUrlFinal = _configuration["FrontendUrls:PaymentFail"];
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

        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnPayIpnHandler()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);

                // 1. Check Checksum
                if (!response.Success)
                {
                    return Ok(new { RspCode = "97", Message = "Invalid Checksum" });
                }

                // 2. Check Order tồn tại
                var order = await _paymentRepository.GetOrderByIdAsync(response.OrderId);
                if (order == null)
                {
                    return Ok(new { RspCode = "01", Message = "Order not found" });
                }

                // 3. Check Amount
                if (order.TotalAmount != response.Amount)
                {
                    return Ok(new { RspCode = "04", Message = "Invalid Amount" });
                }

                // 4. Check trạng thái đơn hàng (Idempotency)
                if (order.Status == "Paid")
                {
                    return Ok(new { RspCode = "02", Message = "Order already confirmed" });
                }

                // 5. Update Database
                await _vnPayService.ProcessVnPayIpnAsync(response);

                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPAY IPN Error");
                return Ok(new { RspCode = "99", Message = "Unknown Error" });
            }
        }

        [Authorize]
        [HttpPost("giftcode/redeem")]
        public async Task<ActionResult<ApiResponse>> RedeemGiftCode([FromBody] GiftCodeRedeemDto redeemDto)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _paymentService.RedeemGiftCodeAsync(redeemDto, studentId);
            return response.ToActionResult();
        }

        [Authorize(Roles = "Admin,Instructor")]
        [HttpPost("giftcode/create")]
        public async Task<ActionResult<ApiResponse>> CreateGiftCode([FromBody] CreateGiftCodeDto createDto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _paymentService.CreateGiftCodeAsync(createDto, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse>> GetPaymentHistory()
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _paymentService.GetPaymentHistoryAsync(studentId);
            return response.ToActionResult();
        }
    }
}