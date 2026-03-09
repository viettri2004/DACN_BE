namespace AccountService.Application.DTOs
{
    public class BanUserDTO
    {
        public string UserId { get; set; } = null!;
        public bool IsBanned { get; set; }
    }
}
