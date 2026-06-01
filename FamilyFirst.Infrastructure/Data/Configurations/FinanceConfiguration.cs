using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyFirst.Infrastructure.Data.Configurations;

public sealed class FinanceConsentConfiguration : IEntityTypeConfiguration<FinanceConsent>
{
    public void Configure(EntityTypeBuilder<FinanceConsent> builder)
    {
        builder.ConfigureBaseEntity("tblFinanceConsent", "FinanceConsentId");

        builder.ToTable("tblFinanceConsent", t =>
        {
            t.HasCheckConstraint("CK_tblFinanceConsent_PrivacyTier", "[PrivacyTier] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_tblFinanceConsent_ConsentStatus",
                "[ConsentStatus] IN ('NotInvited','Invited','Accepted','Declined','OptedOut')");
        });

        builder.Property(c => c.PrivacyTier).IsRequired().HasDefaultValue(2);
        builder.Property(c => c.ConsentStatus).HasMaxLength(20).IsRequired();
        builder.Property(c => c.ConsentToken).HasMaxLength(200);
        builder.Property(c => c.ConsentVersion).HasMaxLength(10);
        builder.Property(c => c.ConsentIpAddress).HasMaxLength(45);

        builder.HasIndex(c => c.FamilyMemberId)
            .IsUnique()
            .HasDatabaseName("UK_tblFinanceConsent_FamilyMemberId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.FamilyId)
            .HasDatabaseName("IDX_tblFinanceConsent_FamilyId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ConfigureBaseEntity("tblTransaction", "TransactionId");

        builder.ToTable("tblTransaction", t =>
        {
            t.HasCheckConstraint("CK_tblTransaction_TransactionType", "[TransactionType] IN ('Debit','Credit')");
            t.HasCheckConstraint("CK_tblTransaction_PrivacyTier", "[PrivacyTierAtCapture] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_tblTransaction_QuestionStatus",
                "[QuestionStatus] IN ('None','Pending','FamilyExpense','Personal','UnderReview','Resolved')");
        });

        builder.Property(t => t.MerchantName).HasMaxLength(300);
        builder.Property(t => t.MerchantNameHash).HasMaxLength(64);
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.TransactionType).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(50).IsRequired();
        builder.Property(t => t.QuestionStatus).HasMaxLength(20).IsRequired();
        builder.Property(t => t.RawSmsText).HasMaxLength(1000);

        builder.HasIndex(t => new { t.FamilyId, t.ParsedAt })
            .HasDatabaseName("IDX_tblTransaction_FamilyId_ParsedAt")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(t => new { t.FamilyMemberId, t.Category })
            .HasDatabaseName("IDX_tblTransaction_FamilyMemberId_Category")
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
    }
}

public sealed class TransactionQuestionConfiguration : IEntityTypeConfiguration<TransactionQuestion>
{
    public void Configure(EntityTypeBuilder<TransactionQuestion> builder)
    {
        builder.ConfigureBaseEntity("tblTransactionQuestion", "TransactionQuestionId");

        builder.ToTable("tblTransactionQuestion", t =>
        {
            t.HasCheckConstraint("CK_tblTransactionQuestion_QuestionType",
                "[QuestionType] IN ('FamilyExpense','PersonalUnderstood','NeedToKnowMore','PossibleError')");
            t.HasCheckConstraint("CK_tblTransactionQuestion_ResolutionStatus",
                "[ResolutionStatus] IS NULL OR [ResolutionStatus] IN ('Resolved','FamilyExpense','Personal','UnderReview')");
        });

        builder.Property(q => q.QuestionType).HasMaxLength(30).IsRequired();
        builder.Property(q => q.ContextNote).HasMaxLength(500);
        builder.Property(q => q.MemberReply).HasMaxLength(1000);
        builder.Property(q => q.ResolutionStatus).HasMaxLength(20);

        builder.HasIndex(q => q.TransactionId)
            .HasDatabaseName("IDX_tblTransactionQuestion_TransactionId")
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
    }
}

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ConfigureBaseEntity("tblBudget", "BudgetId");

        builder.Property(b => b.Category).HasMaxLength(50).IsRequired();
        builder.Property(b => b.BudgetAmount).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(b => new { b.FamilyId, b.Category, b.MonthYear })
            .IsUnique()
            .HasDatabaseName("UK_tblBudget_FamilyId_Category_MonthYear")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(b => b.Family)
            .WithMany()
            .HasForeignKey(b => b.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommitmentConfiguration : IEntityTypeConfiguration<Commitment>
{
    public void Configure(EntityTypeBuilder<Commitment> builder)
    {
        builder.ConfigureBaseEntity("tblCommitment", "CommitmentId");

        builder.ToTable("tblCommitment", t =>
        {
            t.HasCheckConstraint("CK_tblCommitment_CommitmentType",
                "[CommitmentType] IN ('HomeLoanEmi','SIP','LICPremium','SchoolFees','OTTSubscription','ChitFund','Other')");
            t.HasCheckConstraint("CK_tblCommitment_FrequencyType",
                "[FrequencyType] IN ('Monthly','Quarterly','Annual')");
            t.HasCheckConstraint("CK_tblCommitment_Status",
                "[Status] IN ('Upcoming','Paid','Missed','PendingConfirmation')");
            t.HasCheckConstraint("CK_tblCommitment_DueDay",
                "[DueDay] IS NULL OR [DueDay] BETWEEN 1 AND 31");
        });

        builder.Property(c => c.CommitmentName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CommitmentType).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.FrequencyType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(c => new { c.FamilyId, c.NextDueDate })
            .HasDatabaseName("IDX_tblCommitment_FamilyId_NextDueDate")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FamilyMember)
            .WithMany()
            .HasForeignKey(c => c.FamilyMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinanceSettingConfiguration : IEntityTypeConfiguration<FinanceSetting>
{
    public void Configure(EntityTypeBuilder<FinanceSetting> builder)
    {
        builder.ConfigureBaseEntity("tblFinanceSettings", "FinanceSettingId");

        builder.HasIndex(s => s.FamilyId)
            .IsUnique()
            .HasDatabaseName("UK_tblFinanceSettings_FamilyId")
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
    }
}
