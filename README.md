# Isolator.Fody 

[![Visual Studio 2022](https://img.shields.io/badge/Visual%20Studio-2022-blue)](../..)
[![Nuke](https://img.shields.io/badge/Nuke-Build-blue)](https://nuke.build/)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](../../actions/workflows/Build.yml/badge.svg)](../../actions)

### This is an add-in for [Fody](https://github.com/Fody/Home/)

This is a Fody add-in that isolates classes marked with the `[Isolator]` attribute by injection custom code to force the methods and constructor to run in a separate `AssemblyLoadContext`.

## How to use

Inside your project file, add the following lines to reference the `Isolator.Fody` package:

```xml
<ItemGroup>
	<PackageReference Include="Isolator.Fody" Version="*">
		<PrivateAssets>all</PrivateAssets>
		<IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
	</PackageReference>
</ItemGroup>
```

Then, add the following configuration to enable the weaver:
```xml
<PropertyGroup>
	<WeaverConfiguration>
		<Weavers>
			<Isolator />
		</Weavers>
	</WeaverConfiguration>
</PropertyGroup>
```

Next, mark the classes you want to isolate with the `[Isolator]` attribute:
```csharp
[Isolator]
public class MyIsolatedClass
{
	public MyIsolatedClass()
	{
		Console.WriteLine("Constructor implementation");
	}
	public void MyMethod()
	{
		Console.WriteLine("Method implementation");
	}
}
```

When you build your project, the weaver will modify the IL code of the marked classes to ensure that their methods and constructors run in a separate `AssemblyLoadContext`.
```csharp
public class MyIsolatedClass
{
	public MyIsolatedClass()
	{
		if (AssemblyLoader.IsDefault())
		{
			object obj = AssemblyLoader.CreateInstance(this, new object[0]);
			return;
		}
		_isolator_ctor();
	}

	public void MyMethod()
	{
		if (AssemblyLoader.IsDefault())
		{
			object data = AssemblyLoader.GetData(this);
			object[] parameters = new object[0];
			MethodInfo method = data.GetType().GetMethod("_isolator_MyMethod", BindingFlags.Instance | BindingFlags.Private);
			method.Invoke(data, parameters);
		}
		else
		{
			_isolator_MyMethod();
		}
	}

	private void _isolator_ctor()
	{
		Console.WriteLine("Constructor implementation");
	}

	private void _isolator_MyMethod()
	{
		Console.WriteLine("Method implementation");
	}
}
```

Every time a method or constructor of the isolated class is called, it checks if it is running in the default `AssemblyLoadContext`. If it is, it uses reflection to invoke the method or constructor in a separate context.

## Features

- [x] Support attribute `[Isolator]` to mark classes for isolation.
- [x] Support for isolating static classes and methods.
- [x] Support clone method to isolate internally called methods. (Clone to a unique name start with `_isolate_`)
- [x] Support multiple methods with ref/out parameters. (There are some limitations when multiple methods with the same name.)
- [x] Support xml configuration for advanced settings. 
	- [x] Isolate all classes/interfaces by name. (`ClassNames` and `InterfaceNames`)
- [x] Support context name.
	- [ ] Isolate classes into different `AssemblyLoadContext` instances.
	- [x] Find existent `AssemblyLoadContext` and use to share a common context between different `Assembly`.

## Configuration Options

All config options are accessed by modifying the `Isolator` node in FodyWeavers.xml.

Default FodyWeavers.xml:

```xml
<Weavers>
  <Isolator />
</Weavers>
```

### ClassNames

A list of class names to isolate. If a class name matches an entry in this list, it will be isolated even if it is not marked with the `[Isolator]` attribute.

Can take two forms.

As an element with items delimited by a newline.

```xml
<Isolator>
  <ClassNames>
    Foo
    Bar
  </ClassNames>
</Isolator>
```

Or as an attribute with items delimited by a pipe `|`.

```xml
<Isolator ClassNames='Foo|Bar' />
```

### InterfaceNames

A list of interface names to isolate. If a interface name matches an entry in this list, it will be isolated even if it is not marked with the `[Isolator]` attribute.

Can take two forms.

As an element with items delimited by a newline.

```xml
<Isolator>
  <InterfaceNames>
    Foo
    Bar
  </InterfaceNames>
</Isolator>
```

Or as an attribute with items delimited by a pipe `|`.

```xml
<Isolator InterfaceNames='Foo|Bar' />
```
### ContextName

The name of the `AssemblyLoadContext` to use for isolation. If a context with this name already exists, it will be used. Otherwise, a new context will be created with this name.

*Defaults to an empty string, which creates a unique name with the assembly name and random guid included.*
```xml
<Isolator ContextName='MyContextName' />
```
*The context name is created with the `IsolatorContext.` in front of it.*

### EnableCloneMethods

Indicates whether to clone methods that are called internally within the isolated class to ensure they also run in the isolated context.

*Defaults to `true`*

```xml
<Isolator EnableCloneMethods='false' />
```

### EnableWriteBackRefOutParameters

Indicates whether to write back the values of `ref` and `out` parameters to the original method after invoking the isolated method.

*Defaults to `true`*

```xml
<Isolator EnableWriteBackRefOutParameters='false' />
```

## References

This project use [Fody](https://github.com/Fody/Fody) and some of the base implementation was inspired by the [Costura.Fody](https://github.com/Fody/Costura).

## License

This project is [licensed](LICENSE) under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).

---

Do you like this project? Please [star this project on GitHub](../../stargazers)!