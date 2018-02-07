using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DDT = DanielDangToolkit;

namespace DanielDangToolkit.EventManager
{

    public class EventManager : MonoBehaviour
    {
        
        // TO USE:
        // private void OnEnable()
        // {
        //    EventManager.StartListening(Events.SomeEvent, SomeFunction);
        // }

        // private void OnDisable()
        // {
        //    EventManager.StopListening(Events.SomeEvent, SomeFunction);
        // }
        // 

        // To Trigger:
        // 
        //  EventManager.TriggerEvent(Events.SomeEvent);
        // 

        private static EventManager instance;

        public static EventManager Instance
        {
            get
            {

                if (instance == null)
                {
                    new GameObject(typeof(EventManager).ToString(), typeof(EventManager));
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }


        private Dictionary<string, UnityEvent> eventDictionary = new Dictionary<string, UnityEvent>();
        private object triggerData;

        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void StartListeningInstance(string eventName, UnityAction listener)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                Instance.eventDictionary.Add(eventName, thisEvent);
            }
        }

        void StopListeningInstance(string eventName, UnityAction listener)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        void TriggerEventInstance(string eventName)
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                triggerData = null;
                thisEvent.Invoke();
            }
        }

        void TriggerEventInstance<T>(string eventName, T data) where T : class
        {
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                triggerData = data;
                thisEvent.Invoke();
                triggerData = null;
            }
        }

        public static T GetTriggerData<T>() where T : class
        {
            if (Instance.triggerData != null)
                return (T)Instance.triggerData;
            else
                return null;
        }

        public static void TriggerEvent(Events eventEnum)
        {
            Instance.TriggerEventInstance(eventEnum.ToString());
        }

        public static void TriggerEvent(string eventName)
        {
            Instance.TriggerEventInstance(eventName);
        }

        public static void TriggerEvent<T>(Events eventEnum, T data) where T : class
        {
            TriggerEvent(eventEnum.ToString(), data);
        }

        public static void TriggerEvent<T>(string eventName, T data) where T : class
        {
            if (Instance == null)
                return;

            Instance.TriggerEventInstance(eventName, data);
        }

        public static void StopListening(Events eventEnum, UnityAction listener)
        {
            StopListening(eventEnum.ToString(), listener);
        }

        public static void StopListening(string eventName, UnityAction listener)
        {
            if (Instance == null)
                return;

            Instance.StopListeningInstance(eventName, listener);
        }

        public static void StartListening(Events eventEnum, UnityAction listener)
        {
            StartListening(eventEnum.ToString(), listener);
        }

        public static void StartListening(string eventName, UnityAction listener)
        {
            if (Instance == null)
                return;

            Instance.StartListeningInstance(eventName, listener);
        }
    }

}