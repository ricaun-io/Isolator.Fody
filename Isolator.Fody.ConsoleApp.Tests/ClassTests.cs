using System.Diagnostics;
using Isolator.ConsoleApp;

namespace Isolator.Fody.ConsoleApp.Tests
{
    public class ClassTests
    {
        [Test]
        public void TestClass_Execute_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.Execute(), Is.True);
        }

        [Test]
        public void TestClass_ExecuteWithBool_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.Execute(true), Is.True);
        }

        [Test]
        public void TestClass_ExecuteWithString_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.Execute("Test"), Is.True);
        }

        [Test]
        public void TestClass_ExecuteWithInt_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.Execute(123), Is.True);
        }

        [Test]
        public void TestClass_ExecuteOut_ReturnsTrueAndSetsOutParameter()
        {
            var instance = new Class();
            string outResult;
            var result = instance.ExecuteOut("TestOut", out outResult);

            Assert.That(result, Is.True);
            Assert.That(outResult, Is.EqualTo("TestOut"));
        }

        [Test]
        public void TestClass_ExecuteRef_ReturnsTrueAndModifiesRefParameter()
        {
            var instance = new Class();
            string refResult = "Initial";
            var result = instance.ExecuteRef("TestRef", ref refResult);

            Assert.That(result, Is.True);
            Assert.That(refResult, Is.EqualTo("TestRef"));
        }

        [Test]
        public void TestClass_ExecuteDefault_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.ExecuteDefault(), Is.True);
        }

        [Test]
        public void TestClass_ExecuteDefaultWithString_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.ExecuteDefault("Test"), Is.True);
        }

        [Test]
        public void TestClass_ExecuteDefaultWithNumber_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.ExecuteDefault(number: 123), Is.True);
        }

        [Test]
        public void TestClass_ExecuteDefaultWithStringAndNumber_ReturnsTrue()
        {
            var instance = new Class();
            Assert.That(instance.ExecuteDefault("Test", 123), Is.True);
        }

        [Test]
        public void TestClass_ContextNumber_ReturnsGreaterThanZero()
        {
            var instance = new Class();
            Assert.That(instance.ContextNumber(), Is.GreaterThan(0));
        }

        [Test]
        public void TestClass_ExecuteContextName_ReturnsNonDefaultValue()
        {
            var instance = new Class();
            Assert.That(instance.ExecuteContextName(), Is.Not.EqualTo("Default"));
        }

        [Test]
        public void TestClass_ContextName_ReturnsNonDefaultValue()
        {
            var instance = new Class();
            Assert.That(instance.ContextName(), Is.Not.EqualTo("Default"));
        }

        [Test]
        public void TestClass_ContextNameFail_ReturnsDefault()
        {
            var instance = new Class();
            Assert.That(instance.ContextNameFail(), Is.EqualTo("Default"));
        }

        [Test]
        public void TestClass_ToString_ReturnsNonDefaultValue()
        {
            var instance = new Class();
            Assert.That(instance.ToString(), Is.Not.EqualTo("Default"));
        }
    }
}