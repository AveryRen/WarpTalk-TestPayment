namespace WarpTalk.PaymentApi.Models;

public class PaymentResult
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = "";
    public string TransactionNo { get; set; } = "";
    public string ResponseCode { get; set; } = "";
    public string Message { get; set; } = "";
    public long Amount { get; set; }
    public string PlanId { get; set; } = "";
    public DateTime PaidAt { get; set; }
}
