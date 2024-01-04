using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ListViewController<T, K> where K : MonoBehaviour
{ 
    public List<K> listItems { get; private set; }

    private K listItemPrefab;
    private Transform listItemContainer;

    private Action<T, K, int> bindViewCallback;

    private Action<K, int> unbindViewCallback;
    
    
    // Populate callback returns data, the view, and the index
    public ListViewController(K prefab, Transform container, Action<T, K, int> bindCallback, Action<K, int> unbindCallback)
    {
        listItemPrefab = prefab;
        listItemContainer = container;
        
        listItems = new List<K>();
    
        bindViewCallback = bindCallback;
        unbindViewCallback = unbindCallback;
        
        // Check the container for any children, if found, delete them..
        foreach (Transform t in listItemContainer)
        {
            GameObject.Destroy(t.gameObject);
        }
    }
    
    
    public void Reset()
    {
        foreach (var item in listItems) item.gameObject.SetActive(false);
    }

    public void SetListItems(T[] data)
    {
        DynamicallySizeViewList(data.Length);
    
        for (var i = 0; i < listItems.Count; ++i)
        {
            var item = listItems[i];
    
            if (i < data.Length)
            {
                item.gameObject.SetActive(true);
                bindViewCallback?.Invoke(data[i], item, i);
            }
            else
            {
                unbindViewCallback?.Invoke(item, i);
                item.gameObject.SetActive(false);
            }
        }
    }

    private void DynamicallySizeViewList(int size)
    {
        if (size >= listItems.Count)
        {
            for (var i = listItems.Count; i < size; ++i)
            {
                var view = GameObject.Instantiate(listItemPrefab, Vector3.zero, Quaternion.identity, listItemContainer);
                view.gameObject.SetActive(false);

                listItems.Add(view);
            }
        }
    }
}