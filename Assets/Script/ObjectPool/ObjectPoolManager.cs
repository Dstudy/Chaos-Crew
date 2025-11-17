using System;
using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoSingletonDontDestroyOnLoad<ObjectPoolManager>
{
    private Transform _objectPoolParent;

    private Dictionary<string, List<GameObject>> _objectPool;

    public override void Init()
    {
        base.Init();
        var container = new GameObject("Container");
        container.transform.SetParent(transform);
        _objectPoolParent = container.transform;
        _objectPool = new Dictionary<string, List<GameObject>>();
    }

    public void Push<T>(T obj, string poolID) where T : MonoBehaviour
    {
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_objectPoolParent);
        if (_objectPool.TryGetValue(poolID, out var value))
        {
            value.Add(obj.gameObject);
        }
        else
        {
            _objectPool.Add(poolID, new List<GameObject> { obj.gameObject });
        }
    }

    public void Push<T>(T obj) where T : MonoBehaviour, IGetPoolID
    {
        Push(obj, obj.GetPoolID());
    }

    public T Get<T>(string objectId, Transform parent, Func<T> CreateNewObject)
            where T : MonoBehaviour, IGetPoolID
    {
        T obj;
        if (!_objectPool.TryGetValue(objectId, out var value))
        {
            _objectPool.Add(objectId, new List<GameObject>());
            value = _objectPool[objectId];
        }

        if (value.Count > 0)
        {
            obj = _objectPool[objectId][0].GetComponent<T>();
            _objectPool[objectId].RemoveAt(0);
            obj.gameObject.SetActive(true);
            obj.transform.SetParent(parent);
            return obj;
        }


        obj = CreateNewObject?.Invoke();
        if (obj == null)
        {
            return null;
        }

        obj.gameObject.SetActive(true);
        obj.transform.SetParent(parent);
        return obj;
    }

    public void SaveAllPool()
    {
        foreach (var keyValuePair in _objectPool)
        {
            var allObject = keyValuePair.Value;
            foreach (var obj in allObject)
            {
                if (obj.transform.parent != _objectPoolParent)
                {
                    obj.transform.SetParent(_objectPoolParent);
                }
            }
        }
    }
}