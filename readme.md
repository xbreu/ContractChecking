The code for this plugin comes from [a test file](https://github.com/dafny-lang/dafny/blob/master/Source/DafnyLanguageServer.Test/_plugins/PluginsTest.cs) for Dafny's language server.

To compile the plugin, run `make` on the Plugin folder, you should have the following structure:  
├ Plugin repository folder  
└ dafny repository folder  

You also need the following tools:
- .NET 6.0
- Java Development Kit
- Pythonnet

After compiling the code, a `Plugin.dll` file should appear. Add it as a plugin by adding the following to `Dafny:Language Server Launch Args` in VSCode:
```--plugin:/absolute/path/to/Plugin.dll```
<!-- --plugin:/home/me/document/prodei/plugin/Plugin.dll -->