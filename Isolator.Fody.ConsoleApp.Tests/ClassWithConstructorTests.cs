using Isolator.ConsoleApp;

namespace Isolator.Fody.ConsoleApp.Tests
{
    public class ClassWithConstructorTests
    {
        [Test]
        public void TestClassWithStaticConstructor_Execute_ReturnsTrue()
        {
            Assert.That(ClassWithStaticConstructor.Execute(), Is.True);
        }

        [Test]
        public void TestClassWithAbstractionConstructor_Execute_ReturnsTrue()
        {
            var classAbstract = new ClassWithAbstractionConstructor();
            Assert.That(classAbstract.Execute(), Is.True);
        }

        [Test]
        public void TestClassWithPublicConstructor_Execute_ReturnsTrue()
        {
            var classPublic = new ClassWithPublicConstructor();
            Assert.That(classPublic.Execute(), Is.True);
        }

        [Test]
        public void TestClassWithPrivateConstructor_Execute_ReturnsTrue()
        {
            var classPrivate = (Activator.CreateInstance(typeof(ClassWithPrivateConstructor), true) as ClassWithPrivateConstructor);
            Assert.That(classPrivate!.Execute(), Is.True);
        }
    }
}