using UnityEngine;

namespace ShadowCasting.Demo
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Controls : MonoBehaviour
    {
        [SerializeField] private float speed = 1;

        private Vector2 dir;

        private Rigidbody2D rb;
        private Camera cam;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            cam = Camera.main;
        }

        private void Update()
        {
            Move();
            LookAtMouse();
        }

        private void Move()
        {
            dir.x = Input.GetAxis("Horizontal");
            dir.y = Input.GetAxis("Vertical");
        }

        private void FixedUpdate()
        {
            rb.velocity = speed * dir;
        }

        private void LookAtMouse()
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mousePos - (Vector2)transform.position).normalized;
            transform.up = dir;
        }
    }
}