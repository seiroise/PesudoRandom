using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DrawMeshNowTest : MonoBehaviour
{
	public Mesh mesh;
	public Material material;

	private void Update()
	{
		if(mesh && material)
		{
			material.SetPass(0);
			Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix, 0);
		}
	}

	private void OnPostRender()
	{
		if(mesh && material)
		{
			material.SetPass(0);
			Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix, 0);
		}
	}
}
