using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services;
using Microsoft.Extensions.Configuration;

namespace AutoWashPro.API.Services
{
    public class ReflectionPayOsClient : IPayOsClient
    {
        private readonly object _sdkInstance;
        private readonly MethodInfo _createPaymentLinkMethod;
        private readonly MethodInfo _verifyWebhookMethod;

        public ReflectionPayOsClient(IConfiguration configuration)
        {
            var clientId = configuration["PayOSConfig:ClientId"];
            var apiKey = configuration["PayOSConfig:ApiKey"];
            var checksum = configuration["PayOSConfig:ChecksumKey"];

            // Find loaded assembly that likely contains PayOS types
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.ToLower().Contains("payos") == true);

            if (asm == null)
            {
                try
                {
                    asm = Assembly.Load("payOS");
                }
                catch
                {
                    throw new Exception("PayOS assembly not found. Ensure the payOS NuGet package is restored.");
                }
            }

            var type = asm.GetTypes().FirstOrDefault(t =>
            {
                var ctors = t.GetConstructors();
                return ctors.Any(c =>
                {
                    var ps = c.GetParameters();
                    return ps.Length == 3 && ps.All(p => p.ParameterType == typeof(string));
                });
            });

            if (type == null) throw new Exception("No suitable PayOS SDK type found.");

            _sdkInstance = Activator.CreateInstance(type, clientId, apiKey, checksum)!;

            _createPaymentLinkMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Equals("createPaymentLink", StringComparison.InvariantCultureIgnoreCase)
                    || m.Name.Equals("CreatePaymentLink", StringComparison.InvariantCultureIgnoreCase));

            _verifyWebhookMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.IndexOf("verify", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                                      m.Name.IndexOf("webhook", StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (_createPaymentLinkMethod == null) throw new Exception("PayOS method 'createPaymentLink' not found.");
            if (_verifyWebhookMethod == null) throw new Exception("PayOS method 'verifyPaymentWebhookData' not found.");
        }

        public async Task<PaymentLinkResult> CreatePaymentLinkAsync(long orderCode, int amount, string description)
        {
            var paymentDataType = _createPaymentLinkMethod.GetParameters().FirstOrDefault()?.ParameterType;
            object param;

            if (paymentDataType != null && paymentDataType != typeof(object))
            {
                var pd = Activator.CreateInstance(paymentDataType)!;
                var orderProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.IndexOf("order", StringComparison.InvariantCultureIgnoreCase) >= 0);
                var amountProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.IndexOf("amount", StringComparison.InvariantCultureIgnoreCase) >= 0);
                var descProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.IndexOf("desc", StringComparison.InvariantCultureIgnoreCase) >= 0);
                var cancelUrlProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.IndexOf("cancel", StringComparison.InvariantCultureIgnoreCase) >= 0);
                var returnUrlProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.IndexOf("return", StringComparison.InvariantCultureIgnoreCase) >= 0);

                if (orderProp != null) orderProp.SetValue(pd, orderCode);
                if (amountProp != null) amountProp.SetValue(pd, amount);
                if (descProp != null) descProp.SetValue(pd, description);
                if (cancelUrlProp != null) cancelUrlProp.SetValue(pd, "http://localhost:5000/cancel");
                if (returnUrlProp != null) returnUrlProp.SetValue(pd, "http://localhost:5000/success");

                param = pd;
            }
            else
            {
                param = new { orderCode = orderCode, amount = amount, description = description, cancelUrl = "http://localhost:5000/cancel", returnUrl = "http://localhost:5000/success" };
            }

            var result = _createPaymentLinkMethod.Invoke(_sdkInstance, new object[] { param });

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                result = resultProp?.GetValue(task);
            }

            if (result == null) throw new Exception("PayOS result is null.");

            var checkoutUrl = result.GetType().GetProperty("checkoutUrl")?.GetValue(result)?.ToString()
                              ?? result.GetType().GetProperty("CheckoutUrl")?.GetValue(result)?.ToString();
            
            return new PaymentLinkResult { CheckoutUrl = checkoutUrl ?? string.Empty, OrderCode = orderCode };
        }

        public async Task<WebhookVerificationResult?> VerifyWebhookAsync(object webhookBody)
        {
            var result = _verifyWebhookMethod.Invoke(_sdkInstance, new object[] { webhookBody });
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                result = resultProp?.GetValue(task);
            }

            if (result == null) return null;

            var code = result.GetType().GetProperty("code")?.GetValue(result)?.ToString()
                       ?? result.GetType().GetProperty("Code")?.GetValue(result)?.ToString();
            var orderCodeObj = result.GetType().GetProperty("orderCode")?.GetValue(result)
                       ?? result.GetType().GetProperty("OrderCode")?.GetValue(result);

            return new WebhookVerificationResult { Code = code ?? string.Empty, OrderCode = orderCodeObj != null ? Convert.ToInt64(orderCodeObj) : 0 };
        }
    }
}
