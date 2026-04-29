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
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<CourseTag> CourseTags { get; set; } = null!;
        public DbSet<Lecture> Lectures { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<LectureVideo> LectureVideos { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<QuestionAnswer> QuestionAnswers { get; set; } = null!;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Enrollment)
                    .WithMany(e => e.Comments)
                    .HasForeignKey(c => c.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Course)
                    .WithMany()
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Parent)
                    .WithMany(p => p.Replies)
                    .HasForeignKey(c => c.ReplyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.Content).IsRequired();
                entity.Property(c => c.CreatedAt).IsRequired();
                entity.Property(c => c.Type)
                      .HasConversion<string>()
                      .IsRequired();
                entity.HasIndex(c => c.EnrollmentId);
                entity.HasIndex(c => c.ReplyId);
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Student)
                    .WithOne(s => s.Cart)
                    .HasForeignKey<Cart>(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(c => c.StudentId).IsUnique();
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(ci => ci.Id);
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ci => ci.Course)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(ci => new { ci.CartId, ci.CourseId }).IsUnique();
                entity.Property(ci => ci.Price).HasColumnType("decimal(18,2)").IsRequired();
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.HasOne(o => o.Student)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(o => o.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(o => o.Status).IsRequired().HasColumnType("text");
                entity.Property(o => o.CreatedAt).IsRequired();
                entity.Property(o => o.PaymentMethod).HasColumnType("text").IsRequired(false);
                entity.Property(o => o.MoMoRequestId).HasColumnType("text").IsRequired(false);
                entity.Property(o => o.PaidAt).IsRequired(false);
                entity.HasIndex(o => o.StudentId);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.CreatedAt);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(oi => oi.Course)
                    .WithMany(c => c.OrderItems)
                    .HasForeignKey(oi => oi.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(oi => oi.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(oi => oi.FinalPrice).HasColumnType("decimal(18,2)").IsRequired();
            });

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasKey(pt => pt.Id);
                entity.HasOne(pt => pt.Order)
                      .WithMany(o => o.PaymentTransactions) 
                      .HasForeignKey(pt => pt.OrderId)
                      .OnDelete(DeleteBehavior.Cascade); 
                entity.Property(pt => pt.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(pt => pt.PaymentStatus).IsRequired();
                entity.Property(pt => pt.GatewayResponse).IsRequired(false);
                entity.HasIndex(pt => pt.OrderId);
                entity.HasIndex(pt => pt.GatewayTransactionId); 
                entity.HasIndex(pt => pt.GatewayToken);
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
                entity.Property(e => e.EnrolledAt).IsRequired();
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpiresAt);
            });

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(i => i.Courses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseTag>()
                .HasKey(ct => new { ct.CourseId, ct.TagId });

            modelBuilder.Entity<CourseTag>()
                .HasOne(ct => ct.Course)
                .WithMany(c => c.CourseTags)
                .HasForeignKey(ct => ct.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseTag>()
                .HasOne(ct => ct.Tag)
                .WithMany(t => t.Courses)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Lecture)
                .WithMany(l => l.Documents)
                .HasForeignKey(d => d.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LectureVideo>(entity =>
            {
                entity.HasOne(v => v.Lecture)
                    .WithMany(l => l.LectureVideos)
                    .HasForeignKey(v => v.LectureId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(v => v.AnalysisResult).HasColumnType("jsonb");
            });

            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.HasKey(qa => qa.Id);
                entity.HasOne(qa => qa.Enrollment)
                      .WithMany(e => e.QuizAttempts) 
                      .HasForeignKey(qa => qa.EnrollmentId)
                      .OnDelete(DeleteBehavior.Restrict); 
                entity.HasOne(qa => qa.Quiz)
                      .WithMany(q => q.QuizAttempts) 
                      .HasForeignKey(qa => qa.QuizId)
                      .OnDelete(DeleteBehavior.Restrict); 
                entity.Property(qa => qa.AttemptedAt).IsRequired();
                entity.HasIndex(qa => qa.EnrollmentId);
                entity.HasIndex(qa => qa.QuizId);
            });

            modelBuilder.Entity<Quiz>()
                .HasKey(q => q.Id);
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lecture)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Quiz>().HasIndex(q => q.LectureId);

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.HasOne(q => q.Quiz)
                    .WithMany(qz => qz.Questions)
                    .HasForeignKey(q => q.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(q => q.QuizId);
            });

            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(qo => qo.Id);
                entity.HasOne(qo => qo.Question)
                    .WithMany(q => q.QuestionOptions)
                    .HasForeignKey(qo => qo.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(qo => qo.QuestionId);
            });

            modelBuilder.Entity<QuizAttemptAnswer>(entity =>
            {
                entity.HasKey(qaa => qaa.Id);
                entity.HasOne(qaa => qaa.QuizAttempt)
                    .WithMany(qa => qa.QuizAttemptAnswers)
                    .HasForeignKey(qaa => qaa.QuizAttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(qaa => qaa.Question)
                    .WithMany()
                    .HasForeignKey(qaa => qaa.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(qaa => qaa.SelectedOption)
                    .WithMany()
                    .HasForeignKey(qaa => qaa.SelectedOptionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<CourseRequest>(entity =>
            {
                entity.HasKey(cr => cr.Id);
                entity.HasOne(cr => cr.Course)
                    .WithMany(c => c.CourseRequests)
                    .HasForeignKey(cr => cr.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(cr => cr.Status)
                    .HasConversion<string>() 
                    .IsRequired();
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Type)
                    .HasConversion<string>()
                    .IsRequired();
            });

            modelBuilder.Entity<GiftCode>(entity =>
            {
                entity.HasKey(gc => gc.Id);
                entity.Property(gc => gc.Code).IsRequired().HasMaxLength(50);
                entity.HasIndex(gc => gc.Code).IsUnique();
                entity.HasOne(gc => gc.Course)
                    .WithMany()
                    .HasForeignKey(gc => gc.CourseId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(gc => gc.UsedByStudent)
                    .WithMany()
                    .HasForeignKey(gc => gc.UsedByStudentId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(gc => gc.CreatedBy)
                    .WithMany()
                    .HasForeignKey(gc => gc.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionAnswer>(entity =>
            {
                entity.HasKey(qa => qa.Id);
                entity.HasOne(qa => qa.Course)
                    .WithMany()
                    .HasForeignKey(qa => qa.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(qa => qa.User)
                    .WithMany()
                    .HasForeignKey(qa => qa.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(qa => qa.Parent)
                    .WithMany(p => p.Replies)
                    .HasForeignKey(qa => qa.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(qa => qa.Content).IsRequired();
                entity.Property(qa => qa.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<StudentLectureProgress>(entity =>
            {
                entity.HasKey(slp => slp.Id);
                entity.HasOne(slp => slp.Course)
                    .WithMany()
                    .HasForeignKey(slp => slp.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(slp => new { slp.StudentId, slp.CourseId });
                entity.HasIndex(slp => new { slp.StudentId, slp.LectureId, slp.ItemId, slp.ItemType }).IsUnique();
            });
        }
    }
}