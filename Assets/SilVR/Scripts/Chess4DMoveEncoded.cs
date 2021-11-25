using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess4DMoveEncoded : MonoBehaviour
{

    void EncodeMove(int from, int to, int value)
    {
        this.transform.position = new Vector3(from + 0.5f, to + 0.5f, value + 0.5f);
    }
    int DecodeFrom()
    {
        return (int)this.transform.position.x;
    }
    int DecodeTo()
    {
        return (int)this.transform.position.y;
    }


}
