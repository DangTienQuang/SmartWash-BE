using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        public required string FullName { get; set; }
    }

    public class LoginDTO
    {
        [Required(ErrorMessage = "Số điện thoại hoặc Email không được để trống.")]
        public required string PhoneOrEmail { get; set; } 

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public required string Password { get; set; }
    }

    public class AuthResponseDTO
    {
        public int UserId { get; set; }
        public required string PhoneNumber { get; set; }
        public required string FullName { get; set; }
        public required string Token { get; set; }
        public required string RefreshToken { get; set; }
        public required string Role { get; set; }
    }
    public class RefreshTokenDTO
    {
        [Required(ErrorMessage = "Access Token không được để trống.")]
        public required string AccessToken { get; set; }

        [Required(ErrorMessage = "Refresh Token không được để trống.")]
        public required string RefreshToken { get; set; }
    }

    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Mật khẩu cũ không được để trống.")]
        public required string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public required string NewPassword { get; set; }
    }
}