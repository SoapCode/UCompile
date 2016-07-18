#region License...

////-----------------------------------------------------------------------------
//// Date:	24/01/13	Time: 9:00
//// Module:	CSScriptLib.Eval.cs
//// Classes:	CSScript
////			Evaluator
////
//// This module contains the definition of the Evaluator class. Which wraps the common functionality
//// of the Mono.CScript.Evaluator class (compiler as service)
////
//// Written by Oleg Shilo (oshilo@gmail.com)
////----------------------------------------------
//// The MIT License (MIT)
//// Copyright (c) 2014 Oleg Shilo
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//// and associated documentation files (the "Software"), to deal in the Software without restriction,
//// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
//// subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in all copies or substantial
//// portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
//// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////----------------------------------------------

#endregion Licence...

using Mono.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using MCS = Mono.CSharp;

namespace UCompile
{
    /// <summary>
    /// The exception that is thrown when a the script compiler error occurs.
    /// </summary>
    [Serializable]
    public class CompilerException : ApplicationException
    {
        ///// <summary>
        ///// Gets or sets the errors.
        ///// </summary>
        ///// <value>The errors.</value>
        //public CompilerErrorCollection Errors { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        public CompilerException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        public CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CompilerException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// This class encapsulates all errors and warnings
    /// </summary>
    [Serializable]
    public class CompilerOutput
    {
        public CompilerOutput(CompilerException ex)
        {
            Errors = (List<string>)ex.Data["Errors"];
            Warnings = (List<string>)ex.Data["Warnings"];
        }

        public CompilerOutput()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public CompilerOutput(string[] errors, string[] warnings)
        {
            Errors = new List<string>(errors);
            Warnings = new List<string>(warnings);
        }

        public CompilerOutput(List<string> errors, List<string> warnings)
        {
            Errors = errors;
            Warnings = warnings;
        }

        public List<string> Errors { get; set; }

        public List<string> Warnings { get; set; }
    }

    //If you want to implement custom ICompilationSystem,
    //make sure your CompileCode and Run methods throw CompilerException on compilation failed.
    /// <summary>
    /// Interface for all dynamic compilation providers.
    /// </summary>
    public interface ICompilationSystem
    {
        //Properties
        List<Assembly> AllReferencedAssemblies { get; }
        CompilerOutput CompilationOutput { get; }

        //Methods
        Assembly CompileCode(string scriptText);
        void Reset();
        void SoftReset();
        void Run(string sciptText);
        void ReferenceAssemblies(params Assembly[] assemblies);
    }


    //TODO: Add async compilation capabilities.
    /// <summary>
    /// Wrapper class for Mono.Csharp.Evaluator
    /// compiles code at runtime and loads in-memory assemblies
    /// into current app domain.
    /// </summary>
    public class MonoEvaluator : ICompilationSystem
    {
        /// <summary>
        /// Gets the compiling result.
        /// </summary>
        /// <value>The compiling result.</value>
        public CompilingResult CompilationResult { get; private set; }

        /// <summary>
        /// More convenient and generic version of CompilationResult
        /// </summary>
        public CompilerOutput CompilationOutput { get; private set; }

        MCS.Evaluator service = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Evaluator" /> class.
        /// </summary>
        public MonoEvaluator()
        {
            AllReferencedAssemblies = new List<Assembly>();
            Reset();
        }
        /// <summary>
        /// Discards current instance of Evaluator, and CompilingResult, and replaces
        /// them with new ones. Also nullifies the AllREferencedAssemblies array
        /// </summary>
        public void Reset()
        {
            CompilationResult = new CompilingResult();

            CompilationOutput = new CompilerOutput();

            service = new MCS.Evaluator(new CompilerContext(new CompilerSettings(), CompilationResult));

            AllReferencedAssemblies = new List<Assembly>();
        }

        /// <summary>
        /// Discards current instance of Evaluator, and CompilingResult, and replaces
        /// them with new ones. Previously referenced assemblies get referenced again.
        /// </summary>
        public void SoftReset()
        {
            service = new MCS.Evaluator(new CompilerContext(new CompilerSettings(), CompilationResult));

            ReferencePreviouslyReferencedAssemblies();

        }

        void ReferencePreviouslyReferencedAssemblies()
        {
            for(int i = 0;i < AllReferencedAssemblies.Count;i++)
            {
                service.ReferenceAssembly(AllReferencedAssemblies[i]);
            }
        }

        static int AsmCounter = 0;

