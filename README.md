#Runtime C# script engine for Unity3D

##Compile and execute C# code inside Unity3D scene at runtime!

UCompile is a system for compiling and executing strings with C# code inside Unity3D scenes at runtime. You can use it to allow players of your Unity3D game to modify your game with C# code, or as REPL engine, possibilities are restricted by only your imagination! For usage examples see <a href="#usage examples">usage examples chapter</a>. Works in editor and in build.

Tested in Unity3D on:
* Windows 8,8.1,10.

For now project works only on Windows, work in progress on OS X!

If you have found a bug, create an issue on the github page, or a pull request if you have a fix / extension. If you have a question, you can email me at soapcode24@gmail.com. 

##Main Features
* Compilation and execution of classless and methodless C# code
* C# classes compilation
* Control on namespaces exposed to the code to restrict its access of resources
* C# code compilation and execution in separate AppDomain to control memory consumption
* Coroutine compilation and execution
* Works in build as well as in editor
* Example project with console-like GUI interface to test it all out

##Installation

You can simply download/clone this repo and copy UCompile folder to your Unity3D project.

##How it works?
The main principle behind the scenes is simple: take string with C# code, compile it and produce Assembly representing this code, load it into current AppDomain. 

<img src="DocumentationMisc/CompilationScheme.png?raw=true" alt="compilation scheme" width="940px" height="454px"/>

Now this code is officially a part of your application, as long as AppDomain it is loaded into stays loaded. 

There are 2 ways of how UCompile allows you to interact with these assemblies:

1. Compiling classes. You can compile your custom classes, and make them a part of your Unity application. These classes will be able to use any functionality you've decided to expose to it, from assemblies, which are loaded right now in main Unity AppDomain, including assemblies with classes which you dynamically compiled with UCompile earlier.
2. Compiling and executing methodless code. You can compile plain methodless code, using the same exposure restriction system. Imagine that you put your code in some kind of a Main() method, that you can execute at any moment, and this code can use classes that you have already dynamically compiled with the first way.

It's going to make sense soon, I promise! Let's look at some examples:

1. Here let's create an empty scene, and add an empty GameObject to it with following script attached.

```csharp
using UnityEngine;
using UCompile;

public class NewBehaviourScript : MonoBehaviour {

	// Update is called once per frame
	void Update ()
    {
	
        if(Input.GetKeyDown(KeyCode.Space))
        {
            CSScriptEngine engine = new CSScriptEngine();

            engine.AddUsings("using UnityEngine;");

            IScript result = engine.CompileCode("GameObject.CreatePrimitive(PrimitiveType.Cube);");
            result.Execute();
        }

	}
}
```

On Space press, we will create a new instance of the main class you need to worry about in UCompile, CSScriptEngine, then, via AddUsings method, we'll expose UnityEngine namespace functionality to the code, that is to be compiled by CSScriptEngine. So

work in progress on readme
