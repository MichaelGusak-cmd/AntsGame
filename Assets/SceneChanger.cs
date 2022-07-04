using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
	private string currScene;
	public string prevScene;

	void Start()
	{
		DontDestroyOnLoad(this.gameObject);  //Allow this object to persist between scene changes
		prevScene = "Menu";
		currScene = "Menu";
	}

	public void ChangeScene(string sceneName)
	{
		prevScene = currScene;
		currScene = sceneName;
		SceneManager.LoadScene(sceneName);
	}

	public void PrevScene()
	{
		currScene = prevScene;
		prevScene = "DeathMenu";
		SceneManager.LoadScene(currScene);
	}
}
