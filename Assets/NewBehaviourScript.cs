using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public string waffles;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(JsonConvert.SerializeObject(new GameObject()));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
