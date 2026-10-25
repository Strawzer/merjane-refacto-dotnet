using System.Reflection;
using Merjane.Application;
using Merjane.Domain;
using Merjane.Infrastructure;

namespace Refacto.DotNet.Controllers.Tests.Architecture
{
    public class LayerDependencyTests
    {
        private const string Application = "Merjane.Application";
        private const string Infrastructure = "Merjane.Infrastructure";
        private const string Api = "Refacto.DotNet.Controllers";
        private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
        private const string AspNetCore = "Microsoft.AspNetCore";

        [Fact]
        public void DomainReferencesNoOtherLayerNorPersistenceOrWebFramework()
        {
            AssertReferencesNone(typeof(ProductDecision).Assembly, Application, Infrastructure, Api, EntityFrameworkCore, AspNetCore);
        }

        [Fact]
        public void ApplicationReferencesDomainButNoOuterLayerNorPersistenceOrWebFramework()
        {
            Assembly application = typeof(OrderProcessingService).Assembly;

            Assert.Contains("Merjane.Domain", ReferencedAssemblyNames(application));
            AssertReferencesNone(application, Infrastructure, Api, EntityFrameworkCore, AspNetCore);
        }

        [Fact]
        public void InfrastructureReferencesNeitherTheApiNorTheWebFramework()
        {
            AssertReferencesNone(typeof(SystemClock).Assembly, Api, AspNetCore);
        }

        private static void AssertReferencesNone(Assembly assembly, params string[] forbiddenPrefixes)
        {
            Assert.DoesNotContain(ReferencedAssemblyNames(assembly), name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
        }

        private static IEnumerable<string> ReferencedAssemblyNames(Assembly assembly)
        {
            return assembly.GetReferencedAssemblies().Select(reference => reference.Name!);
        }
    }
}
