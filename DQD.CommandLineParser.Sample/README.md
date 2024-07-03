# DQD.CommandLineParser.Sample

This sample project was created for testing out (and debugging) custom command completion. To use this sample:

1. Build the project.
2. Place the directory containing the binary `DQD.CommandLineParser.Sample.exe` (`DQD.CommandLineParser.Sample` on Linux) into the PATH environment variable. This directory is typically something like `bin/Debug/net8.0` off of the main project directory.
3. Run the tool with the command-line argument `--registercompleter`. This argument takes the name of the shell you want to register with. At the time of this writing, this can be `PowerShell` or `bash`.

```
logiclrd@visor:/code/DQD.CommandLineParser/DQD.CommandLineParser.Sample$ export PATH=$PATH:/code/DQD.CommandLineParser/DQD.CommandLineParser.Sample/bin/Debug/net8.0
logiclrd@visor:/code/DQD.CommandLineParser/DQD.CommandLineParser.Sample$ DQD.CommandLineParser.Sample --registercompleter PowerShell
Register-ArgumentCompleter -CommandName "DQD.CommandLineParser.Sample" -Native -ScriptBlock {
  param($wordToComplete, $commandAst, $cursorPosition)
  DQD.CommandLineParser.Sample --complete "$wordToComplete" $($commandAst.ToString().Substring(0, $cursorPosition - $wordToComplete.Length - 1))
}

logiclrd@visor:/code/DQD.CommandLineParser/DQD.CommandLineParser.Sample$ DQD.CommandLineParser.Sample --registercompleter bash
_3e7f6a0caea6480688594bea2e7f985d()
{
  if [ $1 = "DQD.CommandLineParser.Sample" ]
  then
    COMPREPLY=()
    for option in $(DQD.CommandLineParser.Sample --complete "$2" "$3")
    do
      COMPREPLY+=("$option")
    done
  fi
}

complete -D -F _3e7f6a0caea6480688594bea2e7f985d

logiclrd@visor:/code/DQD.CommandLineParser/DQD.CommandLineParser.Sample$ 
```

4. In the shell of your choice, run the corresponding registration code. This code could be made part of a `.bashrc` type file to run automatically each time the shell starts.

After this registration is complete, you can now start using the completion. Try the following:

```
DQD.CommandLineParser.Sample <tab>
DQD.CommandLineParser.Sample --<tab><tab>
DQD.CommandLineParser.Sample --in<tab>
DQD.CommandLineParser.Sample --inputfile <tab><tab>
DQD.CommandLineParser.Sample --inputFile Pr<tab>
DQD.CommandLineParser.Sample --inputfile Program.cs <tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --<tab><tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --o<tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --outputdirectory <tab><tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --outputdirectory b<tab><tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --outputdirectory bin <tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --outputdirectory bin --transform <tab><tab>
DQD.CommandLineParser.Sample --inputfile Program.cs --outputdirectory bin --transform R<tab>
```

## Using in your own program

To use this in your own program, you must define two arguments in your args type:

```
		[Completer("--complete")]
		public string? Complete;
		[RegisterCompleter("--registercompleter", CommandName = "DQD.CommandLineParser.Sample")]
		public ShellType RegisterForShell;
```

The members can be fields or properties, and they can be named whatever you want. The exact switches can be whatever you want as well.

With these defined, using the switch supplied to `RegisterCompleter` will emit the shell code that needs to be sourced to complete registration.

The only constraint is that your application must not do anything significant prior to its call to `CommandLine.Parse<T>`. This is because invocations of the application are used to obtain completion options as the command-line is being built up.

And that's it :-)