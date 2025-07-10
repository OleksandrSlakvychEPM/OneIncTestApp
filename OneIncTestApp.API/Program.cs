using Microsoft.AspNetCore.RateLimiting;
using OneIncTestApp.Hub;
using OneIncTestApp.Infra;
using OneIncTestApp.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<IJobQueue, JobQueue>();
builder.Services.AddSingleton<IJobManager, JobManager>();
builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHostedService<JobProcessingService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Default", limiterOptions =>
    {
        limiterOptions.PermitLimit = 1;
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 1;
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

app.MapHub<ProcessingHub>("/processingHub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
