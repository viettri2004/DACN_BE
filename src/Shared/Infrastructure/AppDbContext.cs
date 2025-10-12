using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; } = null!;

        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<StudentCourse> StudentCourses { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<CourseTag> CourseTags { get; set; } = null!;
        public DbSet<LeaveComment> LeaveComments { get; set; } = null!;
        public DbSet<Lecture> Lectures { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<LectureVideo> LectureVideos { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<Questionnaire> Questionnaires { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");

            // StudentCourse composite key
            modelBuilder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentId, sc.CourseId });

            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)
                .HasForeignKey(sc => sc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // LeaveComment relations
            modelBuilder.Entity<LeaveComment>()
                .HasOne(lc => lc.Student)
                .WithMany(s => s.LeaveComments)
                .HasForeignKey(lc => lc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveComment>()
                .HasOne(lc => lc.Course)
                .WithMany(c => c.LeaveComments)
                .HasForeignKey(lc => lc.CourseId)
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

            // Comment self-ref
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Parent)
                .WithMany(p => p.Replies)
                .HasForeignKey(c => c.ReplyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Lecture)
                .WithMany(l => l.Comments)
                .HasForeignKey(c => c.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz composite key
            modelBuilder.Entity<Quiz>()
                .HasKey(q => new { q.CourseId, q.NumberId });

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Questionnaire composite key
            modelBuilder.Entity<Questionnaire>()
                .HasKey(qn => new { qn.CourseId, qn.NumberId, qn.QuestionNumber });

            modelBuilder.Entity<Questionnaire>()
                .HasOne(qn => qn.Quiz)
                .WithMany(q => q.Questionnaires)
                .HasForeignKey(qn => new { qn.CourseId, qn.NumberId })
                .OnDelete(DeleteBehavior.Cascade);

            // modelBuilder.Entity<User>()
            //     .HasIndex(u => u.Email)
            //     .IsUnique(false); 
        }
    }
}