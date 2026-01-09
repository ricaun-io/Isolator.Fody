using Isolator.ConsoleApp;

namespace Isolator.Fody.Tests
{
    public class ClassIsolatorTests
    {
        [Test]
        public void TestClassIsolator_Execute_ReturnsTrue()
        {
            var instance = new ClassIsolator();
            Assert.That(instance.Execute(), Is.True);
        }

        [Test]
        public void TestClassIsolator_ExecuteClass_ReturnsTrue()
        {
            var instance = new ClassIsolator();
            Assert.That(instance.ExecuteClass(), Is.True);
        }

        [Test]
        public void TestClassIsolator_ExecuteClassWithBool_ReturnsTrue()
        {
            var instance = new ClassIsolator();
            Assert.That(instance.ExecuteClass(true), Is.True);
        }

        [Test]
        public void TestClassIsolator_ContextNumber_ReturnsGreaterThanZero()
        {
            var instance = new ClassIsolator();
            Assert.That(instance.ContextNumber(), Is.GreaterThan(0));
        }

        [Test]
        public void TestClassIsolator_ExecuteContextNumber_ReturnsTrue()
        {
            var instance = new ClassIsolator();
            Assert.That(instance.ExecuteContextNumber(), Is.True);
        }
    }
}