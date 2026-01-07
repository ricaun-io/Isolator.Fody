
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(ClassStatic.ContextName());
        
        TestContextName();
        TestClass();
        TestClassStatic();
        TestClassConstructor();
        TestClassWithConstructor();

        var instance = new ClassIsolatorTest();
        Debug.Assert(instance.Execute());
        Debug.Assert(instance.ExecuteClass());
    }

    private static void TestContextName()
    {
        // Verify that the context name starts with "IsolatorContext"
        var contextName = ClassStatic.ContextName();
        Debug.Assert(contextName.StartsWith("IsolatorContext"));

        // Verify that the context name ends with a valid GUID
        var guidString = contextName.Split('.').LastOrDefault();
        Debug.Assert(Guid.TryParse(guidString, out Guid guid));

        var moduleVersionId = typeof(Program).Assembly.ManifestModule.ModuleVersionId;
        Debug.Assert(moduleVersionId == guid);
    }

    private static void TestClass()
    {
        var instance = new Class();
        Debug.Assert(instance.Execute());
        Debug.Assert(instance.Execute(true));
        Debug.Assert(instance.Execute("Test"));
        Debug.Assert(instance.Execute(123));
        string outResult;
        Debug.Assert(instance.ExecuteOut("TestOut", out outResult) && outResult == "TestOut");
        string refResult = "Initial";
        Debug.Assert(instance.ExecuteRef("TestRef", ref refResult) && refResult == "TestRef");

        Debug.Assert(instance.ExecuteDefault());
        Debug.Assert(instance.ExecuteDefault("Test"));
        Debug.Assert(instance.ExecuteDefault(number: 123));
        Debug.Assert(instance.ExecuteDefault("Test", 123));

        // Check the ContextName
        Debug.Assert(instance.ExecuteContextName() != "Default");
        Debug.Assert(instance.ContextName() != "Default");
        Debug.Assert(instance.ContextNameFail() == "Default");

        // Check the ContextName via ToString override
        Debug.Assert(instance.ToString() != "Default");
    }

    private static void TestClassStatic()
    {
        Debug.Assert(ClassStatic.Execute());
        Debug.Assert(ClassStatic.Execute(true));
        Debug.Assert(ClassStatic.Execute("Test"));
        Debug.Assert(ClassStatic.Execute(123));
        string outResult;
        Debug.Assert(ClassStatic.ExecuteOut("TestOut", out outResult) && outResult == "TestOut");
        string refResult = "Initial";
        Debug.Assert(ClassStatic.ExecuteRef("TestRef", ref refResult) && refResult == "TestRef");

        // Check the ContextName
        Debug.Assert(ClassStatic.ContextName() != "Default");
        Debug.Assert(ClassStatic.ContextNameFail() == "Default");
    }

    private static void TestClassConstructor()
    {
        var instanceFail = new ClassConstructor();
        Debug.Assert(!instanceFail.Execute());

        var instancePass = new ClassConstructor("ValidName");
        Debug.Assert(instancePass.Execute());

        Debug.Assert(instancePass.Execute(10) == 10);
        Debug.Assert(instancePass.Execute(10, 20) == 30);
    }

    private static void TestClassWithConstructor()
    {
        // Test static constructor
        Debug.Assert(ClassWithStaticConstructor.Execute());

        // Test class with abstract constructor
        var classAbstract = new ClassWithAbstractionConstructor();
        Debug.Assert(classAbstract.Execute());

        // Test class with public constructor
        var classPublic = new ClassWithPublicConstructor();
        Debug.Assert(classPublic.Execute());

        // Test if we can access private constructor
        var classPrivate = (Activator.CreateInstance(typeof(ClassWithPrivateConstructor), true) as ClassWithPrivateConstructor);
        Debug.Assert(classPrivate.Execute());
    }
}
