using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;

namespace UCompile
{
    /// <summary>
    /// Helper class, that will perform operations on assemblies
    /// in remote AppDomain
    /// </summary>
    public class RemoteExecutor : MarshalByRefObject {

        CSScriptEngine _engine = null;

        IScript _lastCompiledScript = null;

        //All initializations should be here
        public void Initialize()
        {
            _engine = new CSScriptEngine();
        }

        //We'll use this method to clear everything we need to be cleared
        public void Dispose()
        {
        }

        public void AddUsings(string usings)
        {
            _engine.AddUsings(usings);
        }

        public void RemoveUsings(string usings)
        {
            _engine.RemoveUsings(usings);
        }

        public CompilerOutput CompileCode(string code)
        {
            _lastCompiledScript = _engine.CompileCode(code);
            return _engine.GetLastCompilerOutput();
        }

        public void ExecuteLastCompiledCode()
        {
            if (_lastCompiledScript != null)
            {
                _lastCompiledScript.Execute();
            }
        }

        public CompilerOutput CompileType(string id, string code)
        {
            _engine.CompileType(id, code);
            return _engine.GetLastCompilerOutput();
        }

        public void RemoveTypes(params string[] ids)
        {
            _engine.RemoveTypes(ids);
        }

        public void Reset()
        {
            _engine.Reset();
        }

        //This is needed to prevent LifetimeService object creation
        //and thus infinite lifetime is granted for the MarshalByRefObject
        //Probably should implement lifetime management via ClientSponsor
        //to exclude memory leak risk?
        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region DynamicMonoBehaviourSystem
        //-----------------------------------------------------------------------------------------------------------------
        //TODO: work in progress on this one
        //Here goes all the DynamicMonoBehaviourSystemRemote functionality
        //It is an attempt to work around the bug in Unity, that causes MonoBehaviours Update(), and some other messages 
        //to execute in Unity Child Domain, even though This MonoBehaviour was added to GameObject in remote appDomain

        //public List<WhitelistAPIRemote.MonoBehaviour> AllAttachedMBs = new List<WhitelistAPIRemote.MonoBehaviour>();
        //public List<WhitelistAPIRemote.MonoBehaviour> _allAttachedMBsValues = new List<WhitelistAPIRemote.MonoBehaviour>();

        //bool _changed = false;
        //internal bool Changed { get { return _changed; } set { _changed = value; } }

        //public void UpdateValuesArray()
        //{
        //    _allAttachedMBsValues.Clear();
        //    for (int i = 0; i < AllAttachedMBs.Count; i++)
        //    {
        //        _allAttachedMBsValues.Add(AllAttachedMBs[i]);
        //    }
        //    _changed = false;
        //}

        ////In order to serialization exception not occur while trying to invoke dynamic class method
        ////you need to wrap this method into MarshalByRef object's method
        //public void UpdateRemote()
        //{

        //    if (_allAttachedMBsValues.Count != 0)
        //    {
        //        for (int i = 0; i < _allAttachedMBsValues.Count; i++)
        //        {
        //            _allAttachedMBsValues[i].Update();
        //        }

        //        if (_changed)
        //            UpdateValuesArray();
        //    }

        //}
        #endregion
    }

    //TODO: implement dynamic type compilation with MonoBehaviours
    //TODO: implement coroutine compilation
    /// <summary>
    /// Loads CSScriptEngine in separate remote AppDomain, and sends signals to it, 
    /// to perform operations on input C# code.
    /// </summary>
    public class CSScriptEngineRemote : IDisposable
    {

        AppDomain _remoteDomain = null;
        public AppDomain RemoteDomain { get { return _remoteDomain; } }

        Action<CompilerOutput> OncompilationSucceededDelegate;
        Action<CompilerOutput> OncompilationFailedDelegate;

