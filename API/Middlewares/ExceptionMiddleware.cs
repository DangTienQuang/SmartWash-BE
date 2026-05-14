using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoWashPro.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                int statusCode = 500;
                string message = "Lỗi hệ thống nội bộ. Vui lòng thử lại sau.";

                if (ex.Message.Contains("not found") || ex.Message.Contains("already exists") || ex.Message.Contains("allowed") || ex.Message.Contains("must be"))
                {
                    statusCode = 400;
                    message = ex.Message;
                }

                context.Response.StatusCode = statusCode;

                var response = new
                {
                    statusCode = statusCode,
                    message = message
                };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }
}