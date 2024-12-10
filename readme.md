- Scans files in a folder fort text fast and efficiently.
## Options:
- Folder (First prompt): Folder to scan (scans subfolders)
- Text (Second prompt): Text to find in folder
- Yes/No (Third prompt): Counts amount of files (Disable when searching massive quantities of files)
- Yes/No (Fourth prompt): Shows file in explorer when found (Disable when expecting a large amount of found items)

# Developer refrence
Program.old is the unmaintained non-threaded version, I am keeping it incase if ever needed (Im a horder)<br>
The threaded version, while yes is much faster, makes Antimalware Service Executable go crazy due to how fast its scanning through files. Also prob will take a ton of your CPU up