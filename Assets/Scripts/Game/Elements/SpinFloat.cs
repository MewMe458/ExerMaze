using UnityEngine;

public class SpinFloat : MonoBehaviour
{
    public float spinSpeed = 50f;

    public float floatSpeed = 1f;
    public float minFloatY = 0.5f;
    public float maxFloatY = 1f;
    private float posX;
    private float posZ;

    void Start()
    {
        posX = transform.position.x;
        posZ = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);

        float t = Mathf.PingPong(Time.time * floatSpeed, 1);
        float y = Mathf.Lerp(minFloatY, maxFloatY, t);
        transform.position = new Vector3(posX, y, posZ);
    }
}
