using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq;
using System.IO;

namespace UCompile
{

    /// <summary>
    /// This class is a wrapper around MonoEvaluator.
    /// </summary>
    public class CompilationUnit
    {
        ICompilationSystem _eval = null;

        Assembly _lastAsm = null;

        Dictionary<string, string> _typeCodeStore = new Dictionary<string, string>();
        public Dictionary<string, string> TypeCodeStore { get { return _typeCodeStore; } }

        public List<Assembly> AllReferencedAssemblies {get { return _eval.AllReferencedAssemblies; }}

        StringBuilder _allTypesCode = new StringBuilder();

        public Action<CompilerOutput> OnCompilationSucceededDelegate;
        public Action<CompilerOutput> OnCompilationFailedDelegate;

        //Flag used to determine, if compilation returned error,
        //which delegate to use: OnCompilationSucceededDelegate or OnCompilationFailedDelegate
        bool _lastCompilationFailed = false;

        public CompilerOutput LastCompilerOutput { get { return _eval.CompilationOutput; } }

        //Push dependency up the hierarchy
        public CompilationUnit(ICompilationSystem compilationProvider)
        {
            _eval = compilationProvider;
        }

        //Adds type code to the _typeCodeStore, marked with specific typeID
        public void AddType(string typeID,string code)
        {
            if (_typeCodeStore.ContainsKey(typeID))
                _typeCodeStore[typeID] = code;
            else
                _typeCodeStore.Add(typeID, code);
        }

        public void RemoveType(string typeID)
        {
            if (_typeCodeStore.ContainsKey(typeID))
                _typeCodeStore.Remove(typeID);
        }

        //Takes all types in _typeCodeStore and compiles them into assembly
        public Assembly CompileTypesIntoAssembly()
        {
            _allTypesCode.Length = 0;
            _allTypesCode.Capacity = 0;

            string[] temp = new string[_typeCodeStore.Count];
            _typeCodeStore.Values.CopyTo(temp,0);

            for (int i = 0;i < _typeCodeStore.Count;i++)
            {
                _allTypesCode.Append(temp[i]);
            }

            HandleCompilationErrors(()=>_lastAsm = _eval.CompileCode(_allTypesCode.ToString()));

            return _lastAsm;
        }

        public void Run(string code)
        {
            _eval.Run(code);
        }

        public void ReferenceAssemblies(params Assembly[] assemblies)
        {
            _eval.ReferenceAssemblies(assemblies);
        }

        public void HandleCompilationErrors(Action compilationAction)
        {

            try
            {
                compilationAction();
            }
            catch (CompilerException ex)
            {
                if (OnCompilationFailedDelegate != null)
                    OnCompilationFailedDelegate(new CompilerOutput(ex));

                _lastCompilationFailed = true;

                //if there was exception, we need to set _lastAsm to null here
                _lastAsm = null;
            }
            finally
            {
                //OnCompilationSucceededDelegate only fires, if last compilation succeeded without errors
                //so, _lastCompilationflag must be set to false
                if (!_lastCompilationFailed && OnCompilationSucceededDelegate != null)
                {
                    OnCompilationSucceededDelegate(_eval.CompilationOutput);
                }

                //In case  there were errors, we'll reset _lastCompilationFailed flag to false
                _lastCompilationFailed = false;
            }
        }

        public void Reset()
        {
            _typeCodeStore.Clear();
            _eval.Reset();
        }

        public void SoftReset()
        {
            _eval.SoftReset();
        }
    }
    //TODO: every time recompilation occurs, all types added will be compiled into the single assembly
    //so when 1 type changes, we need to recompile all dynamic types code. It's okay for small quantities of types
    //but probably it's worth it, to add system, that compiles each type into separate assembly.
    //TODO: add file compilation, right now there is only "in memory" compilation of code strings.
    //TODO: implement better logging
    //TODO: add ability to add and remove assembly references, without AddUsing().
    //TODO: need to handle situation with memory leak because of the infinite amount of loading assemblies.
    //TODO: get rid of Linq usage.
    /// <summary>
    /// This class essentially takes C# code as a string, and compiles
    /// it into assembly, so this code becomes a part of the main codebase.
    /// </summary>
    public class CSScriptEngine
    {

