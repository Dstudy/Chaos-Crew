using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform viewportTransform;
    public RectTransform contentPanelTransform;
    public HorizontalLayoutGroup HLG;

    public RectTransform[] ItemList;

    private Vector2 Oldvelocity;
    private bool isUpdated;
    
    private void Start()
    {
        isUpdated = false;
        Oldvelocity = Vector2.zero;
        int ItemsToAdd = Mathf.CeilToInt(viewportTransform.rect.width / (ItemList[0].rect.width + HLG.spacing));

        for (int i = 0; i < ItemsToAdd; i++)
        {
            RectTransform RT = Instantiate(ItemList[i%ItemList.Length], contentPanelTransform);
            RT.SetAsLastSibling();
        }
        
        for (int i = 0; i < ItemsToAdd; i++)
        {
            int num = ItemList.Length - i -1;
            while (num<0)
            {
                num+=ItemList.Length;
            }
            RectTransform RT = Instantiate(ItemList[num], contentPanelTransform);
            RT.SetAsFirstSibling();
        }

        contentPanelTransform.localPosition = new Vector3((0 - (ItemList[0].rect.width+HLG.spacing)*ItemsToAdd),contentPanelTransform.localPosition.y,contentPanelTransform.localPosition.z);
    }

    private void Update()
    {
        if (isUpdated)
        {
            isUpdated = false;
            scrollRect.velocity = Oldvelocity;
        }
        
        if (contentPanelTransform.localPosition.x > 0)
        {
            Canvas.ForceUpdateCanvases();
            Oldvelocity = scrollRect.velocity;
            contentPanelTransform.localPosition -=
                new Vector3(ItemList.Length * (ItemList[0].rect.width + HLG.spacing), 0, 0);
            isUpdated = true;
        }
        
        if(contentPanelTransform.localPosition.x < 0 - ItemList.Length * (ItemList[0].rect.width + HLG.spacing))
        {
            Canvas.ForceUpdateCanvases();
            Oldvelocity = scrollRect.velocity;
            contentPanelTransform.localPosition +=
                new Vector3(ItemList.Length * (ItemList[0].rect.width + HLG.spacing), 0, 0);
            isUpdated = true;
        }
    }
}
