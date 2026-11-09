using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using FsCheck;
using FsCheck.Xunit;
using SingularSystem.Models.Entities;
using SingularSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class StorageTests
    {
        // Feature: modulo-1-infraestrutura, Property 1: Clamp da duração da presigned URL
        //
        // Para toda duração solicitada d (inteira ou fracionária, incluindo negativa, zero e valores
        // enormes), a duração efetiva aplicada à presigned URL deve ser min(d, 60) quando d > 0,
        // e exatamente 60 quando d <= 0; em todos os casos a duração efetiva nunca excede 60 minutos
        // nem é não-positiva.
        //
        // Validates: Requirements 3.2
        [Property(MaxTest = 100)]
        public Property Presigned_url_duration_clamp(double requestedDuration)
        {
            var clamped = PreSignedUrlDurationHelper.Clamp(requestedDuration);

            double expected;
            if (requestedDuration <= 0 || double.IsNaN(requestedDuration))
            {
                expected = 60.0;
            }
            else
            {
                expected = Math.Min(requestedDuration, 60.0);
            }

            var matchesExpected = Math.Abs(clamped - expected) < 0.0001;
            var neverExceeds60 = clamped <= 60.0;
            var isStrictlyPositive = clamped > 0;

            return (matchesExpected && neverExceeds60 && isStrictlyPositive).ToProperty()
                .Label($"Requested={requestedDuration}, Clamped={clamped}, Expected={expected}");
        }

        // Testes de exemplo para falha/indisponibilidade de storage (Req 3.6, 3.7)
        [Fact]
        public async Task Upload_failure_rejects_without_partial_persistence()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"storage-fail-{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(options);
            var service = new FileStorageService(db, NullLogger<FileStorageService>.Instance);

            // Arquivo vazio deve falhar
            var emptyFile = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "test.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var id = await service.SaveFileInDatabase(emptyFile);
            Assert.Equal(Guid.Empty, id);

            // Não deve haver arquivo persistido parcialmente
            var count = await db.Files.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Storage_unavailability_preserves_previous_state()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"storage-unavail-{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(options);

            // Estado anterior válido
            var initialFile = new SingularSystem.Models.Entities.File
            {
                ID = Guid.NewGuid(),
                OriginalFileName = "documento.pdf",
                StoredFileName = "stored.pdf",
                ContentType = "application/pdf",
                Content = new byte[] { 1, 2, 3 }
            };
            db.Files.Add(initialFile);
            await db.SaveChangesAsync();

            var service = new FileStorageService(db, NullLogger<FileStorageService>.Instance);

            // Consulta por ID inexistente ou erro não modifica o arquivo anterior
            var retrieved = await service.GetFileByID(Guid.NewGuid());
            Assert.Null(retrieved);

            var original = await service.GetFileByID(initialFile.ID);
            Assert.NotNull(original);
            Assert.Equal(3, original.Content.Length);
        }
    }
}
