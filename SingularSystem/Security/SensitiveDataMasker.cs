namespace SingularSystem.Security
{
    /// <summary>
    /// Helper compartilhado para mascaramento parcial de dados pessoais (LGPD), evitando a
    /// exposição de valores completos em respostas de API e logs.
    /// </summary>
    /// <remarks>
    /// Centraliza as regras de mascaramento para evitar duplicação (DRY). As convenções seguem
    /// o documento de segurança/LGPD do projeto:
    /// <list type="bullet">
    ///   <item><description>Nome: <c>João da Silva</c> → <c>João D. S.</c> (primeiro nome por extenso; demais, inicial maiúscula seguida de ponto).</description></item>
    ///   <item><description>E-mail: <c>usuario@dominio.com</c> → <c>usu***@dominio.com</c> (até os três primeiros caracteres da parte local + <c>***</c>, preservando o domínio).</description></item>
    /// </list>
    /// </remarks>
    public static class SensitiveDataMasker
    {
        private const int EmailLocalVisibleChars = 3;

        /// <summary>
        /// Mascara parcialmente um nome, mantendo o primeiro nome por extenso e reduzindo os
        /// demais à inicial maiúscula seguida de ponto (ex.: <c>João da Silva</c> → <c>João D. S.</c>).
        /// </summary>
        /// <param name="name">Nome completo a mascarar.</param>
        /// <returns>
        /// O nome mascarado, ou <c>null</c> quando a entrada é nula/vazia/apenas espaços.
        /// </returns>
        public static string? MaskName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            if (parts.Length == 1)
                return parts[0];

            var masked = new List<string>(parts.Length) { parts[0] };
            for (var i = 1; i < parts.Length; i++)
            {
                var word = parts[i];
                var initial = char.ToUpperInvariant(word[0]);
                masked.Add($"{initial}.");
            }

            return string.Join(' ', masked);
        }

        /// <summary>
        /// Mascara parcialmente um e-mail, preservando até os três primeiros caracteres da parte
        /// local e o domínio (ex.: <c>usuario@dominio.com</c> → <c>usu***@dominio.com</c>).
        /// </summary>
        /// <param name="email">E-mail a mascarar.</param>
        /// <returns>
        /// O e-mail mascarado, ou <c>null</c> quando a entrada é nula/vazia/apenas espaços.
        /// Entradas sem <c>@</c> (não parecem e-mail) são tratadas apenas como parte local.
        /// </returns>
        public static string? MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var trimmed = email.Trim();
            var atIndex = trimmed.IndexOf('@');

            if (atIndex <= 0)
            {
                // Sem domínio reconhecível: mascara o valor inteiro como parte local.
                return $"{MaskLocalPart(trimmed.TrimStart('@'))}***";
            }

            var localPart = trimmed[..atIndex];
            var domain = trimmed[atIndex..]; // inclui o '@'

            return $"{MaskLocalPart(localPart)}***{domain}";
        }

        private static string MaskLocalPart(string localPart)
        {
            if (localPart.Length == 0)
                return string.Empty;

            var visible = Math.Min(EmailLocalVisibleChars, localPart.Length);
            return localPart[..visible];
        }
    }
}
