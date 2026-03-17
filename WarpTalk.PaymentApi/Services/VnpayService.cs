using System.Security.Cryptography;
using System.Text;
using System.Web;
using WarpTalk.PaymentApi.Models;

namespace WarpTalk.PaymentApi.Services;

public class VnpayService
{
    private readonly IConfiguration _config;
    // In-memory store for demo (use DB in production)
    private static readonly Dictionary<string, PaymentResult> _paymentStore = new();

    public VnpayService(IConfiguration config)
    {
        _config = config;
    }

    public CreatePaymentResponse CreatePaymentUrl(PaymentRequest request)
    {
        var tmnCode = _config["Vnpay:TmnCode"]!;
        var hashSecret = _config["Vnpay:HashSecret"]!;
        var paymentUrl = _config["Vnpay:PaymentUrl"]!;
        var returnUrl = _config["Vnpay:ReturnUrl"]!;

        var orderId = DateTime.UtcNow.Ticks.ToString();
        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
        var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");

        // Build parameter dictionary (sorted alphabetically)
        var vnpParams = new SortedDictionary<string, string>
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", (request.Amount * 100).ToString() }, // VNPAY requires x100
            { "vnp_CreateDate", createDate },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", request.ClientIpAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", $"Thanh toan goi {request.PlanName} - Ma don: {orderId}" },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", orderId },
            { "vnp_ExpireDate", expireDate },
        };

        // Build raw hash data string
        var hashData = string.Join("&", vnpParams.Select(kv =>
            $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));

        // Sign with HMAC-SHA512
        var secureHash = HmacSha512(hashSecret, hashData);

        // Build final URL
        var query = string.Join("&", vnpParams.Select(kv =>
            $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));
        var finalUrl = $"{paymentUrl}?{query}&vnp_SecureHash={secureHash}";

        return new CreatePaymentResponse
        {
            Success = true,
            PaymentUrl = finalUrl,
            OrderId = orderId,
            Message = "URL tao thanh cong"
        };
    }

    public PaymentResult ValidateAndSaveReturn(IQueryCollection query)
    {
        var hashSecret = _config["Vnpay:HashSecret"]!;

        // Extract all vnp_ params except SecureHash
        var vnpParams = new SortedDictionary<string, string>();
        foreach (var key in query.Keys)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                vnpParams[key] = query[key].ToString();
        }

        // Rebuild hash data
        var hashData = string.Join("&", vnpParams.Select(kv =>
            $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));

        var receivedHash = query["vnp_SecureHash"].ToString();
        var expectedHash = HmacSha512(hashSecret, hashData);

        var responseCode = query["vnp_ResponseCode"].ToString();
        var orderId = query["vnp_TxnRef"].ToString();
        var transactionNo = query["vnp_TransactionNo"].ToString();
        var amountStr = query["vnp_Amount"].ToString();
        long.TryParse(amountStr, out var amountRaw);

        var isValid = string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        var isSuccess = isValid && responseCode == "00";

        var result = new PaymentResult
        {
            Success = isSuccess,
            OrderId = orderId,
            TransactionNo = transactionNo,
            ResponseCode = responseCode,
            Amount = amountRaw / 100,
            Message = isSuccess ? "Thanh toan thanh cong" : GetResponseMessage(responseCode),
            PaidAt = DateTime.Now
        };

        // Store result (in-memory for demo)
        _paymentStore[orderId] = result;

        return result;
    }

    public (bool isValid, string message) ValidateIpn(IQueryCollection query)
    {
        var hashSecret = _config["Vnpay:HashSecret"]!;

        var vnpParams = new SortedDictionary<string, string>();
        foreach (var key in query.Keys)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                vnpParams[key] = query[key].ToString();
        }

        var hashData = string.Join("&", vnpParams.Select(kv =>
            $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}"));

        var receivedHash = query["vnp_SecureHash"].ToString();
        var expectedHash = HmacSha512(hashSecret, hashData);

        if (!string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            return (false, "Invalid signature");

        var responseCode = query["vnp_ResponseCode"].ToString();
        var orderId = query["vnp_TxnRef"].ToString();
        var transactionNo = query["vnp_TransactionNo"].ToString();
        var amountStr = query["vnp_Amount"].ToString();
        long.TryParse(amountStr, out var amountRaw);

        var result = new PaymentResult
        {
            Success = responseCode == "00",
            OrderId = orderId,
            TransactionNo = transactionNo,
            ResponseCode = responseCode,
            Amount = amountRaw / 100,
            Message = responseCode == "00" ? "Thanh toan thanh cong" : GetResponseMessage(responseCode),
            PaidAt = DateTime.Now
        };

        _paymentStore[orderId] = result;

        return (true, "IPN received");
    }

    public PaymentResult? GetPaymentStatus(string orderId)
    {
        _paymentStore.TryGetValue(orderId, out var result);
        return result;
    }

    private static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private static string GetResponseMessage(string code) => code switch
    {
        "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
        "09" => "Thẻ/Tài khoản của KH chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
        "10" => "KH xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
        "11" => "Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
        "12" => "Thẻ/Tài khoản của KH bị khóa.",
        "13" => "Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
        "24" => "Khách hàng hủy giao dịch.",
        "51" => "Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
        "65" => "Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
        "75" => "Ngân hàng thanh toán đang bảo trì.",
        "79" => "KH nhập sai mật khẩu thanh toán quá số lần quy định.",
        _ => $"Giao dịch thất bại. Mã lỗi: {code}"
    };
}
