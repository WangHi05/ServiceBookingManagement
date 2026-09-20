using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServiceBooking.API.Data;
using ServiceBooking.API.Hubs;
using ServiceBooking.API.Middleware;
using ServiceBooking.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Controllers ----------
// Enum (vd: BookingStatus) được truyền qua JSON body dạng chuỗi ("Confirmed") thay vì số (1),
// dễ đọc hơn cho frontend và Swagger.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---------- SignalR (cập nhật booking theo thời gian thực) ----------
// Dùng cùng convention JSON (camelCase + enum dạng chuỗi) như REST API để frontend xử lý
// payload nhận qua Hub giống hệt payload nhận qua axios, không cần map 2 kiểu khác nhau.
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ---------- CORS (cho phép frontend Next.js gọi API) ----------
const string CorsPolicyName = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                              ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---------- Database (EF Core + SQL Server) ----------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Dependency Injection ----------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IServiceManagementService, ServiceManagementService>();
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingNotifier, BookingNotifier>();
builder.Services.AddScoped<IOverdueBookingProcessor, OverdueBookingProcessor>();

// ---------- Hangfire (tự động xử lý booking quá hạn) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        // Hangfire tự tạo bảng riêng (tiền tố HangFire.*) trong CÙNG database - đơn giản
        // cho phạm vi demo, hệ thống thật nên tách DB riêng để tránh tăng tải DB nghiệp vụ.
        PrepareSchemaIfNecessary = true,
        QueuePollInterval = TimeSpan.FromSeconds(15)
    }));
builder.Services.AddHangfireServer();

// ---------- JWT Authentication ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // WebSocket của trình duyệt không cho phép đính custom header (Authorization) khi kết nối,
    // nên SignalR client gửi token qua query string "access_token" thay vì header. Đoạn dưới
    // chỉ áp dụng cho đúng path /hubs/* - các API REST bình thường vẫn bắt buộc dùng header
    // Authorization như cũ, không bị nới lỏng.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

// ---------- Swagger (kèm hỗ trợ JWT Bearer) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Service Booking Management System API",
        Version = "v1",
        Description = "API cho hệ thống quản lý đặt lịch dịch vụ"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Booking API v1");
});


app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingHub>("/hubs/bookings");

// ---------- Hangfire Dashboard + Recurring Job ----------
if (app.Environment.IsDevelopment())
{
    // Dashboard không cấu hình Authorization filter riêng -> CHỈ bật ở Development cho tiện
    // debug/demo. Hangfire dashboard dùng cookie/session riêng của nó, không tái sử dụng được
    // JWT Bearer của API, nên việc gắn đúng quyền Admin vào đây cần thêm hạ tầng auth khác
    // (ngoài phạm vi bài demo). KHÔNG bật dòng này ở production nếu chưa có Authorization filter.
    app.UseHangfireDashboard("/hangfire");
}

using (var scope = app.Services.CreateScope())
{
    // 2. Lấy IRecurringJobManager từ DI container
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    // 3. Đăng ký job thông qua manager
    recurringJobManager.AddOrUpdate<IOverdueBookingProcessor>(
        "process-overdue-bookings", 
        service => service.ProcessAsync(),
        "*/5 * * * *" 
    );
}

app.Run();
