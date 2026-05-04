using Microsoft.EntityFrameworkCore;

namespace LearnToCode.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    public DbSet<RoleRequest> RoleRequests { get; set; }

    public DbSet<Representation> Representations { get; set; }

    public DbSet<RepresentationMembership> RepresentationMemberships { get; set; }

    public DbSet<RepresentationJoinRequest> RepresentationJoinRequests { get; set; }

    public DbSet<TheoryLanguage> TheoryLanguages { get; set; }

    public DbSet<TheoryTopic> TheoryTopics { get; set; }

    public DbSet<TheoryContent> TheoryContents { get; set; }

    public DbSet<TheoryQuiz> TheoryQuizzes { get; set; }

    public DbSet<TheoryQuizQuestion> TheoryQuizQuestions { get; set; }

    public DbSet<TheoryQuizOption> TheoryQuizOptions { get; set; }

    public DbSet<TheoryQuizQuestionAnswer> TheoryQuizQuestionAnswers { get; set; }

    public DbSet<TheoryPageReadProgress> TheoryPageReadProgresses { get; set; }

    public DbSet<TheoryTopicRequest> TheoryTopicRequests { get; set; }

    public DbSet<TheoryContentRequest> TheoryContentRequests { get; set; }

    public DbSet<TheoryQuizRequest> TheoryQuizRequests { get; set; }

    public DbSet<Exercise> Exercises { get; set; }

    public DbSet<ExerciseTestCase> ExerciseTestCases { get; set; }

    public DbSet<ExerciseSubmission> ExerciseSubmissions { get; set; }

    public DbSet<ExerciseRequest> ExerciseRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // FullName ir computed property (FirstName + LastName) — netiek glabāts.
            entity.Ignore(user => user.FullName);

            entity.Property(user => user.Username).HasMaxLength(100);
            entity.Property(user => user.FirstName).HasMaxLength(100);
            entity.Property(user => user.LastName).HasMaxLength(100);
            entity.Property(user => user.BirthDate).HasColumnType("date");
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Representation).HasMaxLength(200);
            entity.Property(user => user.Bio).HasMaxLength(500);
            entity.Property(user => user.Rating).HasDefaultValue(1000);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(32);

            // Unikalitāte tiek panākta ar LOWER() funkcijas indeksu DB pusē
            // (skat. migrate_users.sql). EF Core līmenī šeit nedrīkst būt
            // HasIndex(...).IsUnique() uz Username/Email, jo tas mēģinātu izveidot
            // parastu unikālo indeksu, kas nepārklās case-insensitive lookups.
        });

        modelBuilder.Entity<RoleRequest>(entity =>
        {
            entity.HasIndex(request => new { request.UserId, request.Status });
            entity.Property(request => request.RequestedRole).HasConversion<string>().HasMaxLength(32);
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(request => request.Reason).HasMaxLength(1000).IsRequired();

            entity.HasOne(request => request.User)
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Representation>(entity =>
        {
            entity.HasIndex(representation => representation.NormalizedName).IsUnique();
            entity.Property(representation => representation.Name).HasMaxLength(160).IsRequired();
            entity.Property(representation => representation.NormalizedName).HasMaxLength(160).IsRequired();
            entity.Property(representation => representation.Description).HasMaxLength(800);
            entity.Property(representation => representation.IsPublic).HasDefaultValue(true);

            entity.HasOne(representation => representation.CreatedByUser)
                .WithMany()
                .HasForeignKey(representation => representation.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RepresentationMembership>(entity =>
        {
            entity.HasIndex(membership => new { membership.RepresentationId, membership.UserId }).IsUnique();
            entity.HasIndex(membership => membership.UserId);
            entity.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(32);

            entity.HasOne(membership => membership.Representation)
                .WithMany(representation => representation.Memberships)
                .HasForeignKey(membership => membership.RepresentationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(membership => membership.User)
                .WithMany()
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RepresentationJoinRequest>(entity =>
        {
            entity.HasIndex(request => new { request.RepresentationId, request.Status });
            entity.HasIndex(request => request.UserId);
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(request => request.Message).HasMaxLength(500);

            entity.HasOne(request => request.Representation)
                .WithMany(representation => representation.JoinRequests)
                .HasForeignKey(request => request.RepresentationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.User)
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.ResolvedByUser)
                .WithMany()
                .HasForeignKey(request => request.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TheoryLanguage>(entity =>
        {
            entity.HasIndex(language => language.Title).IsUnique();
            entity.Property(language => language.Title).HasMaxLength(120).IsRequired();
            entity.Property(language => language.Description).HasMaxLength(600).IsRequired();
        });

        modelBuilder.Entity<TheoryTopic>(entity =>
        {
            entity.HasIndex(topic => new { topic.LanguageId, topic.Title }).IsUnique();
            entity.Property(topic => topic.Title).HasMaxLength(160).IsRequired();
            entity.Property(topic => topic.Difficulty).HasMaxLength(40).IsRequired();
            entity.Property(topic => topic.Description).HasMaxLength(900).IsRequired();

            entity.HasOne(topic => topic.Language)
                .WithMany(language => language.Topics)
                .HasForeignKey(topic => topic.LanguageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryContent>(entity =>
        {
            entity.HasIndex(content => content.TopicId).IsUnique();
            entity.Property(content => content.MarkdownObjectName).HasMaxLength(500).IsRequired();
            entity.Property(content => content.PageCount).HasDefaultValue(1);
            entity.Property(content => content.Version).HasDefaultValue(1);

            entity.HasOne(content => content.Topic)
                .WithOne(topic => topic.Content)
                .HasForeignKey<TheoryContent>(content => content.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryQuiz>(entity =>
        {
            entity.HasIndex(quiz => quiz.TopicId).IsUnique();
            entity.Property(quiz => quiz.Title).HasMaxLength(160).IsRequired();
            entity.Property(quiz => quiz.Description).HasMaxLength(900);

            entity.HasOne(quiz => quiz.Topic)
                .WithOne(topic => topic.Quiz!)
                .HasForeignKey<TheoryQuiz>(quiz => quiz.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryQuizQuestion>(entity =>
        {
            entity.HasIndex(question => new { question.QuizId, question.OrderIndex });
            entity.Property(question => question.Prompt).HasMaxLength(1000).IsRequired();
            entity.Property(question => question.Explanation).HasMaxLength(2000);

            entity.HasOne(question => question.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryQuizOption>(entity =>
        {
            entity.HasIndex(option => new { option.QuestionId, option.OrderIndex });
            entity.Property(option => option.Text).HasMaxLength(500).IsRequired();

            entity.HasOne(option => option.Question)
                .WithMany(question => question.Options)
                .HasForeignKey(option => option.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryQuizQuestionAnswer>(entity =>
        {
            entity.HasIndex(answer => new { answer.UserId, answer.QuestionId }).IsUnique();

            entity.HasOne(answer => answer.Question)
                .WithMany()
                .HasForeignKey(answer => answer.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(answer => answer.User)
                .WithMany()
                .HasForeignKey(answer => answer.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(answer => answer.SelectedOption)
                .WithMany()
                .HasForeignKey(answer => answer.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TheoryPageReadProgress>(entity =>
        {
            entity.ToTable("TheoryPageReadProgresses");
            entity.HasIndex(progress => progress.UserId);
            entity.HasIndex(progress => new
            {
                progress.UserId,
                progress.TheoryContentId,
                progress.ContentVersion,
                progress.PageIndex,
            }).IsUnique();

            entity.HasOne(progress => progress.User)
                .WithMany()
                .HasForeignKey(progress => progress.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(progress => progress.TheoryContent)
                .WithMany(content => content.ReadProgress)
                .HasForeignKey(progress => progress.TheoryContentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryTopicRequest>(entity =>
        {
            entity.ToTable("TheoryTopicRequests");
            entity.HasIndex(r => new { r.UserId, r.Status });
            entity.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(r => r.TopicSlug).HasMaxLength(96).IsRequired();
            entity.Property(r => r.ProposedTitle).HasMaxLength(160);
            entity.Property(r => r.ProposedDescription).HasMaxLength(900);
            entity.Property(r => r.ProposedDifficulty).HasMaxLength(40);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryContentRequest>(entity =>
        {
            entity.ToTable("TheoryContentRequests");
            entity.HasIndex(r => new { r.UserId, r.Status });
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(r => r.TopicSlug).HasMaxLength(96).IsRequired();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TheoryQuizRequest>(entity =>
        {
            entity.ToTable("TheoryQuizRequests");
            entity.HasIndex(r => new { r.UserId, r.Status });
            entity.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(r => r.TopicSlug).HasMaxLength(96).IsRequired();
            entity.Property(r => r.ProposedTitle).HasMaxLength(160).IsRequired();
            entity.Property(r => r.ProposedDescription).HasMaxLength(900).IsRequired();
            entity.Property(r => r.ProposedQuestionsJson).IsRequired();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.LanguageVersion).HasMaxLength(32).IsRequired().HasDefaultValue("3.11");
            entity.Property(e => e.Difficulty).HasMaxLength(40).IsRequired();

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExerciseRequest>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Difficulty).HasMaxLength(40).IsRequired();
            entity.Property(e => e.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.LanguageVersion).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SolutionLanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExerciseTestCase>(entity =>
        {
            entity.HasOne(tc => tc.Exercise)
                .WithMany(e => e.TestCases)
                .HasForeignKey(tc => tc.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseSubmission>(entity =>
        {
            entity.HasIndex(s => new { s.ExerciseId, s.UserId });
            entity.Property(s => s.LanguageCode).HasMaxLength(64).IsRequired();
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(s => s.ErrorMessage).HasMaxLength(500);

            entity.HasOne(s => s.Exercise)
                .WithMany(e => e.Submissions)
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
