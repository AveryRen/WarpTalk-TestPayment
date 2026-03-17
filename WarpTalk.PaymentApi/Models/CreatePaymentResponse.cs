namespace WarpTalk.PaymentApi.Models;

public class CreatePaymentResponse
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? OrderId { get; set; }
    public string? Message { get; set; }
}
