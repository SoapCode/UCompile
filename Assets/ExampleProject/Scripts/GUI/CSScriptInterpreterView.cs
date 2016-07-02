using UnityEngine;
using System.Collections;
using UCompile;
using System.Collections.Generic;
using System;

namespace ExampleProjectGUI
{

    public class CSScriptInterpreterView : MonoBehaviour
    {

        CSScriptEngine _engine;
        CSScriptEngineRemote _engineRemote;

        IScript _lastCompiledScript = null;
        IEnumerable _lastCompiledCoroutine = null;
        [HideInInspector]
        public bool Remote = false;
        public bool ResetSceneOnExecute = false;
        bool _currentlyCompilingCoroutine = false;
        bool _currentlyCompilingCode = false;

        bool _lastCompilationSucceeded = true;

        void Awake()
        {
            _engine = new CSScriptEngine();
            _engineRemote = new CSScriptEngineRemote();
        }

        void Start()
        {
            _engine.AddUsings("using WhitelistAPI;");
            _engine.AddUsings("using UnityEngine;");
            
        }

        void Update()
        {
            if (Remote)
            {
                if (_engineRemote.RemoteDomain == null)
                {
                    _engineRemote.LoadDomain("RemoteDomain");
                    _engineRemote.AddUsings("using WhitelistAPI;");
                    _engineRemote.AddUsings("using UnityEngine;");
                    _engineRemote.AddUsings("using System;");
                }
                if (!GameObjectWatcher.Initialized)
                    GameObjectWatcher.Initialize();
            }
            else
                if (_engineRemote.RemoteDomain != null)
            {
                GameObjectWatcher.DestroyAllDynamicGOs();
                if (!ResetSceneOnExecute)
                    GameObjectWatcher.Disable();
                _engineRemote.UnloadDomain();
            }
            if (ResetSceneOnExecute)
            {
                if (!GameObjectWatcher.Initialized)
                    GameObjectWatcher.Initialize();
            }
            else
            {
                if (GameObjectWatcher.Initialized)
                    GameObjectWatcher.Disable();
            }
        }

        void OnEnable()
        {
            _engine.AddOnCompilationSucceededHandler(OnCompilationSucceededAction);
            _engine.AddOnCompilationFailedHandler(OnCompilationFailedAction);
            _engineRemote.AddOnCompilationSucceededHandler(OnCompilationSucceededAction);
            _engineRemote.AddOnCompilationFailedHandler(OnCompilationFailedAction);

            EventManager.Instance.AddListener<CompilationEvent>(OnCompileButton);
            EventManager.Instance.AddListener<ExecutionEvent>(OnExecuteButton);
            EventManager.Instance.AddListener<CompileTypeEvent>(OnCompileAndAddTypeButton);
            EventManager.Instance.AddListener<DeleteTypeEvent>(OnDeleteTypeButton);
            EventManager.Instance.AddListener<CurrentlyCompilingAnimationEvent>(OnAnimationToggleValueChanged);
            EventManager.Instance.AddListener<CurrentlyCompilingCodeEvent>(OnCodeToggleValueChanged);

        }

        void OnDisable()
        {
            //OnDisable later then EventManager's OnApplicationQuit where It's Instance property gets nullified
            //That causes null reference exception, so let's add this check to prevent that
            if (EventManager.Instance != null)
            {
                _engine.RemoveOnCompilationSucceededHandler(OnCompilationSucceededAction);
                _engine.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);
                _engineRemote.RemoveOnCompilationSucceededHandler(OnCompilationSucceededAction);
                _engineRemote.RemoveOnCompilationFailedHandler(OnCompilationFailedAction);

                EventManager.Instance.RemoveListener<CompilationEvent>(OnCompileButton);
                EventManager.Instance.RemoveListener<ExecutionEvent>(OnExecuteButton);
                EventManager.Instance.RemoveListener<CompileTypeEvent>(OnCompileAndAddTypeButton);
                EventManager.Instance.RemoveListener<DeleteTypeEvent>(OnDeleteTypeButton);
            }
            if (Remote)
                if (_engineRemote.RemoteDomain != null)
                    _engineRemote.Dispose();
        }

