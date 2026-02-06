using System;

namespace Entities
{
    public class QuestionOption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }

        public string QuestionId { get; set; } = null!;
        public Question Question { get; set; } = null!;
    }
}
