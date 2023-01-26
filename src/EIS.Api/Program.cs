using EIS.Api.Application.Constants;
using EIS.Infrastructure.Configuration;
using System;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var eisPublishStatus = builder.Configuration["eis:PublishStatus"];
EISConstants.PublishStatus = Boolean.Parse(eisPublishStatus);

// On Infrastructure project add the below.
// public static IApplicationBuilder UseEISInfrastructure(this IApplicationBuilder applicationBuilder) 
// {
//      return applicationBuilder.AddEISProcessor<EisEventProcessorService>();
// }

builder.Services.AddEISServices();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// app.UseEISInfrastructure();

app.Run();
