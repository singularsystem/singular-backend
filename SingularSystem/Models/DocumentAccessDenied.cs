namespace SingularSystem.Models
{
    public static class DocumentAccessDenied
    {
        public static ApiResponse<object> Body() => new ApiResponse<object>
        {
            Success = false,
            Data = null,
            Message = "Você não tem permissão para acessar este documento."
        };
    }
}
