# ThreadedNASZipper

## Table of Contents
- [Usage](#usage)
- [Configuration INI-File](#configuration)
- [Example INI-File](#example)


## Usage
Please copy the application to your NAS or local directory. Configure the INI file and simply start the application.
The application will search for all files in the source directory and then copy them to the destination directory (if no destination directory is specified, the application will copy them to the working directory).

## Configuration 

The INI file for the application contains settings for the file search, zipping, threading, and logging. In the [Search] section, you can specify the directories and file types to search for. The [Zip] section allows you to enable or disable zipping, set the compression level, specify the name and location of the output file, and set various other options. In the [Thread] section, you can set the maximum number of threads to use. Finally, in the [Logging] section, you can enable or disable logging. The comments in the file provide additional information on each setting.

## Example

```ini
[Search]
#Separate directories with a semicolon (;)
SourceDirectories = I:\FolderA;E:\FolderB
#Separate SearchPattern with a semicolon (;)
SearchPattern = *.jpg;*.mp4;*.txt
