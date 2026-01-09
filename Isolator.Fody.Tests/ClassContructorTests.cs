using Isolator.ConsoleApp;

namespace Isolator.Fody.Tests
{
    public class ClassContructorTests
    {
        [Test]
        public void TestClassConstructor_DefaultConstructor_ExecuteReturnsFalse()
        {
            var instanceFail = new ClassConstructor();
            Assert.That(instanceFail.Execute(), Is.False);
        }

        [Test]
        public void TestClassConstructor_WithValidName_ExecuteReturnsTrue()
        {
            var instancePass = new ClassConstructor("ValidName");
            Assert.That(instancePass.Execute(), Is.True);
        }

        [Test]
        public void TestClassConstructor_ExecuteWithSingleInt_ReturnsValue()
        {
            var instancePass = new ClassConstructor("ValidName");
            Assert.That(instancePass.Execute(10), Is.EqualTo(10));
        }

        [Test]
        public void TestClassConstructor_ExecuteWithTwoInts_ReturnsSum()
        {
            var instancePass = new ClassConstructor("ValidName");
            Assert.That(instancePass.Execute(10, 20), Is.EqualTo(30));
        }
    }
}