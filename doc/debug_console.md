The Debug Console
=================

The Debug Console is a feature that allows developers and testers to read the Godot
console output in-game and execute custom commands. The console can be opened
with the default hotkey `F10`.

Custom commands are a way for developers to add methods that can be invoked from
the in-game debug console. This is a way to define custom behaviour from just
submitting commands during the gameplay, and speed up both development and testing.

This feature is to be considered experimental and may be subject to change. New
features will be added in future.

## How to implement custom commands
Programmers may want to implement a custom command to quickly test some feature,
or introduce a new cheat. Defining a new command is as simple as defining a
static method with a special attribute:

```cs
[Command("test", isCheat, "this is a command")]
private static void TestCommand()
{
    GD.Print("Hello, Console!");
}
```

The attribute parameters are, in the same order as the example, the command name
used in the console, a bool flag to determine if the command invocation is a
cheat or not, and an help string that is displayed when the user executes the
built-in `help` command.

The command will be automatically registered when Thrive starts up, and can be
executed in the console by writing the command name, which in this case would be
`test` with output `Hello, Console!`. The command name is case insensitive.

Please note that the method **must** be static, or it will be ignored during the
registration. See **Multicast Commands** below to learn how to use commands on
instance methods.

During the registration, the visibility modifier is ignored, but it's recommended
to keep the method private, as it would be good practice to not invoke the command
method from a place different from the console. The method name also doesn't
matter, but I recommend using the `NameCommand()` style.

### Custom parameters

The command can take parameters:
```cs
[Command("test", isCheat, "this is a command")]
private static void TestCommand(int foo, float bar, string baz)
{
    GD.Print($"Foo is {foo}, bar is {bar} and the baz message is {baz}");
}
```
Then the command must be executed in the console with the same number and type
of the parameters specified by the method signature. In this case the a correct
execution of this command from the console is:
```
> test 123 4.567 "Hello, world!"

Foo is 123, bar is 4.567 and the baz message is Hello, World!
```
As you may have noticed, strings must be delimited by quotes, and you can use
escape characters in it.

Commands can be overloaded, meaning you can define multiple different commands
with the same name, provided that the signature differs in the number or types
of the parameters. So you are allowed to register the two `test` commands of
the previous examples together.

### Special parameters

Commands also support some special parameters:

#### Enums

If you have an `enum`:
```cs
enum ExampleEnum
{
    Foo,
    Bar,
    Baz,
    Qux,
}
```
you can define a command as:
```cs
[Command("test", isCheat, "this is a command")]
private static void TestCommand(ExampleEnum value)
{
    GD.Print($"The chosen value is {value}.");
}
```
then you can execute the command as `test qux`. Note that the enum parameter
input in the command is case insensitive.
Example:
```
> test qux

The chosen value is Qux.
```
#### CommandContext
**`CommandContext`** is another special parameter you **must define as first parameter,
if used,** in the method signature which exposes basic console function to the
method body. These include console clearing and printing custom messages. You
can take a look at the available methods in the `CommandContext` class to get
more info. This is an example of usage:
```cs
[Command("test", isCheat, "this is a command")]
private static void TestCommand(CommandContext context, int foo, float bar, string baz)
{
    context.Print($"Foo is {foo}, bar is {bar} and the baz message is {baz}");
}
```

### Optional parameters

Optional parameters are now supported in the command signature. This is an
example from a command implemented in the game:
```cs
private static void CommandManageHistory(CommandContext context, 
	HistoryCommandMode mode = HistoryCommandMode.Show,
	string attribute = "")
{
    // Some logic
}
```

You can invoke this command from the console with or without the optional
arguments, with the interpreter automatically filling in the default values.
Please note that it's not (yet) possible to declare specific optional parameters,
and the order must be respected. For example, you can't specify the `attribute`
parameter in the command without first specifying `mode` first.

### Return value

Commands support a `bool` return value to determine success (if `true`) or failure.
If you use this return value, a `Success` or `Failure` message will be logged
in the console.
Other return values are silently unsupported, so different return values will
not influence the execution of the command. Therefore, if the command doesn't
have to return a success feedback we just recommend to use void.

## Multicast commands

**Multicast commands** are a special kind of commands that can be registered on top of
non-static methods in order to multicast the method invocation in all *the registered*
instances.

To register a command as multicast you have to use the `MulticastCommand` attribute,
which takes the same parameters as the `Command` attribute. These methods aren't
registered automatically for every instance of the class in which they are declared,
but you have to *register* the current instance with
`CommandRegistry.Instance.TryRegisterMulticastCommandListener`. This adds the object
instance to the registry, so that every time the multicast command is executed in the
console it can call the instance method. Likewise, instances can be *unregistered* with
`CommandRegistry.Instance.TryUnregisterMulticastCommandListener`.

### Safety measures

Multicast commands are inherently more dangerous than regular commands, as they
dispatch invocations to every registered object. If one were to register an undefined
big number of instances, on the command execution Thrive could hang or crash. To solve
this problem there are two major solutions:

- An **enforced instance number cap**, which is used by the Command Registry to
*automatically disable* or limit the number of invocations when the multicast command
is executed;
- Define a singleton object that tracks the instances and defers the invocations not
to run on the main thread, if possible. In this case, a regular static command can be
used instead.

The instance number cap specified above has a default limit of 32, but it can be
modified to be lower or higher by defining a `MulticastAllowedInstancesAttribute` on
top of the class containing the multicast commands. This attribute takes two arguments:

- `maxAllowedRegisteredInstances`, which is the maximum number of allowed registered
instances for the execution of this command. It is recommended this is kept as low as
possible to prevent too many invocations.
- `failOnTooManyInstances`, a `bool` that determines whether the command execution should
fail for **all** instances if the instance cap is reached. This is `true` by default.