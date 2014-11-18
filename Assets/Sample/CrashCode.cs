using UnityEngine;
using System.Collections;

public class CrashCode : MonoBehaviour {

	void OnGUI()
	{
		if( GUILayout.Button("Let's Crash!!")  )
		{
			for(int i=0; i<100;)
			{
				Debug.Log("hoge");
			}
		}
	}
}
