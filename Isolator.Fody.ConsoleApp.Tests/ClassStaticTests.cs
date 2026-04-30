using Isolator.ConsoleApp;

namespace Isolator.Fody.ConsoleApp.Tests
{
    public class ClassStaticTests
    {
        [Test]
        public void Execute_NoParameters_ReturnsTrue()
        {
            Assert.IsTrue(ClassStatic.Execute());
        }
        [Test]
        public void Execute_BooleanParameter_ReturnsSameValue()
        {
            Assert.IsTrue(ClassStatic.Execute(true));
            Assert.IsFalse(ClassStatic.Execute(false));
        }
        [Test]
        public void Execute_StringParameter_ReturnsTrueForNonEmpty()
        {
            Assert.IsTrue(ClassStatic.Execute("Test"));
            Assert.IsFalse(ClassStatic.Execute(string.Empty));
        }
        [Test]
        public void Execute_IntParameter_ReturnsTrueForNonZero()
        {
            Assert.IsTrue(ClassStatic.Execute(123));
            Assert.IsFalse(ClassStatic.Execute(0));
        }
        [Test]
        public void ExecuteOut_StringParameter_SetsOutParameterAndReturnsTrueForNonEmpty()
        {
            string result;
            Assert.IsTrue(ClassStatic.ExecuteOut("TestOut", out result));
            Assert.That(result, Is.EqualTo("TestOut"));
            Assert.IsFalse(ClassStatic.ExecuteOut(string.Empty, out result));
            Assert.That(result, Is.EqualTo(string.Empty));
        }
        [Test]
        public void ExecuteRef_StringParameter_SetsRefParameterAndReturnsTrueForNonEmpty()
        {
            string result = "Initial";
            Assert.IsTrue(ClassStatic.ExecuteRef("TestRef", ref result));
            Assert.That(result, Is.EqualTo("TestRef"));
            result = "Initial";
            Assert.IsFalse(ClassStatic.ExecuteRef(string.Empty, ref result));
            Assert.That(result, Is.EqualTo(string.Empty));
        }
        [Test]
        public void ContextName_ReturnsNonDefaultContextName()
        {
            var contextName = ClassStatic.ContextName();
            Assert.That(contextName, Is.Not.EqualTo("Default"));
        }
        [Test]
        public void ContextNameFail_ReturnsDefaultContextName()
        {
            var contextName = ClassStatic.ContextNameFail();
            Assert.That(contextName, Is.EqualTo("Default"));
        }
    }
}