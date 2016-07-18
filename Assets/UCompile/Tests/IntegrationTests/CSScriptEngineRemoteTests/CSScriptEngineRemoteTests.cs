using UnityEngine;
using UCompile;
using UnityEngine.Assertions;
using System.Collections;
using System;
using System.Reflection;

public class CSScriptEngineRemoteTests : MonoBehaviour {

    CSScriptEngineRemote _engine;

    bool _lastCompilationSucceeded = false;

    public void OnCompilationFailedAction(CompilerOutput output)
    {
        //Uncomment this to acquire additional information
        //for (int i = 0; i < output.Errors.Count; i++)
        //    Debug.LogError(output.Errors[i]);
        //for (int i = 0; i < output.Warnings.Count; i++)
        //    Debug.LogWarning(output.Warnings[i]);

        _lastCompilationSucceeded = false;
    }

    public void OnCompilationSucceededAction(CompilerOutput output)
    {
        //for (int i = 0; i < output.Warnings.Count; i++)
        //    Debug.LogWarning(output.Warnings[i]);

        _lastCompilationSucceeded = true;
    }

    void OnEnable()
    {
        _engine.LoadDomain();

        _engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);
        _engine.AddOnCompilationSucceededHandler(OnCompilationSucceededAction);
    }

    void OnDisable()
    {
        _engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);
        _engine.RemoveOnCompilationSucceededHandler(OnCompilationSucceededAction);

        _engine.UnloadDomain();
    }

    // Use this for initialization
    void Awake()
    {
        _engine = new CSScriptEngineRemote();
    }

    void Start()
    {
        StartCoroutine(StartTests());
    }

    IEnumerator StartTests()
    {
        CompileCodeTest();

        yield return null;

        CompileTypeTest();

        yield return null;

        RemoveTypeTest();

        yield return null;

        MemoryUsageTest();

        yield return null;
    }

    void CompileCodeTest()
    {
        //Setup
        string code =
            @"
                GameObject gob = new GameObject(""DynamicallyCreatedGO"");
            ";
        _engine.AddUsings("using UnityEngine;");

        //Action
        _engine.CompileCode(code);
        _engine.ExecuteLastCompiledCode();

        //Assert
        GameObject go = GameObject.Find("DynamicallyCreatedGO");
        Assert.IsTrue(go != null);

        //TearDown
        Destroy(go);
    }

    void CompileTypeTest()
    {
        _engine.Reset();

        //Setup
        string typeCode =
            @"
                public class DynamicType
                {
                    public void CreateGameObject(){GameObject gob = new GameObject(""DynamicallyCreatedGO"");}
                }
            ";
        _engine.AddUsings("using UnityEngine;");

        //Action
        _engine.CompileType("TestType", typeCode);
        _engine.CompileCode(@"DynamicType dt = new DynamicType();dt.CreateGameObject();");
        _engine.ExecuteLastCompiledCode();

        //Assert
        GameObject go = GameObject.Find("DynamicallyCreatedGO");
        Assert.IsTrue(go != null);

        //TearDown
        Destroy(go);
    }

    void RemoveTypeTest()
    {
        _engine.Reset();

        //Setup
        string typeCode =
            @"
                public class DynamicType
                {
                    public void CreateGameObject(){GameObject gob = new GameObject(""DynamicallyCreatedGO"");}
                }
            ";
        _engine.AddUsings("using UnityEngine;");

        //Action
        _engine.CompileType("TestType", typeCode);
        _engine.RemoveTypes("TestType");
        _engine.CompileCode("DynamicType dt = new DynamicType(); dt.CreateGameObject();");

        //Assert
        Assert.IsTrue(!_lastCompilationSucceeded);
    }

    void MemoryUsageTest()
    {
        _engine.Reset();

        int assembliesInCurrentAppDomain = AppDomain.CurrentDomain.GetAssemblies().Length;
        _engine.AddUsings("using UnityEngine;");

        //Action
        _engine.CompileCode(@"Debug.Log(""Hey!"");");
        int assembliesInCurrentAppDomainAfterCompilation = AppDomain.CurrentDomain.GetAssemblies().Length;

        //Assert
        Assert.IsTrue(assembliesInCurrentAppDomain == assembliesInCurrentAppDomainAfterCompilation);
    }

}
