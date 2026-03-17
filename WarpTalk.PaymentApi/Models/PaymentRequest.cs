namespace WarpTalk.PaymentApi.Models;

public class PaymentRequest
{
    public string PlanId { get; set; } = "";
    public string PlanName { get; set; } = "";
    public long Amount { get; set; }
    public string OrderDescription { get; set; } = "";
    public string ClientIpAddress { get; set; } = "127.0.0.1";
}
