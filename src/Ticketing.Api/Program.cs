using Microsoft.EntityFrameworkCore;
using Ticketing.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "TicketingDatabase")));

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

