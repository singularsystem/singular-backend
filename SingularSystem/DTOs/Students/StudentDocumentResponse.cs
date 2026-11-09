using SingularSystem.Extensions;
using System.ComponentModel.DataAnnotations;

namespace SingularSystem.DTOs.Students
{
    public class StudentDocumentResponse
    {
        public Guid ID { get; set; }
        public string DocumentType { get; set; } = "";
        //public int? SchoolYear { get; set; }
        //public string Status { get; set; } = "";

        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }
        public string LocalCreatedAt => CreatedAt.ToBrazilianTime().ToString("g");

        [Display(Name = "Última atualização")]
        public DateTime? UpdatedAt { get; set; }
        public string LocalUpdatedAt => UpdatedAt?.ToBrazilianTime().ToString("g") ?? string.Empty;

        [Display(Name = "Data de unificação")]
        public DateTime? UnifiedAt { get; set; }
        public string LocalUnifiedAt => UnifiedAt?.ToBrazilianTime().ToString("g") ?? string.Empty;

        //public string? DocumentToken { get; set; }
        //public string? SignedFile { get; set; }
    }
}
