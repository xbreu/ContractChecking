# Contract Repair for Dafny

## Dependencies

To compile the plugin, run `make` on the Plugin folder, you should have the following structure:  
├ Plugin repository folder
├ Python.NET repository folder (from [pythonnet/pythonnet](https://github.com/pythonnet/pythonnet))
└ dafny-custom repository folder (from [xbreu/dafny](https://github.com/xbreu/dafny))

You can get that by running `make setup`, the other repositories will be cloned in the right place.

You also need the following tools:
- .NET 6.0
- Java Development Kit
- Pythonnet

## Absolute Path Configurations

Change the absolute paths in the following lines to your own:
- [Line 19 and 21 of PluginAddComment.cs](PluginAddComment.cs#19)
- [Line 253 of FixGenerator.cs](Fixes/FixGeneration.cs#253) with the Daikon path

After compiling the code, a `Plugin.dll` file should appear. Add it as a plugin by adding the following to `Dafny:Language Server Launch Args` in VSCode:
```--plugin:/absolute/path/to/Plugin.dll```
<!-- --plugin:/home/me/document/prodei/plugin/Plugin.dll -->
