using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class EntityEffect : MonoBehaviour
{
    [SerializeField] private Material material;
    private Color matColor;
    private float tintFadeSpeed;

    private void Start()
    {
        // material = gameObject.GetComponent<Renderer>().material;
        matColor = new Color(0, 0, 0, 0f);
        material.SetColor("_Tint", matColor);
        tintFadeSpeed = 6f;
    }

    private void Update()
    {
        if (matColor.a > 0f)
        {
            matColor.a = Mathf.Clamp01(matColor.a - tintFadeSpeed * Time.deltaTime);
            material.SetColor("_Tint", matColor);
        }
    }
    

    public void SetColor(Color color)
    {
        this.matColor = color;
        material.SetColor("_Tint", color);
    }
    
}
