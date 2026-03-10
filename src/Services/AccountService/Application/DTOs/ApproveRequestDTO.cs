namespace AccountService.Application.DTOs
{
    public class ApproveRequestDTO
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int RequestId { get; set; }
        public bool IsApproved { get; set; }
    }
}
