using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Singleton<T> where T : class, new()
	{
		public static T Instance
		{
			get
			{
				bool flag = Singleton<T>._instance == null;
				if (flag)
				{
					Singleton<T>._instance = Activator.CreateInstance<T>();
				}
				return Singleton<T>._instance;
			}
		}


		protected Singleton()
		{
		}


		private static T _instance;
	}
public class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
	
    public virtual void Awake()
    {
        if(Instance != this) Destroy(this.gameObject);
	}
    public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindAnyObjectByType<T>();
			}
			return _instance;
		}
		set
		{
			_instance = value;
		}
	}

	public virtual void OnApplicationQuit()
	{
		_isQuit = true;
		base.StopAllCoroutines();
	}

	private static T _instance;

	private static bool _isQuit;
}