        RemoteExecutor _remoteExecutor = null;
        RemoteExecutor GetRemoteExecutor
        {
            get
            {
                try
                {
                    if (_remoteDomain != null)
                    {
                        if (_remoteExecutor == null)
                        {
                            _remoteExecutor = (RemoteExecutor)_remoteDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(RemoteExecutor).FullName);
                            _remoteExecutor.Initialize();
                        }
                        return _remoteExecutor;
                    }
                    else
                    {
                        Logger.Log("Remote AppDomain is not loaded.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                    return null;
                }

            }
        }

        public void AddUsings(string usings)
        {
            GetRemoteExecutor.AddUsings(usings);
        }

        public void RemoveUsings(string usings)
        {
            GetRemoteExecutor.RemoveUsings(usings);
        }

        /// <summary>
        /// Sends signal to remote AppDomain to compile C# code
        /// </summary>
        /// <param name="code">String with C# code to compile</param>
        public void CompileCode(string code)
        {
            CompilerOutput tempOutput = GetRemoteExecutor.CompileCode(code);

            ExecuteAppropriateDelegate(tempOutput);
        }

        void ExecuteAppropriateDelegate(CompilerOutput tempOutput)
        {
            if (tempOutput.Errors.Count != 0)
            {
                if (OncompilationFailedDelegate != null)
                    OncompilationFailedDelegate(tempOutput);
            }
            else
            {
                if (OncompilationSucceededDelegate != null)
                    OncompilationSucceededDelegate(tempOutput);
            }
         }


        /// <summary>
        /// Sends signal to remote AppDomain to invoke Execute()
        /// method of the last compiled IScript object.
        /// </summary>
        public void ExecuteLastCompiledCode()
        {
            GetRemoteExecutor.ExecuteLastCompiledCode();
        }

        /// <summary>
        /// EXPERIMENTAL:
        /// Don't use this method with MonoBehaviours!
        /// MonoBehaviours will execute thier Updates in Unity Child Domain
        /// even if they were compiled, loaded and added in remote adpDomain
        /// That leads to unpredictable results when trying to execute Update() in remote appDomain.
        /// So right now using Update() message of dynamically compiled and added to GameObject MonoBehaviour in remote appDomain
        /// will result into crash on Unity Child Domain unload, and who knows what oddities and bugs.
        /// </summary>
        public void CompileType(string id, string code)
        {
            CompilerOutput tempOutput = GetRemoteExecutor.CompileType(id, code);

            ExecuteAppropriateDelegate(tempOutput);
        }

        ///<summary>
        ///EXPERIMENTAL:
        ///Don't use this method with MonoBehaviours!
        ///MonoBehaviours will execute thier Updates in Unity Child Domain
        ///even if they were compiled, loaded and added in remote adpDomain
        ///That leads to unpredictable results when trying to execute Update() from MonoBehaviour in remote appDomain
        ///So right now using Update() message of dynamically compiled and added to GameObject MonoBehaviour in remote appDomain
        ///will result into crash on Unity Child Domain unload, and who knows what oddities and bugs.
        ///<summary>
        public void RemoveTypes(params string[] ids)
        {
            GetRemoteExecutor.RemoveTypes(ids);
        }

        /// <summary>
        /// Sends signal to CSScriptEngine in remote AppDomain to reset
        /// </summary>
        public void Reset()
        {
            GetRemoteExecutor.Reset();
        }

        public void AddOnCompilationSucceededHandler(Action<CompilerOutput> onCompilationSucceededHandler)
        {
            OncompilationSucceededDelegate += onCompilationSucceededHandler;
        }

        public void RemoveOnCompilationSucceededHandler(Action<CompilerOutput> onCompilationSucceededHandler)
        {
            OncompilationSucceededDelegate -= onCompilationSucceededHandler;
        }

        public void AddOnCompilationFailedHandler(Action<CompilerOutput> onCompilationFailedHandler)
        {
            OncompilationFailedDelegate += onCompilationFailedHandler;
        }

        public void RemoveOnCompilationFailedHandler(Action<CompilerOutput> onCompilationFailedHandler)
        {
            OncompilationFailedDelegate -= onCompilationFailedHandler;
        }

        /// <summary>
        /// Loads remote AppDomain, so code compilation 
        /// and execution will be performed in it.
        /// </summary>
        /// <param name="name">Name of the remote AppDomain</param>
        public void LoadDomain(string name = "")
        {
            if (_remoteDomain == null)
            {

                AppDomainSetup setup = new AppDomainSetup();
                //TODO: Implement proper assembly resolving, maybe using resolving handler
                setup.ApplicationBase = Application.dataPath;
                setup.PrivateBinPath = Application.dataPath;
                setup.ShadowCopyFiles = "true";
                setup.ShadowCopyDirectories = setup.ApplicationBase;

                _remoteDomain = AppDomain.CreateDomain(name == "" ? "RemoteDomain" : name, null, setup);

                Logger.Log("RemoteDomain loaded");

            }
            else
            {
                Logger.Log("Remote AppDomain is already loaded");
            }
        }

        /// <summary>
        /// Unloads remote AppDomain
        /// </summary>
        public void UnloadDomain()
        {
            if (_remoteDomain != null)
            {
                //Dispose any resources allocated by RemoteExecutor first
                //Problem here probably
                if (_remoteExecutor != null)
                    _remoteExecutor.Dispose();

                AppDomain.Unload(_remoteDomain);

                _remoteDomain = null;
                _remoteExecutor = null;
                Logger.Log("RemoteDomain Unloaded");

            }
        }

        //Disposable pattern implementation, to unload remote appDomain
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnloadDomain();
            }
        }
    }
}