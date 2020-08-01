using System;
using UnityEngine;

//Useful utility in case when you want one object follow another selectively on axis or rotation.
public class FollowTransform : MonoBehaviour
{
    public PositionAxisMode positionAxisMode = PositionAxisMode.Full;
    public RotationAxisMode rotationAxisMode = RotationAxisMode.None;
    [Space(10)]
    public Transform target;
    private Transform tf;
    
    private void Start()
    {
        tf = GetComponent<Transform>();
    }

    private void LateUpdate()
    {
        if (positionAxisMode == PositionAxisMode.Full)
        {
            tf.position = target.position;
        }
        else if (positionAxisMode == PositionAxisMode.XZ)
        {
            var temp = target.position;
            temp.y = tf.position.y;
            
            tf.position = temp;
        }

        if (rotationAxisMode == RotationAxisMode.Full)
        {
            tf.rotation = target.rotation;
        }
    }

    private void Update()
    {
        
    }
    
    public enum PositionAxisMode
    {
        None,
        Full,
        XZ
    }
    
    public enum RotationAxisMode
    {
        None,
        Full
    }
}
