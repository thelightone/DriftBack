using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class game controller.
/// </summary>
public class GameController :MonoBehaviour
{
	[SerializeField] KeyCode NextCarKey = KeyCode.N;
	[SerializeField] UnityEngine.UI.Button NextCarButton;
	public static GameController Instance;
	public static CarController PlayerCar { get { return Instance.m_PlayerCar; } }
	public static bool RaceIsStarted
	{
		get
		{
			if (RaceFlowManager.Instance == null)
				return true;
			return RaceFlowManager.Instance.IsRaceStarted;
		}
	}

	public static bool RaceIsEnded
	{
		get
		{
			return RaceFlowManager.Instance != null && RaceFlowManager.Instance.IsRaceFinished;
		}
	}

	CarController m_PlayerCar;
	List<CarController> Cars = new List<CarController>();
	int CurrentCarIndex = 0;

	protected virtual void Awake ()
	{

		Instance = this;

		//Find all cars in current game (inactive skin variants must be included).
		Cars.AddRange (FindObjectsByType<CarController> (FindObjectsInactive.Include, FindObjectsSortMode.None));
		Cars = Cars.OrderBy (c => c.name).ToList();

		foreach (var car in Cars)
		{
			var userControl = car.GetComponent<UserControl>();
			var audioListener = car.GetComponent<AudioListener>();

			if (userControl == null)
			{
				userControl = car.gameObject.AddComponent<UserControl> ();
			}

			if (audioListener == null)
			{
				audioListener = car.gameObject.AddComponent<AudioListener> ();
			}

			userControl.enabled = false;
			audioListener.enabled = false;
		}

		m_PlayerCar = Cars[0];
		m_PlayerCar.GetComponent<UserControl> ().enabled = true;
		m_PlayerCar.GetComponent<AudioListener> ().enabled = true;

		if (NextCarButton)
        {
			NextCarButton.onClick.AddListener (NextCar);
		}
	}

	void Update () 
	{ 
		if (Input.GetKeyDown (NextCarKey))
		{
			NextCar ();
		}

	}

	private void NextCar ()
	{
		m_PlayerCar.GetComponent<UserControl> ().enabled = false;
		m_PlayerCar.GetComponent<AudioListener> ().enabled = false;

		CurrentCarIndex = MathExtentions.LoopClamp (CurrentCarIndex + 1, 0, Cars.Count);

		m_PlayerCar = Cars[CurrentCarIndex];
		m_PlayerCar.GetComponent<UserControl> ().enabled = true;
		m_PlayerCar.GetComponent<AudioListener> ().enabled = true;
	}

	/// <summary>
	/// Переключает управление и ссылку PlayerCar на машину выбранного скина (несколько CarController в сцене).
	/// </summary>
	public void SwitchToPlayerCar (CarController newPlayerCar)
	{
		if (newPlayerCar == null || newPlayerCar == m_PlayerCar)
			return;

		if (!Cars.Contains (newPlayerCar))
		{
			Cars.Add (newPlayerCar);
			Cars = Cars.OrderBy (c => c.name).ToList ();
		}

		m_PlayerCar.GetComponent<UserControl> ().enabled = false;
		m_PlayerCar.GetComponent<AudioListener> ().enabled = false;

		m_PlayerCar = newPlayerCar;
		CurrentCarIndex = Mathf.Max (0, Cars.IndexOf (m_PlayerCar));

		m_PlayerCar.GetComponent<UserControl> ().enabled = true;
		m_PlayerCar.GetComponent<AudioListener> ().enabled = true;
	}
}
