using Entities;
using CourseService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using src.Shared.Domain.Entities;
using AccountService.Domain.Enums;
using CourseService.Domain.Enums;

namespace Data.Context
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<CourseTag> CourseTags { get; set; } = null!;
        public DbSet<Lecture> Lectures { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<LectureVideo> LectureVideos { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<QAThread> QAThreads { get; set; } = null!;
        public DbSet<QAMessage> QAMessages { get; set; } = null!;
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
        public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<InstructorRequest> InstructorRequests { get; set; } = null!;
        public DbSet<CourseRequest> CourseRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<GiftCode> GiftCodes { get; set; } = null!;
        public DbSet<StudentLectureProgress> StudentLectureProgresses { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}