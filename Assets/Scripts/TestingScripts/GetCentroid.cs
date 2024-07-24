using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCentroid : MonoBehaviour {

    public Transform wheel;
    public Transform bevelGear;
    public Transform wheelParent; 

    public void Start() {
        Vector3 sum = new Vector3();
        int count = 0;
        foreach (Transform child in transform) {
            if (child == wheelParent) continue;
            sum += child.transform.position;
            count++;
        }
        sum += wheel.position + bevelGear.position;
        Debug.Log(sum / (count + 2));
    }
}
