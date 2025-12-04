using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderControl : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Material material;
    public float speed = 10f;
    private Color c;
    private void OnEnable()
    {
        c = spriteRenderer.color;
        c.a = 0f;
        spriteRenderer.color = c;
        StartCoroutine(StartAnim());
    }

    IEnumerator StartAnim()
    {
        yield return new WaitForSeconds(3f);
        c.a = 0.1f;
        spriteRenderer.color = c;
        material.SetFloat("_Speed", speed);
        yield return new WaitForSeconds(27f);
        c.a = 0f;
        spriteRenderer.color = c;
    }
}
