namespace SingularSystem.DTOs.Users
{
    public class ResetPassword
    {
        public string ID { get; set; }
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
