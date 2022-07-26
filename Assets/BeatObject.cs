using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatObject : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(0, -3f * Time.deltaTime, 0);
        if (this.transform.position.y < -6f)
        {
            Destroy(this.gameObject);
        }
    }

}
