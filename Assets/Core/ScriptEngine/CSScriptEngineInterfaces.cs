using System.Collections;

/// <summary>
/// Interface for script object defined globally.
/// </summary>
public interface IScript
{
    void Execute();
    IEnumerable Coroutine();
}
