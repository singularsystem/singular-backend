namespace SingularSystem.Services
{
    /// <summary>
    /// Tipos de documento assináveis suportados por <see cref="Interfaces.ISignatureService"/>.
    /// Cada valor corresponde exatamente (case-sensitive) a um dos discriminadores de string
    /// aceitos por <c>CreateDoc</c>/<c>DeleteDoc</c>.
    /// </summary>
    public enum DocumentType
    {
        Report,
        Pei,
        Paee,
        CaseStudy
    }

    /// <summary>
    /// Helper compartilhado para validação do discriminador de tipo de documento.
    /// Usado pelas implementações de <see cref="Interfaces.ISignatureService"/>
    /// (<c>ZapSignService</c> e <c>NeckSignService</c>) para validar o parâmetro <c>type</c>
    /// de forma uniforme e sensível a maiúsculas/minúsculas.
    /// </summary>
    public static class DocumentTypeParser
    {
        /// <summary>
        /// Tenta converter uma string de tipo de documento no <see cref="DocumentType"/> correspondente.
        /// A validação é <b>sensível a maiúsculas/minúsculas</b>: somente os valores literais
        /// <c>report</c>, <c>pei</c>, <c>paee</c> e <c>caseStudy</c> são aceitos.
        /// Valores <c>null</c>, vazios, compostos apenas por espaços em branco, com casing diferente
        /// ou quaisquer outros resultam em falha (retorna <c>false</c>) sem lançar exceção.
        /// </summary>
        /// <param name="type">O discriminador de tipo de documento a validar.</param>
        /// <param name="documentType">
        /// Quando o método retorna <c>true</c>, contém o <see cref="DocumentType"/> correspondente;
        /// caso contrário, o valor padrão do enum.
        /// </param>
        /// <returns><c>true</c> se <paramref name="type"/> for um tipo válido; caso contrário, <c>false</c>.</returns>
        public static bool TryParse(string? type, out DocumentType documentType)
        {
            documentType = default;

            if (string.IsNullOrWhiteSpace(type))
                return false;

            switch (type)
            {
                case "report":
                    documentType = DocumentType.Report;
                    return true;
                case "pei":
                    documentType = DocumentType.Pei;
                    return true;
                case "paee":
                    documentType = DocumentType.Paee;
                    return true;
                case "caseStudy":
                    documentType = DocumentType.CaseStudy;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retorna o discriminador canônico (string) correspondente a um <see cref="DocumentType"/>.
        /// É o inverso de <see cref="TryParse(string?, out DocumentType)"/> para valores válidos.
        /// </summary>
        public static string ToCanonicalString(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Report => "report",
                DocumentType.Pei => "pei",
                DocumentType.Paee => "paee",
                DocumentType.CaseStudy => "caseStudy",
                _ => throw new System.ArgumentOutOfRangeException(nameof(documentType), documentType, null)
            };
        }
    }
}
