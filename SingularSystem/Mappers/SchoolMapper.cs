using Amazon.Auth.AccessControlPolicy;
using SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires;
using SingularSystem.DTOs.Schools;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.Data;

namespace SingularSystem.Mappers
{
    public static class SchoolMapper
    {
        public static School ToEntity(
            SchoolRequest model
        )
        {
            return new School()
            {
                Name = model.Name,
                City = Helpers.NormalizeCity(model.City),
                State = Helpers.NormalizeState(model.State),
                PrincipalID = string.IsNullOrWhiteSpace(model.PrincipalID) ? null : model.PrincipalID,
            };
        }

        public static SchoolResponse ToResponse(
            School entity
        )
        {
            return new SchoolResponse()
            {
                ID = entity.ID,
                Name = entity.Name,
                City = Helpers.NormalizeCity(entity.City),
                State = Helpers.NormalizeState(entity.State),
                PrincipalID = entity.PrincipalID,
                PrincipalName = entity?.Principal?.FullName,
                CreatedAt = entity?.CreatedAt,
                UpdatedAt = entity?.UpdatedAt,
                IsActive = entity?.IsActive,
                TotalStudents = entity?.Students?.Count,
                Disabilities = new List<DisabilityResponse>(),
                TotalStudentsAEE = entity?.Students.Where(temp => temp.RegistrationType == "AEE").Count(),
            };
        }

        public static void UpdateEntity(
            School entity,
            SchoolUpdateRequest model
        )
        {
            entity.Name = model.Name;
            entity.PrincipalID = model.PrincipalID != "" ? model.PrincipalID : null;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
