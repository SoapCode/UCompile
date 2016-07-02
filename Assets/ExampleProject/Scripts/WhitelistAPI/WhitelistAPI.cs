using UnityEngine;
using System.Collections;
using ExampleProjectGUI;

//In this namespace developer should place all classes
//he wants to be exposed to users code
namespace WhitelistAPI
{

    #region API for cube movement and other manipulations
    
    public class Cube
    {
        public GameObject _cube = null;

        public Cube(GameObject gob = null)
        {
            if (gob == null)
            {
                _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cube.tag = "DynamicObject";
            }
            else
                _cube = gob;

            if (GameObjectWatcher.Initialized)
                GameObjectWatcher.AllDynamicGOs.Add(_cube);
        }
    }

    public class Mover
    {

        class HelperMB : MonoBehaviour
        {

        }

        static MonoBehaviour mb = null;

        public static void MoveItRight(Cube g, float targetX, float waitingTime = 1f)
        {
            if (mb == null)
            {
                GameObject go = g._cube;
                mb = go.AddComponent<HelperMB>();
            }
           
            mb.StartCoroutine(MoveItActuallyRight(g._cube.transform, targetX,waitingTime));
            //g.go.transform.DOMoveX(5f, 1f);
        }

        static IEnumerator MoveItActuallyRight(UnityEngine.Transform t, float targetX, float waitingTime)
        {
            yield return new WaitForSeconds(waitingTime);

            float temp = t.position.x;

            while (true)
            {
                t.Translate(Vector3.right * Time.deltaTime);
                if (t.position.x >= temp + targetX)
                    break;

                yield return null;
            }
        }

        public static void MoveItLeft(Cube g, float targetX, float waitingTime = 1f)
        {
            if (mb == null)
            {
                GameObject go = g._cube;
                mb = go.AddComponent<HelperMB>();
            }

            mb.StartCoroutine(MoveItActuallyLeft(g._cube.transform, targetX,waitingTime));
        }

        static IEnumerator MoveItActuallyLeft(UnityEngine.Transform t, float targetX, float waitingTime )
        {
            yield return new WaitForSeconds(waitingTime);

            float temp = t.position.x;

            while (true)
            {
                t.Translate(Vector3.left * Time.deltaTime);
                if (t.position.x <= temp -targetX)
                    break;

                yield return null;
            }
        }

        public static void MoveItUp(Cube g, float targetX, float waitingTime = 1f)
        {
            if (mb == null)
            {
                GameObject go = g._cube;
                mb = go.AddComponent<HelperMB>();
            }

            mb.StartCoroutine(MoveItActuallyUp(g._cube.transform, targetX,waitingTime));
            //g.go.transform.DOMoveX(5f, 1f);
        }

        static IEnumerator MoveItActuallyUp(UnityEngine.Transform t, float targetX, float waitingTime )
        {
            yield return new WaitForSeconds(waitingTime);

            float temp = t.position.y;

            while (true)
            {
                t.Translate(Vector3.up * Time.deltaTime);
                if (t.position.y >= temp + targetX)
                    break;

                yield return null;
            }
        }

        public static void MoveItDown(Cube g, float targetX, float waitingTime = 1f)
        {
            if (mb == null)
            {
                GameObject go = g._cube;
                mb = go.AddComponent<HelperMB>();
            }

            mb.StartCoroutine(MoveItActuallyDown(g._cube.transform, targetX,waitingTime));
        }
        
        static IEnumerator MoveItActuallyDown(UnityEngine.Transform t, float targetX, float waitingTime )
        {
            yield return new WaitForSeconds(waitingTime);

            float temp = t.position.y;

            while (true)
            {
                t.Translate(Vector3.down * Time.deltaTime);
                if (t.position.y <= temp - targetX)
                    break;

                yield return null;
            }
        }

        public static void StopAllMovement()
        {
            if (mb != null)
                mb.StopAllCoroutines();
        }

