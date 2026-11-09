using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SingularSystem.Controllers;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using SingularSystem.Models;
using SingularSystem.Models.Entities;
using SingularSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SingularSystem.Tests
{
    /// <summary>
    /// Testes do <see cref="DocumentsSignatureController"/> (spec signed-document-access, task 5).
    /// Usam fakes manuais das portas (<see cref="ISignatureService"/>, <see cref="ISignedMediaResolver"/>)
    /// e um <see cref="AppDbContext"/> EF Core InMemory, no padrão dos demais testes do projeto
    /// (sem framework de mocking).
    /// </summary>
    public sealed class DocumentsSignatureControllerTests
    {
        private static AppDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"docs-signature-{Guid.NewGuid()}")
                .Options);

        private static DocumentsSignatureController CreateController(
            AppDbContext db,
            ISignatureService signatureService,
            ISignedMediaResolver signedMedia) =>
            CreateController(db, signatureService, signedMedia, NullLogger<DocumentsSignatureController>.Instance);

        private static DocumentsSignatureController CreateController(
            AppDbContext db,
            ISignatureService signatureService,
            ISignedMediaResolver signedMedia,
            ILogger<DocumentsSignatureController> logger) =>
            new(
                signatureService,
                signedMedia,
                db,
                logger);

        // Validates: Requirement 10.1
        [Fact]
        public async Task GetSignedUrl_returns_200_with_signedFileUrl_for_provider_signed_document()
        {
            const string documentToken = "necksign-envelope-abc123";
            const string signedUrl = "https://necksign.app/envelopes/abc123/download?token=xyz";

            // Caminho do provedor: nenhuma entidade S3-própria no banco (lookup não encontra nada),
            // então o controller consulta o provedor via ISignatureService.
            using var db = CreateContext();

            var signatureService = new FakeSignatureService(new SignatureDocumentDetails
            {
                ExternalId = Guid.NewGuid().ToString(),
                Status = SignatureDocumentStatus.Signed,
                SignedFileUrl = signedUrl,
                Signers = new List<SignatureSigner>
                {
                    new()
                    {
                        ExternalId = "signer-1",
                        Name = "João da Silva",
                        Email = "usuario@dominio.com",
                        Signed = true,
                        SignedAt = DateTimeOffset.UtcNow
                    }
                }
            });

            // Resolver nunca deve ser acionado para o fallback S3 neste caso (sem entidade),
            // mas é injetado conforme a assinatura do controller.
            var signedMedia = new FakeSignedMediaResolver();

            var controller = CreateController(db, signatureService, signedMedia);

            IActionResult result = await controller.GetSignedUrl(documentToken);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var body = Assert.IsType<ApiResponse<SignedUrlResponse>>(ok.Value);
            Assert.True(body.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(signedUrl, body.Data.SignedFileUrl);
            Assert.Equal("Signed", body.Data.Status);

            // GetDocumentDetails foi consultado com bypassCooldown: true (ramo do provedor).
            Assert.Equal(documentToken, signatureService.LastRequestedToken);
            Assert.True(signatureService.LastBypassCooldown);
            Assert.Equal(1, signatureService.GetDocumentDetailsCallCount);
        }

        // Validates: Requirement 10.2
        [Fact]
        public async Task GetSignedUrl_returns_presigned_s3_url_without_querying_provider_for_migrated_legacy_document()
        {
            const string documentToken = "legacy-report-token-9f8e7d";
            // SignedFile aponta para o S3 próprio (chave de objeto migrada); o resolver reconhece
            // via IsOwnS3Reference e gera a presigned URL a partir dela.
            const string s3ObjectKey = "signed/reports/2026/legacy-report-9f8e7d.pdf";
            const string presignedUrl =
                "https://ipaags-bucket.s3.us-east-2.amazonaws.com/" + s3ObjectKey + "?X-Amz-Signature=deadbeef";

            // Seed: uma entidade assinável (Report) com o DocumentToken da requisição,
            // DocumentStatus = Complete e SignedFile apontando para o S3 próprio.
            using var db = CreateContext();
            db.Reports.Add(new Report
            {
                ID = Guid.NewGuid(),
                DocumentToken = documentToken,
                DocumentStatus = Status.Complete,
                SignedFile = s3ObjectKey
            });
            await db.SaveChangesAsync();

            // Provedor: se GetDocumentDetails for chamado neste caminho, é um bug — o detalhe
            // retornado (status Signed com URL de provedor) ajudaria a detectar um fallback indevido.
            var signatureService = new FakeSignatureService(new SignatureDocumentDetails
            {
                ExternalId = Guid.NewGuid().ToString(),
                Status = SignatureDocumentStatus.Signed,
                SignedFileUrl = "https://provider.example/should-not-be-used"
            });

            // Resolver: reconhece o SignedFile como referência de S3 próprio e devolve a presigned URL.
            var signedMedia = new FakeSignedMediaResolver
            {
                OwnS3Reference = true,
                PreSignedUrl = presignedUrl
            };

            var controller = CreateController(db, signatureService, signedMedia);

            IActionResult result = await controller.GetSignedUrl(documentToken);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);

            var body = Assert.IsType<ApiResponse<SignedUrlResponse>>(ok.Value);
            Assert.True(body.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(presignedUrl, body.Data.SignedFileUrl);
            Assert.Equal("Signed", body.Data.Status);

            // Precedência (Req 4.4/10.2): o fallback S3 próprio NÃO consulta o provedor.
            Assert.Equal(0, signatureService.GetDocumentDetailsCallCount);
            Assert.Null(signatureService.LastRequestedToken);

            // O resolver gerou a presigned URL a partir da chave armazenada em SignedFile, com TTL 60 min.
            Assert.Equal(s3ObjectKey, signedMedia.LastPreSignedKey);
            Assert.Equal(60, signedMedia.LastPreSignedDurationMinutes);
        }

        // Validates: Requirement 10.3
        [Fact]
        public async Task GetSignedUrl_returns_409_with_status_and_no_url_for_unsigned_document()
        {
            const string documentToken = "necksign-envelope-pending-55aa";

            // Caminho do provedor: banco vazio (nenhuma entidade S3-própria) → não há fallback,
            // o controller consulta o provedor via ISignatureService (Req 3.1).
            using var db = CreateContext();

            // Provedor devolve detalhes de um documento AINDA NÃO assinado: Status != Signed
            // (Pending) e SignedFileUrl == null. É o gatilho do ramo 409 do controller.
            var signatureService = new FakeSignatureService(new SignatureDocumentDetails
            {
                ExternalId = Guid.NewGuid().ToString(),
                Status = SignatureDocumentStatus.Pending,
                SignedFileUrl = null,
                Signers = new List<SignatureSigner>
                {
                    new()
                    {
                        ExternalId = "signer-1",
                        Name = "João da Silva",
                        Email = "usuario@dominio.com",
                        Signed = false,
                        SignedAt = null
                    }
                }
            });

            // Resolver padrão: não reconhece S3 próprio nem ZapSign → fluxo recai no provedor.
            var signedMedia = new FakeSignedMediaResolver();

            // Logger capturador: NullLogger não registra nada, então usamos um test double para
            // afirmar que a ocorrência de "documento não assinado" foi logada (Req 3.1).
            var logger = new CapturingLogger<DocumentsSignatureController>();

            var controller = CreateController(db, signatureService, signedMedia, logger);

            IActionResult result = await controller.GetSignedUrl(documentToken);

            // Req 3.1: código distinto do 200 de assinado → 409 Conflict.
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);

            var body = Assert.IsType<ApiResponse<SignedUrlResponse>>(conflict.Value);
            Assert.False(body.Success);
            Assert.NotNull(body.Data);

            // Corpo expõe o status neutro atual do documento (Pending), sem signedFileUrl (Req 3.1/3.3).
            Assert.Equal("Pending", body.Data.Status);
            Assert.Null(body.Data.SignedFileUrl);

            // O provedor foi consultado com bypassCooldown: true (ramo do provedor, task 3.4).
            Assert.Equal(1, signatureService.GetDocumentDetailsCallCount);
            Assert.Equal(documentToken, signatureService.LastRequestedToken);
            Assert.True(signatureService.LastBypassCooldown);

            // Req 3.1: a ocorrência foi registrada via ILogger (LogInformation). O token completo
            // nunca deve aparecer no log — apenas a forma mascarada (Req 3.4).
            LogEntry logEntry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, logEntry.Level);
            Assert.DoesNotContain(documentToken, logEntry.Message);
        }

        // Validates: Requirement 10.4
        [Fact]
        public async Task GetSignedUrl_returns_404_when_no_entity_matches_and_provider_returns_null()
        {
            const string documentToken = "necksign-envelope-missing-77cc";

            // Nenhuma entidade com este DocumentToken: banco InMemory vazio (sem seed), então o
            // lookup das quatro DbSet não encontra nada e o fallback S3 próprio não se aplica.
            using var db = CreateContext();

            // Provedor sem detalhes disponíveis: GetDocumentDetails retorna null (não lança).
            // Com lookup vazio + null, o controller recai no ramo 404 (Req 3.2/4.6).
            var signatureService = new FakeSignatureService(details: null);

            // Resolver padrão: não reconhece S3 próprio nem ZapSign (não há entidade a resolver).
            var signedMedia = new FakeSignedMediaResolver();

            var controller = CreateController(db, signatureService, signedMedia);

            IActionResult result = await controller.GetSignedUrl(documentToken);

            // Req 10.4: token inexistente + GetDocumentDetails null → 404 Not Found neutro.
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);

            var body = Assert.IsType<ApiResponse<object>>(notFound.Value);
            Assert.False(body.Success);

            // O provedor foi consultado (ramo do provedor), com bypassCooldown: true, e retornou null.
            Assert.Equal(1, signatureService.GetDocumentDetailsCallCount);
            Assert.Equal(documentToken, signatureService.LastRequestedToken);
            Assert.True(signatureService.LastBypassCooldown);
        }

        // Validates: Requirement 10.5, 5.1
        [Fact]
        public async Task GetSignedUrl_returns_neutral_500_and_logs_masked_token_when_provider_throws()
        {
            // Token longo o suficiente para que o mascaramento seja observável: MaskToken preserva
            // os 4 primeiros e os 4 últimos caracteres ("neck...9f0a"), ocultando o miolo. Assim
            // podemos afirmar que o token COMPLETO não aparece nem na resposta nem no log.
            const string documentToken = "necksign-envelope-super-secret-token-abc123-9f0a";

            // Mensagem interna da exceção deliberadamente carregada de detalhes sensíveis
            // (segredo interno, bucket, chave de objeto, endpoint do provedor e o token completo).
            // NADA disso pode vazar para a resposta HTTP (Req 5.1/5.2).
            const string internalMessage =
                "secret-internal-detail: bucket=ipaags-private-bucket key=signed/reports/secret.pdf "
                + "endpoint=https://provider.internal/api token=" + documentToken;

            // Caminho do provedor: banco vazio (nenhuma entidade S3-própria), então o controller
            // consulta o provedor — cujo GetDocumentDetails LANÇA, acionando o ramo catch.
            using var db = CreateContext();

            var signatureService =
                FakeSignatureService.ThatThrows(new InvalidOperationException(internalMessage));

            var signedMedia = new FakeSignedMediaResolver();

            // Logger capturador: NullLogger descartaria tudo; aqui precisamos afirmar o LogError.
            var logger = new CapturingLogger<DocumentsSignatureController>();

            var controller = CreateController(db, signatureService, signedMedia, logger);

            IActionResult result = await controller.GetSignedUrl(documentToken);

            // Req 5.1: exceção do provedor → ObjectResult 500 (convenção StatusCode(500, ...)).
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            // Req 2.5/5.1: corpo neutro no envelope padrão (ApiResponse) com Success = false.
            var body = Assert.IsType<ApiResponse<object>>(objectResult.Value);
            Assert.False(body.Success);
            Assert.NotNull(body.Message);

            // Req 5.1/5.2: a mensagem neutra NÃO expõe mensagem interna, stack trace, token
            // completo, segredo, endpoint do provedor, bucket nem chave de objeto bruta.
            Assert.DoesNotContain(internalMessage, body.Message);
            Assert.DoesNotContain(documentToken, body.Message);
            Assert.DoesNotContain("secret-internal-detail", body.Message);
            Assert.DoesNotContain("ipaags-private-bucket", body.Message);
            Assert.DoesNotContain("signed/reports/secret.pdf", body.Message);
            Assert.DoesNotContain("provider.internal", body.Message);
            Assert.DoesNotContain("InvalidOperationException", body.Message);
            // Agnosticismo (Req 5.2): a resposta não revela o provedor de origem.
            Assert.DoesNotContain("ZapSign", body.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NeckSign", body.Message, StringComparison.OrdinalIgnoreCase);

            // O provedor foi efetivamente consultado (ramo do provedor) e lançou.
            Assert.Equal(1, signatureService.GetDocumentDetailsCallCount);
            Assert.Equal(documentToken, signatureService.LastRequestedToken);
            Assert.True(signatureService.LastBypassCooldown);

            // Req 5.1: o erro foi registrado via ILogger no nível Error, carregando a exceção.
            LogEntry logEntry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, logEntry.Level);

            // Req 3.4/5.3: o log NÃO contém o token completo — apenas a forma mascarada
            // (prefixo "neck" + sufixo "9f0a"). Confirmamos a ausência do token inteiro e a
            // presença do token mascarado para garantir que o mascaramento foi aplicado.
            Assert.DoesNotContain(documentToken, logEntry.Message);
            Assert.Contains("neck...9f0a", logEntry.Message);
        }

        /// <summary>
        /// Fake de <see cref="ISignatureService"/>: devolve um resultado pré-configurado de
        /// <c>GetDocumentDetails</c> e registra os argumentos recebidos para asserção.
        /// </summary>
        private sealed class FakeSignatureService : ISignatureService
        {
            private readonly SignatureDocumentDetails? _details;
            private readonly Exception? _throwOnGetDetails;

            public FakeSignatureService(SignatureDocumentDetails? details)
            {
                _details = details;
            }

            private FakeSignatureService(Exception throwOnGetDetails)
            {
                _throwOnGetDetails = throwOnGetDetails;
            }

            /// <summary>
            /// Cria um fake cujo <c>GetDocumentDetails</c> LANÇA a exceção informada (task 5.5,
            /// Req 10.5/5.1). Usado para exercitar o ramo <c>catch</c> do controller (resposta
            /// neutra + log com token mascarado).
            /// </summary>
            public static FakeSignatureService ThatThrows(Exception exception) => new(exception);

            public string? LastRequestedToken { get; private set; }
            public bool LastBypassCooldown { get; private set; }

            /// <summary>
            /// Quantas vezes <c>GetDocumentDetails</c> foi invocado. Permite afirmar que o ramo
            /// de fallback S3 próprio NÃO consulta o provedor (task 5.2, Req 10.2/4.4).
            /// </summary>
            public int GetDocumentDetailsCallCount { get; private set; }

            public Task<SignatureDocumentDetails?> GetDocumentDetails(string documentToken, bool bypassCooldown = false)
            {
                GetDocumentDetailsCallCount++;
                LastRequestedToken = documentToken;
                LastBypassCooldown = bypassCooldown;
                if (_throwOnGetDetails is not null)
                {
                    throw _throwOnGetDetails;
                }
                return Task.FromResult(_details);
            }

            public Task<bool> CreateDoc(Guid idDoc, string type) => Task.FromResult(true);
            public Task<bool> DeleteDoc(Guid idDoc, string type) => Task.FromResult(true);
            public Task<bool> SyncDocument(string documentToken, bool bypassCooldown = false) => Task.FromResult(true);
            public Task SyncPendingDocuments() => Task.CompletedTask;
        }

        /// <summary>
        /// Fake de <see cref="ISignedMediaResolver"/>: por padrão não reconhece referências de S3
        /// próprio nem de ZapSign, de modo que o fluxo recai no provedor.
        /// </summary>
        private sealed class FakeSignedMediaResolver : ISignedMediaResolver
        {
            /// <summary>Quando <c>true</c>, <see cref="IsOwnS3Reference"/> reconhece o SignedFile
            /// como referência de S3 próprio (habilita o fallback). Padrão: <c>false</c>.</summary>
            public bool OwnS3Reference { get; init; }

            /// <summary>URL pré-assinada devolvida por <see cref="GeneratePreSignedUrl"/> quando
            /// configurada; caso contrário, uma URL derivada da chave.</summary>
            public string? PreSignedUrl { get; init; }

            /// <summary>Última chave recebida por <see cref="GeneratePreSignedUrl"/>.</summary>
            public string? LastPreSignedKey { get; private set; }

            /// <summary>Último TTL (em minutos) recebido por <see cref="GeneratePreSignedUrl"/>.</summary>
            public double? LastPreSignedDurationMinutes { get; private set; }

            public bool IsZapSignUrl(string? signedFile) => false;
            public bool IsOwnS3Reference(string? signedFile) => OwnS3Reference;

            public string GeneratePreSignedUrl(string s3ObjectKey, double durationMinutes = 60)
            {
                LastPreSignedKey = s3ObjectKey;
                LastPreSignedDurationMinutes = durationMinutes;
                return PreSignedUrl ?? $"https://s3.local/presigned/{s3ObjectKey}";
            }
        }

        /// <summary>
        /// Uma entrada de log capturada: nível e mensagem já formatada. Permite afirmar que o
        /// controller registrou a ocorrência sem acoplar o teste ao template exato.
        /// </summary>
        private sealed record LogEntry(LogLevel Level, string Message);

        /// <summary>
        /// Test double mínimo de <see cref="ILogger{T}"/> que acumula as entradas registradas
        /// (<see cref="NullLogger{T}"/> descarta tudo e não permite afirmar que um log foi emitido).
        /// </summary>
        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<LogEntry> Entries { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
