using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Graphics.DrawMeshのテスト
/// </summary>
[ExecuteInEditMode]
public class DrawMeshTest : MonoBehaviour
{
	public Mesh mesh;
	public Material material;

	private void Update()
	{
		if(mesh && material)
		{
			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer);
		}
	}
}
