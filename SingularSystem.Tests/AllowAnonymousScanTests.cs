using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SingularSystem.Tests
{
    public sealed class AllowAnonymousScanTests
    {
        private static string FindControllersDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "SingularSystem", "Controllers");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                var candidateWithBackend = Path.Combine(dir.FullName, "singular-backend", "SingularSystem", "Controllers");
                if (Directory.Exists(candidateWithBackend))
                {
                    return candidateWithBackend;
                }

                dir = dir.Parent;
            }

            // Fallback to relative path from solution
            var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SingularSystem/Controllers"));
            if (Directory.Exists(fallback)) return fallback;

            throw new DirectoryNotFoundException("Could not locate SingularSystem/Controllers directory");
        }

        [Fact]
        public void AllowAnonymous_OnlyExistsOnPermittedControllers()
        {
            var controllersDir = FindControllersDirectory();
            var csFiles = Directory.GetFiles(controllersDir, "*.cs", SearchOption.TopDirectoryOnly);

            var permittedControllers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "IBGEController.cs",
                "PublicValidationController.cs",
                "UsersController.cs"
            };

            var regex = new Regex(@"\[\s*AllowAnonymous\s*\]", RegexOptions.Compiled);
            var violations = new List<string>();

            foreach (var file in csFiles)
            {
                var fileName = Path.GetFileName(file);
                var content = File.ReadAllText(file);

                if (regex.IsMatch(content))
                {
                    if (!permittedControllers.Contains(fileName))
                    {
                        violations.Add(fileName);
                    }
                }
            }

            Assert.Empty(violations);
        }
    }
}
