# CScriptEz


## Introduction 
The purpose of this program is to allow users to use C# as a scripting language. What this means in practice is that they do not have to go through the process of compiling and managing an executable, instead they create a script file with extension .csez, and this extension is associated with the CScriptEz program. 
When you double click or run from the command line a .csez file it compiles the code, and runs it. 
This makes C# into a scripting language for basic system administrative tasks: like Powershell scripts but without the powershell complexity. 
Note that C# 9.0 introduced top level statements, however, this is quite different than CscriptEz. 
Again the purpose of CScriptEz is to prevent the user from having to manage the compile process and executables. 


## How It works 
Imagine we have the following script in MoveFiles.csez 
```
using System.File.IO;
 
var now = DateTime.Now; 
foreach(var file in Directory.Files(@"c:\LogFiles\*.log"))
{ 
    if((now - File.GetLastWriteTime(file)).TotalHours > 24) 
      File.Move(file, Path.Combine(@"c:\ArchiveFiles", Path.GetFilename(file)); 
} 
```

This is a simple C# program fragment that copies some files from a folder to an archive if they are more than 24 hours old. 
Since the file extension csez is associated with CScriptEz, double clicking on this will run CScriptEz and pass in the name of the script file above. 
CScriptEz will do the following: 
- Read the code 
- Do some transformations to make it a valid program 
- Compile it to an assembly in memory 
- Execute the code. 
- Gather any errors from the compile or exceptions thrown by the code and write to the console. 

Please notice this is not wrapped in a namespace, class or function. In this respect it is similar the C# 9.0’s top level statement, however, that feature is only for executables so will not work in our case. 

Instead, the algorithm is this: 
- Skip all initial comment lines 
- Copy all using statements 
- Insert template code like this: 

```
namespace CScriptEzExeNs { 
  public class CScriptEzExe { 
    void Main(string[] args) { 
```
- After this the body of code is copied, but six spaces of indent is added to each line. 
- Finally template code is added like this: 

```
    } 
  }
} 
```

- So the final code that the compiler actually saw would be:

```
using System.File.IO; 
namespace CScriptEzExeNs { 
  public class CScriptEzExe { 
     void Main(string[] args) { 
       var now = DateTime.Now; 
       foreach(var file in Directory.Files(@"c:\LifeFiles\*.log") 
       { 
          if((now - File.GetLastWriteTime(file)).TotalHours > 24)) 
            File.Move(file, Path.Combine(@"c:\ArchiveFiles", Path.GetFilename(file)); 
       } 
     } 
   } 
} 
```

- This is the most basic case. A few extensions will be given below. 


## Execution of Script 
Please note that compile and execution take place one immediately after the other. 

The script is executed immediately. Please note that any arguments to the command are passed in as a variable called args, as if there were a main program. So if the command is executed as: 
```
MoveFiles.csez arg1 arg2 
```

Then the args variable will be: 
```
string[] {“arg1”, “arg2”} 
```

If it is executed as: 
```
Cscriptez.exe MoveFiles.csez arg1 arg2 
```

It will also have args set the same way. 

Console output will write to standard output. However, there are some other options. 

If you wish the return a result code from the “program” you should use the Environment.Exit() function. 


## Comment Block Commands 
At the beginning of a script a block of comments can be included. These comments give extra commands to set how the script is to be executed. These commands are: 
```
// @library: library-filename 
// @console [auto-close] 
// @cache 
// @compile-errors: filename
```

In the first line the library-filename path is the path to a library that will be included in the reference of the compiled code. 

In the second, @console, the program will run and create a console into which the user can view and interact. It has an optional parameter auto-close. When this is set then the console will be automatically closed when the program finishes. Otherwise the console will remain open until the user closes it. 

The third line @cache helps with caching of the compiled code as descripted below. 

The fourth line allows what to do in the case of an error being found during compilation.

## Compile Errors
If the program fails to compile then the generated program will be printed out, and then the standard compiler errors shown. Note if this program is not run from a console a console will be created for that purpose.
However, if the @compile-errors is set in the initial comments then the filename specified will instead be the destination of the text of the program and the compiler errors. In this case the program will be preceded by the text:
```
Compile of xxx.csez failed at 1/2/22 10:45pm:
```

Where xxx.csez is replaced with the file’s name, and the time by the current time.


