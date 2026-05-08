using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace OrderingService.API.Controllers
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
            IPaymentService OrderingService,
            ILogger<PaymentController> logger,
            ISepayService sepayService,
            IVnPayService vnPayService,
            IPaymentRepository paymentRepository,
            IConfiguration configuration,
            IStringLocalizer<SharedResources> localizer)
        {
            _paymentService = OrderingService;
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
                // Security Check: verify API Key from Authorization header
                var authHeader = Request.Headers["Authorization"].ToString();
                var expectedApiKey = _configuration["Sepay:ApiKey"];

                if (string.IsNullOrEmpty(authHeader) || authHeader != $"Bearer {expectedApiKey}")
                {
                    _logger.LogWarning("Phát hiện Webhook SePay không hợp lệ (Sai API Key). Auth: {Auth}", authHeader);
                    return Unauthorized(new { success = false, message = "Unauthorized Webhook" });
                }

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
                    return Ok(new { RspCode = "97", Message = _localizer["InvalidChecksum"].Value });
                }

                // 2. Check Order tồn tại
                var order = await _paymentRepository.GetOrderByIdAsync(response.OrderId);
                if (order == null)
                {
                    return Ok(new { RspCode = "01", Message = _localizer["OrderNotFound"].Value });
                }

                // 3. Check Amount
                if (order.TotalAmount != response.Amount)
                {
                    return Ok(new { RspCode = "04", Message = _localizer["InvalidAmount"].Value });
                }

                // 4. Check trạng thái đơn hàng (Idempotency)
                if (order.Status == "Paid")
                {
                    return Ok(new { RspCode = "02", Message = _localizer["OrderAlreadyConfirmed"].Value });
                }

                // 5. Update Database
                await _vnPayService.ProcessVnPayIpnAsync(response);

                return Ok(new { RspCode = "00", Message = _localizer["ConfirmSuccess"].Value });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPAY IPN Error");
                return Ok(new { RspCode = "99", Message = _localizer["UnknownError"].Value });
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

        [Authorize(Roles = "Admin,Instructor")]
        [HttpPut("giftcode/{giftCodeId}")]
        public async Task<ActionResult<ApiResponse>> UpdateGiftCode(string giftCodeId, [FromBody] UpdateGiftCodeDto updateDto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _paymentService.UpdateGiftCodeAsync(giftCodeId, updateDto, userId);
            return response.ToActionResult();
        }

        [Authorize(Roles = "Admin,Instructor")]
        [HttpDelete("giftcode/{giftCodeId}")]
        public async Task<ActionResult<ApiResponse>> DeleteGiftCode(string giftCodeId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _paymentService.DeleteGiftCodeAsync(giftCodeId, userId);
            return response.ToActionResult();
        }

        [Authorize(Roles = "Admin,Instructor")]
        [HttpGet("giftcode/course/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetGiftCodesByCourse(string courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _paymentService.GetGiftCodesByCourseAsync(courseId, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse>> GetPaymentHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _paymentService.GetPaymentHistoryAsync(studentId, pageNumber, pageSize);
            return response.ToActionResult();
        }
    }
}


