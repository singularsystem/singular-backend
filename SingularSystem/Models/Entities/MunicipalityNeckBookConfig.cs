using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    /// <summary>
    /// Configuração da integração NeckBook por município (par <c>(State, City)</c>),
    /// armazenando a <c>NeckBook_Api_Key</c> criptografada em repouso via ASP.NET Core Data Protection.
    /// </summary>
    public class MunicipalityNeckBookConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string City { get; set; } = string.Empty;

        [Required]
        public string EncryptedApiKey { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
