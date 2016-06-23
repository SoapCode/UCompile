using UCompile;
using UnityEngine;
using UnityEngine.UI;
using WhitelistAPI;

namespace ExampleProjectGUI
{

    public class ConsoleWindowPanel : MonoBehaviour
    {
        //All elements of this panel
        public GameObject ConsoleWindow;
        public GameObject CompileButton;
        public GameObject ExecuteButton;
        public GameObject OutputWindow;
        public GameObject AnimationToggle;
        public GameObject CodeToggle;

        InputField _consoleWindow;
        Button _compileButton;
        Button _executeButton;
        Toggle _animationToggle;
        Toggle _codeToggle;

        void Awake()
        {
            _consoleWindow = ConsoleWindow.GetComponent<InputField>();
            _compileButton = CompileButton.GetComponent<Button>();
            _executeButton = ExecuteButton.GetComponent<Button>();
            _animationToggle = AnimationToggle.GetComponent<Toggle>();
            _codeToggle = CodeToggle.GetComponent<Toggle>();
        }

        public void OnCompileButton()
        {
            if (_consoleWindow.text != "")
            {
                EventManager.Instance.QueueEvent(new CompilationEvent(_consoleWindow.text));
                //_compileButton.interactable = false;
                //_consoleWindow.interactable = false;
                //_executeButton.interactable = false;
            }
        }

        public void OnExecuteButton()
        {
            if (_consoleWindow.text != "")
            {
                if (GameObjectWatcher.Initialized)
                    EventManager.Instance.QueueEvent(new BeforeExecutionEvent());
                EventManager.Instance.QueueEvent(new ExecutionEvent());
                //_compileButton.interactable = false;
                //_consoleWindow.interactable = false;
                //_executeButton.interactable = false;
            }
        }

        public void OnAnimationToggleValueChanged()
        {

            if (!_animationToggle.isOn && !_codeToggle.isOn)
            {
                _codeToggle.interactable = true;
                _consoleWindow.interactable = false;
                _executeButton.interactable = false;
                _compileButton.interactable = false;
            }
            else if (_animationToggle.isOn && !_codeToggle.isOn)
            {
                if (_codeToggle.isOn)
                    _codeToggle.isOn = false;
                _codeToggle.interactable = false;
                _consoleWindow.interactable = true;
                _executeButton.interactable = true;
                _compileButton.interactable = true;
                EventManager.Instance.QueueEvent(new CurrentlyCompilingAnimationEvent());
            }
            else if (_animationToggle.isOn && _codeToggle.isOn)
            {
                _animationToggle.isOn = false;
            }
        }

        public void OnCodeToggleValueChanged()
        {
            if (!_animationToggle.isOn && !_codeToggle.isOn)
            {
                _animationToggle.interactable = true;
                _consoleWindow.interactable = false;
                _executeButton.interactable = false;
                _compileButton.interactable = false;
            }
            else if (!_animationToggle.isOn && _codeToggle.isOn)
            {
                if (_animationToggle.isOn)
                    _animationToggle.isOn = false;
                _animationToggle.interactable = false;
                _consoleWindow.interactable = true;
                _executeButton.interactable = true;
                _compileButton.interactable = true;
                EventManager.Instance.QueueEvent(new CurrentlyCompilingCodeEvent());
            }
            else if (_animationToggle.isOn && _codeToggle.isOn)
            {
                _codeToggle.isOn = false;
            }
        }
    }
}