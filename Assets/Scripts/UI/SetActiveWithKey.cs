using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetActiveWithKey : MonoBehaviour
{
    public GameObject target;
    public bool enabledAtStart = true;

    public KeyCode key;
    
    private void Start()
    {
        target.SetActive(enabledAtStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
