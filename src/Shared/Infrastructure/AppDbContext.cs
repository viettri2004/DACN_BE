using Entities;
using CourseService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using src.Shared.Domain.Entities;

namespace Data.Context
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //public DbSet<User> Users { get; set; } = null!;
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
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;
        public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<InstructorRequest> InstructorRequests { get; set; } = null!;
        public DbSet<CourseRequest> CourseRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");
            //Student → Cart 
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Student)
                    .WithOne(s => s.Cart)
                    .HasForeignKey<Cart>(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.StudentId).IsUnique();
            });
            // CartItem
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(ci => ci.Id);

                // Cart → CartItem 
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                // CartItem → Course 
                entity.HasOne(ci => ci.Course)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ci => new { ci.CartId, ci.CourseId }).IsUnique();

                entity.Property(ci => ci.Price)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
            });
            // Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.HasOne(o => o.Student)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(o => o.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(o => o.TotalAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(o => o.Status)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(o => o.CreatedAt).IsRequired();

                entity.Property(o => o.PaymentMethod).HasColumnType("text").IsRequired(false);
                entity.Property(o => o.MoMoRequestId).HasColumnType("text").IsRequired(false);
                entity.Property(o => o.PaidAt).IsRequired(false);

                entity.HasIndex(o => o.StudentId);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.CreatedAt);
            });

            // OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);

                // Order → OrderItem
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // OrderItem → Course
                entity.HasOne(oi => oi.Course)
                    .WithMany(c => c.OrderItems)
                    .HasForeignKey(oi => oi.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(oi => oi.Price)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(oi => oi.FinalPrice)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
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

            //Enrollment
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Enrollment → Student
                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Enrollment → Course
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Enrollment → Order
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

            // Course -> Instructor
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(i => i.Courses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // CourseTag composite key
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

            // Lecture relations
            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Document
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Lecture)
                .WithMany(l => l.Documents)
                .HasForeignKey(d => d.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            // LectureVideo
            modelBuilder.Entity<LectureVideo>()
                .HasOne(v => v.Lecture)
                .WithMany(l => l.LectureVideos)
                .HasForeignKey(v => v.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizAttempt
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

            // Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Enrollment)
                      .WithMany(e => e.Comments) 
                      .HasForeignKey(c => c.EnrollmentId)
                      .OnDelete(DeleteBehavior.Restrict); 

                entity.HasOne(c => c.Parent)
                      .WithMany(p => p.Replies)
                      .HasForeignKey(c => c.ReplyId)
                      .OnDelete(DeleteBehavior.Restrict); 

                entity.Property(c => c.Content).IsRequired();
                entity.Property(c => c.CreatedAt).IsRequired();

                entity.HasIndex(c => c.EnrollmentId);
                entity.HasIndex(c => c.ReplyId);
            });
            // Quiz
            modelBuilder.Entity<Quiz>()
                .HasKey(q => q.Id);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lecture)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Quiz>()
                .HasIndex(q => q.LectureId);

            // Question
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.HasOne(q => q.Quiz)
                    .WithMany(qz => qz.Questions)
                    .HasForeignKey(q => q.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(q => q.QuizId);
            });

            // QuestionOption
            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(qo => qo.Id);
                entity.HasOne(qo => qo.Question)
                    .WithMany(q => q.QuestionOptions)
                    .HasForeignKey(qo => qo.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(qo => qo.QuestionId);
            });

            // QuizAttemptAnswer
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
            
            // CourseRequest
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

            // modelBuilder.Entity<User>()
            //     .HasIndex(u => u.Email)
            //     .IsUnique(false); 
        }
    }
}