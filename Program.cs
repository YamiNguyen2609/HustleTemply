using System.Text.Json;
using HustleTemply.Services;
using HustleTemply.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GoogleSheetsSettings>(
    builder.Configuration.GetSection(GoogleSheetsSettings.SectionName));
builder.Services.Configure<SheetCopySettings>(
    builder.Configuration.GetSection(SheetCopySettings.SectionName));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IGoogleSheetService, GoogleSheetService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ISheetExportService, SheetExportService>();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("hustle-temply", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseCors("hustle-temply");
app.Run();
