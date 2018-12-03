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
![]()
