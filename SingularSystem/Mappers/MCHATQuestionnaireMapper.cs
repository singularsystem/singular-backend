using SingularSystem.DTOs.PsychopedagogueReports.MCHAT;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class MCHATQuestionnaireMapper
    {
        private record MChatAnswer(Evaluation Response, bool IsSpecial);

        private static (int score, MCHATResult result) CalculateScore(
            IEnumerable<MChatAnswer> responses
        )
        {
            int score = responses.Count(r =>
                r.IsSpecial
                    ? r.Response == Evaluation.Yes
                    : r.Response == Evaluation.Not
            );

            MCHATResult result =
                score <= 2 ? MCHATResult.LowRisk :
                score <= 7 ? MCHATResult.ModerateRisk :
                MCHATResult.HighRisk;

            return (score, result);
        }

        private static IEnumerable<MChatAnswer> ExtractResponses(MCHATQuestionnaire entity)
        {

            return new List<MChatAnswer>
            {
                new(entity.FollowsPointingGesture, false),
                new(entity.SuspectedDeafness, true),
                new(entity.PretendPlay, false),
                new(entity.ClimbsObjects, false),
                new(entity.UnusualFingerMovementsNearEyes, true),
                new(entity.PointsToRequest, false),
                new(entity.PointsToShowInterest, false),
                new(entity.InterestedInOtherChildren, false),
                new(entity.SharesObjectsForAttention, false),
                new(entity.RespondsToName, false),
                new(entity.SmilesBack, false),
                new(entity.SensitiveToNoise, false),
                new(entity.CanWalk, false),
                new(entity.MaintainsEyeContact, false),
                new(entity.ImitatesActions, false),
                new(entity.FollowsGaze, false),
                new(entity.SeeksAttention, false),
                new(entity.UnderstandsVerbalInstructions, false),
                new(entity.ChecksFacialReactionToEvents, false),
                new(entity.EnjoysMovementActivities, false),
            };
        }

        public static MCHATQuestionnaire ToEntity(
            MCHATQuestionnaireRequest model,
            string userId
        )
        {
            var entity = new MCHATQuestionnaire()
            {
                UserID = userId,
                StudentID = model.StudentID,
                PsychopedagogueReportID = model.PsychopedagogueReportID,
                FollowsPointingGesture = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FollowsPointingGesture),
                SuspectedDeafness = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SuspectedDeafness),
                PretendPlay = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PretendPlay),
                ClimbsObjects = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ClimbsObjects),
                UnusualFingerMovementsNearEyes = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnusualFingerMovementsNearEyes),
                PointsToRequest = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PointsToRequest),
                PointsToShowInterest = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PointsToShowInterest),
                InterestedInOtherChildren = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.InterestedInOtherChildren),
                SharesObjectsForAttention = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SharesObjectsForAttention),
                RespondsToName = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RespondsToName),
                SmilesBack = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SmilesBack),
                SensitiveToNoise = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SensitiveToNoise),
                CanWalk = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CanWalk),
                MaintainsEyeContact = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.MaintainsEyeContact),
                ImitatesActions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ImitatesActions),
                FollowsGaze = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FollowsGaze),
                SeeksAttention = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SeeksAttention),
                UnderstandsVerbalInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsVerbalInstructions),
                ChecksFacialReactionToEvents = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ChecksFacialReactionToEvents),
                EnjoysMovementActivities = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EnjoysMovementActivities),
            };

            var responses = ExtractResponses(entity);
            var (score, result) = CalculateScore(responses);
            
            entity.Score = score;
            entity.Result = result;

            return entity;
        }

        public static MCHATQuestionnaireResponse? ToResponse(
            MCHATQuestionnaire entity, UserSummaryResponse? createdBy
        )
        {
            if (entity == null) return null;

            return new MCHATQuestionnaireResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                PsychopedagogueReportID = entity.PsychopedagogueReportID,
                FollowsPointingGesture = entity.FollowsPointingGesture.GetDisplayName(),
                SuspectedDeafness = entity.SuspectedDeafness.GetDisplayName(),
                PretendPlay = entity.PretendPlay.GetDisplayName(),
                ClimbsObjects = entity.ClimbsObjects.GetDisplayName(),
                UnusualFingerMovementsNearEyes = entity.UnusualFingerMovementsNearEyes.GetDisplayName(),
                PointsToRequest = entity.PointsToRequest.GetDisplayName(),
                PointsToShowInterest = entity.PointsToShowInterest.GetDisplayName(),
                InterestedInOtherChildren = entity.InterestedInOtherChildren.GetDisplayName(),
                SharesObjectsForAttention = entity.SharesObjectsForAttention.GetDisplayName(),
                RespondsToName = entity.RespondsToName.GetDisplayName(),
                SmilesBack = entity.SmilesBack.GetDisplayName(),
                SensitiveToNoise = entity.SensitiveToNoise.GetDisplayName(),
                CanWalk = entity.CanWalk.GetDisplayName(),
                MaintainsEyeContact = entity.MaintainsEyeContact.GetDisplayName(),
                ImitatesActions = entity.ImitatesActions.GetDisplayName(),
                FollowsGaze = entity.FollowsGaze.GetDisplayName(),
                SeeksAttention = entity.SeeksAttention.GetDisplayName(),
                UnderstandsVerbalInstructions = entity.UnderstandsVerbalInstructions.GetDisplayName(),
                ChecksFacialReactionToEvents = entity.ChecksFacialReactionToEvents.GetDisplayName(),
                EnjoysMovementActivities = entity.EnjoysMovementActivities.GetDisplayName(),
                Score = entity.Score,
                Result = entity.Result.GetDisplayName(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            MCHATQuestionnaire entity,
            MCHATQuestionnaireUpdateRequest model
        )
        {
            entity.FollowsPointingGesture = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FollowsPointingGesture);
            entity.SuspectedDeafness = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SuspectedDeafness);
            entity.PretendPlay = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PretendPlay);
            entity.ClimbsObjects = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ClimbsObjects);
            entity.UnusualFingerMovementsNearEyes = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnusualFingerMovementsNearEyes);
            entity.PointsToRequest = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PointsToRequest);
            entity.PointsToShowInterest = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.PointsToShowInterest);
            entity.InterestedInOtherChildren = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.InterestedInOtherChildren);
            entity.SharesObjectsForAttention = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SharesObjectsForAttention);
            entity.RespondsToName = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.RespondsToName);
            entity.SmilesBack = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SmilesBack);
            entity.SensitiveToNoise = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SensitiveToNoise);
            entity.CanWalk = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.CanWalk);
            entity.MaintainsEyeContact = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.MaintainsEyeContact);
            entity.ImitatesActions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ImitatesActions);
            entity.FollowsGaze = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.FollowsGaze);
            entity.SeeksAttention = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.SeeksAttention);
            entity.UnderstandsVerbalInstructions = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.UnderstandsVerbalInstructions);
            entity.ChecksFacialReactionToEvents = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.ChecksFacialReactionToEvents);
            entity.EnjoysMovementActivities = Helpers.GetEnumValueFromDisplayName<Evaluation>(model.EnjoysMovementActivities);

            var responses = ExtractResponses(entity);
            var (score, result) = CalculateScore(responses);

            entity.Score = score;
            entity.Result = result;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
