using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	private GameObject sceneManager;

	public void Start()
	{
		sceneManager = GameObject.FindWithTag("SceneManager");
	}

	public void PlayGame()
	{
		ChangeToScene("GameSettings");
	}

	public void DevModeSlime()
    {
		ChangeToScene("Slime");
    }

	public void QuitGame()
	{
		Debug.Log("Quit!");
		Application.Quit();
	}

	private void ChangeToScene(string scene)
	{
		sceneManager.GetComponent<SceneChanger>().ChangeScene(scene);
	}

	public void OnMouseOver()
	{
		Debug.Log("mouse over");
	}
	public void OnMouseExit()
	{
		//The mouse is no longer hovering over the GameObject so output this message each frame
		Debug.Log("Mouse is no longer on GameObject.");
	}
}
