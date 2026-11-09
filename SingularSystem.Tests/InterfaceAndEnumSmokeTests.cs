using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SingularSystem.DTOs;
using SingularSystem.Interfaces;
using SingularSystem.Models.Enums;
using SingularSystem.Services;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class InterfaceAndEnumSmokeTests
    {
        [Fact]
        public void ISignatureService_has_the_expected_five_methods_and_signatures()
        {
            var t = typeof(ISignatureService);

            var createDoc = t.GetMethod(nameof(ISignatureService.CreateDoc))!;
            Assert.Equal(typeof(Task<bool>), createDoc.ReturnType);
            Assert.Equal(new[] { typeof(Guid), typeof(string) }, createDoc.GetParameters().Select(p => p.ParameterType).ToArray());

            var deleteDoc = t.GetMethod(nameof(ISignatureService.DeleteDoc))!;
            Assert.Equal(typeof(Task<bool>), deleteDoc.ReturnType);
            Assert.Equal(new[] { typeof(Guid), typeof(string) }, deleteDoc.GetParameters().Select(p => p.ParameterType).ToArray());

            var getDetails = t.GetMethod(nameof(ISignatureService.GetDocumentDetails))!;
            Assert.Equal(typeof(Task<SignatureDocumentDetails?>), getDetails.ReturnType);

            var sync = t.GetMethod(nameof(ISignatureService.SyncDocument))!;
            Assert.Equal(typeof(Task<bool>), sync.ReturnType);

            var syncPending = t.GetMethod(nameof(ISignatureService.SyncPendingDocuments))!;
            Assert.Equal(typeof(Task), syncPending.ReturnType);
        }

        [Fact]
        public void NeckSignService_implements_ISignatureService()
        {
            Assert.True(typeof(ISignatureService).IsAssignableFrom(typeof(NeckSignService)));
        }

        [Fact]
        public void GetDocumentDetails_returns_neutral_dto_in_NeckSignService()
        {
            var map = typeof(NeckSignService).GetInterfaceMap(typeof(ISignatureService));
            var idx = Array.FindIndex(map.InterfaceMethods, m => m.Name == nameof(ISignatureService.GetDocumentDetails));
            var target = map.TargetMethods[idx];
            Assert.Equal(typeof(Task<SignatureDocumentDetails?>), target.ReturnType);
        }

        [Fact]
        public void SignatureDocumentDetails_exposes_only_neutral_fields()
        {
            var props = typeof(SignatureDocumentDetails).GetProperties().Select(p => p.Name).ToArray();
            Assert.Contains("ExternalId", props);
            Assert.Contains("SignedFileUrl", props);
            Assert.Contains("Status", props);
            Assert.Contains("Signers", props);
            Assert.DoesNotContain(props, p => p.Contains("ZapSign", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(props, p => p.Contains("NeckSign", StringComparison.OrdinalIgnoreCase));

            foreach (var name in new[] { "Pending", "Signing", "Signed", "Declined", "Expired", "Voided" })
                Assert.Contains(name, Enum.GetNames<SignatureDocumentStatus>());
        }

        [Fact]
        public void Status_enum_values_are_preserved_and_distinct()
        {
            Assert.Equal(2, (int)Status.Complete);
            Assert.Equal(4, (int)Status.InProgress);
            Assert.Equal(5, (int)Status.None);
            Assert.Equal(6, (int)Status.Declined);
            Assert.Equal(7, (int)Status.Expired);

            var values = Enum.GetValues<Status>().Select(v => (int)v).ToArray();
            Assert.Equal(values.Length, values.Distinct().Count());
        }

        [Fact]
        public void SignatureProvider_enum_values()
        {
            Assert.Equal(1, (int)SignatureProvider.ZapSign);
            Assert.Equal(2, (int)SignatureProvider.NeckSign);
        }
    }
}
