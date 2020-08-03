using System;
using UnityEngine;

public class SettingsHolder : Singleton<SettingsHolder>
{
    public GameObject player;
    
    [Space(10)]
    public CurrentGenerationSettings currentGenerationSettings;
}