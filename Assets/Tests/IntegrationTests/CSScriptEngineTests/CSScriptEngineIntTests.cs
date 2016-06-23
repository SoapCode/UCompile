using UnityEngine;
using UCompile;
using UnityEngine.Assertions;
using System.Reflection;
using System;
using System.Collections;

public class CSScriptEngineIntTests : MonoBehaviour {

    CSScriptEngine _engine;

    bool _lastCompilationSucceeded = false;

    public void OnCompilationFailedAction(CompilerOutput output)
    {
        ////Uncomment this to acquire additional information
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
        _engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);
        _engine.AddOnCompilationSucceededHandler(OnCompilationSucceededAction);
    }

    void OnDisable()
    {
        _engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);
        _engine.RemoveOnCompilationSucceededHandler(OnCompilationSucceededAction);
    }

    void Awake()
    {
        _engine = new CSScriptEngine();
    }

	// Use this for initialization
	void Start ()
    {
        StartCoroutine(StartTests());
    }

    IEnumerator StartTests()
    {
        CompileCodeTest();

        yield return null;

        CompileCoroutineTest();

        yield return null;

        CompileTypeTest();

        yield return null;

        RemoveTypeTest();
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
        _engine.CompileCode(code).Execute();

        //Assert
        GameObject go = GameObject.Find("DynamicallyCreatedGO");
        Assert.IsTrue(go != null);

        //TearDown
        Destroy(go);
    }

    void CompileCoroutineTest()
    {
        _engine.Reset();

        //Setup
        string code =
            @"
                GameObject gob = new GameObject(""DynamicallyCreatedGO"");
                yield return null;
            ";
        _engine.AddUsings("using UnityEngine;");

        //Action
        StartCoroutine(_engine.CompileCoroutine(code).GetEnumerator());

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
        _engine.CompileType("TestType",typeCode);
        _engine.CompileCode(@"DynamicType dt = new DynamicType();dt.CreateGameObject();").Execute();

        //Assert
        Type dynamicType = _engine.CompileCurrentTypesIntoAssembly().GetType("DynamicType");
        GameObject go = GameObject.Find("DynamicallyCreatedGO");
        Assert.IsTrue(go != null && dynamicType != null);

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
        string anotherTypeCode =
            @"
                public class AnotherDynamicType
                {
                    public void CreateGameObject(){GameObject gob = new GameObject(""DynamicallyCreatedGO"");}
                }
            ";

        _engine.AddUsings("using UnityEngine;");
        bool currentCompilationSucceeded;

        //Action
        _engine.CompileType("TestType", typeCode);
        _engine.CompileType("AnotherTestType", anotherTypeCode);
        _engine.RemoveTypes("TestType");
        _engine.CompileCode(@"DynamicType dt = new DynamicType();dt.CreateGameObject();");

        currentCompilationSucceeded = _lastCompilationSucceeded;

        Type dynamicType = _engine.CompileCurrentTypesIntoAssembly().GetType("DynamicType");

        //Assert
        Assert.IsTrue(dynamicType == null && !currentCompilationSucceeded);
    }
}