        /// <summary>
        /// Evaluates (compiles) C# code.
        /// </summary>
        /// <param name="scriptText">The C# script text.</param>
        /// <returns>The compiled assembly.</returns>
        public Assembly CompileCode(string scriptText)
        {
            Assembly result = null;

            HandleCompilingErrors(() =>
            {
                int id = AsmCounter++;
                service.Compile(scriptText + GetConnectionPointClassDeclaration(id));
                result = GetCompiledAssembly(id);
            });

            return result;
        }

        /// <summary>
        /// Evaluates the specified C# statement. The statement must be "void" (returning no result).
        /// </summary>
        /// <example>
        /// <code>
        /// MonoEvaluator.Run("using System;");
        /// MonoEvaluator.Run("Console.WriteLine(\"Hello World!\");");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# statement.</param>
        public void Run(string scriptText)
        {
            //Using HandlecompilingErrors will update CompilationOutput, we don't want that
            //on Run
            //HandleCompilingErrors(() =>
            //{
                service.Run(scriptText);
            //});
        }

        Assembly GetCompiledAssembly(int id)
        {
            string className = GetConnectionPointGetTypeExpression(id);
            return ((Type)service.Evaluate(className)).Assembly;
        }

        /// <summary>
        /// Gets a type from the last Compile/Evaluate/Load call.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <returns>The type instance</returns>
        public Type GetCompiledType(string type)
        {
            return (Type)service.Evaluate("typeof(" + type + ");");
        }

        string GetConnectionPointClassDeclaration(int id)
        {
            return "\n public struct CSS_ConnectionPoint_" + id + " {}";
        }
        string GetConnectionPointGetTypeExpression(int id)
        {
            return "typeof(CSS_ConnectionPoint_" + id + ");";
        }

        #region Obsolete Assembly Referencing control
        //I presume this magic is for assemblies to not be referenced if they already were
        //ReflectionImporter Importer
        //{
        //    get
        //    {
        //        FieldInfo info = service.GetType().GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic);
        //        return (ReflectionImporter)info.GetValue(service);
        //    }
        //}

        /// <summary>
        /// Gets the referenced assemblies. The set of assemblies is get cleared 
        /// on Reset.
        /// </summary>
        /// <returns></returns>
        //public Assembly[] GetReferencedAssemblies()
        //{
        //    return Assembly2Definition.Keys.ToArray();
        //}

        //static FieldInfo _FieldInfo;
        //Dictionary<Assembly, IAssemblyDefinition> Assembly2Definition
        //{
        //    get
        //    {
        //        if (_FieldInfo == null)
        //            _FieldInfo = Importer.GetType().GetField("assembly_2_definition", BindingFlags.Instance | BindingFlags.NonPublic);
        //        return (Dictionary<Assembly, IAssemblyDefinition>)_FieldInfo.GetValue(Importer);
        //    }
        //}


        /// <summary>
        /// References the assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The assembly instance.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        //public void ReferenceAssembly(Assembly assembly)
        //{
        //    if (!Assembly2Definition.ContainsKey(assembly))
        //        service.ReferenceAssembly(assembly);
        //}


        //public void ReferenceDomainAssemblies()
        //{
        //    //NOTE: It is important to avoid loading the runtime itself (mscorelib) as it
        //    //will break the code evaluation (compilation).
        //    //
        //    //On .NET mscorelib is filtered out by GlobalAssemblyCache check but
        //    //on Mono it passes through so there is a need to do a specific check for mscorelib assembly.
        //    var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        //    //foreach (Assembly asm in relevantAssemblies)
        //    //    UnityEngine.Debug.Log(asm.GetName().Name);

        //    //checks if assembly is dynamic
        //    //relevantAssemblies = relevantAssemblies.Where(x => !IsDynamic(x) && x != mscorelib).ToArray();
        //    ReferenceAssembly(Assembly.GetExecutingAssembly());

        //    //foreach (var asm in relevantAssemblies)
        //    //    ReferenceAssembly(asm);
        //}
        #endregion

        public List<Assembly> AllReferencedAssemblies { get; private set; }

        public void ReferenceAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly asm in assemblies)
            {
                //NOTE: It is important to avoid loading the runtime itself (mscorelib) as it
                //    //will break the code evaluation (compilation).
                //    //
                //    //On .NET mscorelib is filtered out by GlobalAssemblyCache check but
                //    //on Mono it passes through so there is a need to do a specific check for mscorelib assembly.
                if (!AllReferencedAssemblies.Contains(asm) && !IsDynamic(asm) && asm != mscorelib)
                {
                    service.ReferenceAssembly(asm);
                    AllReferencedAssemblies.Add(asm);
                }

            }
        }

