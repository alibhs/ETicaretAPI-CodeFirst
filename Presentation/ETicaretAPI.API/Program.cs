using ETicaretAPI.API.Logs.Configurations.ColumnWriters;
using ETicaretAPI.Application;
using ETicaretAPI.Application.Validators.Products;
using ETicaretAPI.Infrastucture;
using ETicaretAPI.Infrastucture.Filters;
using ETicaretAPI.Infrastucture.Services.Storage.Azure;
using ETicaretAPI.Persistance;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Sinks.PostgreSQL;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPersistanceServices();
builder.Services.AddInfrastuctureServices();
builder.Services.AddApplicationServices();

//builder.Services.AddStorage(StorageType.Azure);
//builder.Services.AddStorage<LocalStorage>();
builder.Services.AddStorage<AzureStorage>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
        .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    )
);

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilters>())
    .AddFluentValidation(configuration => configuration.RegisterValidatorsFromAssemblyContaining<CreateProductValidator>())
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

Logger log = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt")
    .WriteTo.PostgreSQL(builder.Configuration.GetConnectionString("PostgreSQL"), "logs", needAutoCreateTable: true,
    columnOptions: new Dictionary<string, ColumnWriterBase> //manuel tablo olu�turmay� sa�lar
    {
        {"message",new RenderedMessageColumnWriter()},
        {"message_template", new MessageTemplateColumnWriter()},
        {"level", new LevelColumnWriter()},
        {"time_stamp",new TimestampColumnWriter() },
        {"exception", new ExceptionColumnWriter() },
        {"log_event", new LogEventSerializedColumnWriter()},
        {"user_name", new UsernameColumnWriter() }
    })
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog(log);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.ResponseHeaders.Add("MyResponseHeader");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Admin", options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateAudience = true, //Olu�turulacak token de�erini kimlerin/hangi originlerin/sitelerin kullan�c� belirledi�imiz de�erdir.
            ValidateIssuer = true, //Olu�turulacak token de�erinin kimin da��tt���n� ifade edece�imiz aland�r.
            ValidateIssuerSigningKey = true, //�retilecek token de�erinin s�recini kontrol edecek olan do�rulamad�r.
            ValidateLifetime = true, //Olu�tuurlan token de�erinin s�resini kontrol edecek de�erdir.

            ValidAudiences = new List<string> { builder.Configuration["Token:Audience"] },
            ValidIssuer = builder.Configuration["Token:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:SecurityKey"])),
            LifetimeValidator = (notBefore, expires, securityToken, validationParameters) => expires != null ? expires > DateTime.UtcNow : false,

            NameClaimType = ClaimTypes.Name //Jwt �zerinden Name claimne kar��l�k gelen de�eri User.Identity.Name propertsyi �zerinden elde edebilmemyi sa�lar.            

        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseHttpLogging();

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseAuthentication();

app.Use(async (context, next) =>
{

    var username = context.User?.Identity?.IsAuthenticated != null || true ? context.User.Identity.Name : null;
    LogContext.PushProperty("user_name", username);
    await next();
});

app.MapControllers();

app.Run();
