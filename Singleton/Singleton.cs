﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DanielDangToolkit
{
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
   	public bool PersistThroughSceneLoads = true;

	private static T instance;
	public static T Instance { 

		get {

            if (applicationIsQuitting)
            {
                return null;
            }

            if (instance == null)
			{
				new GameObject(typeof(T).ToString(), typeof(T));
			}
			return instance;
		} 
		private set {
			instance = value;
		}
	}

    private static bool applicationIsQuitting = false;
    private static int duplicateDeleteCounter = 0;

    public void OnDestroy()
    {
    	if (PersistThroughSceneLoads)
    	{
		if (duplicateDeleteCounter > 0)
		{
		    duplicateDeleteCounter--;
		    return;
		}
		// Debug.Log("Gets destroyed");
		applicationIsQuitting = true;
	}
    }

	protected virtual void Awake()
	{ 
		if (PersistThroughSceneLoads)
		{
		    if (instance != null)
		    {
			Destroy(this);
			Destroy(gameObject);
			duplicateDeleteCounter++;
			return;
		    }
		    Instance = (T)this;
		    DontDestroyOnLoad(gameObject);
		    OnAwake();
		}
		else
		{
		    if (instance != null)
		    {
			Destroy(instance);
			Destroy(instance.gameObject);
			instance = null;
		    }
		    Instance = (T)this;
		    OnAwake();
		}
    }

    protected virtual void OnAwake()
    {

    }
}
}
