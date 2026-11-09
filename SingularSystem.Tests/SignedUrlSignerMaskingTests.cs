using System;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.DTOs;
using SingularSystem.Mappers;
using SingularSystem.Security;
using Xunit;

namespace SingularSystem.Tests
{
    /// <summary>
    /// Testes do mascaramento parcial de signatários aplicado no mapeamento
    /// <c>SignatureSigner</c> → <see cref="SignedUrlSigner"/> (spec signed-document-access, task 2.2).
    /// </summary>
    public sealed class SignedUrlSignerMaskingTests
    {
        // --- Mascaramento de nome ---

        [Theory]
        [InlineData("João da Silva", "João D. S.")]
        [InlineData("Maria Clara Souza Lima", "Maria C. S. L.")]
        [InlineData("Ana", "Ana")]                 // nome único: preservado
        [InlineData("  João   da   Silva  ", "João D. S.")] // espaços extras normalizados
        public void MaskName_reduces_surnames_to_uppercase_initials(string input, string expected)
        {
            Assert.Equal(expected, SensitiveDataMasker.MaskName(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void MaskName_returns_null_for_empty_input(string? input)
        {
            Assert.Null(SensitiveDataMasker.MaskName(input));
        }

        // --- Mascaramento de e-mail ---

        [Theory]
        [InlineData("usuario@dominio.com", "usu***@dominio.com")]
        [InlineData("jo@dominio.com", "jo***@dominio.com")]   // parte local curta: mantém o que há
        [InlineData("a@dominio.com", "a***@dominio.com")]
        [InlineData("JOAO@EXEMPLO.COM.BR", "JOA***@EXEMPLO.COM.BR")]
        public void MaskEmail_keeps_up_to_three_local_chars_and_full_domain(string input, string expected)
        {
            Assert.Equal(expected, SensitiveDataMasker.MaskEmail(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void MaskEmail_returns_null_for_empty_input(string? input)
        {
            Assert.Null(SensitiveDataMasker.MaskEmail(input));
        }

        // --- Mapeamento SignatureSigner → SignedUrlSigner ---

        [Fact]
        public void ToResponse_masks_name_and_email_and_carries_over_signed_state()
        {
            var signedAt = DateTimeOffset.UtcNow;
            var signer = new SignatureSigner
            {
                ExternalId = Guid.NewGuid().ToString(),
                Name = "João da Silva",
                Email = "usuario@dominio.com",
                Signed = true,
                SignedAt = signedAt
            };

            var result = SignedUrlSignerMapper.ToResponse(signer);

            Assert.Equal("João D. S.", result.Name);
            Assert.Equal("usu***@dominio.com", result.Email);
            Assert.True(result.Signed);
            Assert.Equal(signedAt, result.SignedAt);
        }

        [Fact]
        public void ToResponse_handles_null_name_email_and_signedAt()
        {
            var signer = new SignatureSigner
            {
                ExternalId = Guid.NewGuid().ToString(),
                Name = null,
                Email = null,
                Signed = false,
                SignedAt = null
            };

            var result = SignedUrlSignerMapper.ToResponse(signer);

            Assert.Null(result.Name);
            Assert.Null(result.Email);
            Assert.False(result.Signed);
            Assert.Null(result.SignedAt);
        }

        [Fact]
        public void ToResponseList_returns_empty_for_null()
        {
            Assert.Empty(SignedUrlSignerMapper.ToResponseList(null));
        }

        [Fact]
        public void ToResponseList_preserves_order_and_count()
        {
            var signers = new[]
            {
                new SignatureSigner { ExternalId = "1", Name = "Ana Beatriz", Email = "ana@x.com", Signed = true },
                new SignatureSigner { ExternalId = "2", Name = "Carlos Dias", Email = "carlos@y.com", Signed = false },
            };

            var result = SignedUrlSignerMapper.ToResponseList(signers);

            Assert.Equal(2, result.Count);
            Assert.Equal("Ana B.", result[0].Name);
            Assert.Equal("ana***@x.com", result[0].Email);
            Assert.Equal("Carlos D.", result[1].Name);
            Assert.Equal("car***@y.com", result[1].Email);
        }

        // Propriedade: o mascaramento nunca expõe o e-mail completo nem o domínio é alterado.
        // Gerador restrito ao espaço de entrada: partes local/domínio alfanuméricas não vazias
        // (sem '@' nem espaços em branco), formando um e-mail bem-formado "local@dominio.tld".
        [Property(MaxTest = 200, Arbitrary = new[] { typeof(EmailPartsArbContainer) })]
        public Property MaskEmail_never_leaks_full_local_part_and_preserves_domain(EmailParts parts)
        {
            var email = $"{parts.Local}@{parts.Domain}";
            var masked = SensitiveDataMasker.MaskEmail(email)!;

            var hasMask = masked.Contains("***");
            var domainPreserved = masked.EndsWith($"@{parts.Domain}");
            // Quando a parte local tem mais de 3 chars, o valor completo não deve aparecer.
            var noFullLocalLeak = parts.Local.Length <= 3 || !masked.StartsWith($"{parts.Local}***");

            return (hasMask && domainPreserved && noFullLocalLeak).ToProperty()
                .Label($"email={email} masked={masked}");
        }

        public sealed class EmailParts
        {
            public string Local { get; }
            public string Domain { get; }
            public EmailParts(string local, string domain)
            {
                Local = local;
                Domain = domain;
            }
            public override string ToString() => $"EmailParts({Local}@{Domain})";
        }

        public static class EmailPartsArbContainer
        {
            private static readonly Gen<char> AlphaNum =
                Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray());

            private static Gen<string> Token =>
                Gen.NonEmptyListOf(AlphaNum).Select(cs => new string(cs.ToArray()));

            public static Arbitrary<EmailParts> EmailParts() =>
                Arb.From(
                    from local in Token
                    from label in Token
                    from tld in Token
                    select new EmailParts(local, $"{label}.{tld}"));
        }
    }
}
