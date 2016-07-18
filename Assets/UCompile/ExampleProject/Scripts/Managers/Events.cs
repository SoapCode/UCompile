    /// In this file all events in application
    /// shall be kept

    #region GUI events

    public class CompilationEvent : GameEvent
    {
        public string Code { get; private set; }

        public CompilationEvent(string code)
        {
            Code = code;
        }
    }

    public class ExecutionEvent : GameEvent
    {
        
    }

    public class BeforeExecutionEvent : GameEvent
    {

    }

    public class CompileTypeEvent : GameEvent
    {
        public string Code { get; private set; }
        public string TypeID { get; private set; }

        public bool EditingAlreadyExistingType = false;


        public CompileTypeEvent(string typeID,string code, bool typeAlreadyExists)
        {
            Code = code;
            TypeID = typeID;
            EditingAlreadyExistingType = typeAlreadyExists;
        }
    }

    public class TypeCompilationSucceededEvent : CompileTypeEvent
    {
        public TypeCompilationSucceededEvent(CompileTypeEvent ev) : base(ev.TypeID,ev.Code,ev.EditingAlreadyExistingType) { }
    }

    public class EditTypeEvent : GameEvent
    {
        public string Code { get; private set; }
        public string TypeID { get; private set; }

        public EditTypeEvent(string typeID, string code)
        {
            Code = code;
            TypeID = typeID;
        }

    }

    public class DeleteTypeEvent : GameEvent
    {
        public string TypeID { get; private set; }

        public DeleteTypeEvent(string typeID)
        {
            TypeID = typeID;
        }
    }

    public class CurrentlyCompilingAnimationEvent : GameEvent
    {

    }

    public class CurrentlyCompilingCodeEvent : GameEvent
    {

    }
    #endregion