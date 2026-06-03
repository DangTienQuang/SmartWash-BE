using System.Security.Claims;
using AutoWashPro.BLL.Exceptions;

namespace BLL.Helpers
{
    public static class ClaimHelper
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                throw new UnauthorizedException("Khong tim thay UserId trong token. Vui long dang nhap lai.");

            if (!int.TryParse(claimValue, out var userId))
                throw new UnauthorizedException("UserId trong token khong hop le. Vui long dang nhap lai.");

            return userId;
        }

        public static string GetRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        public static string GetUserRole(ClaimsPrincipal user)
        {
            return GetRole(user);
        }

        public static string GetUserPhone(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.MobilePhone)?.Value ?? string.Empty;
        }
    }
}
