using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SingularSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialSingular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalityNeckBookConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityNeckBookConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalityStudentTargets",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TotalStudents = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityStudentTargets", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    EnvelopeOrDocumentToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTermAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AcceptanceHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RenderedContentSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTermAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTermAcceptances_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdaptiveBehaviorScalesDI",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychologistReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgeAppropriateReadingComprehension = table.Column<int>(type: "int", nullable: false),
                    PerformsBasicMathematicalCalculations = table.Column<int>(type: "int", nullable: false),
                    UnderstandsTimeConcepts = table.Column<int>(type: "int", nullable: false),
                    FollowsMultiStepInstructions = table.Column<int>(type: "int", nullable: false),
                    InteractsAppropriatelyWithPeers = table.Column<int>(type: "int", nullable: false),
                    RecognizesOwnAndOthersEmotions = table.Column<int>(type: "int", nullable: false),
                    FollowsBasicSocialRules = table.Column<int>(type: "int", nullable: false),
                    ResolvesConflictsAppropriately = table.Column<int>(type: "int", nullable: false),
                    MaintainsPersonalHygieneIndependently = table.Column<int>(type: "int", nullable: false),
                    OrganizesSchoolMaterials = table.Column<int>(type: "int", nullable: false),
                    DressesAppropriately = table.Column<int>(type: "int", nullable: false),
                    ShowsConfidenceInFamiliarEnvironments = table.Column<int>(type: "int", nullable: false),
                    ScoreConceptualSkills = table.Column<int>(type: "int", nullable: false),
                    PercentageConceptualSkills = table.Column<double>(type: "float", nullable: false),
                    MaxPointsConceptualSkills = table.Column<int>(type: "int", nullable: false),
                    ScoreSocialSkills = table.Column<int>(type: "int", nullable: false),
                    PercentageSocialSkills = table.Column<double>(type: "float", nullable: false),
                    MaxPointsSocialSkills = table.Column<int>(type: "int", nullable: false),
                    ScorePracticalSkills = table.Column<int>(type: "int", nullable: false),
                    PercentagePracticalSkills = table.Column<double>(type: "float", nullable: false),
                    MaxPointsPracticalSkills = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    MaxPoints = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptiveBehaviorScalesDI", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    S3Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    S3Bucket = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BaseDocuments",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseDocuments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PEIs",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolYear = table.Column<int>(type: "int", nullable: false),
                    Serie = table.Column<int>(type: "int", nullable: false),
                    AcademicRecord = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicalReport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    SignatureProvider = table.Column<int>(type: "int", nullable: false),
                    TeacherSign = table.Column<bool>(type: "bit", nullable: false),
                    SignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEIs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PEIs_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortageGuides",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortageAgeRange = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortageGuides", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PortageGuides_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegularTeacherQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomDevelopment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassroomBehavior = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MainDifficulties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LearningCharacteristics = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReadingAndWritingDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogicalMathematicalReasoningDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletesSchoolActivities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletesHomework = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReactionToFrustration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InappropriateBehaviorDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyWorkingInGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyOrganizingTasks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeerAcceptance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BehaviorObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DevelopmentLevel = table.Column<int>(type: "int", nullable: false),
                    DevelopmentObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdditionalComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularTeacherQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RegularTeacherQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegularTeacherScreenings",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LimitedVocabulary = table.Column<int>(type: "int", nullable: false),
                    LimitedVocabularyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildlikeSpeech = table.Column<int>(type: "int", nullable: false),
                    ChildlikeSpeechObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyFollowingCommands = table.Column<int>(type: "int", nullable: false),
                    DifficultyFollowingCommandsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DelayedResponse = table.Column<int>(type: "int", nullable: false),
                    DelayedResponseObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvoidsTalking = table.Column<int>(type: "int", nullable: false),
                    AvoidsTalkingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyCopying = table.Column<int>(type: "int", nullable: false),
                    DifficultyCopyingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExcessiveSpellingErrors = table.Column<int>(type: "int", nullable: false),
                    ExcessiveSpellingErrorsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingIssues = table.Column<int>(type: "int", nullable: false),
                    WritingIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneticAssociationDifficulty = table.Column<int>(type: "int", nullable: false),
                    PhoneticAssociationDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingLevel = table.Column<int>(type: "int", nullable: false),
                    WritingLevelObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecognizesLettersOrWords = table.Column<int>(type: "int", nullable: false),
                    RecognizesLettersOrWordsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadingFluencyIssues = table.Column<int>(type: "int", nullable: false),
                    ReadingFluencyIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswersReadingQuestions = table.Column<int>(type: "int", nullable: false),
                    AnswersReadingQuestionsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvoidsReading = table.Column<int>(type: "int", nullable: false),
                    AvoidsReadingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberRecognitionIssues = table.Column<int>(type: "int", nullable: false),
                    NumberRecognitionIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComparisonConceptDifficulty = table.Column<int>(type: "int", nullable: false),
                    ComparisonConceptDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicOperationDifficulty = table.Column<int>(type: "int", nullable: false),
                    BasicOperationDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSpaceConceptDifficulty = table.Column<int>(type: "int", nullable: false),
                    TimeSpaceConceptDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyOrganizingMaterials = table.Column<int>(type: "int", nullable: false),
                    DifficultyOrganizingMaterialsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsHelpWithBasicActivities = table.Column<int>(type: "int", nullable: false),
                    NeedsHelpWithBasicActivitiesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyWithRoutines = table.Column<int>(type: "int", nullable: false),
                    DifficultyWithRoutinesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoesNotInitiateTasks = table.Column<int>(type: "int", nullable: false),
                    DoesNotInitiateTasksObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmatureBehavior = table.Column<int>(type: "int", nullable: false),
                    ImmatureBehaviorObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrefersYoungerChildren = table.Column<int>(type: "int", nullable: false),
                    PrefersYoungerChildrenObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BelowAgeLevelPerformance = table.Column<int>(type: "int", nullable: false),
                    BelowAgeLevelPerformanceObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EarlyChildhoodSkillLevel = table.Column<int>(type: "int", nullable: false),
                    EarlyChildhoodSkillLevelObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ForgetsContent = table.Column<int>(type: "int", nullable: false),
                    ForgetsContentObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttentionDifficulty = table.Column<int>(type: "int", nullable: false),
                    AttentionDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EasilyDistracted = table.Column<int>(type: "int", nullable: false),
                    EasilyDistractedObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsRepeatedExplanations = table.Column<int>(type: "int", nullable: false),
                    NeedsRepeatedExplanationsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FriendshipDifficulty = table.Column<int>(type: "int", nullable: false),
                    FriendshipDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EasilyFrustrated = table.Column<int>(type: "int", nullable: false),
                    EasilyFrustratedObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LowSelfEsteem = table.Column<int>(type: "int", nullable: false),
                    LowSelfEsteemObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DependsOnAdults = table.Column<int>(type: "int", nullable: false),
                    DependsOnAdultsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LowAcademicHistory = table.Column<int>(type: "int", nullable: false),
                    LowAcademicHistoryObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoProgressWithSupport = table.Column<int>(type: "int", nullable: false),
                    NoProgressWithSupportObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousReportsIndicateImpairment = table.Column<int>(type: "int", nullable: false),
                    PreviousReportsIndicateImpairmentObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurricularAdaptations = table.Column<int>(type: "int", nullable: false),
                    CurricularAdaptationsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularTeacherScreenings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RegularTeacherScreenings_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BodySocialWorker = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyPsychologist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BodyPsychopedagogue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeDisability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ReportStatus = table.Column<int>(type: "int", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PsychologistReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PsychopedagogueReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    SignatureProvider = table.Column<int>(type: "int", nullable: false),
                    SocialWorkerSign = table.Column<bool>(type: "bit", nullable: false),
                    PsychologistSign = table.Column<bool>(type: "bit", nullable: false),
                    PsychopedagogueSign = table.Column<bool>(type: "bit", nullable: false),
                    SocialWorkerSignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PsychologistSignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PsychopedagogueSignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialWorkerSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PsychologistSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PsychopedagogueSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignatureBlocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Reports_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PEI_Subjects",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PEIID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEI_Subjects", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PEI_Subjects_PEIs_PEIID",
                        column: x => x.PEIID,
                        principalTable: "PEIs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEEs",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortageID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicRecord = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicalReport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeeklyFrequency = table.Column<int>(type: "int", nullable: false),
                    OtherFrequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssistingTimePerSession = table.Column<int>(type: "int", nullable: false),
                    OtherServiceTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompositionOfAssisting = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicalSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherProfessionalsDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    SignatureProvider = table.Column<int>(type: "int", nullable: false),
                    TeacherSign = table.Column<bool>(type: "bit", nullable: false),
                    SignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEEs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEEs_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEEs_PortageGuides_PortageID",
                        column: x => x.PortageID,
                        principalTable: "PortageGuides",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PortageGuideResponses",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value_Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value_Response = table.Column<int>(type: "int", maxLength: 500, nullable: false),
                    PortageGuideID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortageGuideResponses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PortageGuideResponses_PortageGuides_PortageGuideID",
                        column: x => x.PortageGuideID,
                        principalTable: "PortageGuides",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegularTeacherQuestionnaire_BehaviourTraits",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegularTeacherQuestionnaireID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BehaviorTrait = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularTeacherQuestionnaire_BehaviourTraits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RegularTeacherQuestionnaire_BehaviourTraits_RegularTeacherQuestionnaires_RegularTeacherQuestionnaireID",
                        column: x => x.RegularTeacherQuestionnaireID,
                        principalTable: "RegularTeacherQuestionnaires",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegularTeacherEvaluationDocuments",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegularTeacherScreeningID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegularTeacherQuestionnaireID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationStatus = table.Column<int>(type: "int", nullable: false),
                    DocumentToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentStatus = table.Column<int>(type: "int", nullable: false),
                    SignatureProvider = table.Column<int>(type: "int", nullable: false),
                    TeacherSign = table.Column<bool>(type: "bit", nullable: false),
                    SignUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegularTeacherEvaluationDocuments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RegularTeacherEvaluationDocuments_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegularTeacherEvaluationDocuments_RegularTeacherQuestionnaires_RegularTeacherQuestionnaireID",
                        column: x => x.RegularTeacherQuestionnaireID,
                        principalTable: "RegularTeacherQuestionnaires",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegularTeacherEvaluationDocuments_RegularTeacherScreenings_RegularTeacherScreeningID",
                        column: x => x.RegularTeacherScreeningID,
                        principalTable: "RegularTeacherScreenings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PsychologistReports",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychologistReports_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychologistReports_Reports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "Reports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PsychopedagogueReports",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychopedagogueReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueReports_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueReports_Reports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "Reports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReports",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtherProceduresUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyComposition = table.Column<int>(type: "int", nullable: false),
                    OtherFamilyComposition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfPeopleInTheHousehold = table.Column<int>(type: "int", nullable: false),
                    OtherNumberOfPeopleInTheHousehold = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalObservationHouseholdMembers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOfHousing = table.Column<int>(type: "int", nullable: false),
                    OtherTypeOfHousing = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HousingConditions = table.Column<int>(type: "int", nullable: false),
                    OtherHousingConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfRooms = table.Column<int>(type: "int", nullable: false),
                    OtherNumberOfRooms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonthlyHouseholdIncome = table.Column<int>(type: "int", nullable: false),
                    OtherMonthlyHouseholdIncome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherFamilyIncomeSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherReceivedGovernmentBenefits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherDetailsHealthConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContinuousMedication = table.Column<int>(type: "int", nullable: false),
                    DetailsContinuousMedication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherContinuousMedication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthServiceAccess = table.Column<int>(type: "int", nullable: false),
                    OtherHealthServiceAccess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SchoolAttendance = table.Column<int>(type: "int", nullable: false),
                    OtherSchoolAttendance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcademicPerformance = table.Column<int>(type: "int", nullable: false),
                    OtherAcademicPerformance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherMainSchoolDifficulties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyMeals = table.Column<int>(type: "int", nullable: false),
                    OtherDailyMeals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoodSecurity = table.Column<int>(type: "int", nullable: false),
                    OtherFoodSecurity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyRelationship = table.Column<int>(type: "int", nullable: false),
                    OtherFamilyRelationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialSupportNetwork = table.Column<int>(type: "int", nullable: false),
                    OtherSocialSupportNetwork = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommunityActivityParticipation = table.Column<int>(type: "int", nullable: false),
                    OtherCommunityActivityParticipation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherRecommendations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocioeconomicContext = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReports_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReports_Reports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "Reports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PEI_Responses",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Response = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PEISubjectID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEI_Responses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PEI_Responses_PEI_Subjects_PEISubjectID",
                        column: x => x.PEISubjectID,
                        principalTable: "PEI_Subjects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_ActivitiesAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_ActivitiesAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_ActivitiesAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_EvaluationCriterionAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Criterion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_EvaluationCriterionAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_EvaluationCriterionAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_MaterialResourcesAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_MaterialResourcesAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_MaterialResourcesAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_ProfessionalsAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Professional = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_ProfessionalsAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_ProfessionalsAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_ResourcesAndStrategiesAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_ResourcesAndStrategiesAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_ResourcesAndStrategiesAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_TypeAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_TypeAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_TypeAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEE_WorkMethodologyAEE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PAEEID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Methodology = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAEE_WorkMethodologyAEE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PAEE_WorkMethodologyAEE_PAEEs_PAEEID",
                        column: x => x.PAEEID,
                        principalTable: "PAEEs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsychologistQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychologistReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LimitedVocabulary = table.Column<int>(type: "int", nullable: false),
                    LimitedVocabularyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChildlikeSpeech = table.Column<int>(type: "int", nullable: false),
                    ChildlikeSpeechObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyFollowingCommands = table.Column<int>(type: "int", nullable: false),
                    DifficultyFollowingCommandsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DelayedResponse = table.Column<int>(type: "int", nullable: false),
                    DelayedResponseObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvoidsTalking = table.Column<int>(type: "int", nullable: false),
                    AvoidsTalkingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyCopying = table.Column<int>(type: "int", nullable: false),
                    DifficultyCopyingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExcessiveSpellingErrors = table.Column<int>(type: "int", nullable: false),
                    ExcessiveSpellingErrorsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingIssues = table.Column<int>(type: "int", nullable: false),
                    WritingIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneticAssociationDifficulty = table.Column<int>(type: "int", nullable: false),
                    PhoneticAssociationDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WritingLevel = table.Column<int>(type: "int", nullable: false),
                    WritingLevelObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecognizesLettersOrWords = table.Column<int>(type: "int", nullable: false),
                    RecognizesLettersOrWordsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadingFluencyIssues = table.Column<int>(type: "int", nullable: false),
                    ReadingFluencyIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswersReadingQuestions = table.Column<int>(type: "int", nullable: false),
                    AnswersReadingQuestionsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvoidsReading = table.Column<int>(type: "int", nullable: false),
                    AvoidsReadingObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberRecognitionIssues = table.Column<int>(type: "int", nullable: false),
                    NumberRecognitionIssuesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComparisonConceptDifficulty = table.Column<int>(type: "int", nullable: false),
                    ComparisonConceptDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicOperationDifficulty = table.Column<int>(type: "int", nullable: false),
                    BasicOperationDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeSpaceConceptDifficulty = table.Column<int>(type: "int", nullable: false),
                    TimeSpaceConceptDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyOrganizingMaterials = table.Column<int>(type: "int", nullable: false),
                    DifficultyOrganizingMaterialsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsHelpWithBasicActivities = table.Column<int>(type: "int", nullable: false),
                    NeedsHelpWithBasicActivitiesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DifficultyWithRoutines = table.Column<int>(type: "int", nullable: false),
                    DifficultyWithRoutinesObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoesNotInitiateTasks = table.Column<int>(type: "int", nullable: false),
                    DoesNotInitiateTasksObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmatureBehavior = table.Column<int>(type: "int", nullable: false),
                    ImmatureBehaviorObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrefersYoungerChildren = table.Column<int>(type: "int", nullable: false),
                    PrefersYoungerChildrenObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BelowAgeLevelPerformance = table.Column<int>(type: "int", nullable: false),
                    BelowAgeLevelPerformanceObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EarlyChildhoodSkillLevel = table.Column<int>(type: "int", nullable: false),
                    EarlyChildhoodSkillLevelObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ForgetsContent = table.Column<int>(type: "int", nullable: false),
                    ForgetsContentObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttentionDifficulty = table.Column<int>(type: "int", nullable: false),
                    AttentionDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EasilyDistracted = table.Column<int>(type: "int", nullable: false),
                    EasilyDistractedObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NeedsRepeatedExplanations = table.Column<int>(type: "int", nullable: false),
                    NeedsRepeatedExplanationsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FriendshipDifficulty = table.Column<int>(type: "int", nullable: false),
                    FriendshipDifficultyObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EasilyFrustrated = table.Column<int>(type: "int", nullable: false),
                    EasilyFrustratedObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LowSelfEsteem = table.Column<int>(type: "int", nullable: false),
                    LowSelfEsteemObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DependsOnAdults = table.Column<int>(type: "int", nullable: false),
                    DependsOnAdultsObservation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychologistQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychologistQuestionnaires_PsychologistReports_PsychologistReportID",
                        column: x => x.PsychologistReportID,
                        principalTable: "PsychologistReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SnapIVQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychologistReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HasCarelessMistakes = table.Column<int>(type: "int", nullable: false),
                    HasDifficultySustainingAttention = table.Column<int>(type: "int", nullable: false),
                    AppearsNotToListenWhenSpokenToDirectly = table.Column<int>(type: "int", nullable: false),
                    FailsToFollowThroughOnInstructions = table.Column<int>(type: "int", nullable: false),
                    HasDifficultyOrganizingTasksAndActivities = table.Column<int>(type: "int", nullable: false),
                    AvoidsTasksRequiringSustainedMentalEffort = table.Column<int>(type: "int", nullable: false),
                    OftenLosesItemsNecessaryForTasks = table.Column<int>(type: "int", nullable: false),
                    EasilyDistractedByExternalStimuli = table.Column<int>(type: "int", nullable: false),
                    ForgetfulInDailyActivities = table.Column<int>(type: "int", nullable: false),
                    FidgetsWithHandsOrFeet = table.Column<int>(type: "int", nullable: false),
                    LeavesSeatInSituationsRequiringSitting = table.Column<int>(type: "int", nullable: false),
                    RunsOrClimbsInappropriately = table.Column<int>(type: "int", nullable: false),
                    HasDifficultyPlayingQuietly = table.Column<int>(type: "int", nullable: false),
                    OftenOnTheGoOrActsAsDrivenByAMotor = table.Column<int>(type: "int", nullable: false),
                    TalksExcessively = table.Column<int>(type: "int", nullable: false),
                    BlurtsOutAnswers = table.Column<int>(type: "int", nullable: false),
                    HasDifficultyWaitingTurn = table.Column<int>(type: "int", nullable: false),
                    InterruptsOrIntrudesOnOthers = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    DesattentionCount = table.Column<int>(type: "int", nullable: false),
                    HyperactivityCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapIVQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SnapIVQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SnapIVQuestionnaires_PsychologistReports_PsychologistReportID",
                        column: x => x.PsychologistReportID,
                        principalTable: "PsychologistReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CARSQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CARSQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CARSQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CARSQuestionnaires_PsychopedagogueReports_PsychopedagogueReportID",
                        column: x => x.PsychopedagogueReportID,
                        principalTable: "PsychopedagogueReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CognitiveAssessments",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdentifiedBasicColors = table.Column<int>(type: "int", nullable: false),
                    IdentifiedBasicShapes = table.Column<int>(type: "int", nullable: false),
                    CompletesAgeAppropriatePuzzles = table.Column<int>(type: "int", nullable: false),
                    GroupsObjectsBySimilarCharacteristics = table.Column<int>(type: "int", nullable: false),
                    IdentifiesVisualSequencePatterns = table.Column<int>(type: "int", nullable: false),
                    UnderstandsSimpleVerbalInstructions = table.Column<int>(type: "int", nullable: false),
                    NamesCommonEverydayObjects = table.Column<int>(type: "int", nullable: false),
                    FormsAgeAppropriateSentences = table.Column<int>(type: "int", nullable: false),
                    AnswersQuestionsAboutFamilyInformation = table.Column<int>(type: "int", nullable: false),
                    UnderstandsBasicAbstractConcepts = table.Column<int>(type: "int", nullable: false),
                    RemembersTwoToThreeStepInstructions = table.Column<int>(type: "int", nullable: false),
                    MaintainsAttentionForAppropriateDuration = table.Column<int>(type: "int", nullable: false),
                    RecognizesFamiliarPeopleAndPlaces = table.Column<int>(type: "int", nullable: false),
                    RecallsRecentEvents = table.Column<int>(type: "int", nullable: false),
                    RepeatsSimpleSequences = table.Column<int>(type: "int", nullable: false),
                    SeeksSolutionsToSimpleEverydayProblems = table.Column<int>(type: "int", nullable: false),
                    DemonstratesCreativityInPlay = table.Column<int>(type: "int", nullable: false),
                    LearnsFromExperience = table.Column<int>(type: "int", nullable: false),
                    UnderstandsCauseAndEffectRelationships = table.Column<int>(type: "int", nullable: false),
                    AdaptsStrategiesWhenSomethingDoesNotWork = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ScorePerceptualReasoning = table.Column<int>(type: "int", nullable: false),
                    ScoreVerbalReasoning = table.Column<int>(type: "int", nullable: false),
                    ScoreMemoryAndAttention = table.Column<int>(type: "int", nullable: false),
                    ScoreProblemSolving = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    PercentagePerceptualReasoning = table.Column<double>(type: "float", nullable: false),
                    PercentageVerbalReasoning = table.Column<double>(type: "float", nullable: false),
                    PercentageMemoryAndAttention = table.Column<double>(type: "float", nullable: false),
                    PercentageProblemSolving = table.Column<double>(type: "float", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CognitiveAssessments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CognitiveAssessments_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CognitiveAssessments_PsychopedagogueReports_PsychopedagogueReportID",
                        column: x => x.PsychopedagogueReportID,
                        principalTable: "PsychopedagogueReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MCHATQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FollowsPointingGesture = table.Column<int>(type: "int", nullable: false),
                    SuspectedDeafness = table.Column<int>(type: "int", nullable: false),
                    PretendPlay = table.Column<int>(type: "int", nullable: false),
                    ClimbsObjects = table.Column<int>(type: "int", nullable: false),
                    UnusualFingerMovementsNearEyes = table.Column<int>(type: "int", nullable: false),
                    PointsToRequest = table.Column<int>(type: "int", nullable: false),
                    PointsToShowInterest = table.Column<int>(type: "int", nullable: false),
                    InterestedInOtherChildren = table.Column<int>(type: "int", nullable: false),
                    SharesObjectsForAttention = table.Column<int>(type: "int", nullable: false),
                    RespondsToName = table.Column<int>(type: "int", nullable: false),
                    SmilesBack = table.Column<int>(type: "int", nullable: false),
                    SensitiveToNoise = table.Column<int>(type: "int", nullable: false),
                    CanWalk = table.Column<int>(type: "int", nullable: false),
                    MaintainsEyeContact = table.Column<int>(type: "int", nullable: false),
                    ImitatesActions = table.Column<int>(type: "int", nullable: false),
                    FollowsGaze = table.Column<int>(type: "int", nullable: false),
                    SeeksAttention = table.Column<int>(type: "int", nullable: false),
                    UnderstandsVerbalInstructions = table.Column<int>(type: "int", nullable: false),
                    ChecksFacialReactionToEvents = table.Column<int>(type: "int", nullable: false),
                    EnjoysMovementActivities = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MCHATQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MCHATQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MCHATQuestionnaires_PsychopedagogueReports_PsychopedagogueReportID",
                        column: x => x.PsychopedagogueReportID,
                        principalTable: "PsychopedagogueReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PsychopedagogueQuestionnaires",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComportamentalObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotebookObservations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherObservations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherTypesOfDisability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JustificationForClassification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherRecommendations = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychopedagogueQuestionnaires", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueQuestionnaires_BaseDocuments_ID",
                        column: x => x.ID,
                        principalTable: "BaseDocuments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueQuestionnaires_PsychopedagogueReports_PsychopedagogueReportID",
                        column: x => x.PsychopedagogueReportID,
                        principalTable: "PsychopedagogueReports",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_FamilyIncomeSources",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_FamilyIncomeSources", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_FamilyIncomeSources_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_GovernmentBenefits",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Benefit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_GovernmentBenefits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_GovernmentBenefits_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_HealthConditions",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_HealthConditions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_HealthConditions_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_MainSchoolDifficulties",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_MainSchoolDifficulties", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_MainSchoolDifficulties_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_Procedures",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Procedure = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_Procedures", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_Procedures_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialWorkerReport_Recommendations",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocialWorkerReportID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recommendation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialWorkerReport_Recommendations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialWorkerReport_Recommendations_SocialWorkerReports_SocialWorkerReportID",
                        column: x => x.SocialWorkerReportID,
                        principalTable: "SocialWorkerReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsychopedagogueQuestionnaire_Recommendations",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueQuestionnaireID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recommendation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychopedagogueQuestionnaire_Recommendations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueQuestionnaire_Recommendations_PsychopedagogueQuestionnaires_PsychopedagogueQuestionnaireID",
                        column: x => x.PsychopedagogueQuestionnaireID,
                        principalTable: "PsychopedagogueQuestionnaires",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsychopedagogueQuestionnaire_TypeOfDisabilities",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueQuestionnaireID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Disability = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychopedagogueQuestionnaire_TypeOfDisabilities", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueQuestionnaire_TypeOfDisabilities_PsychopedagogueQuestionnaires_PsychopedagogueQuestionnaireID",
                        column: x => x.PsychopedagogueQuestionnaireID,
                        principalTable: "PsychopedagogueQuestionnaires",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PsychopedagogueQuestionnaire_TypeOfDisorders",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PsychopedagogueQuestionnaireID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Disorder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychopedagogueQuestionnaire_TypeOfDisorders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PsychopedagogueQuestionnaire_TypeOfDisorders_PsychopedagogueQuestionnaires_PsychopedagogueQuestionnaireID",
                        column: x => x.PsychopedagogueQuestionnaireID,
                        principalTable: "PsychopedagogueQuestionnaires",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionURL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrincipalID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sex = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Serie = table.Column<int>(type: "int", nullable: false),
                    NameOfPersonResponsible = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaritalStatusParents = table.Column<int>(type: "int", nullable: false),
                    SkinColor = table.Column<int>(type: "int", nullable: false),
                    SUSCard = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RG = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CPF = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SchoolID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistrationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Students_Schools_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "Schools",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sex = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Schools_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "Schools",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptiveBehaviorScalesDI_PsychologistReportID",
                table: "AdaptiveBehaviorScalesDI",
                column: "PsychologistReportID",
                unique: true,
                filter: "[PsychologistReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_StudentID",
                table: "Attachments",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_BaseDocuments_StudentID",
                table: "BaseDocuments",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_BaseDocuments_UserID",
                table: "BaseDocuments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_CARSQuestionnaires_PsychopedagogueReportID",
                table: "CARSQuestionnaires",
                column: "PsychopedagogueReportID",
                unique: true,
                filter: "[PsychopedagogueReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CognitiveAssessments_PsychopedagogueReportID",
                table: "CognitiveAssessments",
                column: "PsychopedagogueReportID",
                unique: true,
                filter: "[PsychopedagogueReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MCHATQuestionnaires_PsychopedagogueReportID",
                table: "MCHATQuestionnaires",
                column: "PsychopedagogueReportID",
                unique: true,
                filter: "[PsychopedagogueReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityNeckBookConfigs_State_City",
                table: "MunicipalityNeckBookConfigs",
                columns: new[] { "State", "City" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityStudentTargets_State_City",
                table: "MunicipalityStudentTargets",
                columns: new[] { "State", "City" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_ActivitiesAEE_PAEEID",
                table: "PAEE_ActivitiesAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_EvaluationCriterionAEE_PAEEID",
                table: "PAEE_EvaluationCriterionAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_MaterialResourcesAEE_PAEEID",
                table: "PAEE_MaterialResourcesAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_ProfessionalsAEE_PAEEID",
                table: "PAEE_ProfessionalsAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_ResourcesAndStrategiesAEE_PAEEID",
                table: "PAEE_ResourcesAndStrategiesAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_TypeAEE_PAEEID",
                table: "PAEE_TypeAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEE_WorkMethodologyAEE_PAEEID",
                table: "PAEE_WorkMethodologyAEE",
                column: "PAEEID");

            migrationBuilder.CreateIndex(
                name: "IX_PAEEs_PortageID",
                table: "PAEEs",
                column: "PortageID",
                unique: true,
                filter: "[PortageID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PEI_Responses_PEISubjectID_Key",
                table: "PEI_Responses",
                columns: new[] { "PEISubjectID", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PEI_Subjects_PEIID_Subject",
                table: "PEI_Subjects",
                columns: new[] { "PEIID", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortageGuideResponses_PortageGuideID",
                table: "PortageGuideResponses",
                column: "PortageGuideID");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhookEvents_Provider_EnvelopeOrDocumentToken_EventState",
                table: "ProcessedWebhookEvents",
                columns: new[] { "Provider", "EnvelopeOrDocumentToken", "EventState" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistQuestionnaires_PsychologistReportID",
                table: "PsychologistQuestionnaires",
                column: "PsychologistReportID",
                unique: true,
                filter: "[PsychologistReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistReports_ReportID",
                table: "PsychologistReports",
                column: "ReportID",
                unique: true,
                filter: "[ReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PsychopedagogueQuestionnaire_Recommendations_PsychopedagogueQuestionnaireID_Recommendation",
                table: "PsychopedagogueQuestionnaire_Recommendations",
                columns: new[] { "PsychopedagogueQuestionnaireID", "Recommendation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychopedagogueQuestionnaire_TypeOfDisabilities_PsychopedagogueQuestionnaireID_Disability",
                table: "PsychopedagogueQuestionnaire_TypeOfDisabilities",
                columns: new[] { "PsychopedagogueQuestionnaireID", "Disability" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychopedagogueQuestionnaire_TypeOfDisorders_PsychopedagogueQuestionnaireID_Disorder",
                table: "PsychopedagogueQuestionnaire_TypeOfDisorders",
                columns: new[] { "PsychopedagogueQuestionnaireID", "Disorder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychopedagogueQuestionnaires_PsychopedagogueReportID",
                table: "PsychopedagogueQuestionnaires",
                column: "PsychopedagogueReportID",
                unique: true,
                filter: "[PsychopedagogueReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PsychopedagogueReports_ReportID",
                table: "PsychopedagogueReports",
                column: "ReportID",
                unique: true,
                filter: "[ReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegularTeacherEvaluationDocuments_RegularTeacherQuestionnaireID",
                table: "RegularTeacherEvaluationDocuments",
                column: "RegularTeacherQuestionnaireID",
                unique: true,
                filter: "[RegularTeacherQuestionnaireID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegularTeacherEvaluationDocuments_RegularTeacherScreeningID",
                table: "RegularTeacherEvaluationDocuments",
                column: "RegularTeacherScreeningID",
                unique: true,
                filter: "[RegularTeacherScreeningID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegularTeacherQuestionnaire_BehaviourTraits_RegularTeacherQuestionnaireID_BehaviorTrait",
                table: "RegularTeacherQuestionnaire_BehaviourTraits",
                columns: new[] { "RegularTeacherQuestionnaireID", "BehaviorTrait" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_IsActive",
                table: "Schools",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_PrincipalID",
                table: "Schools",
                column: "PrincipalID",
                unique: true,
                filter: "[PrincipalID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SnapIVQuestionnaires_PsychologistReportID",
                table: "SnapIVQuestionnaires",
                column: "PsychologistReportID",
                unique: true,
                filter: "[PsychologistReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_FamilyIncomeSources_SocialWorkerReportID_Source",
                table: "SocialWorkerReport_FamilyIncomeSources",
                columns: new[] { "SocialWorkerReportID", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_GovernmentBenefits_SocialWorkerReportID_Benefit",
                table: "SocialWorkerReport_GovernmentBenefits",
                columns: new[] { "SocialWorkerReportID", "Benefit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_HealthConditions_SocialWorkerReportID_Condition",
                table: "SocialWorkerReport_HealthConditions",
                columns: new[] { "SocialWorkerReportID", "Condition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_MainSchoolDifficulties_SocialWorkerReportID_Difficulty",
                table: "SocialWorkerReport_MainSchoolDifficulties",
                columns: new[] { "SocialWorkerReportID", "Difficulty" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_Procedures_SocialWorkerReportID_Procedure",
                table: "SocialWorkerReport_Procedures",
                columns: new[] { "SocialWorkerReportID", "Procedure" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReport_Recommendations_SocialWorkerReportID_Recommendation",
                table: "SocialWorkerReport_Recommendations",
                columns: new[] { "SocialWorkerReportID", "Recommendation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialWorkerReports_ReportID",
                table: "SocialWorkerReports",
                column: "ReportID",
                unique: true,
                filter: "[ReportID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CPF",
                table: "Students",
                column: "CPF",
                unique: true,
                filter: "[CPF] IS NOT NULL AND [CPF] <> '' AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_IsActive",
                table: "Students",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Students_RG",
                table: "Students",
                column: "RG",
                unique: true,
                filter: "[RG] IS NOT NULL AND [RG] <> '' AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolID",
                table: "Students",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SUSCard",
                table: "Students",
                column: "SUSCard",
                unique: true,
                filter: "[SUSCard] IS NOT NULL AND [SUSCard] <> '' AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Key_IsActive",
                table: "Terms",
                columns: new[] { "Key", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Key_Version",
                table: "Terms",
                columns: new[] { "Key", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SchoolID",
                table: "Users",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserTermAcceptances_TermId",
                table: "UserTermAcceptances",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTermAcceptances_UserId_TermId",
                table: "UserTermAcceptances",
                columns: new[] { "UserId", "TermId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptiveBehaviorScalesDI_BaseDocuments_ID",
                table: "AdaptiveBehaviorScalesDI",
                column: "ID",
                principalTable: "BaseDocuments",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptiveBehaviorScalesDI_PsychologistReports_PsychologistReportID",
                table: "AdaptiveBehaviorScalesDI",
                column: "PsychologistReportID",
                principalTable: "PsychologistReports",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Students_StudentID",
                table: "Attachments",
                column: "StudentID",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseDocuments_Students_StudentID",
                table: "BaseDocuments",
                column: "StudentID",
                principalTable: "Students",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseDocuments_Users_UserID",
                table: "BaseDocuments",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserID",
                table: "Notifications",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Schools_Users_PrincipalID",
                table: "Schools",
                column: "PrincipalID",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schools_Users_PrincipalID",
                table: "Schools");

            migrationBuilder.DropTable(
                name: "AdaptiveBehaviorScalesDI");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "CARSQuestionnaires");

            migrationBuilder.DropTable(
                name: "CognitiveAssessments");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "MCHATQuestionnaires");

            migrationBuilder.DropTable(
                name: "MunicipalityNeckBookConfigs");

            migrationBuilder.DropTable(
                name: "MunicipalityStudentTargets");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PAEE_ActivitiesAEE");

            migrationBuilder.DropTable(
                name: "PAEE_EvaluationCriterionAEE");

            migrationBuilder.DropTable(
                name: "PAEE_MaterialResourcesAEE");

            migrationBuilder.DropTable(
                name: "PAEE_ProfessionalsAEE");

            migrationBuilder.DropTable(
                name: "PAEE_ResourcesAndStrategiesAEE");

            migrationBuilder.DropTable(
                name: "PAEE_TypeAEE");

            migrationBuilder.DropTable(
                name: "PAEE_WorkMethodologyAEE");

            migrationBuilder.DropTable(
                name: "PEI_Responses");

            migrationBuilder.DropTable(
                name: "PortageGuideResponses");

            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvents");

            migrationBuilder.DropTable(
                name: "PsychologistQuestionnaires");

            migrationBuilder.DropTable(
                name: "PsychopedagogueQuestionnaire_Recommendations");

            migrationBuilder.DropTable(
                name: "PsychopedagogueQuestionnaire_TypeOfDisabilities");

            migrationBuilder.DropTable(
                name: "PsychopedagogueQuestionnaire_TypeOfDisorders");

            migrationBuilder.DropTable(
                name: "RegularTeacherEvaluationDocuments");

            migrationBuilder.DropTable(
                name: "RegularTeacherQuestionnaire_BehaviourTraits");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SnapIVQuestionnaires");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_FamilyIncomeSources");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_GovernmentBenefits");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_HealthConditions");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_MainSchoolDifficulties");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_Procedures");

            migrationBuilder.DropTable(
                name: "SocialWorkerReport_Recommendations");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTermAcceptances");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "PAEEs");

            migrationBuilder.DropTable(
                name: "PEI_Subjects");

            migrationBuilder.DropTable(
                name: "PsychopedagogueQuestionnaires");

            migrationBuilder.DropTable(
                name: "RegularTeacherScreenings");

            migrationBuilder.DropTable(
                name: "RegularTeacherQuestionnaires");

            migrationBuilder.DropTable(
                name: "PsychologistReports");

            migrationBuilder.DropTable(
                name: "SocialWorkerReports");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Terms");

            migrationBuilder.DropTable(
                name: "PortageGuides");

            migrationBuilder.DropTable(
                name: "PEIs");

            migrationBuilder.DropTable(
                name: "PsychopedagogueReports");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "BaseDocuments");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Schools");
        }
    }
}
