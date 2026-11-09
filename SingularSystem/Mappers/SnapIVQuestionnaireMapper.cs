using SingularSystem.DTOs.PsychologistReports.SnapIV;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class SnapIVQuestionnaireMapper
    {
        private static bool HasAtLeastSixHighScores(IEnumerable<SnapIV?> responses)
        {
            return responses.Count(r => r == SnapIV.QuiteALot || r == SnapIV.TooMuch) >= 6;
        }

        private static int CountHighScores(IEnumerable<SnapIV?> responses)
        {
            return responses.Count(r =>
                r == SnapIV.QuiteALot ||
                r == SnapIV.TooMuch
            );
        }

        private static (bool score, SnapIVResult result, int desattentionCount, int hyperactiveCount) CalculateScore(
            IEnumerable<SnapIV?> desattentionResponses,
            IEnumerable<SnapIV?> hyperactiveResponses
        )
        {
            int desattentionCount = CountHighScores(desattentionResponses);
            int hyperactiveCount = CountHighScores(hyperactiveResponses);

            bool scoreDesattention = desattentionCount >= 6;
            bool scoreHyperactive = hyperactiveCount >= 6;

            SnapIVResult result =
                scoreDesattention && scoreHyperactive
                    ? SnapIVResult.Combined
                    : scoreDesattention
                        ? SnapIVResult.Inattentive
                        : scoreHyperactive
                            ? SnapIVResult.HyperactiveImpulsive
                            : SnapIVResult.None;

            return (
                scoreDesattention || scoreHyperactive,
                result,
                desattentionCount,
                hyperactiveCount
            );
        }

        private static (IEnumerable<SnapIV?> desattention, IEnumerable<SnapIV?> hyperactive)
            ExtractResponses(SnapIVQuestionnaire entity)
        {
            var desattentionResponses = new List<SnapIV?>
            {
                entity.HasCarelessMistakes,
                entity.HasDifficultySustainingAttention,
                entity.AppearsNotToListenWhenSpokenToDirectly,
                entity.FailsToFollowThroughOnInstructions,
                entity.HasDifficultyOrganizingTasksAndActivities,
                entity.AvoidsTasksRequiringSustainedMentalEffort,
                entity.OftenLosesItemsNecessaryForTasks,
                entity.EasilyDistractedByExternalStimuli,
                entity.ForgetfulInDailyActivities
            };

            var hyperactiveResponses = new List<SnapIV?>
            {
                entity.FidgetsWithHandsOrFeet,
                entity.LeavesSeatInSituationsRequiringSitting,
                entity.RunsOrClimbsInappropriately,
                entity.HasDifficultyPlayingQuietly,
                entity.OftenOnTheGoOrActsAsDrivenByAMotor,
                entity.TalksExcessively,
                entity.BlurtsOutAnswers,
                entity.HasDifficultyWaitingTurn,
                entity.InterruptsOrIntrudesOnOthers
            };

            return (desattentionResponses, hyperactiveResponses);
        }

        public static SnapIVQuestionnaire ToEntity(
            SnapIVQuestionnaireRequest model,
            string userId
        )
        {
            var entity = new SnapIVQuestionnaire()
            {
                StudentID = model.StudentID,
                UserID = userId,
                PsychologistReportID = model.PsychologistReportID,
                HasCarelessMistakes = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasCarelessMistakes),
                HasDifficultySustainingAttention = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultySustainingAttention),
                AppearsNotToListenWhenSpokenToDirectly = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.AppearsNotToListenWhenSpokenToDirectly),
                FailsToFollowThroughOnInstructions = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.FailsToFollowThroughOnInstructions),
                HasDifficultyOrganizingTasksAndActivities = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyOrganizingTasksAndActivities),
                AvoidsTasksRequiringSustainedMentalEffort = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.AvoidsTasksRequiringSustainedMentalEffort),
                OftenLosesItemsNecessaryForTasks = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.OftenLosesItemsNecessaryForTasks),
                EasilyDistractedByExternalStimuli = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.EasilyDistractedByExternalStimuli),
                ForgetfulInDailyActivities = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.ForgetfulInDailyActivities),
                FidgetsWithHandsOrFeet = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.FidgetsWithHandsOrFeet),
                LeavesSeatInSituationsRequiringSitting = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.LeavesSeatInSituationsRequiringSitting),
                RunsOrClimbsInappropriately = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.RunsOrClimbsInappropriately),
                HasDifficultyPlayingQuietly = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyPlayingQuietly),
                OftenOnTheGoOrActsAsDrivenByAMotor = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.OftenOnTheGoOrActsAsDrivenByAMotor),
                TalksExcessively = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.TalksExcessively),
                BlurtsOutAnswers = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.BlurtsOutAnswers),
                HasDifficultyWaitingTurn = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyWaitingTurn),
                InterruptsOrIntrudesOnOthers = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.InterruptsOrIntrudesOnOthers),
            };

            var (desattention, hyperactive) = ExtractResponses(entity);
            var (score, result, desattentionCount, hyperactiveCount) = CalculateScore(desattention, hyperactive);

            entity.Score = score;
            entity.Result = result;
            entity.DesattentionCount = desattentionCount;
            entity.HyperactivityCount = hyperactiveCount;

            return entity;
        }

        public static SnapIVQuestionnaireResponse? ToResponse(
            SnapIVQuestionnaire entity, UserSummaryResponse? createdBy
        )
        {
            if (entity == null) return null;

            return new SnapIVQuestionnaireResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychologistReportID = entity.PsychologistReportID,
                HasCarelessMistakes = entity.HasCarelessMistakes.GetDisplayName(),
                HasDifficultySustainingAttention = entity.HasDifficultySustainingAttention.GetDisplayName(),
                AppearsNotToListenWhenSpokenToDirectly = entity.AppearsNotToListenWhenSpokenToDirectly.GetDisplayName(),
                FailsToFollowThroughOnInstructions = entity.FailsToFollowThroughOnInstructions.GetDisplayName(),
                HasDifficultyOrganizingTasksAndActivities = entity.HasDifficultyOrganizingTasksAndActivities.GetDisplayName(),
                AvoidsTasksRequiringSustainedMentalEffort = entity.AvoidsTasksRequiringSustainedMentalEffort.GetDisplayName(),
                OftenLosesItemsNecessaryForTasks = entity.OftenLosesItemsNecessaryForTasks.GetDisplayName(),
                EasilyDistractedByExternalStimuli = entity.EasilyDistractedByExternalStimuli.GetDisplayName(),
                ForgetfulInDailyActivities = entity.ForgetfulInDailyActivities.GetDisplayName(),
                FidgetsWithHandsOrFeet = entity.FidgetsWithHandsOrFeet.GetDisplayName(),
                LeavesSeatInSituationsRequiringSitting = entity.LeavesSeatInSituationsRequiringSitting.GetDisplayName(),
                RunsOrClimbsInappropriately = entity.RunsOrClimbsInappropriately.GetDisplayName(),
                HasDifficultyPlayingQuietly = entity.HasDifficultyPlayingQuietly.GetDisplayName(),
                OftenOnTheGoOrActsAsDrivenByAMotor = entity.OftenOnTheGoOrActsAsDrivenByAMotor.GetDisplayName(),
                TalksExcessively = entity.TalksExcessively.GetDisplayName(),
                BlurtsOutAnswers = entity.BlurtsOutAnswers.GetDisplayName(),
                HasDifficultyWaitingTurn = entity.HasDifficultyWaitingTurn.GetDisplayName(),
                InterruptsOrIntrudesOnOthers = entity.InterruptsOrIntrudesOnOthers.GetDisplayName(),
                Score = entity.Score ? "Positivo" : "Negativo",
                Result = entity.Result.GetDisplayName(),
                HyperactivityCount = entity.HyperactivityCount,
                DesattentionCount = entity.DesattentionCount,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            SnapIVQuestionnaire entity,
            SnapIVQuestionnaireUpdateRequest model
        )
        {
            entity.HasCarelessMistakes = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasCarelessMistakes);
            entity.HasDifficultySustainingAttention = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultySustainingAttention);
            entity.AppearsNotToListenWhenSpokenToDirectly = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.AppearsNotToListenWhenSpokenToDirectly);
            entity.FailsToFollowThroughOnInstructions = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.FailsToFollowThroughOnInstructions);
            entity.HasDifficultyOrganizingTasksAndActivities = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyOrganizingTasksAndActivities);
            entity.AvoidsTasksRequiringSustainedMentalEffort = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.AvoidsTasksRequiringSustainedMentalEffort);
            entity.OftenLosesItemsNecessaryForTasks = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.OftenLosesItemsNecessaryForTasks);
            entity.EasilyDistractedByExternalStimuli = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.EasilyDistractedByExternalStimuli);
            entity.ForgetfulInDailyActivities = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.ForgetfulInDailyActivities);
            entity.FidgetsWithHandsOrFeet = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.FidgetsWithHandsOrFeet);
            entity.LeavesSeatInSituationsRequiringSitting = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.LeavesSeatInSituationsRequiringSitting);
            entity.RunsOrClimbsInappropriately = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.RunsOrClimbsInappropriately);
            entity.HasDifficultyPlayingQuietly = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyPlayingQuietly);
            entity.OftenOnTheGoOrActsAsDrivenByAMotor = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.OftenOnTheGoOrActsAsDrivenByAMotor);
            entity.TalksExcessively = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.TalksExcessively);
            entity.BlurtsOutAnswers = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.BlurtsOutAnswers);
            entity.HasDifficultyWaitingTurn = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.HasDifficultyWaitingTurn);
            entity.InterruptsOrIntrudesOnOthers = Helpers.GetEnumValueFromDisplayName<SnapIV>(model.InterruptsOrIntrudesOnOthers);

            var (desattention, hyperactive) = ExtractResponses(entity);
            var (score, result, desattentionCount, hyperactiveCount) = CalculateScore(desattention, hyperactive);

            entity.Score = score;
            entity.Result = result;
            entity.DesattentionCount = desattentionCount;
            entity.HyperactivityCount = hyperactiveCount;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
