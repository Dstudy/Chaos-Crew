using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    public override void OnStartAuthority()
    {
        if (!isActiveAndEnabled) return;
        SnapCamera();
    }

    private void SnapCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var anchor = new Vector3(transform.position.x, 0, -50f);
        cam.transform.position = anchor;
    }

    public void ScreenShake()
    {
        // var cam = Camera.main;+
        // cam.transform.DOShakePosition(0.2f, 0.3f);
    }
}
