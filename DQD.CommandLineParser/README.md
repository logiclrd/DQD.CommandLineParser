# DQD.CommandLineParser

Parses command lines. :-)

## How To Use

Step 1: Instantiate `CommandLine`.

Step 2: Ask it for your arguments type:

```
var args = new CommandLine().Parse<ArgsType>();
```

It finds the command-line of the current process automatically.

## How To Make Arguments Type

In the world of DQD.CommandLineParser, there are two kinds of thing that can be found on the command-line:

* Switches:

    Switches do not have values. The simplest switch is a `bool` value: Is it there, or is it not? Switches can also be `int` values, in which case the number of occurrences is counted.

    ```
    public class Arguments
    {
      [Switch] public bool Help;

      [Switch("/Horse")] public bool IncludeEquine;

      [Switch("/Debug")] public int DebugLevel;
    }
    ```

* Arguments:

   Arguments collect values from the command-line. The simplest argument has a `string` value. DQD.CommandLineParser will try to coerce to other types as needed.

    ```
    public class Arguments
    {
      [Argument] public string FileToProcess;
      [Argument("/OUT")] public string OutputFilePath;
      [Argument] public int TimeoutSeconds; // E.g.: /TimeoutSeconds 10
    }
    ```

    Arguments can also populate members of a structure.
    ```
    public class Point3D
    {
      public double X, Y, Z;
    }

    public class Arguments
    {
      // E.g.: /CameraPosition 0 -100 6.5
      [Argument(Properties = [ "X", "Y", "Z" ])] public Point3D CameraPosition;
    }
    ```

    Arguments can also be "floating", which means they do not have an associated switch. For instance, if you want the output filename to be specified by any argument that isn't introduced with a switch:

    ```
    // E.g.: /InputFile A.txt /InputFile B.txt /GronkulationLevel 42 OutputFile.txt
    public class Arguments
    {
      [Argument] public int GronkulationLevel;
      [Argument] public List<string> InputFile;
      [Argument(IsFloating = true)] public string OutputFile;
    }
    ```

    Arguments also have an `IsRequired` property to do with as you see fit. Exceptions may be involved.

## Other Sources Of Arguments

By default, arguments come from the current process's `argv`. You can override this, though, and parse arbitrary sequences.

* The `Parse` method can be supplied an `IEnumerable<string>`.
* The `CommandLine` object can persist an `IEnumerable<string>` to use in place of the default argument list. This is done by calling the `SetArgumentData` method.

## Autocompletion

Most shells have autocompletion, where if you type part of a filename or keyword and press Tab, the rest is filled in for you. Some shells allow you to hook into and customize this functionality.

DQD.CommandLineParser supports doing this for you automatically in PowerShell and Bash.

To use this in your own program, you must define two arguments in your args type:

```
		[Completer("--complete")]
		public string? Complete;
		[RegisterCompleter("--registercompleter", CommandName = "DQD.CommandLineParser.Sample")]
		public ShellType RegisterForShell;
```

The members can be fields or properties, and they can be named whatever you want. The exact switches can be whatever you want as well. They simply have to exist.

With these defined, running your application and using the switch supplied to `RegisterCompleter` will emit the shell code that needs to be sourced to complete registration.

The completion assumes that you will be using the stub executable that provides a direct binary for executing your application. This is a file called e.g. `MyProject.exe`, or simply `MyProject` on UNIX systems, in the build output directory.

The only constraint is that your application must not do anything significant prior to its call to `CommandLine.Parse<T>`. This is because invocations of the application are used to obtain completion options as the command-line is being built up.
