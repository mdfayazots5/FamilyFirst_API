using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FinanceConsentConfiguration : IEntityTypeConfiguration<FinanceConsent>
{
    public void Configure(EntityTypeBuilder<FinanceConsent> builder)
    {
        builder.ToTable(
            "FinanceConsents",
            t =>
            {
                t.HasCheckConstraint("CK_FinanceConsents_PrivacyTier", "[PrivacyTier] BETWEEN 1 AND 3");
                t.HasCheckConstraint("CK_FinanceConsents_ConsentStatus",
                    "[ConsentStatus] IN ('NotInvited','Invited','Accepted','Declined','OptedOut')");
            });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("FinanceConsentId").ValueGeneratedOnAdd();
        builder.Property(c => c.PrivacyTier).IsRequired().HasDefaultValue(2);
        builder.Property(c => c.ConsentStatus).HasMaxLength(20).IsRequired();
        builder.Property(c => c.ConsentToken).HasMaxLength(200);
        builder.Property(c => c.ConsentVersion).HasMaxLength(10);
        builder.Property(c => c.ConsentIpAddress).HasMaxLength(45);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.FamilyMemberId)
            .HasDatabaseName("UX_FinanceConsents_FamilyMemberId")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.FamilyId)
            .HasDatabaseName("IX_FinanceConsents_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(
            "Transactions",
            t =>
            {
                t.HasCheckConstraint("CK_Transactions_TransactionType", "[TransactionType] IN ('Debit','Credit')");
                t.HasCheckConstraint("CK_Transactions_PrivacyTier", "[PrivacyTierAtCapture] BETWEEN 1 AND 3");
                t.HasCheckConstraint("CK_Transactions_QuestionStatus",
                    "[QuestionStatus] IN ('None','Pending','FamilyExpense','Personal','UnderReview','Resolved')");
            });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("TransactionId").ValueGeneratedOnAdd();
        builder.Property(t => t.MerchantName).HasMaxLength(300);
        builder.Property(t => t.MerchantNameHash).HasMaxLength(64);
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.TransactionType).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(50).IsRequired();
        builder.Property(t => t.QuestionStatus).HasMaxLength(20).IsRequired();
        builder.Property(t => t.RawSmsText).HasMaxLength(1000);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => new { t.FamilyId, t.ParsedAt })
            .HasDatabaseName("IX_Transactions_FamilyId_ParsedAt")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(t => new { t.FamilyMemberId, t.Category })
            .HasDatabaseName("IX_Transactions_FamilyMemberId_Category")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.FamilyMember)
            .WithMany()
            .HasForeignKey(t => t.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Commitment)
            .WithMany()
            .HasForeignKey(t => t.CommitmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

public sealed class TransactionQuestionConfiguration : IEntityTypeConfiguration<TransactionQuestion>
{
    public void Configure(EntityTypeBuilder<TransactionQuestion> builder)
    {
        builder.ToTable(
            "TransactionQuestions",
            t =>
            {
                t.HasCheckConstraint("CK_TransactionQuestions_QuestionType",
                    "[QuestionType] IN ('FamilyExpense','PersonalUnderstood','NeedToKnowMore','PossibleError')");
                t.HasCheckConstraint("CK_TransactionQuestions_ResolutionStatus",
                    "[ResolutionStatus] IS NULL OR [ResolutionStatus] IN ('Resolved','FamilyExpense','Personal','UnderReview')");
            });

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("TransactionQuestionId").ValueGeneratedOnAdd();
        builder.Property(q => q.QuestionType).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ContextNote).HasMaxLength(500);
        builder.Property(q => q.MemberReply).HasMaxLength(1000);
        builder.Property(q => q.ResolutionStatus).HasMaxLength(20);
        builder.Property(q => q.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(q => q.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(q => q.TransactionId)
            .HasDatabaseName("IX_TransactionQuestions_TransactionId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(q => q.Family)
            .WithMany()
            .HasForeignKey(q => q.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Transaction)
            .WithMany()
            .HasForeignKey(q => q.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.ResolvedByUser)
            .WithMany()
            .HasForeignKey(q => q.ResolvedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("BudgetId").ValueGeneratedOnAdd();
        builder.Property(b => b.Category).HasMaxLength(50).IsRequired();
        builder.Property(b => b.BudgetAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(b => new { b.FamilyId, b.Category, b.MonthYear })
            .HasDatabaseName("UX_Budgets_FamilyId_Category_MonthYear")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(b => b.Family)
            .WithMany()
            .HasForeignKey(b => b.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

public sealed class CommitmentConfiguration : IEntityTypeConfiguration<Commitment>
{
    public void Configure(EntityTypeBuilder<Commitment> builder)
    {
        builder.ToTable(
            "Commitments",
            t =>
            {
                t.HasCheckConstraint("CK_Commitments_CommitmentType",
                    "[CommitmentType] IN ('HomeLoanEmi','SIP','LICPremium','SchoolFees','OTTSubscription','ChitFund','Other')");
                t.HasCheckConstraint("CK_Commitments_FrequencyType",
                    "[FrequencyType] IN ('Monthly','Quarterly','Annual')");
                t.HasCheckConstraint("CK_Commitments_Status",
                    "[Status] IN ('Upcoming','Paid','Missed','PendingConfirmation')");
                t.HasCheckConstraint("CK_Commitments_DueDay",
                    "[DueDay] IS NULL OR [DueDay] BETWEEN 1 AND 31");
            });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("CommitmentId").ValueGeneratedOnAdd();
        builder.Property(c => c.CommitmentName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CommitmentType).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.FrequencyType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => new { c.FamilyId, c.NextDueDate })
            .HasDatabaseName("IX_Commitments_FamilyId_NextDueDate")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public sealed class FinanceSettingConfiguration : IEntityTypeConfiguration<FinanceSetting>
{
    public void Configure(EntityTypeBuilder<FinanceSetting> builder)
    {
        builder.ToTable("FinanceSettings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("FinanceSettingId").ValueGeneratedOnAdd();
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(s => s.FamilyId)
            .HasDatabaseName("UX_FinanceSettings_FamilyId")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(s => s.Family)
            .WithMany()
            .HasForeignKey(s => s.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CfoFamilyMember)
            .WithMany()
            .HasForeignKey(s => s.CfoFamilyMemberId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
