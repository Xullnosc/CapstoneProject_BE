using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services;

namespace CapstoneProject_BE.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISystemErrorLogService errorLogService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");

                try
                {
                    await errorLogService.AddLogAsync(new SystemErrorLogDTO
                    {
                        Level = "Error",
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to log error to the database.");
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var result = JsonSerializer.Serialize(new
            {
                message = "An internal server error occurred.",
                details = ex.Message
            });

            return context.Response.WriteAsync(result);
        }
    }
}