        public static void ChangeColour(GameObject cube)
        {
            cube.GetComponent<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value);
        }

    }

    #endregion

    #region API for bubble sort simulation

    public static class Utility
    {

        class UtilityHelperMB : MonoBehaviour
        {
            public bool AllCoroutinesEnded { get; set; }
        }


        static GameObject _helperMBGO = null;
        static UtilityHelperMB HelperMB
        {
            get
            {
                if (_helperMBGO == null)
                {
                    _helperMBGO = new GameObject("UtilityHelperMBGO");
                    return _helperMBGO.AddComponent<UtilityHelperMB>();
                }
                else
                {
                    return _helperMBGO.GetComponent<UtilityHelperMB>();
                }
            }
        }

        public static void StartAnimation(IEnumerator animation)
        {
            HelperMB.StartCoroutine(animation);
        }

        public static Column[] GenerateColumnArray(params int[] columns)
        {
            Column[] tempArray = new Column[columns.Length];

            for(int i = 0;i < columns.Length;i++)
            {
                tempArray[i] = new Column(columns[i], (float)i);
            }

            return tempArray;
        }

        public static YieldInstruction PrettySwap (ref Column clmn1,ref Column clmn2)
        {
            Vector3 centre = clmn1.VisualRepresentation.transform.position + ((clmn2.VisualRepresentation.transform.position - clmn1.VisualRepresentation.transform.position) / 2f);

            Transform tran1 = clmn1.VisualRepresentation.transform;
            Transform tran2 = clmn2.VisualRepresentation.transform;

            float rad = Mathf.Abs(tran1.position.x - tran2.position.x) / 2f;

            HelperMB.StartCoroutine(SwapPositions(tran1,tran2,rad,centre));

            Column tempclmn = clmn1;
            clmn1 = clmn2;
            clmn2 = tempclmn;

            return new WaitForSeconds(1.5f);
        }

        static IEnumerator SwapPositions(Transform t1,Transform t2,float rad, Vector3 centre)
        {
            float tran1Time = Mathf.PI;
            float tran2Time = Mathf.PI * 2f;

            Vector3 tran1TargetPos = t2.position;
            Vector3 tran2TargetPos = t1.position;

            while ((Mathf.PI * 2f - tran1Time) > 0.00001f)
            {

                t1.position = new Vector3(centre.x + Mathf.Cos(tran1Time) * rad, 0, Mathf.Sin(tran1Time) * rad);
                t2.position = new Vector3(centre.x + Mathf.Cos(tran2Time) * rad, 0, Mathf.Sin(tran2Time) * rad);

                tran1Time += Time.deltaTime * 3f;
                tran2Time += Time.deltaTime * 3f;
                yield return null;
            }

            t1.position = tran1TargetPos;
            t2.position = tran2TargetPos;
            yield return null;
        }

        //PrettySwap using DOTween framework
        //public static YieldInstruction PrettySwap(ref Column clmn1, ref Column clmn2)
        //{
        //    Vector3 centre = clmn1.VisualRepresentation.transform.position + ((clmn2.VisualRepresentation.transform.position - clmn1.VisualRepresentation.transform.position) / 2f);

        //    Transform tran1 = clmn1.VisualRepresentation.transform;
        //    Transform tran2 = clmn2.VisualRepresentation.transform;

        //    float rad = Mathf.Abs(tran1.position.x - tran2.position.x) / 2f;

        //    Tween tw = DOTween.To((x) => tran1.DOMove(new Vector3(centre.x + Mathf.Cos(x) * rad, 0, Mathf.Sin(x) * rad), 0), Mathf.PI, Mathf.PI * 2f, 1f);
        //    DOTween.To((x) => tran2.DOMove(new Vector3(centre.x + Mathf.Cos(x) * rad, 0, Mathf.Sin(x) * rad), 0), 0, Mathf.PI, 1f);

        //    Column tempclmn = clmn1;
        //    clmn1 = clmn2;
        //    clmn2 = tempclmn;

        //    return tw.WaitForCompletion();
        //}

        public static float RandomRange(float min, float max)
        {
            return Random.Range(min,max);
        }

    }

    public class Column
    { 

        public int _actualInt;
        GameObject _visualRepresentation;
        public GameObject VisualRepresentation { get { return _visualRepresentation; }
            internal set { _visualRepresentation = value; }
        }

        public void ChangeColour(Color color)
        {
            foreach(Transform child in _visualRepresentation.transform)
            {
                child.GetComponent<Renderer>().material.color = color;
            }
        }

        public Column(int actualInt, float positionX = 0, float positionY = 0, float positionZ = 0)
        {

            _actualInt = actualInt;
            _visualRepresentation = new GameObject();

            Vector3 position = new Vector3(positionX,positionY,positionZ);
            _visualRepresentation.transform.position = position;
            _visualRepresentation.tag = "DynamicObject";

            for (int i = 0;i < actualInt;i++)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                temp.transform.SetParent(_visualRepresentation.transform);
                temp.transform.localPosition = new Vector3(0,i,0);
            }
            if(GameObjectWatcher.Initialized)
               GameObjectWatcher.AllDynamicGOs.Add(_visualRepresentation);
        }

        void Destroy()
        {
            if (GameObjectWatcher.Initialized)
                GameObjectWatcher.AllDynamicGOs.Remove(_visualRepresentation);

            GameObject.Destroy(_visualRepresentation);
        }

        public static bool operator < (Column clmn1, Column clmn2)
        {
            return clmn1._actualInt < clmn2._actualInt;
        }

        public static bool operator >(Column clmn1, Column clmn2)
        {
            return clmn1._actualInt > clmn2._actualInt;
        }

    }
    #endregion

}