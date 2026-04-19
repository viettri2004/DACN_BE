namespace CourseService.Application.DTOs
{
    public class MarkItemCompletedDTO
    {
        public string LectureId { get; set; } = null!;
        public string ItemId { get; set; } = null!;
        public string ItemType { get; set; } = null!; // "Video", "Document", "Quiz"
    }
}
