﻿using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if ( instance == null )
            {
                instance = FindObjectOfType<T> ();
                if ( instance == null )
                {
                    GameObject obj = new GameObject ();
                    obj.name = typeof ( T ).Name;
                    instance = obj.AddComponent<T> ();
                }
            }

            wasCreated = true;
            return instance;
        }
    }

    public static bool wasCreated = false;
    
    public static bool Exist()
    {
        return instance;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    protected virtual void Awake ()
    {
        if ( instance == null )
        {
            instance = this as T;
            wasCreated = true;
            //DontDestroyOnLoad ( gameObject );
        }
    }
}