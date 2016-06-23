using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UCompile;

namespace ExampleProjectGUI
{

    public class EditMBPanel : MonoBehaviour
    {

        ClassNameButton _classNameButton = null;

        public Button EditButton;
        public Button DeleteButton;
        [HideInInspector]
        public GameObject TypeEditorWindow;

        void Start()
        {
            _classNameButton = transform.parent.GetComponent<ClassNameButton>();
        }

        public void OnEditButton()
        {
            EventManager.Instance.QueueEvent(new EditTypeEvent(_classNameButton.TypeID, _classNameButton.TypeCode));
        }

        public void OnDeleteButton()
        {
            EventManager.Instance.QueueEvent(new DeleteTypeEvent(_classNameButton.GetComponent<ClassNameButton>().TypeID));
        }
    }
}
