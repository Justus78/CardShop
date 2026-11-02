using api.Helpers;
using api.Interfaces;
using api.Middleware;
using api.Models;
using api.Repositories;
using api.Services;
using api.UnitOfWork;
using api.BackgroundServices;
using CardShop.Data;
using CardShop.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides; // NEW: for ForwardedHeaders
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Security.Claims;
using api.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(120); // Set command timeout to 120 seconds
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5, // Retry up to 5 times
                maxRetryDelay: TimeSpan.FromSeconds(10), // Wait 10 seconds between retries
                errorNumbersToAdd: null); // Retry on default transient errors
        });

    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// add services for the connection string with Cloudinary
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

// services for identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 10;
    options.SignIn.RequireConfirmedEmail = true; // make sure user confirms email before logging in
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthRateLimiting();

// NEW: forwarded headers, so the rate limiter sees the REAL visitor IP once this
// runs behind Azure's reverse proxy. Without this, every user would appear to
// come from the proxy's address and share one rate-limit bucket.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // X-Forwarded-For carries the visitor's IP; X-Forwarded-Proto says the original
    // request was HTTPS (Azure ends the TLS connection before it reaches the app).
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // By default the middleware only trusts these headers from the same machine.
    // Azure's proxy addresses aren't fixed, so we accept them from Azure's side.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // ForwardLimit stays at its default of 1: only the LAST entry in X-Forwarded-For
    // (the one Azure's proxy added) is used. Anything a client puts earlier in the
    // header is ignored, which is what stops people faking their IP.
});

// add jwt scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
        ),

        RoleClaimType = ClaimTypes.Role
    };

    // Read token from HTTP-only cookie named "access_token"
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHostedService<OrderExpirationBackgroundService>();

// configuration for stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// configuration for services
builder.Services.AddScoped<ITokenService, api.Services.TokenService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, api.Services.ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICheckoutService, api.Services.CheckoutService>();
builder.Services.AddScoped<IAdminService, AdminRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//builder.Services.AddScoped<ITradeInService, TradeInService>();
//builder.Services.AddScoped<IAdminTradeInService, AdminTradeInService>();


builder.Services.AddHttpClient<ISetService, SetService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "CardShopApp/1.0 (https://yourdomain.com)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // update if using another port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for cookies
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();