        CompilationUnit _unit = null;

        List<string> _allUsings = new List<string>();
        public List<string> AllUsings { get { return _allUsings; } }

        public List<Assembly> AllReferencedAssemblies { get { return _unit.AllReferencedAssemblies; } }

        Dictionary<string,Assembly> _allUsingsAndRelatedAssemblies = new Dictionary<string, Assembly>();

        Assembly[] _allAppDomainAssemblies;

        Assembly _lastCompiledAssembly = null;

        Regex _usingsDerictivesRegEx = new Regex(@"^(\s*using\s*[A-Z,a-z]+(\.[A-z,a-z]+)*\s*;\s*)+$");


        string _template = 
          @"public class Script : IScript{               
                $METHODS$
          }";

        string _allMethods =
          @"public void Execute()
            {
                  $BODY$
            }
            public System.Collections.IEnumerable Coroutine()
            {
                  $CBODY$
            }";

        string _lastCompiledCode = "";
        string _lastCompiledCoroutine = "yield return null;";
        
        /// <summary>
        /// Creates new instance of CSScriptEngine with MonoEvaluator as default ICompilationSystem.
        /// </summary>
        public CSScriptEngine()
        {
            _unit = new CompilationUnit(new MonoEvaluator()); 

            _allAppDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            //We need to reference executing assembly, because it contains IScript interface, which 
            //is used in CompileCode() as wrapper class. 
            _unit.ReferenceAssemblies(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Creates new instance of CSScriptEngine with provided ICompilationSystem under the hood.
        /// </summary>
        /// <param name="compilationProvider">Implementation of ICompilationSystem interface.</param>
        public CSScriptEngine(ICompilationSystem compilationProvider)
        {
            _unit = new CompilationUnit(compilationProvider);

            _allAppDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            //We need to reference executing assembly, because it contains IScript interface, which 
            //is used in CompileCode as wrapper class. 
            _unit.ReferenceAssemblies(Assembly.GetExecutingAssembly());
        }

        public Assembly CompileCurrentTypesIntoAssembly()
        {
            if (_lastCompiledAssembly == null && _unit.TypeCodeStore.Count != 0)
            {
                return _lastCompiledAssembly = _unit.CompileTypesIntoAssembly();
            }
            else return _lastCompiledAssembly;

        }

        /// <summary>
        /// Returns CompilerOutput, that contains errors 
        /// and warnings from the last compilation.
        /// </summary>
        /// <returns></returns>
        public CompilerOutput GetLastCompilerOutput()
        {
            return _unit.LastCompilerOutput;
        }

        /// <summary>
        /// Nullifies all added usings, referenced assemblies and last compiled assembly
        /// </summary>
        public void Reset()
        {
            ResetCompilationUnit();

            _allUsings = new List<string>();
            _allUsingsAndRelatedAssemblies = new Dictionary<string, Assembly>();

            _lastCompiledAssembly = null;
        }

        //Resets compilation unit exclusively.
        void ResetCompilationUnit()
        {
            _unit.Reset();
            _unit.ReferenceAssemblies(Assembly.GetExecutingAssembly());
        }

        //Resets CompilationUnit, then declares usings again
        void UpdateUsings()
        {
            _unit.Run(string.Join("", _allUsings.ToArray()));
        }

        Assembly GetAssemblyByNamespace(string namespaceName)
        {
            if (Assembly.GetExecutingAssembly().GetTypes().Any((x) => x.Namespace == namespaceName))
                return Assembly.GetExecutingAssembly();
            else
            {
                try
                {
                    //Get all namespace hierarchy
                    string[] allNamespacesNames = namespaceName.Split('.');

                    if (allNamespacesNames.Length > 1)
                    {
                        Assembly[] possibleAssemblies = _allAppDomainAssemblies.Where((x) => x.GetName().Name.Split('.')[0] == allNamespacesNames[0]).ToArray();

                        //If we can't find assemblies with assembly name in namespaceName, then
                        //it's probably because this assembly name is wrong
                        if (possibleAssemblies.Length == 0)
                            return null;

                        StringBuilder tempSB = new StringBuilder(namespaceName);

                        for (int i = allNamespacesNames.Length - 1; i >= 0; i--)
                        {
                            for(int j = 0; j < possibleAssemblies.Length;j++)
                            {
                                if(possibleAssemblies[j].GetName().Name == tempSB.ToString())
                                    return Assembly.Load(new AssemblyName(tempSB.ToString()));
                            }

                            tempSB.Remove(tempSB.Length - allNamespacesNames[i].Length - 1, allNamespacesNames[i].Length + 1);
                        }

                        return null;
                    }

                    return Assembly.Load(new AssemblyName(namespaceName));
                }
                catch(Exception ex)
                {
                    if (ex is FileNotFoundException)
                        return null;
                    else
                        throw ex;
                }
            }
        }
        /// <summary>
        /// Adds using directives, so it applies to all code, that is to be dynamically compiled.
        /// Also references assemblies, which these usings refer to. Namespaces hierarchy must be fully described, for example:
        /// 
        /// "using System.CodeDOM; using UnityEngine.UI; using UnityEngine.Advertisements;"
        ///  
        /// </summary>
        /// <param name="usings">String with all usings in it, for example:
        /// "using UnityEngine; using System.CodeDOM; using AssemblyName.NamespaceName;"</param>
        public void AddUsings(string usings)
        {
            if (usings != null && _usingsDerictivesRegEx.IsMatch(usings))
            {
                string[] temp = usings.Split(';');

                for (int i = 0; i < temp.Length - 1; i++)
                {
                    string currentUsing = temp[i].TrimStart() + ';';

                    //let's get rid of empty string that gets added to array, because of the last
                    //';'
                    temp[temp.Length - 1] = null;

                    if (!_allUsings.Contains(currentUsing))
                    {
                        try
                        {
                            //strip away "using " so only the assembly name is left
                            Assembly usingsAssembly = GetAssemblyByNamespace(temp[i].Substring(6).TrimStart());

                            //if Assembly was found
                            if (usingsAssembly != null)
                            {
                                _allUsingsAndRelatedAssemblies.Add(currentUsing, usingsAssembly);
                                _allUsings.Add(currentUsing);
                                temp[i] = currentUsing;
                            }
                            else
                            {
                                //if we can't find this using's namespace among currently loaded assemblies, then it's invalid
                                throw new ArgumentException("The namespace in one of using derictives is not valid, or assembly containing it is not loaded to current AppDomain");
                            }
                        }
                        catch (Exception ex)
                        {
                            //If exception isn't connected with invalid input, then rethrow it
                            throw ex;
                        }
                    }
                    else
                    {
                        //if using was already added, delete it from temp array
                        temp[i] = null;
                    }
                }

                StringBuilder tempSB = new StringBuilder();

                List<Assembly> assembliesToReference = new List<Assembly>();

                for (int i = 0; i < temp.Length - 1; i++)
                {
                    if (temp[i] != null)
                    {
                        tempSB.Append(temp[i]);
                        assembliesToReference.Add(_allUsingsAndRelatedAssemblies[temp[i]]);
                    }
                }

                _unit.ReferenceAssemblies(assembliesToReference.ToArray());

                try {
                    _unit.Run(tempSB.ToString());
                }
                catch(Exception ex)
                {
                    //it should be unlikely, but if Run threw exception, we will reset all changes, 
                    //and rethrow this exception
                    ResetCompilationUnit();
                    for (int i = 0; i < temp.Length - 1; i++)
                    {
                        if (temp[i] != null)
                        {
                            _allUsings.Remove(temp[i]);
                            _allUsingsAndRelatedAssemblies.Remove(temp[i]);
                        }
                    }

                    StringBuilder sB = new StringBuilder();

                    foreach (string usng in _allUsings)
                        sB.Append(usng);

                    _unit.ReferenceAssemblies(_allUsingsAndRelatedAssemblies.Values.ToArray());
                    _unit.Run(sB.ToString());

                    throw ex;
                }
            }
            else
                throw new ArgumentException("Invalid argument");
        }

        /// <summary>
        /// Removes usings already referenced in system. Also removes related assemblies and thier references.
        /// </summary>
        /// <param name="usings">Usings to remove</param>
        public void RemoveUsings(string usings)
        {
            if (usings != null && _usingsDerictivesRegEx.IsMatch(usings) && _allUsings.Count != 0)
            {

                string[] temp = usings.Split(';');

                //Check if usings are already exists in _allUsings list
                for (int i = 0; i < temp.Length - 1; i++)
                    if (!_allUsings.Contains(temp[i].TrimStart() + ';'))
                        throw new ArgumentException("Can't find using directive");

                for (int i = 0; i < temp.Length - 1; i++)
                {
                    string currentUsing = temp[i] + ';';

                    _allUsings.Remove(currentUsing);
                    _allUsingsAndRelatedAssemblies.Remove(currentUsing);

                    temp[i] = currentUsing;
                }
                ResetCompilationUnit();

                if (_allUsings.Count != 0)
                {

                    StringBuilder tempSB = new StringBuilder();

                    for (int i = 0; i < _allUsings.Count; i++)
                        tempSB.Append(_allUsings[i]);
                    _unit.ReferenceAssemblies(_allUsingsAndRelatedAssemblies.Values.ToArray());
                    _unit.Run(tempSB.ToString());
                }
            }
            else
                throw new ArgumentException("Invalid argument");
        }

        /// <summary>
        /// Compiles string with C# code without class declaration. For example:
        /// 
        /// CSScriptEngine engine = new CSScriptEngine();
        /// IScript result = engine.CompileCode(@"UnityEngine.Debug.Log(""Hello!"");");
        /// result.Execute(); 
        /// 
        /// </summary>
        /// <param name="code">String with C# code</param>
        /// <returns>IScript object, encapsulating the code, or null if compilation failed</returns>
        public IScript CompileCode(string code = "")
        {
            if (code != "")
                _lastCompiledCode = code; 
            else
                return null;

            return CompileFinalSource(_lastCompiledCode, false);
           
        }

        /// <summary>
        /// Compiles string with C# coroutine code without class declaration. For example:
        /// 
        /// CSScriptEngine engine = new CSScriptEngine();
        /// IEnumerable result = engine.CompileCoroutine(@"yield return new UnityEngine.WaitForSeconds(1f);UnityEngine.Debug.Log(""Hello!"");");
        /// StartCoroutine(result.GetEnumerator());
        /// 
        /// </summary>
        /// <param name="code">String with coroutine code</param>
        /// <returns>IScript object, encapsulating the code, or null if compilation failed</returns>
        public IEnumerable CompileCoroutine(string coroutineCode = "")
        {
            if (coroutineCode != "")
                _lastCompiledCoroutine = coroutineCode; 
            else
                return null;

            IScript result = CompileFinalSource(_lastCompiledCoroutine, true);

            return result == null ? null : result.Coroutine();
        }

        //TODO: get rid of isCoroutine flag argument, maybe implement isCoroutine function
        /// <summary>
        /// Prepares classless code for compilation by wrapping it into Script : IScript class,
        /// then compiles it into assembly and returns Script object in form of IScript interface.
        /// </summary>
        /// <param name="finalMethodSource"></param>
        /// <param name="isCoroutine"></param>
        /// <returns>IScript object, encapsulating the code, or null if compilation failed</returns>
        IScript CompileFinalSource(string finalMethodSource, bool isCoroutine)
        {
            StringBuilder finalSource = new StringBuilder(_allMethods);

            if (!isCoroutine)
                finalSource = finalSource.Replace("$BODY$", finalMethodSource).Replace("$CBODY$", _lastCompiledCoroutine);
            else
                finalSource = finalSource.Replace("$CBODY$", finalMethodSource).Replace("$BODY$", _lastCompiledCode);

            _unit.AddType("ScriptType", _template.Replace("$METHODS$", finalSource.ToString()));

            _lastCompiledAssembly = _unit.CompileTypesIntoAssembly();

            _unit.RemoveType("ScriptType");//We need to remove ScriptType, so it won't clash with other types on compilation

            //Reset lastCompiled containers, so coroutine and plain method code are not going to clash on compilation
            if (isCoroutine)
                _lastCompiledCoroutine = "yield return null;";
            else
                _lastCompiledCode = "";

            if (_lastCompiledAssembly == null)
            {
                UpdateUsings();
                return null;
            }
            else return (IScript)Activator.CreateInstance(_lastCompiledAssembly.GetType("Script"));
        }

        /// <summary>
        /// Compiles string with class code and returns Type of this class. Compiled class is added to Assembly
        /// with all dynamically compiled classes, and can be used by them.
        /// Don't use usings directives in class code, for adding usings use AddUsings(string usings).
        /// For example:
        /// 
        /// CSScriptEngine engine = new CSScriptEngine();
        /// 
        /// engine.AddUsings("using UnityEngine;");
        /// 
        /// string code = @"public class SomeClass
        /// {
        ///    public void Print() { Debug.Log(""Hello!""); }
        /// }";
        /// 
        /// engine.CompileType("SomeClass", code);
        /// 
        /// engine.CompileCode(@"SomeClass sc = new SomeClass(); sc.Print();").Execute();
        /// 
        /// </summary>
        /// <param name="typeID">Unique ID of the type to compile</param>
        /// <param name="code">String with type code</param>
        /// <returns>Type of the class to compile. null, if compilation failed</returns>
        public Type CompileType(string typeID, string code)
        {
            //if type we are trying to compile already exists in _unit.TypeCodeStore
            //then we're editing existing type, and not adding new one.
            //In this case we need to SoftReset _unit, to get rid of a reference to previously compiled type
            //with the same ID, so it becomes inaccessible on further compilations of code or types.
            if (_unit.TypeCodeStore.ContainsKey(typeID))
            {
                _unit.SoftReset();
                UpdateUsings();
            }

            _unit.AddType(typeID, code);

            _lastCompiledAssembly = _unit.CompileTypesIntoAssembly();

            if (_lastCompiledAssembly == null)
            {
                UpdateUsings();
                _unit.RemoveType(typeID);
                return null;
            }

            return _lastCompiledAssembly.GetType(typeID);
        }

        /// <summary>
        /// Removes type from system, so it can't be used in dynamically compiled code.
        /// </summary>
        /// <param name="typeID">Unique IDs of the types to remove</param>
        public void RemoveTypes(params string[] typeIDs)
        {
            for(int i = 0; i < typeIDs.Length;i++)
                _unit.RemoveType(typeIDs[i]);

            _unit.SoftReset();
            UpdateUsings();
            
        }

        public void AddOnCompilationSucceededHandler(Action<CompilerOutput> onCompilationSucceededHandler)
        {
            _unit.OnCompilationSucceededDelegate += onCompilationSucceededHandler;
        }

        public void RemoveOnCompilationSucceededHandler(Action<CompilerOutput> onCompilationSucceededHandler)
        {
            _unit.OnCompilationSucceededDelegate -= onCompilationSucceededHandler;
        }

        public void AddOnCompilationFailedHandler(Action<CompilerOutput> onCompilationFailedHandler)
        {
            _unit.OnCompilationFailedDelegate += onCompilationFailedHandler;
        }

        public void RemoveOnCompilationFailedHandler(Action<CompilerOutput> onCompilationFailedHandler)
        {
            _unit.OnCompilationFailedDelegate -= onCompilationFailedHandler;
        }
    }
}