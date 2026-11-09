using SingularSystem.DTOs.Portage;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;

namespace SingularSystem.Mappers
{
    public static class PortageGuideMapper
    {
        public static PortageGuide ToEntity(
            PortageRequest model,
            string userId
        )
        {
            return new PortageGuide()
            {
                StudentID = model.StudentID,
                UserID = userId,
                PortageAgeRange = Helpers.GetEnumValueFromDisplayName<AgeRange>(model.AgeRange),
                Responses = model.Responses.Select(r => new PortageGuideResponse
                {
                    Key = r.Key,
                    Value = new PortageGuideValue()
                    {
                        Response = Helpers.GetEnumValueFromDisplayName<PlanResponse>(r.Value.Response),
                        Category = r.Value.Category
                    }
                }).ToList()
            };
        }

        public static PortageResponse ToResponse(PortageGuide entity, UserSummaryResponse? createdBy)
        {
            return new PortageResponse()
            {
                ID = entity.ID,
                Professional = createdBy,
                Student = entity.Student != null ? StudentMapper.ToSummaryResponse(entity.Student) : null,
                AgeRange = Enum.IsDefined(typeof(AgeRange), entity.PortageAgeRange)
            ? entity.PortageAgeRange.GetDisplayName()
            : "Nenhum(a)",
                Responses = entity.Responses.ToResponseDictionary(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        public static void UpdateEntity(
            PortageGuide entity,
            PortageUpdateRequest model
        )
        {
            entity.PortageAgeRange = Helpers.GetEnumValueFromDisplayName<AgeRange>(model.AgeRange);
            entity.Responses = model.Responses.Select(r => new Models.Entities.PortageGuideResponse
            {
                Key = r.Key,
                Value = new PortageGuideValue()
                {
                    Response = Helpers.GetEnumValueFromDisplayName<PlanResponse>(r.Value.Response),
                    Category = r.Value.Category
                }
            }).ToList();
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
