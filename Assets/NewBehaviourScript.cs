using UnityEngine;
using UCompile;

public class NewBehaviourScript : MonoBehaviour {

	// Update is called once per frame
	void Update ()
    {
	
        if(Input.GetKeyDown(KeyCode.Space))
        {
            CSScriptEngine engine = new CSScriptEngine();

            engine.AddUsings("using UnityEngine;");

            string typeCode = @"
                
                                public class ColourChanger : MonoBehaviour
                                {
                                    void Update()
                                    {
                                        if (Input.GetKeyDown(KeyCode.C))
                                        {
                                            this.gameObject.GetComponent<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value);
                                        }
                                    }
                                }

                              ";

            engine.CompileType("ColorChanger", typeCode);

            IScript result = engine.CompileCode(@"
                                                 GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                                 cube.AddComponent<ColourChanger>();
                                               ");
            result.Execute();
        }

	}
}
