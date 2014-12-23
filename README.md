CRMTestability
================

Helper classes for Dynamics CRM 2011 onwards.

`PluginTestContext`

Provides helper and extension methods to make standing up plugin dependencies in unit and acceptance tests a lot cleaner.  Although this class makes our testing lives a little easier, you should always try and minimise the amount of logic in plugin classes.  Break out this logic into reusable classes that take minimal dependencies on the CRM plugin types.

`PluginExtensions`

Provides extension methods to `IPluginExecutionContext` for more easily reading the common data off the IPluginExecutionContext and a much cleaner API for resolving IServiceProvider references.

I'd recommend avoiding the use of the the Visual Studio Developer Toolkit Plugin base class.  The hardcoding of the plugin steps can lead to issues and the implementation of the overriden Execute method makes the classes completely untestable without going through the pain of defining the InternalsVisibleToAttribute. 
