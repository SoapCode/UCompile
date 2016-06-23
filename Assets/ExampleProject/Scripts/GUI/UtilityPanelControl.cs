using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UCompile;
using System;

namespace ExampleProjectGUI
{

    public class UtilityPanelControl : MonoBehaviour
    {

        public GameObject ClassNameButton;
        public GameObject MBList;

        public GameObject TypeEditorWindow;

        void OnApplicationQuit()
        {
            EventManager.Instance.RemoveListener<TypeCompilationSucceededEvent>(OnCompileAndAddTypeButton);
            EventManager.Instance.RemoveListener<DeleteTypeEvent>(OnDeleteTypeButton);
            EventManager.Instance.RemoveListener<EditTypeEvent>(OnEditTypeButton);

        }

        void Awake()
        {
            EventManager.Instance.AddListener<TypeCompilationSucceededEvent>(OnCompileAndAddTypeButton);
            EventManager.Instance.AddListener<DeleteTypeEvent>(OnDeleteTypeButton);
            EventManager.Instance.AddListener<EditTypeEvent>(OnEditTypeButton);

        }

        public void OnAddTypeButton()
        {
            TypeEditorWindow.SetActive(true);
            TypeEditorWindow.GetComponent<TypeEditorWindowControl>().EditingAlreadyExistingType = false;
        }

        public void OnCompileAndAddTypeButton(TypeCompilationSucceededEvent ev)
        {
            if (!ev.EditingAlreadyExistingType || (ev.EditingAlreadyExistingType && !CheckIfExistsInChildrenOfByTipeID(MBList.transform,ev.TypeID)))
            {
                GameObject classNameButton = Instantiate(ClassNameButton);
                classNameButton.transform.SetParent(MBList.transform);
                classNameButton.transform.GetChild(0).GetComponent<Text>().text = ev.TypeID;
                ClassNameButton temp = classNameButton.GetComponent<ClassNameButton>();
                temp.TypeCode = ev.Code;
                temp.TypeID = ev.TypeID;
            }
            else
            {
                foreach (ClassNameButton btn in MBList.transform.GetComponentsInChildren<ClassNameButton>())
                {
                    if (btn.TypeID == ev.TypeID)
                    {
                        btn.TypeCode = ev.Code;
                    }
                }
            }
        }

        bool CheckIfExistsInChildrenOfByTipeID(Transform parent, string typeID)
        {
            ClassNameButton[] allChildren = parent.transform.GetComponentsInChildren<ClassNameButton>();

            for(int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].TypeID == typeID)
                    return true;
            }

            return false;
        }

        public void OnEditTypeButton(EditTypeEvent ev)
        {
            TypeEditorWindow.SetActive(true);
            TypeEditorWindow.GetComponent<TypeEditorWindowControl>().EditingAlreadyExistingType = true;
            TypeEditorWindow.GetComponent<TypeEditorWindowControl>().IDField.text = ev.TypeID;
            TypeEditorWindow.GetComponent<TypeEditorWindowControl>().CodeField.text = ev.Code;
        }

        public void OnDeleteTypeButton(DeleteTypeEvent ev)
        {
            foreach (ClassNameButton btn in MBList.transform.GetComponentsInChildren<ClassNameButton>())
            {
                if (btn.TypeID == ev.TypeID)
                {
                    Destroy(btn.gameObject);
                }
            }
        }

        public void OnDeleteAllGameObjectsButton()
        {
            foreach(GameObject go in GameObject.FindGameObjectsWithTag("DynamicObject"))
            {
                Destroy(go);
            }
        }

    }
}
