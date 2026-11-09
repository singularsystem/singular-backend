using SingularSystem.DTOs.PsychopedagogueReports.PsychopedagogueQuestionnaires;
using SingularSystem.DTOs.Users;
using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using System.Data;
using System.Globalization;

namespace SingularSystem.Mappers
{
    public static class ApplicationUserMapper
    {
        public static ApplicationUser ToEntity(
            UserRequest model,
            string userName
        )
        {
            return new ApplicationUser()
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = userName,
                Sex = Helpers.GetEnumValueFromDisplayName<Sex>(model.Sex),
                DateOfBirth = model.DateOfBirth,
                Address = model.Address,
                State = Helpers.NormalizeState(model.State),
                PhoneNumber = model.PhoneNumber,
                SchoolID = model.SchoolID != "" && model.SchoolID != null ? Guid.Parse(model.SchoolID) : null,
                City = Helpers.NormalizeCity(model.City),
                EmailConfirmed = true
            };
        }

        public static UserResponse ToResponse(
            ApplicationUser entity, IEnumerable<string> roles
        )
        {
            return new UserResponse
            {
                ID = entity.Id,
                FullName = entity.FullName,
                UserName = entity.UserName ?? "",
                Email = entity.Email ?? "",
                City = Helpers.NormalizeCity(entity.City),
                Sex = entity.Sex.GetDisplayName(),
                State = Helpers.NormalizeState(entity.State),
                PhoneNumber = entity.PhoneNumber,
                DateOfBirth = entity.DateOfBirth,
                Address = entity.Address,
                SchoolID = entity.SchoolID,
                SchoolName = entity.School?.Name ?? "",
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                MustChangePassword = entity.MustChangePassword,
                LastLogin = entity.LastLogin,
                Status = entity.IsActive ? "Ativo" : "Inativo",
                Roles = [.. roles]
            };
        }

        public static UserSummaryResponse ToSummaryResponse(
            ApplicationUser entity, IEnumerable<string> roles
        )
        {
            return new UserSummaryResponse
            {
                ID = entity.Id,
                FullName = entity.FullName,
                City = Helpers.NormalizeCity(entity.City),
                State = Helpers.NormalizeState(entity.State),
                SchoolID = entity.SchoolID,
                SchoolName = entity.School?.Name ?? "",
                Roles = [.. roles]
            };
        }

        public static void UpdateEntity(
            ApplicationUser entity,
            UserUpdateRequest model
        )
        {
            entity.Email = model.Email;
            entity.FullName = model.FullName;
            entity.Address = model.Address;
            entity.City = Helpers.NormalizeCity(model.City);
            entity.State = Helpers.NormalizeState(model.State);
            entity.PhoneNumber = model.PhoneNumber;
            entity.SchoolID = string.IsNullOrWhiteSpace(model.SchoolID)
                ? null
                : Guid.Parse(model.SchoolID);
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
