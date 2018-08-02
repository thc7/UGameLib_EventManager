using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Libs{
    
    //公共委托事件 返回 true 中断事件向后传播
    public delegate bool OnEvent(string eventName, object data);

    public interface IEventListener
    {
        bool OnEvent(string eventName, object data);
    }

    class ListenerSorterComparer : IComparer<ListenerSorter>
    {
        public int Compare(ListenerSorter x, ListenerSorter y)
        {
            //return x.level - y.level;//降序
            return y.level - x.level;//升序
        }
    }

    class ListenerSorter
    {
        public int level;
        public System.Delegate deleg;

        public ListenerSorter(int levelp, System.Delegate delegp)
        {
            level = levelp;
            deleg = delegp;
        }

        public override bool Equals(object obj)
        {
            ListenerSorter listenerSorter = (ListenerSorter)obj;
            return deleg.Equals(listenerSorter.deleg);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        //排序时使用 降序 
        /*
    	public int CompareTo (object x){
    		return level - ((ListenerSorter)x).level;  //降序
    		//return ((ListenerSorter)x).level - level;//升序
    	}
    	*/
    }

    public class EventManager : MonoBehaviour
    {
        
        class SendData{
            public string eventName;
            public OnEvent onEvent;
            public object data;
            public bool isStopAble;
        }

        //用于游戏中的事件回调管理
        private static EventManager instance;

        public static EventManager getInstance()
        {
            if (instance == null)
            {
                GameObject eventManagerGameObject = new GameObject("EventManager");
                DontDestroyOnLoad(eventManagerGameObject);
                instance = eventManagerGameObject.AddComponent<EventManager>();
            }
            return instance;
        }

        public static EventManager initForGameObject(GameObject dontDestroyOnLoadGameObject) {

            if (instance == null) {
            instance = dontDestroyOnLoadGameObject.AddComponent<EventManager>();
            }
            return instance;
        }

        public static EventManager I
        {
            get
            {
                return instance;
            }
        }
        //事件列表
        private Dictionary<string, OnEvent> eventsDic = new Dictionary<string, OnEvent>();
        //排序
        private Dictionary<string, List<ListenerSorter>> sortDic = new Dictionary<string, List<ListenerSorter>>();

        private ListenerSorterComparer listenerSorterComparer = new ListenerSorterComparer();

        private Queue<SendData> sendQueue = new Queue<SendData>();

        public void Add(string eventName, OnEvent onEventp)
        {
            List<ListenerSorter> list;
            sortDic.TryGetValue(eventName, out list);

            if (list != null) {
                AddDefLevel(eventName, onEventp);
                return;
            }

            OnEvent onEvent;
            eventsDic.TryGetValue(eventName, out onEvent);

            if (onEvent == null) {

                if (eventsDic.ContainsKey(eventName)) {
                    eventsDic.Remove(eventName);
                }

                eventsDic.Add(eventName,onEventp);
            }
            else {
                System.Delegate[] invocationList = onEvent.GetInvocationList();

                for (int i = invocationList.Length - 1; i >= 0; i--)
                {
                    if (invocationList[i].Method.Equals(onEventp.Method))
                    {
                        Debug.LogError("eventName " + eventName + ",Method " + onEventp.Method + " is already in eventsDic!!!!");
                        return;
                    }
                }
                
                eventsDic[eventName] += onEventp;
            }
        }

        private void AddDefLevel(string eventName, OnEvent onEventp) 
        {
            Add(eventName, onEventp, 0);
        }

        public void Add(string eventName, OnEvent onEventp, int level)
        {
            OnEvent onEvent;
            eventsDic.TryGetValue(eventName, out onEvent);

            List<ListenerSorter> list;
            sortDic.TryGetValue(eventName, out list);

            if (onEvent != null && list == null) {
                Debug.LogError("eventName "+ eventName + " is not all target in sortDic!!!!");
                return;
            }
                
            if (onEvent == null)
            {
                if (eventsDic.ContainsKey(eventName))
                {
                    eventsDic.Remove(eventName);
                }
                eventsDic.Add(eventName, onEventp);
            }
            else
            {
                eventsDic[eventName] += onEventp;
            }

            ListenerSorter listenerSorter = new ListenerSorter(level, onEventp);

            if (list == null)
            {
                list = new List<ListenerSorter>();
                sortDic.Add(eventName, list);
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (listenerSorter.Equals(list[i]))
                {
                    Debug.LogWarning("Added Again >>>>>>>>>>>>>" + eventName);
                    return;
                }
            }

            list.Add(listenerSorter);
            list.Sort(listenerSorterComparer);

            System.Delegate[] invocationList = eventsDic[eventName].GetInvocationList();
           
            for (int i = invocationList.Length - 1; i >= 0; i--)
            {
                eventsDic[eventName] -= (OnEvent)invocationList[i];
            }

            for (int i = 0; i < list.Count; i++)
            {
                eventsDic[eventName] += (OnEvent)list[i].deleg;
            }
        }

        public void Rem(string eventName, OnEvent onEventp)
        {
            OnEvent onEvent;
            eventsDic.TryGetValue(eventName, out onEvent);

            if (onEvent == null) { 
                Debug.LogWarning("EventManager.Send EventName " + eventName + " is not in m_dicEvents！！");
            }else{

                List<ListenerSorter> list;
                sortDic.TryGetValue(eventName, out list);

                if (list != null)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        ListenerSorter listenerSorter = list[i];
                        if (listenerSorter.deleg.Method.Equals(onEventp.Method))
                        {
                            list.RemoveAt(i);
                            Debug.Log("rem sortDic " + "eventName " + eventName + "Target " + onEventp.Target + " Method " + onEventp.Method);
                        }
                    }
                }

                System.Delegate[] invocationList = onEvent.GetInvocationList();

                    for (int i = invocationList.Length-1; i >= 0; i--){
                    if (invocationList[i].Method.Equals(onEventp.Method)) {

                        Debug.Log("rem eventsDic " + "eventName " + eventName + "Target " + onEventp.Target + " Method " + onEventp.Method);

                        eventsDic[eventName] -= onEventp;
                    }
                }
               
            }

        }

        public void RemByTarget(string eventName, object target)
        {
            OnEvent onEvent;
            eventsDic.TryGetValue(eventName, out onEvent);

            if (onEvent == null)
            {
                Debug.LogWarning("EventManager.Send EventName " + eventName + " is not in eventsDic！！");
            }else{
                System.Delegate[] invocationList = eventsDic[eventName].GetInvocationList();

                for (int i = invocationList.Length - 1; i >= 0; i--){
                    if (invocationList[i].Target.Equals(target)) {
                        eventsDic[eventName] -= (OnEvent)invocationList[i];
                    }
                }
            }

            List<ListenerSorter> list;
            sortDic.TryGetValue(eventName, out list);

            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ListenerSorter listenerSorter = list[i];
                    if (listenerSorter.deleg.Target.Equals(target))
                    {
                        list.RemoveAt(i);
                        Debug.Log("rem" + "eventName " + eventName + "Target " + target);
                    }
                }
            }

        }

        public void RemByTargetAll(object target)
        {
            List<string> keyList = new List<string>(eventsDic.Keys);

            foreach (string eventName in keyList)
            {
                OnEvent onEvent;
                eventsDic.TryGetValue(eventName, out onEvent);

                if (onEvent == null) continue;

                System.Delegate[] invocationList = onEvent.GetInvocationList();
                for (int i = invocationList.Length - 1; i >= 0; i--){
                    if (invocationList[i].Target.Equals(target))
                        eventsDic[eventName] -= (OnEvent)invocationList[i];
                }

                List<ListenerSorter> list;
                sortDic.TryGetValue(eventName, out list);

                if (list != null)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        ListenerSorter listenerSorter = list[i];
                        if (listenerSorter.deleg.Target.Equals(target))
                        {
                            list.RemoveAt(i);
                            Debug.Log("rem" + "eventName " + eventName + "Target " + target);
                        }
                    }
                }//end if (list != null)

            }//foreach (string eventName in keyList)

        }

        public void RemByEventNameAll(string eventName)
        {
            eventsDic.Remove(eventName);
            sortDic.Remove(eventName);
        }

        public void RemAll()
        {
            eventsDic.Clear();
            sortDic.Clear();
        }

        public void SendImmediate(string eventName, object data = null,bool isStopAble = false)
        {
            OnEvent onEvent;
            eventsDic.TryGetValue(eventName, out onEvent);
            
            if (onEvent == null)
            {
                Debug.LogWarning("EventManager.Send EventName "+ eventName + " is not in eventsDic！！");
            }
            else
            {
                if (isStopAble)
                {
                    System.Delegate[] invocationList = onEvent.GetInvocationList();
                    for (int i = 0; i < invocationList.Length; i++)
                    {
                        if (((OnEvent)invocationList[i])(eventName, data))
                        {
                            Debug.LogWarning("EventName "+ eventName + " Stop by Target " + invocationList[i].Target );
                            return;
                        }
                    }
                } else { 
                    onEvent(eventName, data);
                }
            }
        }

        public void Send(string eventName, object data = null,bool isStopAble = false){

            SendData sendData = GetFreeSendData();

            sendData.eventName = eventName;
            sendData.data = data;
            sendData.isStopAble = isStopAble;

            sendQueue.Enqueue(sendData);
        }

        SendData GetFreeSendData()
        {
            return new SendData();
        }

        public int sendMax = 8;
        public int curSendCount = 0;
        SendData curSendData;

        void Update()
        {
            curSendCount = 0;

            while(sendQueue.Count > 0 && curSendCount < sendMax){

                curSendData = sendQueue.Dequeue();
                SendImmediate(curSendData.eventName,curSendData.data,curSendData.isStopAble);

                curSendCount++;
            }

            curSendData = null;
        }

    }

    /// <summary>
    /// 简化调用接口
    /// </summary>
    public class EM : EventManager{

        public static EventManager I{
            get{
                return EventManager.getInstance();
            }
        }
    }

}