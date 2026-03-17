using Microsoft.AspNetCore.Mvc;
using WarpTalk.PaymentApi.Models;
using WarpTalk.PaymentApi.Services;

namespace WarpTalk.PaymentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly VnpayService _vnpay;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(VnpayService vnpay, ILogger<PaymentController> logger)
    {
        _vnpay = vnpay;
        _logger = logger;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPAY từ thông tin đơn hàng.
    /// Frontend gọi endpoint này để lấy URL redirect sang VNPAY Gateway.
    /// </summary>
    [HttpPost("create-url")]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreatePaymentUrl([FromBody] PaymentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Số tiền không hợp lệ." });

        if (string.IsNullOrEmpty(request.PlanId))
            return BadRequest(new { message = "PlanId không được để trống." });

        // Get real client IP
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (ipAddress == "::1") ipAddress = "127.0.0.1";
        request.ClientIpAddress = ipAddress;

        _logger.LogInformation("Tạo URL thanh toán cho plan {PlanId}, số tiền {Amount}", request.PlanId, request.Amount);

        var result = _vnpay.CreatePaymentUrl(request);
        return Ok(result);
    }

    /// <summary>
    /// VNPAY Return URL — VNPAY redirect người dùng về đây sau khi thanh toán.
    /// Endpoint này xác minh chữ ký và trả kết quả cho frontend.
    /// </summary>
    [HttpGet("return")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    public IActionResult VnpayReturn([FromQuery] IQueryCollection query)
    {
        _logger.LogInformation("VNPAY Return callback received với {Count} params", Request.Query.Count);

        var result = _vnpay.ValidateAndSaveReturn(Request.Query);

        if (result.Success)
        {
            _logger.LogInformation("Thanh toán thành công: OrderId={OrderId}, TxnNo={TxnNo}", result.OrderId, result.TransactionNo);
            // Redirect to frontend success page
            return Redirect($"http://localhost:5173?payment=success&orderId={result.OrderId}&txnNo={result.TransactionNo}");
        }
        else
        {
            _logger.LogWarning("Thanh toán thất bại: OrderId={OrderId}, Code={Code}", result.OrderId, result.ResponseCode);
            return Redirect($"http://localhost:5173?payment=failed&orderId={result.OrderId}&code={result.ResponseCode}");
        }
    }

    /// <summary>
    /// VNPAY IPN (Instant Payment Notification) — VNPAY gọi server-to-server.
    /// Dùng để cập nhật trạng thái đơn hàng trong database một cách đáng tin cậy.
    /// VNPAY yêu cầu response {"RspCode":"00","Message":"Confirm Success"}.
    /// </summary>
    [HttpPost("ipn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult VnpayIpn()
    {
        _logger.LogInformation("VNPAY IPN callback received");

        var (isValid, message) = _vnpay.ValidateIpn(Request.Query);

        if (!isValid)
        {
            _logger.LogWarning("IPN: chữ ký không hợp lệ");
            return Ok(new { RspCode = "97", Message = "Invalid Checksum" });
        }

        // TODO: Update database order status here in production
        _logger.LogInformation("IPN: {Message}", message);
        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Kiểm tra trạng thái thanh toán theo OrderId.
    /// Frontend có thể poll endpoint này sau khi người dùng quay về.
    /// </summary>
    [HttpGet("status/{orderId}")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPaymentStatus(string orderId)
    {
        var result = _vnpay.GetPaymentStatus(orderId);

        if (result == null)
            return NotFound(new { message = $"Không tìm thấy đơn hàng: {orderId}" });

        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint để kiểm tra API còn hoạt động.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "OK",
            service = "WarpTalk Payment API",
            timestamp = DateTime.UtcNow,
            vnpayMode = "Sandbox"
        });
    }
}
