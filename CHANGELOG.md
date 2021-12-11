
# Changelog

## 2.0
**2021-12-08**
- [x] Followed Mads lead and dropped support for x86/pre-VS2022.  The old x86 version will remain on the original Marketplace page, but there will
be a new version published under "Add DbUp File (64-bit)".
- [x] Minor update to sql.txt template regarding exception handling that is more suitable for SQL2012 and higher.

## 1.7
**2019-06-02**

- [x] Added support for Visual Studio 2019.  Also switched to nuget packages following Mads original code.


## 1.6
**2017-03-07**

- [x] Added support for Visual Studio 2017

## 1.5
**2016-04-23**

- [x] Fixed bug where cursor positioning in sql template wasn't working

## 1.4
**2016-04-22**

- [x] Fixed bug where sql files weren't being marked as embedded resource by default

## 1.3
**2016-04-22**

- [x] Fixed bug where extension dropdown didn't move as the dialog grew (when typing long filenames)

## 1.2
**2016-04-21**

- [x] Fixed bug with editing text causing cursor to jump to end
- [x] Added dropdown for file extensions
- [x] Reenabled ability to create folders via forward-slash
- [x] Updated sql template

## 1.0
**2016-04-17**

- [x] Shamelessly gutted and tweaked the awesome AddAnyFile extension from Mads into AddDbUpFile to work specifically with DbUp sql scripts.  Extension 
now automatically forces a date/time stamp on the front of the script filenames and gives an option to mark as an embedded resource.

