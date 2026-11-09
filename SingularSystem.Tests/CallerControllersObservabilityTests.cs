using System;
using System.Linq;
using System.Reflection;
using SingularSystem.Controllers;
using SingularSystem.Interfaces;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class CallerControllersObservabilityTests
    {
        private static readonly Type[] CallerControllers =
        {
            typeof(ReportsController),
            typeof(PEIsController),
            typeof(PAEEsController),
            typeof(RegularTeacherScreeningsController),
            typeof(RegularTeacherQuestionnairesController),
            typeof(RegularTeacherEvaluationDocumentsController),
        };

        [Fact]
        public void All_caller_controllers_depend_on_ISignatureService()
        {
            foreach (var controller in CallerControllers)
            {
                var ctor = controller.GetConstructors().Single();
                var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

                Assert.Contains(typeof(ISignatureService), paramTypes);
            }
        }

        [Fact]
        public void Caller_controller_field_is_typed_as_ISignatureService()
        {
            foreach (var controller in CallerControllers)
            {
                var fields = controller.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.Contains(fields, f => f.FieldType == typeof(ISignatureService));
            }
        }
    }
}
