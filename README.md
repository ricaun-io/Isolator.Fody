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
	public void MyMethod()
	{
		// Method implementation
	}
}
```

When you build your project, the weaver will modify the IL code of the marked classes to ensure that their methods and constructors run in a separate `AssemblyLoadContext`.
```csharp
public class MyIsolatedClass
{
	public void MyMethod()
	{
		if (AssemblyLoader.IsDefault())
		{
			object data = AssemblyLoader.GetData(this);
			MethodInfo method = data.GetType().GetMethod("MyMethod", BindingFlags.Instance | BindingFlags.Public);
			object[] parameters = new object[0];
			method.Invoke(data, parameters);
		}
		else
		{
			Console.WriteLine("Method implementation");
		}
	}

	public MyIsolatedClass()
	{
		if (AssemblyLoader.IsDefault())
		{
			object obj = AssemblyLoader.CreateInstance(this, new object[0]);
		}
	}
}
```

Every time a method or constructor of the isolated class is called, it checks if it is running in the default `AssemblyLoadContext`. If it is, it uses reflection to invoke the method or constructor in a separate context.

## Features

- [x] Support attribute `[Isolator]` to mark classes for isolation.
- [x] Support for isolating static classes and methods.
- [ ] Support multiple methods with ref/out parameters
- [ ] Support xml configuration for advanced settings. 
	- [ ] Isolate all classes/interfaces by name.
- [ ] Support context name.
	- [ ] Isolate classes into different `AssemblyLoadContext` instances.
	- [ ] Find existent `AssemblyLoadContext` and use to share a common context between different `Assembly`.

## References

This project use [Fody](https://github.com/Fody/Fody) and some of the base implementation was inspired by the [Costura.Fody](https://github.com/Fody/Costura).

## License

This project is [licensed](LICENSE) under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).

---

Do you like this project? Please [star this project on GitHub](../../stargazers)!