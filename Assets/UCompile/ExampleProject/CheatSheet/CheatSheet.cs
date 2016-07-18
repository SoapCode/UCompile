using UnityEngine;
using UCompile;
using System.Collections;
using System;

//This namespace is for demonstration of how usings management system in CSScriptEngine
//can allow you to restrict access of resources to dynamic code
namespace SomeCustomNamespace
{

    public class HeyPrinter
    {
        public void PrintHey()
        {
            Debug.Log("Hey!");
        }
    }

}

public class CheatSheet : MonoBehaviour {

    #region CSScriptEngine examples

    //-------------------------------------------------------------------------------------------------

    //Compile simple methodless code
    void CompileCode()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddUsings("using UnityEngine;");

        IScript result = engine.CompileCode(@"Debug.Log(""Hey!"");");

        result.Execute();
        
    }

    //-------------------------------------------------------------------------------------------------

    //Compile custom type, and then invoke it's method via CompileCode
    void CompileType()
    {

        CSScriptEngine engine = new CSScriptEngine();

        engine.AddUsings("using UnityEngine;");

        string typeCode = @"
                
                                public class SimpleType
                                {
                                    public void PrintHey()
                                    {
                                        Debug.Log(""Hey!"");
                                    }
                                }

                              ";

        engine.CompileType("SimpleType", typeCode);

        IScript result = engine.CompileCode(@"
                                                 SimpleType sm = new SimpleType();sm.PrintHey();
                                               ");

        result.Execute();
    }

    //-------------------------------------------------------------------------------------------------

    //Compile custom type, and then compile another type, using previously compiled type in its code, and then
    //execute last compiled types method via CompileCode
    void CompileMultipleTypes()
    {

        CSScriptEngine engine = new CSScriptEngine();

        engine.AddUsings("using UnityEngine;");

        string typeCode = @"
                
                                public class SimpleType
                                {
                                    public void PrintHey()
                                    {
                                        Debug.Log(""Hey!"");
                                    }
                                }

                              ";

        engine.CompileType("SimpleType", typeCode);

        string anotherTypeCode = @"
                
                                public class AnotherSimpleType
                                {
                                    public void InvokeSimpleTypesPrintHey()
                                    {
                                        Debug.Log(""Greetings from AnotherSimpleType! Invoking SimpleTypes PrintHey method..."");
                                        SimpleType sm = new SimpleType(); sm.PrintHey();
                                    }
                                }

                              ";

        engine.CompileType("AnotherSimpleType", anotherTypeCode);

        IScript result = engine.CompileCode(@"
                                                 AnotherSimpleType sm = new AnotherSimpleType();sm.InvokeSimpleTypesPrintHey();
                                               ");

        result.Execute();
    }

    //-------------------------------------------------------------------------------------------------

    //Here we determine which method is going to automatically be invoked when compilation ends with errors
    void AddCompilationFailedHandler()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);

        engine.CompileCode("This will result in error");

        engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);
    }

    //Method of type Action<CompilerOutput> that will be executed on failed compilation(with errors)
    //Outputs errors occured during compilation to editor console
    public void OnCompilationFailedAction(CompilerOutput output)
    {
        for (int i = 0; i < output.Errors.Count; i++)
            Debug.LogError(output.Errors[i]);
        for (int i = 0; i < output.Warnings.Count; i++)
            Debug.LogWarning(output.Warnings[i]);
    }

    //-------------------------------------------------------------------------------------------------

    //Here we determine which method is going to automatically be invoked when compilation ends without errors
    void AddCompilationSucceededHandler()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddOnCompilationSucceededHandler(OnCompilationSucceededAction);

        engine.CompileCode("string warningCauser = \"This will result in warning, but compillation succeeds\";");

        engine.RemoveOnCompilationSucceededHandler(OnCompilationSucceededAction);
    }

    //Method of type Action<CompilerOutput> that will be executed on succesfull compilation(without errors)
    //Outputs warnings occured during compilation to editor console
    public void OnCompilationSucceededAction(CompilerOutput output)
    {
        for (int i = 0; i < output.Warnings.Count; i++)
            Debug.LogWarning(output.Warnings[i]);
    }

    //-------------------------------------------------------------------------------------------------

    //Remove compiled type from system, now this type is inaccessible to dynamic code
    void RemoveType()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddUsings("using UnityEngine;");

        engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);

        string typeCode = @"
                
                                public class SimpleType
                                {
                                    public void PrintHey()
                                    {
                                        Debug.Log(""Hey!"");
                                    }
                                }

                              ";

        engine.CompileType("SimpleType", typeCode);

        string anotherTypeCode = @"
                
                                public class AnotherSimpleType
                                {
                                    public void InvokeSimpleTypesPrintHey()
                                    {
                                        Debug.Log(""Greetings from AnotherSimpleType! Invoking SimpleTypes PrintHey method..."");
                                        SimpleType sm = new SimpleType(); sm.PrintHey();
                                    }
                                }

                              ";

        engine.CompileType("AnotherSimpleType", anotherTypeCode);

        engine.RemoveTypes("AnotherSimpleType");

        //This will cause a compilation error, beacause we removed AnotherSimpleType 
        engine.CompileCode(@"
                                                 AnotherSimpleType sm = new AnotherSimpleType(); 
                                               ");

        engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);

    }

    //-------------------------------------------------------------------------------------------------

    //add using directives to CSScriptEngine, to control what is exposed to dynamic code
    void AddUsings()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);

        //There's no usings right now in system, so nothing, apart from basic types is accesible to dynamic code
        engine.CompileCode("Debug.Log(\"This will result in an error, because Debug is a part of UnityEngine namespace\");");

        //Here we add using UnityEngine to system, so everything in this namespace is accessible now
        engine.AddUsings("using UnityEngine;");

        engine.CompileCode("Debug.Log(\"Now compilation of this code will succeed\");").Execute();

        engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);

    }

    //-------------------------------------------------------------------------------------------------

    //Add using of custom namespace, this way you can restrict access of resources to only your custom namespace
    void AddCustomNamespaceUsings()
    {
        CSScriptEngine engine = new CSScriptEngine();

        //Here we add using UnityEngine to system, so everything in this namespace is accessible now
        engine.AddUsings("using SomeCustomNamespace;");

        engine.CompileCode("HeyPrinter hp = new HeyPrinter(); hp.PrintHey();").Execute();

    }

    //-------------------------------------------------------------------------------------------------

    //Removes usings from system, so it's namespace resources become inaccessible for dynamic code
    void RemoveUsings()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);

        engine.AddUsings("using SomeCustomNamespace;");

        engine.CompileCode("HeyPrinter hp = new HeyPrinter(); hp.PrintHey();").Execute();

        engine.RemoveUsings("using SomeCustomNamespace;");

        //Now this will result in error
        engine.CompileCode("HeyPrinter hp = new HeyPrinter(); hp.PrintHey();");

        engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);

    }

    //-------------------------------------------------------------------------------------------------

    //Coroutine compilation is similar to methodless code compilation, but with some nuances.
    void CompileCoroutine()
    {
        CSScriptEngine engine = new CSScriptEngine();

        engine.AddUsings("using UnityEngine;");

        //CompileCoroutine returns IEnumerable object, so you need to use GetEnumerator on it in order to
        //be able to pass it to StartCoroutine
        IEnumerator coroutine = engine.CompileCoroutine(@"yield return new WaitForSeconds(1f);Debug.Log(""Hey!"");").GetEnumerator();

        StartCoroutine(coroutine);

    }

    #endregion CSScriptEngine examples

    #region CSScriptEngineRemote examples

    //With CSScriptEngineRemote you must always invoke LoadDomain before using it, 
    //and always invoke Unloaddomain or Dispose when you're done using it
    void RemoteCompileCode()
    {
        //Let's check that no dynamic assemblies were loaded in main appDomain
        Debug.Log("Assemblies currently loaded in main appdomain: " + AppDomain.CurrentDomain.GetAssemblies().Length);

        //You can ensure dispose call by utilizing using block, for example
        using (CSScriptEngineRemote engineRemote = new CSScriptEngineRemote())
        {
            engineRemote.LoadDomain();

            engineRemote.AddUsings("using UnityEngine;");

            engineRemote.CompileCode(@"Debug.Log(""Hey!"");");

            engineRemote.ExecuteLastCompiledCode();
        }

        Debug.Log("Assemblies currently loaded in main appdomain: " + AppDomain.CurrentDomain.GetAssemblies().Length);
    }

    //Remember, you can't compile MonoBehaviours with CSSCriptEngineRemote, stick to
    //plain C# classes for safety
    void RemoteCompileType()
    {

        CSScriptEngineRemote engineRemote = new CSScriptEngineRemote();

        engineRemote.LoadDomain();//Important!

        engineRemote.AddUsings("using UnityEngine;");

        string typeCode = @"
                
                                public class SimpleType
                                {
                                    public void PrintHey()
                                    {
                                        Debug.Log(""Hey!"");
                                    }
                                }

                              ";

        engineRemote.CompileType("SimpleType", typeCode);

        engineRemote.CompileCode(@"
                                    SimpleType sm = new SimpleType();sm.PrintHey();
                                ");

        engineRemote.ExecuteLastCompiledCode();

        engineRemote.UnloadDomain();//Important!
    }

    #endregion CSScriptEngineRemote examples

    void Update ()
    {
        #region CSScriptEngine

        if (Input.GetKeyDown(KeyCode.A))
        {
            CompileCode();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            CompileType();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            CompileMultipleTypes();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            AddCompilationFailedHandler();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            AddCompilationSucceededHandler();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            RemoveType();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddUsings();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            AddCustomNamespaceUsings();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            RemoveUsings();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            CompileCoroutine();
        }

        #endregion CSScriptEngine

        #region CSSCriptEngineRemote

        if (Input.GetKeyDown(KeyCode.K))
        {
            RemoteCompileCode();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            RemoteCompileType();
        }

        #endregion CSSCriptEngineRemote
    }
}

