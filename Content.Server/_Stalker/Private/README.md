# Private Directory
This directory **MUST** never be exposed in a public repository.
This directory stores the files which are sensitive for being exposed.
For example, this directory stores the sources for screengrab logic and response tracker which **HAD** to be hidden from the end player.

## How to handle repository change to public?
You need to remove all the files in this directory or move them to a private repository. You can use git filter-branch or BFG Repo-Cleaner to remove the files from the commit history.
If you still need this code you can hide it somewhere in a private repository and use SS14's ModLoader to load a separate solution's DLL.
