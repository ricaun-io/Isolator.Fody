# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] / 2025-12-29
### Features
- Support attribute `[Isolator]` to mark classes for isolation.
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

[vNext]: ../../compare/1.0.0...HEAD
[1.0.0]: ../../compare/1.0.0