## Using Functions 
If you want to break your code up into functions this will be a problem since the code above is wrapped into one function and class. Consequently you can have this automatic code insertion turned off. To do this you simply specify your own function: 
```
using System.File.IO; 
void Main(string[] args) { 
  var now = DateTime.Now; 
  foreach(var file in Directory.Files(@"c:\LifeFiles\*.log") 
  { 
    if((now - File.GetLastWriteTime(file)).TotalHours > 24) 
    DoMove(file, Path.Combine(@"c:\ArchiveFiles", Path.GetFilename(file)); 
  } 
} 
void DoMove(string src, string dest) { 
  File.Move(src, dest); 
} 
```

Please note that the first function MUST be in the form “void Main(string[] args)” or “ void Main()”, anything else is invalid. This is transformed into: 
```
using System.File.IO; 
namespace CScriptEzExeNs { 
  public class CScriptEzExe { 
    void Main(string[] args) { 
      var now = DateTime.Now; 
 
      foreach(var file in Directory.Files(@"c:\LifeFiles\*.log") 
      { 
        if((now - File.GetLastWriteTime(file)).TotalHours > 24) 
          DoMove(file, Path.Combine(@"c:\ArchiveFiles", Path.GetFilename(file)); 
      } 
    }
    void DoMove(string src, string dest) { 
      File.Move(src, dest); 
    } 
  }
} 
```

So the actual algorithm is this: 
- Scan through the file looking for comments or blank lines. Process the comment `@` commands 
- Once you are passed comments, look for lines starting “using” and copy them into the code to be compiled. ср
- Keep scanning past all lines beginning with “using”, and once you hit the next line look for “^[\w]*void[\w\+]Main(“ as a regular expression ([\w] matches whitespace). If that is found copy all lines and insert in the second format. Otherwise copy all lines and insert in the first format. 
- Compile the code, and execute it if no compiler errors. 


## Caching the Results 
It is possible to cache the results of the compilation. This is invoked by the `@cache` comment. In this case a cache of the compiled library is saved in binary.
This is accomplished by having a hidden folder under the current user called CScriptEzCache. (The exact location is in accordance with typical placement of these types of folder in Windows.) 
In this folder is a Sqlite database containing the following tables:

**CsezFiles**

***Columns***
- **Id**			    (*The primary key*)										
- **Filename**          (*The full filename of the source file `the csez file`*)
- **ModifiedDate**	    (*The date the current file was last modified*)			
- **SourceHash**	    (*A hash of the source file*)							
- **BinaryFile**	    (*An foreign key into the BinaryFiles table*)			

**BinaryFiles**

***Columns***

- **Id**	(*The primary key*)
- **BinaryData**	(*The bytes of the compiled assembly*)

In this case when CScriptEz is run with a file called, for example: `c:\Users\john\documents\bin\my.csez` it does the following:

- Checks the database to see if there is a CsezFile with a matching filename.
- If there is a matching filename checks to see if the modified date is the same
- If the modified date is the same checks to see a hash of the file is the same
- If so, it loads the binary data from the linked BinaryFiles data and uses that as the assembly
- Otherwise it removes any matching records (cascading to BinaryFile) and compiles the file as normal.


## Clearing cache
It is possible to clear cached compilations. This can be invoked by calling CscriptEz with --clear-cache [FileName] command. 
If filename is given then the cached compilation will be cleared from the database for the specified file only.
If the --clear-cache command was invoked without a filename then all cached compilations will be removed from database.


## Clearing stale cache.
It is possible to clear cached compilations for all files where the original csez file were removed from the disk. 
To do this run CscriptEz with --clear-stale [FileName] command. 

If filename is given then cached compilation will be cleared from the database for the specified file only if the file doesn't exist on the disk.
If the --clear-stale command was invoked without the filename then the cache is searched for any entries that are for a csez file that no longer exists and is cleared for that entry.



## Using Nuget packages
If your script uses Nuget packages then the package should be specified with following directive in the top of the script:
```
//@nuget PACKAGE-NAME [VERSION] [ADDRESS]
```

[VERSION] is optional. If omitted then latest version will be used.

[ADDRESS] is optional. If omitted then standard Nuget package repository at address **https://api.nuget.org/v3/index.json** will be used.

```
Example:
//@nuget Newtonsoft.Json 13.0.1
```


## How to install the CScriptEz
First of all download the installer and install the software. (You can if you prefer build it directly from the source code too.)

[Installers are here](https://github.com/EngagingGear/CScriptEz/releases/tag/initial-release)

Once the application installed you may start using the CScriptEz from Windows Explorer:

Right click on the `csez` script file in Windows Explorer and select `Run CScriptEz File` option

Or you can integrate the CScriptEz into the command script

```
CScript.exe (SCRIPT FILE NAME).csez [arg1, arg2, ...]
```
