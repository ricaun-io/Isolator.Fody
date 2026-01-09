# Isolator.Fody 

[![Visual Studio 2022](https://img.shields.io/badge/Visual%20Studio-2022-blue)](https://github.com/ricaun-io/Isolator.Fody)
[![Nuke](https://img.shields.io/badge/Nuke-Build-blue)](https://nuke.build/)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/ricaun-io/Isolator.Fody/actions/workflows/Build.yml/badge.svg)](https://github.com/ricaun-io/Isolator.Fody/actions)
[![Release](https://img.shields.io/nuget/v/Isolator.Fody?logo=nuget&label=release&color=blue)](https://www.nuget.org/packages/Isolator.Fody)

### This is an add-in for [Fody](https://github.com/Fody/Home/)

This is a Fody add-in that isolate classes marked with the `[Isolator]` attribute by injection custom code to force the methods and constructor to run in a separate `AssemblyLoadContext`.

## How to use

Inside your project file, add the following lines to reference the `Isolator.Fody` package:

```xml
<ItemGroup>
	<PackageReference Include="Isolator.Fody" Version="*" IncludeAssets="build; compile" PrivateAssets="all" />
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

*The class and methods are modified and the `[CompilerGenerated]` attribute is added.*

```csharp
[CompilerGenerated]
public class MyIsolatedClass
{
	[CompilerGenerated]
	public MyIsolatedClass()
	{
		if (AssemblyLoader.IsDefault())
		{
			object obj = AssemblyLoader.CreateInstance(this, new object[0]);
			return;
		}
		_isolator_ctor();
	}
	[CompilerGenerated]
	public void MyMethod()
	{
		if (AssemblyLoader.IsDefault())
		{
			object[] args = new object[0];
			object obj = AssemblyLoader.InvokeMethod(this, "_isolator_1_MyMethod", args, BindingFlags.Instance | BindingFlags.NonPublic);
		}
		else
		{
			_isolator_1_MyMethod();
		}
	}
	[CompilerGenerated]
	private void _isolator_ctor()
	{
		Console.WriteLine("Constructor implementation");
	}
	[CompilerGenerated]
	private void _isolator_1_MyMethod()
	{
		Console.WriteLine("Method implementation");
	}
}
```

Every time a method or constructor of the isolated class is called, it checks if it is running in the default `AssemblyLoadContext`. If it is, it uses reflection to invoke the method or constructor in a separate context.

### AssemblyLoader

The `AssemblyLoader` class is injected into the assembly to handle the loading of the isolated classes into separate `AssemblyLoadContext` instances.

[Isolator.Template](./Isolator.Template) project contains the source code for the `AssemblyLoader` class in the [ILTemplate.cs](./Isolator.Template/ILTemplate.cs) file.

## Features

- [x] Support attribute `[Isolator]` to mark classes for isolation.
- [x] Support for isolating static classes and methods.
- [x] Support clone method to isolate internally called methods. (Clone to a unique name start with `_isolate_`)
- [x] Support multiple methods with ref/out parameters. *(There are some limitations when multiple methods with the same name.)*
- [x] Support isolate classes into different `AssemblyLoadContext` instances. (`[Isolator("ContextName")]`)
- [x] Support find existent `AssemblyLoadContext` and use to share a common context between different `Assembly`.
- [x] Support xml configuration for advanced settings. 
	- [x] Isolate all classes/interfaces by name. (`ClassNames` and `InterfaceNames`)
	- [x] Support context name with `{name}` and `{guid}` placeholders. (`ContextName`)

## Configuration Options

All config options are accessed by modifying the `Isolator` node in FodyWeavers.xml.

Default FodyWeavers.xml:

```xml
<Weavers>
	<Isolator />
</Weavers>
```

### ContextName

The name of the `AssemblyLoadContext` to use for isolation. If a context with this name already exists, it will be used. Otherwise, a new context will be created with this name.

*Defaults to an empty string, which creates a unique name with the assembly name and the assembly module guid separated with dot `"{name}.{guid}"`.*
```xml
<Isolator ContextName='MyContextName' />
```

This can be overridden on a per-class basis by specifying a context name in the `[Isolator("ContextName")]` attribute.

*The `{name}` will be replaced with the assembly name and the `{guid}` will be replaced with the assembly module guid at runtime.*

The `[Isolator("{name}.{guid}")]` is equivalent to the default context name in the current assembly.

### ClassNames

A list of class names to isolate. If a class name matches an entry in this list, it will be isolated even if it is not marked with the `[Isolator]` attribute.

Can take two forms.

As an element with items delimited by a newline.

```xml
<Isolator>
	<ClassNames>
		App
		AppDB
		Command
	</ClassNames>
</Isolator>
```

Or as an attribute with items delimited by a pipe `|`.

```xml
<Isolator ClassNames='App|AppDB|Command' />
```

### InterfaceNames

A list of interface names to isolate. If a interface name matches an entry in this list, it will be isolated even if it is not marked with the `[Isolator]` attribute.

Can take two forms.

As an element with items delimited by a newline.

```xml
<Isolator>
	<InterfaceNames>
		IExternalApplication
		IExternalDBApplication
		IExternalCommand
	</InterfaceNames>
</Isolator>
```

Or as an attribute with items delimited by a pipe `|`.

```xml
<Isolator InterfaceNames='IExternalApplication|IExternalDBApplication|IExternalCommand' />
```

## Debug Configuration Options

These options are used for debugging purposes and can be enabled or disabled as needed.

### EnableDebug

Indicates whether to enable debug logging for the isolator. When enabled, additional debug information will be logged in the `Debug` console.

*Defaults to `false`*
```xml
<Isolator EnableDebug='true' />
```

### SkipIsolator

Disable the isolator process without the need to remove the weaver from the project. This is useful for debugging purposes when you want to temporarily disable the isolator without modifying the project configuration.

*Defaults to `false`*
```xml
<Isolator SkipIsolator='true' />
```

*This force the `[Isolator]` to be removed, and the class is not isolated.*

### LoadAtModuleInit

Indicates whether to load the `AssemblyLoadContext` at module initialization. When enabled, the context will be created and attached when the module is initialized.

*Defaults to `true`*
```xml
<Isolator LoadAtModuleInit='false' />
```

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

Do you like this project? Please [star this project on GitHub](https://github.com/ricaun-io/Isolator.Fody/stargazers)!