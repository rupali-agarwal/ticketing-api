using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Events;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Services;
using Ticketing.Api.ExceptionHandling;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TicketingDatabase")));

builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

