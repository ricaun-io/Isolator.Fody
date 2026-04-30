using Isolator.ConsoleApp;

namespace Isolator.Fody.ConsoleApp.Tests
{
    public class ContextNameTests
    {
        [Test]
        public void ContextName_ShouldStartWithIsolatorContext()
        {
            // Arrange & Act
            var contextName = ClassStatic.ContextName();

            // Assert
            Assert.That(contextName, Does.StartWith("IsolatorContext"));
        }

        [Test]
        public void ContextName_ShouldEndWithValidGuid()
        {
            // Arrange
            var contextName = ClassStatic.ContextName();

            // Act
            var guidString = contextName.Split('.').LastOrDefault();

            // Assert
            Assert.That(Guid.TryParse(guidString, out _), Is.True);
        }

        [Test]
        public void ContextName_GuidShouldMatchModuleVersionId()
        {
            // Arrange
            var contextName = ClassStatic.ContextName();
            var guidString = contextName.Split('.').LastOrDefault();
            var moduleVersionId = typeof(Program).Assembly.ManifestModule.ModuleVersionId;

            // Act
            Guid.TryParse(guidString, out Guid guid);

            // Assert
            Assert.That(guid, Is.EqualTo(moduleVersionId));
        }
    }
}