using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UCompile;

namespace ExampleProjectGUI
{

    public class TypeEditorWindowControl : MonoBehaviour
    {

        [HideInInspector]
        public bool EditingAlreadyExistingType;

        public InputField CodeField;
        public InputField IDField;

        void Start()
        {
            EditingAlreadyExistingType = false;
        }

        void Update()
        {
            if (EditingAlreadyExistingType)
                IDField.interactable = false;
            else
                IDField.interactable = true;
        }

        void OnDisable()
        {
            EditingAlreadyExistingType = false;
            CodeField.text = "";
            IDField.text = "";
        }

        public void OnCompileAndAddTypeButton()
        {
            EventManager.Instance.QueueEvent(new CompileTypeEvent(IDField.text, CodeField.text, EditingAlreadyExistingType));
            EditingAlreadyExistingType = true;
        }
    }
}