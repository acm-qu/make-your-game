using UnityEngine;

public class PickupTarget : MonoBehaviour
{
    //setup variables for the pickup target
    public Transform onhand;
    private Rigidbody rb;
    private Transform player;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void Pickup()
    {
        if (rb == null || player == null || onhand == null) return;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = onhand.position; //transformation of my pickup will be equal to onhand
        transform.SetParent(player, true);
    }

    public void Drop()
    {
        if (rb == null) return;
        rb.useGravity = true;
        transform.SetParent(null, true);
    }

    public void OnDisable()
    {
        if (transform.parent == player)
            transform.SetParent(null, true);
    }
    // Update is called once per frame
    void Update()
    {

    }
}
