# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.0] / 2026-04-29
### Features
### Updates
- Add `BaseModuleWeaverImportExtension` to import `Type` and `Method` to fix issue with `dotnet build`. 
- Rename `Isolator.ConsoleApp` to `Isolator.Fody.ConsoleApp`.
### Tests
- Add `Isolator.Fody.ConsoleApp.Tests` to `dotnet build` and run sample.

## [1.1.0] / 2026-01-19 - 2025-01-20
### Features
- Support ignore isolator for specific class using attribute `[Isolator(" ")]`.
- Support copy properties in `BaseType` before and after execute methods. (Fix: #4)
- Support dynamic or in-memory assembly by disabling isolator to avoid infinite loops. (Fix: #2)
### Fixes
- Fix issue with methods inside abstract class. (Fix: #3)
### Updates
- Add `GetFirstInstructionAfterBaseConstructor` to inject after base constructor. (Fix: #3)
- Ignore isolator in abstract classes without `[Isolator]` attribute. (Fix: #3)
### Tests
- Add `AssemblyLoadContextTests` with `MemoryAssemblyLoadContext` tests.

## [1.0.0] / 2025-12-29 - 2025-01-09
### Features
- Support attribute `[Isolator]` to mark classes for isolation.
- Support attribute `[Isolator("ContextName")]` to isolate a specific context.
- Support `{name}` and `{guid}` placeholders in context name to be replaced by assembly name and assembly module guid.
- Support for isolating static classes and methods.
- Support clone method to isolate internally called methods. (Clone to a unique name start with `_isolate_`)
- Support multiple methods with ref/out parameters.
- Support configuration to isolate class or interface by name. (`ClassNames` and `InterfaceNames`)
### Updates
- Unique method name does not need Types parameters to identify method.
- Update `Configuration` class to support future xml configuration.
- Update to find interface names inside base class.
- Update to clone all methods to try to isolate internally called methods. (`EnableCloneMethods`)
- Update to search `ContextName` and try to use existent `AssemblyLoadContext`. (`ContextName`)
- Update `ILTemplate` to use `AssemblyLoader.InvokeMethod` to invoke isolated method.
- Update `IsolatorAssemblyLoadContext` to support multiple resolvers and search in all resolvers.
- Update `Isolator.ConsoleApp` and remove random class.
- Update clone method to use `CompilerGenerated` attribute.
- Update to add `CompilerGenerated` attribute in the methods and type using `AddCompilerGeneratedAttribute`.
- Update `AssemblyLoaderImporter.` to copy if method `HasDefault` parameter.
- Update `Common` with `Log` method with `DEBUG` level.
- Update `EnableDebug` to remove `Log` in the `ILTemplate` when disabled.
- Update `EnableInjectDebug` to inject debug information inside the clone and inject methods.
- Update `ILTemplate` to find context using context name.
- Update `ILTemplate` with method to `SetContextName` to enable changing context name by code.
- Update `IsolatorAttribute` with context name constructor.
- Update `IsolatorExecute` with logic to find attribute context name and inject in methods.
- Update `ILTemplate` to support `{name}` and `{guid}` placeholders in context name.
- Update `IsolatorAvailable` to run `IsolatorExecuteRemoveAttributes` to remove attributes when available.
- Update `Configuration` with `SkipIsolator` to skip isolator for specific assemblies.
- Update `SkipIsolator` to show warning log when assembly is skipped.
- Create `Isolator.Template.Debug` project to hold debug with `DEBUG` level logs.
- Update `AssemblyLoaderImporter` to select `debug` or `release` version of `Isolator.Template` based on `EnableDebug` setting.
- Update `Build` to release in `Nuget`.
- Create `Isolator.Fody.Tests` project to test `Isolator.ConsoleApp` and `Isolator.Fody` functionality.
- Update `Configuration` to use `LoadAtModuleInit` and force `Attach` to create default `AssemblyLoadContext` at module init.

[vNext]: ../../compare/1.0.0...HEAD
[1.2.0]: ../../compare/1.1.0...1.2.0
[1.1.0]: ../../compare/1.0.0...1.1.0
[1.0.0]: ../../compare/1.0.0