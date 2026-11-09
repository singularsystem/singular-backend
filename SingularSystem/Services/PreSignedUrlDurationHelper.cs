using System;

namespace SingularSystem.Services
{
    /// <summary>
    /// Helper puro para clamp de duração de presigned URLs (Req 3.2, Property 1).
    /// </summary>
    public static class PreSignedUrlDurationHelper
    {
        public const double MaxPreSignedUrlMinutes = 60;

        /// <summary>
        /// Aplica o teto de 60 minutos à duração solicitada.
        /// Valores não positivos (<= 0) ou NaN caem no default de 60 minutos.
        /// Valores > 60 sofrem clamp para 60 minutos.
        /// Valores entre (0, 60] são mantidos.
        /// </summary>
        public static double Clamp(double durationMinutes)
        {
            if (double.IsNaN(durationMinutes) || durationMinutes <= 0)
                return MaxPreSignedUrlMinutes;

            return Math.Min(durationMinutes, MaxPreSignedUrlMinutes);
        }
    }
}
