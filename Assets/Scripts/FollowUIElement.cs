using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FollowUIElement : MonoBehaviour
{
    public RectTransform target;
    public float distance = 0.05f;
    
    private Transform tf;
    private Camera mainCamera;

    private Vector3 initialPosition;

    private void Start()
    {
        tf = GetComponent<Transform>();
        initialPosition = tf.localPosition;

        mainCamera = Camera.main;
        
    }

    private void Update()
    {
        var temp = tf.position;
        temp = mainCamera.ScreenPointToRay(target.position).GetPoint(distance);

        //temp.z = distance;
        tf.position = temp;
    }
}
