using SingularSystem.DTOs.Portage;
using SingularSystem.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SingularSystem.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue
                .GetType()
                .GetMember(enumValue.ToString())[0]
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName() ?? enumValue.ToString();
        }

        public static Dictionary<string, PortageValueResponse> ToResponseDictionary(
            this ICollection<PortageGuideResponse> responses)
        {
            return responses?
                .ToDictionary(
                    r => r.Key,
                    r => new PortageValueResponse()
                    {
                        Response = r.Value.Response.GetDisplayName() ?? "",
                        Category = r.Value.Category
                    })
                ?? new Dictionary<string, PortageValueResponse>();
        }
    }
}
