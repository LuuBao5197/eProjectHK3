using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Xml;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<StaffQualification> StaffQualifications { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<StaffSubject> StaffSubjects { get; set; }
    public DbSet<StudentClass> StudentClasses { get; set; }
    public DbSet<Contest> Contests { get; set; }
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<Artwork> Artworks { get; set; }
    public DbSet<SubmissionReview> SubmissionReviews { get; set; }
    public DbSet<Exhibition> Exhibitions { get; set; }
    public DbSet<ExhibitionArtwork> ExhibitionArtworks { get; set; }
    public DbSet<Award> Awards { get; set; }
    public DbSet<StudentAward> StudentAwards { get; set; }
    public DbSet<RatingLevel> RatingLevels { get; set; }
    public DbSet<Condition> Conditions { get; set; }

    public DbSet<Reject> Rejects { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<ContestJudge> ContestJudges { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Many-to-many relationships
        modelBuilder.Entity<StudentClass>()
            .HasKey(sc => new { sc.StudentId, sc.ClassId });

        modelBuilder.Entity<StaffSubject>()
            .HasKey(ss => new { ss.StaffId, ss.SubjectId });

        modelBuilder.Entity<ExhibitionArtwork>()
            .HasKey(ea => new { ea.ExhibitionId, ea.ArtworkId });

        modelBuilder.Entity<StudentAward>()
            .HasKey(sa => new { sa.StudentId, sa.AwardId });

        modelBuilder.Entity<SubmissionReview>()
            .HasKey(sr => new { sr.SubmissionId, sr.StaffId });

        modelBuilder.Entity<StaffQualification>()
            .HasKey(sq => new { sq.StaffId, sq.QualificationId });

        modelBuilder.Entity<ContestJudge>()
            .HasKey(cj=> new {cj.StaffId, cj.ContestId });
        // One-to-one relationships

        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Staff>()
            .HasOne(s => s.User)
            .WithOne(u => u.Staff)
            .HasForeignKey<Staff>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Artwork>()
            .HasOne(a => a.Submission)
            .WithOne(s => s.Artwork)
            .HasForeignKey<Artwork>(a => a.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationships
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Staff)
            .WithMany(s => s.Classes)
            .HasForeignKey(c => c.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Award>()
            .HasOne(a => a.Contest)
            .WithMany(c => c.Awards)
            .HasForeignKey(a => a.ContestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contest>()
            .HasOne(c => c.Organizer)
            .WithMany(s => s.OrganizedContests)
            .HasForeignKey(c => c.OrganizedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Exhibition>()
            .HasOne(e => e.Organizer)
            .WithMany(s => s.OrganizedExhibitions)
            .HasForeignKey(e => e.OrganizedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StaffQualification>()
            .HasOne(sq => sq.Qualification)
            .WithMany(q => q.StaffQualifications)
            .HasForeignKey(sq => sq.QualificationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StaffQualification>()
            .HasOne(sq => sq.Staff)
            .WithMany(s => s.StaffQualifications)
            .HasForeignKey(sq => sq.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubmissionReview>()
            .HasOne(sr => sr.RatingLevel)
            .WithMany(rl => rl.SubmissionReviews)
            .HasForeignKey(sr => sr.RatingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Condition>()
          .HasOne(c => c.Contest)
          .WithMany(c => c.Conditions)
          .HasForeignKey(c => c.ContestId)
          .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ContestJudge>()
            .HasOne(cj=>cj.Contest)
            .WithMany(c=>c.ContestJudge)
            .HasForeignKey(cj=>cj.ContestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ContestJudge>()
          .HasOne(cj => cj.Staff)
          .WithMany(c => c.ContestJudge)
          .HasForeignKey(cj => cj.StaffId)
          .OnDelete(DeleteBehavior.Restrict);

    }
}
