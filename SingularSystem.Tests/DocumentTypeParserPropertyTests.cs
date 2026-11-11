using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    // Feature: 012-singular-migration, Property 1: Validação de tipo de documento
    //
    // Para qualquer string que não corresponda exatamente (case-sensitive) a um de
    // `report`, `pei`, `paee`, `caseStudy` — incluindo `null`, vazio, só-espaços e
    // variações de capitalização — a validação de tipo de documento deve retornar
    // `false` e nunca propagar exceção. Para os quatro valores canônicos exatos, deve
    // retornar `true` com o `DocumentType` correspondente.
    //
    // A unidade sob teste é o helper compartilhado `DocumentTypeParser.TryParse`,
    // usado por ambas as implementações de ISignatureService (ZapSignService e
    // NeckSignService) para validar o parâmetro `type` de CreateDoc/DeleteDoc.
    //
    // Validates: Requirements 1.5, 1.6
    public sealed class DocumentTypeParserPropertyTests
    {
        /// <summary>
        /// Os quatro discriminadores canônicos exatos (case-sensitive) e o
        /// <see cref="DocumentType"/> esperado para cada um.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, DocumentType> ValidTypes =
            new Dictionary<string, DocumentType>(StringComparer.Ordinal)
            {
                ["report"] = DocumentType.Report,
                ["pei"] = DocumentType.Pei,
                ["paee"] = DocumentType.Paee,
                ["caseStudy"] = DocumentType.CaseStudy,
            };

        /// <summary>
        /// Gerador inteligente que cobre o espaço de entrada relevante da propriedade:
        /// strings válidas (exatas), casing errado, null, vazio, só-espaços e strings
        /// arbitrárias. Cada caso é marcado como válido/inválido para permitir uma
        /// asserção precisa, mantendo o custo das 100+ iterações baixo.
        /// </summary>
        private static Arbitrary<DocumentTypeCase> DocumentTypeCaseArb()
        {
            // Casos válidos: exatamente os quatro discriminadores canônicos.
            var validGen = Gen.Elements(ValidTypes.Keys.ToArray())
                .Select(DocumentTypeCase.Valid);

            // Casing errado de um valor válido (ex.: "Report", "PEI", "casestudy").
            var casingGen = Gen.Elements(ValidTypes.Keys.ToArray())
                .SelectMany(valid => Gen.Elements(
                    valid.ToUpperInvariant(),
                    valid.ToLowerInvariant(),
                    FlipFirstChar(valid)))
                // Garante que o casing realmente difere do canônico; caso contrário
                // (ex.: "pei".ToLowerInvariant() == "pei"), degenera para inválido só
                // quando difere, senão descarta via filtro para não poluir o espaço.
                .Where(s => !ValidTypes.ContainsKey(s))
                .Select(DocumentTypeCase.Invalid);

            // null, vazio e strings compostas apenas por espaços em branco.
            var blankGen = Gen.Elements<string?>(
                null,
                string.Empty,
                " ",
                "   ",
                "\t",
                "\n",
                " \t \n ")
                .Select(DocumentTypeCase.Invalid);

            // Strings arbitrárias; filtra as que por acaso coincidam com um valor válido.
            var arbitraryGen = Arb.Default.String().Generator
                .Where(s => s is null || !ValidTypes.ContainsKey(s))
                .Select(DocumentTypeCase.Invalid);

            // Frequências: mantém equilíbrio entre válidos e as várias classes de inválidos.
            var gen = Gen.Frequency(
                Tuple.Create(3, validGen),
                Tuple.Create(2, casingGen),
                Tuple.Create(2, blankGen),
                Tuple.Create(3, arbitraryGen));

            return Arb.From(gen);
        }

        private static string FlipFirstChar(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            char first = value[0];
            char flipped = char.IsUpper(first) ? char.ToLowerInvariant(first) : char.ToUpperInvariant(first);
            return flipped + value.Substring(1);
        }

        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DocumentTypeCaseArbContainer) })]
        public Property TryParse_classifies_every_input_without_throwing(DocumentTypeCase testCase)
        {
            bool threw = false;
            bool result = false;
            DocumentType parsed = default;

            try
            {
                result = DocumentTypeParser.TryParse(testCase.Input, out parsed);
            }
            catch
            {
                threw = true;
            }

            // Invariante universal: TryParse nunca propaga exceção para nenhuma entrada.
            if (threw)
                return false.ToProperty().Label("TryParse propagou exceção");

            if (testCase.IsValid)
            {
                // Valor canônico exato → true com o DocumentType correto.
                bool ok = result
                          && testCase.Input is not null
                          && ValidTypes.TryGetValue(testCase.Input, out var expected)
                          && parsed == expected;

                return ok.ToProperty()
                    .Label($"Esperava true + DocumentType correto para '{testCase.Input}'");
            }

            // Qualquer outra entrada (null/vazio/espaços/casing/arbitrária) → false.
            return (result == false).ToProperty()
                .Label($"Esperava false para entrada inválida '{Describe(testCase.Input)}'");
        }

        private static string Describe(string? input)
        {
            if (input is null)
                return "<null>";
            if (input.Length == 0)
                return "<empty>";
            return input;
        }

        /// <summary>
        /// Container exposto ao atributo [Property] para registrar o Arbitrary customizado.
        /// </summary>
        public static class DocumentTypeCaseArbContainer
        {
            public static Arbitrary<DocumentTypeCase> DocumentTypeCase() => DocumentTypeCaseArb();
        }

        /// <summary>
        /// Caso de teste gerado: a entrada e se ela deve ser considerada válida.
        /// </summary>
        public sealed class DocumentTypeCase
        {
            public string? Input { get; }
            public bool IsValid { get; }

            private DocumentTypeCase(string? input, bool isValid)
            {
                Input = input;
                IsValid = isValid;
            }

            public static DocumentTypeCase Valid(string input) => new(input, true);

            public static DocumentTypeCase Invalid(string? input) => new(input, false);

            public override string ToString() =>
                $"DocumentTypeCase(Input='{(Input is null ? "<null>" : Input)}', IsValid={IsValid})";
        }
    }
}
