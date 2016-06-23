using UnityEngine;
using UCompile;
using System;
using UnityEngine.Assertions;

namespace UCompileIntegrationTests
{

    public class CSScriptEngineUsingsSystemTest : MonoBehaviour
    {

        CSScriptEngine _engine;

        bool _lastCompilationSucceeded = false;

        public void OnCompilationFailedAction(CompilerOutput output)
        {
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

        // Use this for initialization
        void Awake()
        {
            _engine = new CSScriptEngine();
        }

        void Start()
        {
            AddUsingTest();
            AddUsingWithMultipleNamespaces();
            AddUsingByNamespaceName();
            RemoveMultipleUsings();
            AddMultipleUsings();
            AddMultipleUsingsWIthOnlyNamespaceNames();
            CheckIfUsingsAreDereferencedInEvaluatorOnRemove();
            AddUsingInvalidInputTest();
            RemoveUsingInvalidInputTest();
            AddUsingWithUnknownUsingDirectiveTest();
            RemoveNonExistentUsing();
        }

        void AddUsingTest()
        {
            string usingString = "using UnityEngine;";

            _engine.AddUsings(usingString);                                                           //Don't forget about already and permamently referenced executing Assembly
            Assert.IsTrue(_engine.AllUsings.Count == 1 && _engine.AllUsings.Contains(usingString) && _engine.AllReferencedAssemblies.Count == 2);
        }

        void AddUsingWithMultipleNamespaces()
        {
            string usingString = "using UnityEngine.UI;";

            _engine.AddUsings(usingString);
            Assert.IsTrue(_engine.AllUsings.Count == 2 && _engine.AllUsings.Contains(usingString) && _engine.AllReferencedAssemblies.Count == 3);
        }

        void AddUsingByNamespaceName()
        {
            string usingString = "using UnityEngine.Advertisements;";

            _engine.AddUsings(usingString);
            Assert.IsTrue(_engine.AllUsings.Count == 3 && _engine.AllUsings.Contains(usingString) && _engine.AllReferencedAssemblies.Count == 3);
        }

        void RemoveMultipleUsings()
        {
            string usingString = "using UnityEngine.Advertisements;using UnityEngine.UI;using UnityEngine;";

            _engine.RemoveUsings(usingString);
            Assert.IsTrue(_engine.AllUsings.Count == 0 && _engine.AllReferencedAssemblies.Count == 1);
        }

        void AddMultipleUsings()
        {

            string usingString = "using UnityEngine.Advertisements; using UnityEngine; using UCompile;";

            _engine.AddUsings(usingString);
            Assert.IsTrue(_engine.AllUsings.Count == 3 && _engine.AllReferencedAssemblies.Count == 2);
        }


        void AddMultipleUsingsWIthOnlyNamespaceNames()
        {
            string usingString = "using UnityEngine.Advertisements; using UCompile;using System.CodeDom;";

            _engine.AddUsings(usingString);
            Assert.IsTrue(_engine.AllUsings.Count == 4 && _engine.AllReferencedAssemblies.Count == 3);
        }

        void CheckIfUsingsAreDereferencedInEvaluatorOnRemove()
        {
            _engine.RemoveUsings("using UnityEngine.Advertisements; using UCompile; using System.CodeDom;using UnityEngine;");

            _engine.CompileCode(@"Debug.Log(""Success"");");

            Assert.IsFalse(_lastCompilationSucceeded);

            _engine.AddUsings("using UnityEngine;");

            _engine.CompileCode(@"Debug.Log(""Success"");");

            Assert.IsTrue(_lastCompilationSucceeded);

        }

        void AddUsingInvalidInputTest()
        {
            _engine.Reset();

            try
            {
                _engine.AddUsings("Invalid input");
            }
            catch(Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                Assert.IsTrue(_engine.AllUsings.Count == 0 && _engine.AllReferencedAssemblies.Count == 1);
            }
        }

        void RemoveUsingInvalidInputTest()
        {
            _engine.AddUsings("using UnityEngine;");

            try
            {
                _engine.RemoveUsings("Invalid input");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                Assert.IsTrue(_engine.AllUsings.Count == 1 && _engine.AllReferencedAssemblies.Count == 2);
            }
        }

        void AddUsingWithUnknownUsingDirectiveTest()
        {
            _engine.Reset();

            try
            {
                _engine.AddUsings("using Unknown.Namespace;");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                Assert.IsTrue(_engine.AllUsings.Count == 0 && _engine.AllReferencedAssemblies.Count == 1);
            }

        }

        void RemoveNonExistentUsing()
        {
            _engine.Reset();

            try
            {
                _engine.RemoveUsings("using UnityEngine;");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                Assert.IsTrue(_engine.AllUsings.Count == 0 && _engine.AllReferencedAssemblies.Count == 1);
            }
        }

    }
}
