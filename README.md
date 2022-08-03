# Folder Icon Creator
Utility that will change the folder icon for window, based on a folder.jpg file that is inside that folder. It will also hide metadata files that aren't related to a video file. Useful with programs like Emby, Kodi cleaning up folders.

```
You can modify the list of extensions that are flagged as media files and other filetypes you don't want to hide 
in the Config.ini File

Usage: Folder Icon Creator.exe [-d] [-r] [-c] [-h] [-l] [-p poster.jpg] -f Folder1 [Folder2] [...]
Usage: Folder Icon Creator.exe [-ch] -f Folder1 [Folder2] [...]

  -f, --folders    Required. Folders to process
  -p, --poster     (Default: folder.jpg) Filename for the Image file that will be used to create the Folder Icon
  -d, --delete     Deletes folders that don't have any media files or subfolders
  -c, --cleanup    Deletes the metadata files that don't have a related video file
  -h, --hide       Hides every file that isn't a video file (or subtitle)
  -l, --log        Creates a log on the Desktop
  -r, --force      Recreates icons from folders that already have a folder.ico
  --help           Display this help screen.
```
## Example
![](https://user-images.githubusercontent.com/11904426/49398126-70903500-f70b-11e8-9331-8547e333f993.jpg)

### Emby Plugin

I also included a plugin I did for myself for Emby a while ago. To install it just copy the `Emby.FolderIconCreator.dll` file that is in the folder and copy it in your `%appdata%\Emby-Server\plugins` and restart the server. Options are in the server interface. 

The plugins does some thing a little bit differently because of the architecture requirement for it (.netstandard 2.0). But it still uses win32 calls, so I wouldn't try on a Linux server.

That plugin will be launched whenever a poster image is downloaded or changed. In your Library make sure that you have enabled the option _Save artwork into media folders_, so that the poster files are saved next to the video files.

It support Season posters also that aren't placed in the season folder, but inside the root show folder. it will copy the corresponding poster inside the season folder.

It also as an option that the exe doesn't have and that is change the date of folder to the latest video file. So that way you can have a folder that contains an existing TV series and if you have your windows grouped by date whenever a new episode arrives that folder will pop to the top.

![TV](https://user-images.githubusercontent.com/11904426/182549766-39311300-05b4-472c-9c58-1be8681490c8.png)

[![GitHub All Releases](https://img.shields.io/github/downloads/maforget/Folder-Icon-Creator/total.svg)](https://github.com/maforget/Folder-Icon-Creator)
