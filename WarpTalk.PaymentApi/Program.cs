using WarpTalk.PaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddScoped<VnpayService>();

// ─── Swagger / OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WarpTalk Payment API",
        Version = "v1",
        Description = """
            Backend xử lý thanh toán VNPAY cho WarpTalk AI Speech Translation Platform.

            **Môi trường:** Sandbox (Test)
            **VNPAY TmnCode:** 932AHOW2

            ## Luồng thanh toán
            1. Frontend gọi `POST /api/payment/create-url` để lấy URL VNPAY
            2. Frontend redirect người dùng đến URL đó
            3. Người dùng thanh toán tại cổng VNPAY
            4. VNPAY gọi IPN qua `POST /api/payment/ipn` (server-to-server)
            5. VNPAY redirect người dùng về `GET /api/payment/return`
            6. Frontend poll `GET /api/payment/status/{orderId}` để lấy kết quả
            """,
        Contact = new() { Name = "WarpTalk Team", Email = "support@warptalk.ai" }
    });

    // Include XML comments for Swagger doc
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ─── CORS — allow React frontend ─────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
// Always show Swagger (even in production for this demo)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WarpTalk Payment API v1");
    options.RoutePrefix = "swagger"; // http://localhost:5001/swagger
    options.DocumentTitle = "WarpTalk Payment API - VNPAY Sandbox";
});

app.UseHttpsRedirection();
app.UseCors("ReactFrontend");
app.UseAuthorization();
app.MapControllers();

// ─── Root redirect to Swagger ─────────────────────────────────────────────────
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
