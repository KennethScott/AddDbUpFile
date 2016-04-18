# Add DbUp File


Shamelessly tweaked and stripped down version of Mads Kristensen's excellent AddAnyFile extension. It has one simple purpose - to provide a way 
for easily adding new dbup sql files to any project. Simply hit Shift+F2 to create an empty script file in the selected folder or in the same folder 
as the selected file.  

See the [changelog](CHANGELOG.md) for updates and roadmap.

### Features

- Files are automatically prefixed with ccyymmdd_hhmmss_
- Create deeper folder structures easily if required
- Create folders when the entered name ends with a /
- Marks new files as embedded resources by default

![Add new file dialog](art/dialog.png)

### Show the dialog

A new button is added to the context menu in Solution Explorer.

![Add new file dialog](art/menu.png)

You can either click that button or use the keybord shortcut **Shift+F2**.

### Create folders

Create additional folders for your file by using forward-slash to
specify the structure.

For example, by typing **scripts/test.sql** in the dialog, the
folder **scripts** is created if it doesn't exist and the file
**(prefix)_test.sql** is then placed into it.

## Contribute
Check out the [contribution guidelines](.github/CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)