        //Get mscorelib assembly
        static Assembly mscorelib = 333.GetType().Assembly;
        //Little helper method
        static bool IsDynamic(Assembly asm)
        {
            //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
            //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
            return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
        }

        void HandleCompilingErrors(Action action)
        {
            CompilationResult.Reset();

            try
            {
                action();
            }
            catch (Exception)
            {

                if (CompilationResult.HasErrors)
                {
                    throw CompilationResult.CreateException();
                }
                else 
                {
                    //The exception is most likely related to the compilation error
                    //so do noting. Alternatively (may be in the future) we can add
                    //it to the errors collection.
                    //CompilingResult.Errors.Add(e.ToString());
                    throw;
                }
            }
            finally
            {
                //We need to reset Evaluator every time after compilation exception occurs
                //because Mono throws error on attempt to compile again in this case
                if(CompilationResult.HasErrors)
                    SoftReset();

                //Keep CompilationOutput instance with relevant information about last compilation
                UpdateCompilationOutput();
            }
        }

        void UpdateCompilationOutput()
        {

            if (CompilationResult.HasErrors)
                CompilationOutput.Errors = CompilationResult.Errors;
            if (CompilationResult.HasWarnings)
                CompilationOutput.Warnings = CompilationResult.Warnings;
        }

        /// <summary>
        /// Custom implementation of <see cref="T:Mono.CSharp.ReportPrinter"/> required by
        /// <see cref="T:Mono.CSharp"/> API model for handling (reporting) compilation errors.
        /// <para><see cref="T:Mono.CSharp"/> default compiling error reporting (e.g. <see cref="T:Mono.CSharp.ConsoleReportPrinter"/>)
        /// is not dev-friendly, thus <c>CompilingResult</c> is acting as an adapter bringing the Mono API close to the
        /// traditional CodeDOM error reporting model.</para>
        /// </summary>
        public class CompilingResult : ReportPrinter
        {
            /// <summary>
            /// The collection of compiling errors.
            /// </summary>
            public List<string> Errors = new List<string>();

            /// <summary>
            /// The collection of compiling warnings.
            /// </summary>
            public List<string> Warnings = new List<string>();

            /// <summary>
            /// Indicates if the last compilation yielded any errors.
            /// </summary>
            /// <value>If set to <c>true</c> indicates presence of compilation error(s).</value>
            public bool HasErrors
            {
                get
                {
                    return Errors.Count > 0;
                }
            }

            /// <summary>
            /// Indicates if the last compilation yielded any warnings.
            /// </summary>
            /// <value>If set to <c>true</c> indicates presence of compilation warning(s).</value>
            public bool HasWarnings
            {
                get
                {
                    return Warnings.Count > 0;
                }
            }

            //TODO: implement custom error hendling, to break dependency on csscript library
            /// <summary>
            /// Creates the <see cref="T:System.Exception"/> containing combined error information.
            /// Optionally warnings can also be included in the exception info.
            /// </summary>
            /// <param name="hideCompilerWarnings">The flag indicating if compiler warnings should be included in the error (<see cref="T:System.Exception"/>) info.</param>
            /// <returns>Instance of the <see cref="CompilerException"/>.</returns>
            public CompilerException CreateException()
            {
                var compileErr = new StringBuilder();

                foreach (string err in Errors)
                    compileErr.AppendLine(err);

                foreach (string item in Warnings)
                    compileErr.AppendLine(item);

                CompilerException retval = new CompilerException(compileErr.ToString());

                retval.Data.Add("Errors", Errors);

                retval.Data.Add("Warnings", Warnings);


                return retval;
            }

            /// <summary>
            /// Clears all errors and warnings.
            /// </summary>
            public new void Reset()
            {
                Errors.Clear();
                Warnings.Clear();
                base.Reset();
            }

            /// <summary>
            /// Handles compilation event message.
            /// </summary>
            /// <param name="msg">The compilation event message.</param>
            /// <param name="showFullPath">if set to <c>true</c> [show full path].</param>
            public override void Print(Mono.CSharp.AbstractMessage msg, bool showFullPath)
            {
                string msgInfo = string.Format("{0} {1} CS{2:0000}: {3}", msg.Location, msg.MessageType, msg.Code, msg.Text);
                if (!msg.IsWarning)
                {
                    Errors.Add(msgInfo);
                }
                else
                {
                    Warnings.Add(msgInfo);
                }

            }
        }
    }

    
}