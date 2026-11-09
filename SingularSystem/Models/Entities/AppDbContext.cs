using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace SingularSystem.Models.Entities
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<BaseDocument> BaseDocuments { get; set; }
        public DbSet<Report> Reports { get; set; }

        #region DbSets for RegularTeacherEvaluation and related entitiess
        public DbSet<RegularTeacherQuestionnaire> RegularTeacherQuestionnaires { get; set; }
        public DbSet<RegularTeacherQuestionnaire_BehaviourTrait> RegularTeacherQuestionnaire_BehaviourTraits { get; set; }
        public DbSet<RegularTeacherScreening> RegularTeacherScreenings { get; set; }
        public DbSet<RegularTeacherEvaluationDocument> RegularTeacherEvaluationDocuments { get; set; }
        #endregion

        #region DbSets for SocialWorkerReport and related entities
        public DbSet<SocialWorkerReport> SocialWorkerReports { get; set; }
        public DbSet<SocialWorkerReport_GovernmentBenefit> SocialWorkerReport_GovernmentBenefits { get; set; }
        public DbSet<SocialWorkerReport_FamilyIncomeSource> SocialWorkerReport_FamilyIncomeSources { get; set; }
        public DbSet<SocialWorkerReport_HealthCondition> SocialWorkerReport_HealthConditions { get; set; }
        public DbSet<SocialWorkerReport_MainSchoolDifficulty> SocialWorkerReport_MainSchoolDifficulties { get; set; }
        public DbSet<SocialWorkerReport_Procedure> SocialWorkerReport_Procedures { get; set; }
        public DbSet<SocialWorkerReport_Recommendation> SocialWorkerReport_Recommendations { get; set; }
        #endregion

        #region DbSets for PsychologistReport and related entities
        public DbSet<PsychologistReport> PsychologistReports { get; set; }
        public DbSet<SnapIVQuestionnaire> SnapIVQuestionnaires { get; set; }
        public DbSet<AdaptiveBehaviorScaleDI> AdaptiveBehaviorScalesDI { get; set; }
        public DbSet<PsychologistQuestionnaire> PsychologistQuestionnaires { get; set; }
        #endregion

        #region DbSets for PsychopedagogueReport and related entities
        public DbSet<PsychopedagogueReport> PsychopedagogueReports { get; set; }
        public DbSet<PsychopedagogueQuestionnaire> PsychopedagogueQuestionnaires { get; set; }
        public DbSet<PsychopedagogueQuestionnaire_TypeOfDisability> PsychopedagogueQuestionnaire_TypeOfDisabilities { get; set; }
        public DbSet<PsychopedagogueQuestionnaire_TypeOfDisorder> PsychopedagogueQuestionnaire_TypeOfDisorders { get; set; }
        public DbSet<PsychopedagogueQuestionnaire_Recommendation> PsychopedagogueQuestionnaire_Recommendations { get; set; }
        public DbSet<CognitiveAssessment> CognitiveAssessments { get; set; }
        public DbSet<MCHATQuestionnaire> MCHATQuestionnaires { get; set; }
        public DbSet<CARSQuestionnaire> CARSQuestionnaires { get; set; }
        #endregion

        #region DbSets for PAEE and related entities
        public DbSet<PAEE> PAEEs { get; set; }
        public DbSet<PAEETypeAEE> PAEE_TypeAEE { get; set; }
        public DbSet<PAEEProfessionalsAEE> PAEE_ProfessionalsAEE { get; set; }
        public DbSet<PAEEActivitiesAEE> PAEE_ActivitiesAEE { get; set; }
        public DbSet<PAEEWorkMethodologyAEE> PAEE_WorkMethodologyAEE { get; set; }
        public DbSet<PAEEMaterialResourcesAEE> PAEE_MaterialResourcesAEE { get; set; }
        public DbSet<PAEEEvaluationCriterionAEE> PAEE_EvaluationCriterionAEE { get; set; }
        public DbSet<PAEEResourcesAndStrategiesAEE> PAEE_ResourcesAndStrategiesAEE { get; set; }
        #endregion

        public DbSet<PortageGuide> PortageGuides { get; set; }
        public DbSet<PortageGuideResponse> PortageGuideResponses { get; set; }

        public DbSet<PEI> PEIs { get; set; }
        public DbSet<PEI_Subject> PEI_Subjects { get; set; }
        public DbSet<PEI_Response> PEI_Responses { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<UserTermAcceptance> UserTermAcceptances {  get; set; }
        public DbSet<MunicipalityStudentTarget> MunicipalityStudentTargets { get; set; }
        public DbSet<MunicipalityNeckBookConfig> MunicipalityNeckBookConfigs { get; set; }
        public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; set; }

        private static void ApplySoftDeleteQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDelete
        {
            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e => e.IsActive);
        }

        public override int SaveChanges()
        {
            HandleSoftDelete();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleSoftDelete();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void HandleSoftDelete()
        {
            var entries = ChangeTracker.Entries<ISoftDelete>()
                .Where(e => e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsActive = false;
                entry.Entity.DeactivatedAt = DateTime.UtcNow;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TermConfiguration());
            modelBuilder.ApplyConfiguration(new UserTermAcceptanceConfiguration());
            modelBuilder.ApplyConfiguration(new MunicipalityNeckBookConfigConfiguration());
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(AppDbContext)
                        .GetMethod(nameof(ApplySoftDeleteQueryFilter), BindingFlags.NonPublic | BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(null, new object[] { modelBuilder });
                }
            }

            #region IDENTITY USER RELATIONS
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasQueryFilter(u => u.IsActive);

                entity.HasOne(u => u.School)
                      .WithMany()
                      .HasForeignKey(u => u.SchoolID)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
            #endregion

            #region SCHOOL RELATIONS
            modelBuilder.Entity<School>(entity =>
            {
                entity.HasOne(s => s.Principal)
                      .WithOne(u => u.SchoolAsPrincipal)
                      .HasForeignKey<School>(s => s.PrincipalID)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => s.PrincipalID)
                      .IsUnique()
                      .HasFilter("[PrincipalID] IS NOT NULL");

                entity.HasIndex(s => s.IsActive);
            });
            #endregion

            #region STUDENT RELATIONS
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasQueryFilter(s => s.IsActive);

                entity.HasIndex(s => s.CPF)
                      .IsUnique()
                      .HasFilter("[CPF] IS NOT NULL AND [CPF] <> '' AND [IsActive] = 1");

                entity.HasIndex(s => s.SUSCard)
                      .IsUnique()
                      .HasFilter("[SUSCard] IS NOT NULL AND [SUSCard] <> '' AND [IsActive] = 1");

                entity.HasIndex(s => s.RG)
                    .IsUnique()
                    .HasFilter("[RG] IS NOT NULL AND [RG] <> '' AND [IsActive] = 1");

                modelBuilder.Entity<Student>()
                    .HasOne(s => s.School)
                    .WithMany(s => s.Students)
                    .HasForeignKey(s => s.SchoolID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasIndex(s => s.IsActive);
            });
            #endregion

            #region BASE DOCUMENT RELATIONS (Unificado)
            modelBuilder.Entity<BaseDocument>(entity =>
            {
                entity.ToTable("BaseDocuments");

                entity.HasOne(d => d.Student)
                      .WithMany(b => b.BaseDocuments)
                      .HasForeignKey(d => d.StudentID)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                      .WithMany()
                      .HasForeignKey(d => d.UserID)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion

            #region REGULAR TEACHER SCREENING AND QUESTIONNAIRE RELATIONS
            modelBuilder.Entity<RegularTeacherQuestionnaire>(entity =>
            {
                entity.ToTable("RegularTeacherQuestionnaires");
            });

            modelBuilder.Entity<RegularTeacherQuestionnaire_BehaviourTrait>(entity =>
            {
                entity.ToTable("RegularTeacherQuestionnaire_BehaviourTraits");

                entity.HasOne(bt => bt.RegularTeacherQuestionnaire)
                    .WithMany(q => q.BehaviorTraits)
                    .HasForeignKey(bt => bt.RegularTeacherQuestionnaireID)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.RegularTeacherQuestionnaireID, x.BehaviorTrait }).IsUnique();
            });

            modelBuilder.Entity<RegularTeacherScreening>(entity =>
            {
                entity.ToTable("RegularTeacherScreenings");

            });

            modelBuilder.Entity<RegularTeacherEvaluationDocument>(entity =>
            {
                entity.ToTable("RegularTeacherEvaluationDocuments");

                entity.HasOne(d => d.RegularTeacherScreening)
                      .WithOne(s => s.RegularTeacherEvaluationDocument)
                      .HasForeignKey<RegularTeacherEvaluationDocument>(d => d.RegularTeacherScreeningID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.RegularTeacherQuestionnaire)
                      .WithOne(q => q.RegularTeacherEvaluationDocument)
                      .HasForeignKey<RegularTeacherEvaluationDocument>(d => d.RegularTeacherQuestionnaireID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.RegularTeacherScreeningID).IsUnique();
                entity.HasIndex(x => x.RegularTeacherQuestionnaireID).IsUnique();
            });
            #endregion

            #region REPORTS RELATIONS
            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("Reports");

                entity.HasOne(ur => ur.SocialWorkerReport)
                      .WithOne(sw => sw.Report)
                      .HasForeignKey<SocialWorkerReport>(sw => sw.ReportID)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(ur => ur.PsychologistReport)
                      .WithOne(pr => pr.Report)
                      .HasForeignKey<PsychologistReport>(pr => pr.ReportID)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(ur => ur.PsychopedagogueReport)
                      .WithOne(pp => pp.Report)
                      .HasForeignKey<PsychopedagogueReport>(pp => pp.ReportID)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.ClientCascade);
            });

            #region SOCIAL WORKER REPORT RELATIONS
            modelBuilder.Entity<SocialWorkerReport>(entity =>
            {
                entity.ToTable("SocialWorkerReports");
                entity.HasIndex(pr => pr.ReportID).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_Procedure>(entity =>
            {
                entity.ToTable("SocialWorkerReport_Procedures");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.ProceduresUsed)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Procedure }).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_GovernmentBenefit>(entity =>
            {
                entity.ToTable("SocialWorkerReport_GovernmentBenefits");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.ReceivedGovernmentBenefits)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Benefit }).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_FamilyIncomeSource>(entity =>
            {
                entity.ToTable("SocialWorkerReport_FamilyIncomeSources");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.FamilyIncomeSource)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Source }).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_HealthCondition>(entity =>
            {
                entity.ToTable("SocialWorkerReport_HealthConditions");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.HealthConditions)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Condition }).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_MainSchoolDifficulty>(entity =>
            {
                entity.ToTable("SocialWorkerReport_MainSchoolDifficulties");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.MainSchoolDifficulties)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Difficulty }).IsUnique();
            });

            modelBuilder.Entity<SocialWorkerReport_Recommendation>(entity =>
            {
                entity.ToTable("SocialWorkerReport_Recommendations");
                entity.HasOne(e => e.SocialWorkerReport)
                      .WithMany(e => e.Recommendations)
                      .HasForeignKey(e => e.SocialWorkerReportID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SocialWorkerReportID, x.Recommendation }).IsUnique();
            });
            #endregion

            #region PSYCHOLOGIST REPORT RELATIONS
            modelBuilder.Entity<SnapIVQuestionnaire>(entity =>
            {
                entity.ToTable("SnapIVQuestionnaires");
            });

            modelBuilder.Entity<AdaptiveBehaviorScaleDI>(entity =>
            {
                entity.ToTable("AdaptiveBehaviorScalesDI");
            });

            modelBuilder.Entity<PsychologistQuestionnaire>(entity =>
            {
                entity.ToTable("PsychologistQuestionnaires");
            });

            modelBuilder.Entity<PsychologistReport>(entity =>
            {
                entity.ToTable("PsychologistReports");
                entity.HasIndex(pr => pr.ReportID).IsUnique();

                entity.HasOne(pr => pr.AdaptiveBehaviorScaleDI)
                      .WithOne(q => q.PsychologistReport)
                      .HasForeignKey<AdaptiveBehaviorScaleDI>(q => q.PsychologistReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(pr => pr.PsychologistQuestionnaire)
                      .WithOne(q => q.PsychologistReport)
                      .HasForeignKey<PsychologistQuestionnaire>(q => q.PsychologistReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(pr => pr.SnapIVQuestionnaire)
                      .WithOne(q => q.PsychologistReport)
                      .HasForeignKey<SnapIVQuestionnaire>(q => q.PsychologistReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);
            });
            #endregion

            #region PSYCHOPEDAGOGUE REPORT RELATIONS
            modelBuilder.Entity<PsychopedagogueQuestionnaire>(entity =>
            {
                entity.ToTable("PsychopedagogueQuestionnaires");
            });

            modelBuilder.Entity<PsychopedagogueQuestionnaire_TypeOfDisability>(entity =>
            {
                entity.ToTable("PsychopedagogueQuestionnaire_TypeOfDisabilities");
                entity.HasOne(e => e.PsychopedagogueQuestionnaire)
                      .WithMany(e => e.TypeOfDisability)
                      .HasForeignKey(e => e.PsychopedagogueQuestionnaireID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.PsychopedagogueQuestionnaireID, x.Disability }).IsUnique();
            });

            modelBuilder.Entity<PsychopedagogueQuestionnaire_TypeOfDisorder>(entity =>
            {
                entity.ToTable("PsychopedagogueQuestionnaire_TypeOfDisorders");
                entity.HasOne(e => e.PsychopedagogueQuestionnaire)
                      .WithMany(e => e.TypeOfDisorder)
                      .HasForeignKey(e => e.PsychopedagogueQuestionnaireID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.PsychopedagogueQuestionnaireID, x.Disorder }).IsUnique();
            });

            modelBuilder.Entity<PsychopedagogueQuestionnaire_Recommendation>(entity =>
            {
                entity.ToTable("PsychopedagogueQuestionnaire_Recommendations");
                entity.HasOne(e => e.PsychopedagogueQuestionnaire)
                      .WithMany(e => e.Recommendations)
                      .HasForeignKey(e => e.PsychopedagogueQuestionnaireID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.PsychopedagogueQuestionnaireID, x.Recommendation }).IsUnique();
            });

            modelBuilder.Entity<CognitiveAssessment>(entity =>
            {
                entity.ToTable("CognitiveAssessments");
            });

            modelBuilder.Entity<CARSQuestionnaire>(entity =>
            {
                entity.ToTable("CARSQuestionnaires");
            });

            modelBuilder.Entity<MCHATQuestionnaire>(entity =>
            {
                entity.ToTable("MCHATQuestionnaires");
            });

            modelBuilder.Entity<PsychopedagogueReport>(entity =>
            {
                entity.ToTable("PsychopedagogueReports");
                entity.HasIndex(pr => pr.ReportID).IsUnique();

                entity.HasOne(pr => pr.CognitiveAssessment)
                      .WithOne(q => q.PsychopedagogueReport)
                      .HasForeignKey<CognitiveAssessment>(q => q.PsychopedagogueReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(pr => pr.CARSQuestionnaire)
                      .WithOne(q => q.PsychopedagogueReport)
                      .HasForeignKey<CARSQuestionnaire>(q => q.PsychopedagogueReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(pr => pr.MCHATQuestionnaire)
                      .WithOne(q => q.PsychopedagogueReport)
                      .HasForeignKey<MCHATQuestionnaire>(q => q.PsychopedagogueReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);

                entity.HasOne(pr => pr.PsychopedagogueQuestionnaire)
                      .WithOne(q => q.PsychopedagogueReport)
                      .HasForeignKey<PsychopedagogueQuestionnaire>(q => q.PsychopedagogueReportID)
                      .OnDelete(DeleteBehavior.ClientCascade);
            });
            #endregion

            #endregion

            #region PORTAGE GUIDE RELATIONS
            modelBuilder.Entity<PortageGuide>(entity =>
            {
                entity.ToTable("PortageGuides");
                
                //entity.HasOne(p => p.PAEE)
                //    .WithOne(e => e.Portage)
                //    .HasForeignKey<PAEE>(e => e.PortageID)
                //    .IsRequired(false)
                //    .OnDelete(DeleteBehavior.ClientCascade);
            });

            modelBuilder.Entity<PortageGuideResponse>(entity =>
            {
                entity.HasOne(pr => pr.PortageGuide)
                    .WithMany(pg => pg.Responses)
                    .HasForeignKey(pr => pr.PortageGuideID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.OwnsOne(pr => pr.Value, v =>
                {
                    v.Property(v => v.Category)
                        .HasMaxLength(100)
                        .IsRequired();

                    v.Property(v => v.Response)
                        .HasMaxLength(500)
                        .IsRequired();
                });

                entity.Property(p => p.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(pr => pr.PortageGuideID);
            });
            #endregion

            #region PAEE RELATIONS
            modelBuilder.Entity<PAEE>(entity =>
            {
                entity.ToTable("PAEEs");

                entity.HasOne(p => p.Portage)
                    .WithOne(e => e.PAEE)
                    .HasForeignKey<PAEE>(e => e.PortageID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientCascade);
            });

            modelBuilder.Entity<PAEETypeAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.OrganizationOfCare)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEProfessionalsAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.OtherProfessionals)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEActivitiesAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.DifferentiatedActivitiesInAEE)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEWorkMethodologyAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.WorkMethodology)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEMaterialResourcesAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.MaterialResourcesAndEquipment)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEEvaluationCriterionAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.CriteriaInEvaluation)
                      .HasForeignKey(e => e.PAEEID);
            });

            modelBuilder.Entity<PAEEResourcesAndStrategiesAEE>(entity =>
            {
                entity.HasOne(e => e.PAEE)
                      .WithMany(e => e.ResourcesAndStrategies)
                      .HasForeignKey(e => e.PAEEID);
            });

            #endregion

            #region ATTACHMENT RELATIONS
            modelBuilder.Entity<Attachment>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region NOTIFICATION RELATIONS
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            });
            #endregion

            #region PEI RELATIONS

            modelBuilder.Entity<PEI>(entity =>
            {
                entity.ToTable("PEIs");


                entity.HasMany(p => p.Subjects)
                      .WithOne(s => s.PEI)
                      .HasForeignKey(s => s.PEIID)
                      .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<PEI_Subject>(entity =>
            {
                entity.ToTable("PEI_Subjects");

                entity.Property(s => s.Subject).IsRequired();

                entity.HasIndex(s => new { s.PEIID, s.Subject })
                      .IsUnique();

                entity.HasMany(s => s.Responses)
                      .WithOne(r => r.PEISubject)
                      .HasForeignKey(r => r.PEISubjectID)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<PEI_Response>(entity =>
            {
                entity.ToTable("PEI_Responses");

                entity.Property(r => r.Key)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(r => new { r.PEISubjectID, r.Key })
                      .IsUnique();

                entity.Property(r => r.Category)
                      .HasMaxLength(100);
            });
            #endregion

            modelBuilder.Entity<Term>(entity =>
            {
                entity.ToTable("Terms");
            });

            modelBuilder.Entity<UserTermAcceptance>(entity =>
            {
                entity.ToTable("UserTermAcceptances");
            });

            modelBuilder.Entity<MunicipalityStudentTarget>(entity =>
            {
                entity.ToTable("MunicipalityStudentTargets");
                entity.HasIndex(m => new { m.State, m.City }).IsUnique();
                entity.Property(m => m.TotalStudents).IsRequired();
            });

            #region PROCESSED WEBHOOK EVENT (dedup de webhooks)
            modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
            {
                entity.ToTable("ProcessedWebhookEvents");

                // Idempotência atômica no nível do banco: a mesma chave de evento não pode ser
                // inserida duas vezes (Req 4.12, 12.5).
                entity.HasIndex(e => new { e.Provider, e.EnvelopeOrDocumentToken, e.EventState })
                      .IsUnique();
            });
            #endregion
        }
    }
}
