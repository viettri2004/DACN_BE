using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Shared.Domain.Entities;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using InteractionService.Domain.Entities;
using InteractionService.Domain.Enums;
using LearningService.Domain.Entities;
using IdentityService.Domain.Entities;
using OrderingService.Domain.Entities;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace Shared.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");
        }
    }

    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.HasKey(c => c.Id);
            builder.HasOne(c => c.Enrollment)
                .WithMany(e => e.Comments)
                .HasForeignKey(c => c.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Parent)
                .WithMany(p => p.Replies)
                .HasForeignKey(c => c.ReplyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(c => c.Content).IsRequired();
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.Type)
                  .HasConversion<string>()
                  .IsRequired();
            builder.HasIndex(c => c.EnrollmentId);
            builder.HasIndex(c => c.ReplyId);
        }
    }

    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);
            builder.HasOne(o => o.Student)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.Status).IsRequired().HasColumnType("text");
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.PaymentMethod).HasColumnType("text").IsRequired(false);
            builder.Property(o => o.MoMoRequestId).HasColumnType("text").IsRequired(false);
            builder.Property(o => o.PaidAt).IsRequired(false);
            builder.HasIndex(o => o.StudentId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.CreatedAt);
        }
    }

    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);
            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(oi => oi.Course)
                .WithMany(c => c.OrderItems)
                .HasForeignKey(oi => oi.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(oi => oi.Price).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(oi => oi.FinalPrice).HasColumnType("decimal(18,2)").IsRequired();
        }
    }

    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasKey(pt => pt.Id);
            builder.HasOne(pt => pt.Order)
                  .WithMany(o => o.PaymentTransactions) 
                  .HasForeignKey(pt => pt.OrderId)
                  .OnDelete(DeleteBehavior.Cascade); 
            builder.Property(pt => pt.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(pt => pt.PaymentStatus).IsRequired();
            builder.Property(pt => pt.GatewayResponse).IsRequired(false);
            builder.HasIndex(pt => pt.OrderId);
            builder.HasIndex(pt => pt.GatewayTransactionId); 
            builder.HasIndex(pt => pt.GatewayToken);
        }
    }

    public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.HasKey(e => e.Id);
            builder.HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            builder.Property(e => e.EnrolledAt).IsRequired();
            builder.Property(e => e.ExpiresAt).IsRequired();
            builder.Property(e => e.Status).IsRequired();
            builder.HasIndex(e => e.Status);
            builder.HasIndex(e => e.ExpiresAt);
        }
    }

    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.HasOne(c => c.Instructor)
                .WithMany(i => i.Courses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CourseTagConfiguration : IEntityTypeConfiguration<CourseTag>
    {
        public void Configure(EntityTypeBuilder<CourseTag> builder)
        {
            builder.HasKey(ct => new { ct.CourseId, ct.TagId });

            builder.HasOne(ct => ct.Course)
                .WithMany(c => c.CourseTags)
                .HasForeignKey(ct => ct.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ct => ct.Tag)
                .WithMany(t => t.Courses)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LectureConfiguration : IEntityTypeConfiguration<Lecture>
    {
        public void Configure(EntityTypeBuilder<Lecture> builder)
        {
            builder.HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.HasOne(d => d.Lecture)
                .WithMany(l => l.Documents)
                .HasForeignKey(d => d.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LectureVideoConfiguration : IEntityTypeConfiguration<LectureVideo>
    {
        public void Configure(EntityTypeBuilder<LectureVideo> builder)
        {
            builder.HasOne(v => v.Lecture)
                .WithMany(l => l.LectureVideos)
                .HasForeignKey(v => v.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(v => v.AnalysisResult).HasColumnType("jsonb");
        }
    }

    public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
    {
        public void Configure(EntityTypeBuilder<QuizAttempt> builder)
        {
            builder.HasKey(qa => qa.Id);
            builder.HasOne(qa => qa.Enrollment)
                  .WithMany(e => e.QuizAttempts) 
                  .HasForeignKey(qa => qa.EnrollmentId)
                  .OnDelete(DeleteBehavior.Restrict); 
            builder.HasOne(qa => qa.Quiz)
                  .WithMany(q => q.QuizAttempts) 
                  .HasForeignKey(qa => qa.QuizId)
                  .OnDelete(DeleteBehavior.Restrict); 
            builder.Property(qa => qa.AttemptedAt).IsRequired();
            builder.HasIndex(qa => qa.EnrollmentId);
            builder.HasIndex(qa => qa.QuizId);
        }
    }

    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.HasKey(q => q.Id);
            builder.HasOne(q => q.Lecture)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(q => q.LectureId);
        }
    }

    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.HasKey(q => q.Id);
            builder.HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(q => q.QuizId);
        }
    }

    public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
    {
        public void Configure(EntityTypeBuilder<QuestionOption> builder)
        {
            builder.HasKey(qo => qo.Id);
            builder.HasOne(qo => qo.Question)
                .WithMany(q => q.QuestionOptions)
                .HasForeignKey(qo => qo.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(qo => qo.QuestionId);
        }
    }

    public class QuizAttemptAnswerConfiguration : IEntityTypeConfiguration<QuizAttemptAnswer>
    {
        public void Configure(EntityTypeBuilder<QuizAttemptAnswer> builder)
        {
            builder.HasKey(qaa => qaa.Id);
            builder.HasOne(qaa => qaa.QuizAttempt)
                .WithMany(qa => qa.QuizAttemptAnswers)
                .HasForeignKey(qaa => qaa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(qaa => qaa.Question)
                .WithMany()
                .HasForeignKey(qaa => qaa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(qaa => qaa.SelectedOption)
                .WithMany()
                .HasForeignKey(qaa => qaa.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class CourseRequestConfiguration : IEntityTypeConfiguration<CourseRequest>
    {
        public void Configure(EntityTypeBuilder<CourseRequest> builder)
        {
            builder.HasKey(cr => cr.Id);
            builder.HasOne(cr => cr.Course)
                .WithMany(c => c.CourseRequests)
                .HasForeignKey(cr => cr.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(cr => cr.Status)
                .HasConversion<string>() 
                .IsRequired();
        }
    }

    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(n => n.Type)
                .HasConversion<string>()
                .IsRequired();
        }
    }

    public class GiftCodeConfiguration : IEntityTypeConfiguration<GiftCode>
    {
        public void Configure(EntityTypeBuilder<GiftCode> builder)
        {
            builder.HasKey(gc => gc.Id);
            builder.Property(gc => gc.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(gc => new { gc.Code, gc.CourseId }).IsUnique();
            builder.HasOne(gc => gc.Course)
                .WithMany()
                .HasForeignKey(gc => gc.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class QAThreadConfiguration : IEntityTypeConfiguration<QAThread>
    {
        public void Configure(EntityTypeBuilder<QAThread> builder)
        {
            builder.HasKey(t => t.Id);
            builder.HasOne(t => t.Course)
                .WithMany(c => c.QAThreads)
                .HasForeignKey(t => t.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(t => t.Title).IsRequired();
            builder.HasIndex(t => t.CourseId);
        }
    }

    public class QAMessageConfiguration : IEntityTypeConfiguration<QAMessage>
    {
        public void Configure(EntityTypeBuilder<QAMessage> builder)
        {
            builder.HasKey(m => m.Id);
            builder.HasOne(m => m.Thread)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(m => m.Content).IsRequired();
            builder.HasIndex(m => m.ThreadId);
        }
    }

    public class StudentLectureProgressConfiguration : IEntityTypeConfiguration<StudentLectureProgress>
    {
        public void Configure(EntityTypeBuilder<StudentLectureProgress> builder)
        {
            builder.HasKey(slp => slp.Id);
            builder.HasOne(slp => slp.Course)
                .WithMany()
                .HasForeignKey(slp => slp.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(slp => new { slp.StudentId, slp.CourseId });
            builder.HasIndex(slp => new { slp.StudentId, slp.LectureId, slp.ItemId, slp.ItemType }).IsUnique();
        }
    }

    public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
    {
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.HasKey(w => w.Id);
            builder.HasOne(w => w.Student)
                .WithMany()
                .HasForeignKey(w => w.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(w => w.Course)
                .WithMany()
                .HasForeignKey(w => w.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(w => new { w.StudentId, w.CourseId }).IsUnique();
        }
    }
}
