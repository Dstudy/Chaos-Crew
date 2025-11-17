using System.Collections;
using System.Collections.Generic;
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
        var anchor = cameraTransform != null ? cameraTransform : transform;
        anchor.position = new Vector3(anchor.position.x, 0, -50);
        cam.transform.position = anchor.position;
    }
}
