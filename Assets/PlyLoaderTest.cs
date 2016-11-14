using UnityEngine;
using System.Collections;
//using Loaders;

public class PlyLoaderTest : MonoBehaviour {
	// Use this for initialization
	void Start () {
    PlyLoader loader = new PlyLoader();
    //Mesh [] mesh = loader.load("2PTC_EI_bs_1.ply");
    Mesh [] mesh = loader.load("cube.ply");

    for (int i = 0; i != mesh.Length; ++i) {
      GameObject g = new GameObject();
      mesh[i].name = g.name = "mesh" + i;
      MeshFilter mf = g.AddComponent<MeshFilter>();
      mf.mesh = mesh[i];
      MeshRenderer mr = g.AddComponent<MeshRenderer>();
    }
  }
	
	// Update is called once per frame
	void Update () {
	  
	}
}
