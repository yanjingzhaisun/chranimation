using UnityEngine;
using System.Collections;

public class AutoRotate : MonoBehaviour {

    public float SpeedX;
    public float SpeedY;
    public float SpeedZ;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        transform.Rotate(new Vector3(SpeedX * Time.deltaTime, SpeedY * Time.deltaTime, SpeedZ * Time.deltaTime));
    }
}
