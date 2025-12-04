using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarMove : MonoBehaviour
{
    [Header("Oval settings")]
    public Vector2 center = Vector2.zero;   // center of the oval
    public float radiusX = 5f;              // horizontal radius (half width)
    public float radiusY = 3f;              // vertical radius (half height)
    public float speed = 1f;                // how fast the object moves around the oval (in cycles per second or modifications)

    [SerializeField]private float _angle = 0f;              // current "angle" parameter (in radians)

    
    
    void Update()
    {
        // increment angle over time
        _angle += speed * Time.deltaTime;

        // optional: keep angle from growing indefinitely (avoid floating point drift)
        if (_angle > Mathf.PI * 2f)
            _angle -= Mathf.PI * 2f;

        // compute new position
        float x = center.x + radiusX * Mathf.Cos(_angle);
        float y = center.y + radiusY * Mathf.Sin(_angle);

        // set object position
        transform.localPosition = new Vector3(x, y, 0);
    }
    
}