        public void OnCompileButton(CompilationEvent cmpEv)
        {
            if (!Remote)
            {
                if (_currentlyCompilingCode)
                    _lastCompiledScript = _engine.CompileCode(cmpEv.Code);
                else if (_currentlyCompilingCoroutine)
                    _lastCompiledCoroutine = _engine.CompileCoroutine(cmpEv.Code);
            }
            else
            {
                if (_currentlyCompilingCode)
                    _engineRemote.CompileCode(cmpEv.Code);
            }
        }

        public void OnExecuteButton(ExecutionEvent cmpEv)
        {
            if (!Remote)
            {
                if (_currentlyCompilingCode && _lastCompiledScript != null)
                    _lastCompiledScript.Execute();
                else if (_currentlyCompilingCoroutine && _lastCompiledCoroutine != null)
                    StartCoroutine(_lastCompiledCoroutine.GetEnumerator());
            }
            else
            {
                if (_currentlyCompilingCode)
                    _engineRemote.ExecuteLastCompiledCode();
            }

        }

        public void OnCompileAndAddTypeButton(CompileTypeEvent ev)
        {
            if (!Remote)
                _engine.CompileType(ev.TypeID, ev.Code);
            else
                _engineRemote.CompileType(ev.TypeID, ev.Code);

            if(_lastCompilationSucceeded)
                EventManager.Instance.QueueEvent(new TypeCompilationSucceededEvent(ev));

        }

        public void OnDeleteTypeButton(DeleteTypeEvent ev)
        {
            if (!Remote)
                _engine.RemoveTypes(ev.TypeID);
            else
                _engineRemote.RemoveTypes(ev.TypeID);

        }

        public void OnAnimationToggleValueChanged(CurrentlyCompilingAnimationEvent ev)
        {
            _currentlyCompilingCoroutine = true;
            _currentlyCompilingCode = false;
        }
        public void OnCodeToggleValueChanged(CurrentlyCompilingCodeEvent ev)
        {
            _currentlyCompilingCode = true;
            _currentlyCompilingCoroutine = false;
        }

        public void OnRemoteToggleValueChanged()
        {
            Remote = !Remote;
        }

        public void OnCompilationSucceededAction(CompilerOutput output)
        {
            for (int i = 0; i < output.Warnings.Count; i++)
                Debug.LogWarning(output.Warnings[i]);

            _lastCompilationSucceeded = true;
        }

        public void OnCompilationFailedAction(CompilerOutput output)
        {
            for (int i = 0; i < output.Errors.Count; i++)
                Debug.LogError(output.Errors[i]);
            for (int i = 0; i < output.Warnings.Count; i++)
                Debug.LogWarning(output.Warnings[i]);

            _lastCompilationSucceeded = false;
        }

    }

    //This class will watch over dynamically created GameObjects, and delete them all before each
    //Execute.
    internal static class GameObjectWatcher
    {
        public static List<GameObject> AllDynamicGOs;

        static bool _initialized = false;
        public static bool Initialized { get { return _initialized; } private set { _initialized = value; } }

        public static void Initialize()
        {
            AllDynamicGOs = new List<GameObject>();
            EventManager.Instance.AddListener<BeforeExecutionEvent>(OnBeforeExecuteEvent);
            Initialized = true;
        }

        public static void Disable()
        {
            AllDynamicGOs.Clear();
            AllDynamicGOs = null;
            EventManager.Instance.RemoveListener<BeforeExecutionEvent>(OnBeforeExecuteEvent);
            Initialized = false;
        }

        static void OnBeforeExecuteEvent(BeforeExecutionEvent ev)
        {
            DestroyAllDynamicGOs();
        }

        public static void DestroyAllDynamicGOs()
        {
            if (GameObjectWatcher.Initialized && AllDynamicGOs.Count != 0)
            {

                foreach (GameObject go in AllDynamicGOs)
                {
                    GameObject.Destroy(go);
                }

                AllDynamicGOs.Clear();
            }
        }
    }
}