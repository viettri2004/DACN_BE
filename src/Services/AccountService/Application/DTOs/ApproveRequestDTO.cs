namespace AccountService.Application.DTOs
{
    public class ApproveRequestDTO
    {
        public int RequestId { get; set; }
        public bool IsApproved { get; set; }
        public string? Reason { get; set; }
    }
}
