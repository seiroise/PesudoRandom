using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
	[SerializeField]
	Vector3 _axis;

	[SerializeField]
	float _rotationSpeed = 30f;

	private void Update()
	{
		Quaternion rot = Quaternion.AngleAxis(_rotationSpeed * Time.deltaTime, _axis.normalized);
		transform.rotation *= rot;
	}
}
