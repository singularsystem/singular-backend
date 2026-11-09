using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models.Entities
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string LogLevel { get; set; }

        [Required]
        [MaxLength(255)]
        public string Message { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } // Onde o log foi gerado (ex: "UserController", "RelatorioService")

        [MaxLength(2000)]
        public string Details { get; set; }
        public Guid UserID { get; set; }

        [MaxLength(100)]
        public string EntityType { get; set; }  // Tipo da entidade afetada (ex: "Aluno", "RelatorioUnificado")
        public Guid EntityID { get; set; }

        [MaxLength(50)]
        public string Action { get; set; } // Ação realizada (ex: "Create", "Update", "Delete", "Login")
    }
}
