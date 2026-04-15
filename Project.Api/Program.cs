using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Project.Api.Extensions;
using Project.Api.Middleware;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Project API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/project-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));


    builder.Services
        .AddInfrastructure()
        .AddApplication()
        .AddSwagger();

    builder.Services.AddControllers();

    builder.Services.AddHealthChecks();


    // JWT Authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);



    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });


    var app = builder.Build();


    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();
    var uploadsDirectory = Path.Combine(app.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsDirectory);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsDirectory),
        RequestPath = "/uploads"
    });
    app.UseAuthentication();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        var url = builder.Configuration["applicationUrl"]
            ?? app.Urls.FirstOrDefault()
            ?? "http://localhost:5033";

        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            OpenBrowser(url);
        });
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}


static void OpenBrowser(string url)
{
    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else
        {
            Process.Start("xdg-open", url);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not auto-open browser at {Url}", url);
    }
}
