using Microsoft.EntityFrameworkCore;
using VoiceAssistantForBlind.Models;

namespace VoiceAssistantForBlind.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        
        // Existing DbSets
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<EducationItem> EducationItems { get; set; }
        public DbSet<ExperienceItem> ExperienceItems { get; set; }
        public DbSet<ProjectItem> ProjectItems { get; set; }
        public DbSet<Certification> Certifications { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<User> Users { get; set; }
        
        // NEW: Interview Module DbSets
        public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
        public DbSet<InterviewSession> InterviewSessions { get; set; }
        public DbSet<InterviewAnswer> InterviewAnswers { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Existing configurations...
            ConfigureExistingEntities(modelBuilder);
            
            // Interview Question configuration
            modelBuilder.Entity<InterviewQuestion>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Topic).IsRequired();
                entity.Property(q => q.Question).IsRequired();
                entity.HasIndex(q => new { q.Topic, q.DisplayOrder });
            });
            
            // Interview Session configuration
            modelBuilder.Entity<InterviewSession>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.SessionId).IsRequired();
                entity.HasIndex(s => s.SessionId).IsUnique();
                
                entity.HasMany(s => s.Answers)
                      .WithOne()
                      .HasForeignKey(a => a.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Interview Answer configuration
            modelBuilder.Entity<InterviewAnswer>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Question).IsRequired();
                entity.Property(a => a.UserAnswer).IsRequired();
            });
        }
        
        private void ConfigureExistingEntities(ModelBuilder modelBuilder)
        {
            // Your existing entity configurations
            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Education)
                .WithOne(e => e.UserProfile)
                .HasForeignKey(e => e.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Experience)
                .WithOne(e => e.UserProfile)
                .HasForeignKey(e => e.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Projects)
                .WithOne(p => p.UserProfile)
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Certifications)
                .WithOne(c => c.UserProfile)
                .HasForeignKey(c => c.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Achievements)
                .WithOne(a => a.UserProfile)
                .HasForeignKey(a => a.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasIndex(j => j.JobCode).IsUnique();
                entity.Property(j => j.JobCode).IsRequired().HasMaxLength(20);
                entity.Property(j => j.CompanyName).IsRequired().HasMaxLength(100);
                entity.Property(j => j.JobTitle).IsRequired().HasMaxLength(100);
                entity.Property(j => j.RequiredSkills).IsRequired();
                entity.Property(j => j.HREmail).IsRequired().HasMaxLength(100);
                entity.Property(j => j.IsActive).HasDefaultValue(true);
                entity.Property(j => j.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(j => new { j.IsActive, j.LastDateToApply });
            });
            
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.HasIndex(a => a.Username).IsUnique();
                entity.HasIndex(a => a.Email).IsUnique();
                entity.Property(a => a.Username).IsRequired().HasMaxLength(50);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(100);
                entity.Property(a => a.PasswordHash).IsRequired();
                entity.Property(a => a.Salt).IsRequired();
                entity.Property(a => a.Role).HasMaxLength(50).HasDefaultValue("Admin");
                entity.Property(a => a.IsActive).HasDefaultValue(true);
                entity.HasIndex(a => new { a.Username, a.IsActive });
            });
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Salt).IsRequired();
                
                entity.HasOne(u => u.UserProfile)
                      .WithOne()
                      .HasForeignKey<User>(u => u.UserProfileId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}