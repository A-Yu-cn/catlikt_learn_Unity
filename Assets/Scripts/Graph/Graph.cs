using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField]
    Transform pointPrefab;
    [SerializeField, Range(10,100)]
    int resolution = 10;
    //[SerializeField, Range(0, 2)]
    //int function;
    [SerializeField]
    FunctionLibrary.FunctionName function;

    Transform[] points;
    void Awake()
    {
        float step = 2f / resolution;
        var position = Vector3.zero;
        var scale = Vector3.one * step;
        points = new Transform[resolution * resolution];
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z++;
            }
            Transform point = points[i] = Instantiate(pointPrefab);
            position.x = (x + 0.5f) * step - 1f;
            position.z = (z + 0.5f) * step - 1f;

            point.localPosition = position;
            point.localScale = scale;
            point.SetParent(transform,false);
            
        }

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
        float time = Time.time;
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            Vector3 position = point.localPosition;
            //if (function == 0)
            //{
            //    position.y = FunctionLibrary.Wave(position.x, time);
            //}
            //else if(function == 1)
            //{
            //    position.y = FunctionLibrary.MultiWave(position.x, time);
            //}
            //else
            //{
            //    position.y = FunctionLibrary.Ripple(position.x, time);
            //}
            position.y = f(position.x, position.z, time);
            point.localPosition = position;
        }
    }
}
